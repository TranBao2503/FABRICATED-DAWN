using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace HorrorCaseFile.UI
{
    /// <summary>Renders procedural grain and scanlines over a UI Toolkit screen.</summary>
    [UxmlElement]
    public partial class HcfGrainOverlay : VisualElement
    {
        public static readonly string ussClassName = "hcf-grain-overlay";

        public static readonly string grainLayerUssClassName = "hcf-grain-layer";

        public static readonly string scanlineLayerUssClassName = "hcf-scanline-layer";

        static readonly List<HcfGrainOverlay> s_Live = new List<HcfGrainOverlay>();

        static readonly CustomStyleProperty<Color> s_GrainColorProperty =
            new CustomStyleProperty<Color>("--hcf-grain-color");

        static readonly CustomStyleProperty<Color> s_ScanlineColorProperty =
            new CustomStyleProperty<Color>("--hcf-scanline-color");

        static readonly Color32 k_GrainColor = new Color32(255, 239, 208, 13);
        static readonly Color32 k_ScanlineColor = new Color32(0, 0, 0, 255);

        // Each stripe uses four vertices addressed by ushort indices.
        const int k_MaxStripes = 65535 / 4;

        readonly VisualElement m_ScanlineLayer;
        readonly VisualElement m_GrainLayer;

        IVisualElementScheduledItem m_PppWatch;
        float m_WatchedPpp = -1f;

        float m_Grain = 55f;
        bool m_Scanlines = true;

        Color m_StyleGrainColor;
        bool m_HasStyleGrainColor;
        Color m_StyleScanlineColor;
        bool m_HasStyleScanlineColor;

        int m_GrainPeriod = 2;
        int m_GrainThickness = 1;
        int m_ScanlinePeriod = 6;
        int m_ScanlineThickness = 2;

        public static IReadOnlyList<HcfGrainOverlay> live => s_Live;

        public static void SetGrainAll(float value)
        {
            for (int i = 0; i < s_Live.Count; i++)
            {
                s_Live[i].grain = value;
            }
        }

        public static void SetScanlinesAll(bool value)
        {
            for (int i = 0; i < s_Live.Count; i++)
            {
                s_Live[i].scanlines = value;
            }
        }

        /// <summary>Grain intensity in the 0 to 100 range.</summary>
        [UxmlAttribute]
        public float grain
        {
            get => m_Grain;
            set
            {
                m_Grain = Mathf.Clamp(value, 0f, 100f);
                ApplyGrain();
            }
        }

        [UxmlAttribute]
        public bool scanlines
        {
            get => m_Scanlines;
            set
            {
                m_Scanlines = value;
                ApplyScanlines();
            }
        }

        // Pattern geometry is measured in points: the values below are multiplied by the rounded
        // pixels per point, so stripes always land on whole device pixels.
        [UxmlAttribute]
        public int grainPeriod
        {
            get => m_GrainPeriod;
            set { m_GrainPeriod = Mathf.Max(1, value); RepaintGrain(); }
        }

        [UxmlAttribute]
        public int grainThickness
        {
            get => m_GrainThickness;
            set { m_GrainThickness = Mathf.Max(1, value); RepaintGrain(); }
        }

        [UxmlAttribute]
        public int scanlinePeriod
        {
            get => m_ScanlinePeriod;
            set { m_ScanlinePeriod = Mathf.Max(1, value); RepaintScanlines(); }
        }

        [UxmlAttribute]
        public int scanlineThickness
        {
            get => m_ScanlineThickness;
            set { m_ScanlineThickness = Mathf.Max(1, value); RepaintScanlines(); }
        }

        public HcfGrainOverlay()
        {
            AddToClassList(ussClassName);
            pickingMode = PickingMode.Ignore;

            m_ScanlineLayer = new VisualElement { name = "scanline-layer", pickingMode = PickingMode.Ignore };
            m_ScanlineLayer.AddToClassList(scanlineLayerUssClassName);
            m_ScanlineLayer.generateVisualContent += OnGenerateScanlines;

            m_GrainLayer = new VisualElement { name = "grain-layer", pickingMode = PickingMode.Ignore };
            m_GrainLayer.AddToClassList(grainLayerUssClassName);
            m_GrainLayer.generateVisualContent += OnGenerateGrain;

            Add(m_ScanlineLayer);
            Add(m_GrainLayer);

            ApplyGrain();
            ApplyScanlines();

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (!s_Live.Contains(this))
            {
                s_Live.Add(this);
            }

            m_WatchedPpp = -1f;
            m_PppWatch = schedule.Execute(CheckPixelsPerPoint).Every(200);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            s_Live.Remove(this);

            if (m_PppWatch != null)
            {
                m_PppWatch.Pause();
                m_PppWatch = null;
            }

            m_WatchedPpp = -1f;
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            RepaintLayers();
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            Color resolved;
            bool has = evt.customStyle.TryGetValue(s_GrainColorProperty, out resolved);
            if (has != m_HasStyleGrainColor || resolved != m_StyleGrainColor)
            {
                m_HasStyleGrainColor = has;
                m_StyleGrainColor = resolved;
                RepaintGrain();
            }

            has = evt.customStyle.TryGetValue(s_ScanlineColorProperty, out resolved);
            if (has != m_HasStyleScanlineColor || resolved != m_StyleScanlineColor)
            {
                m_HasStyleScanlineColor = has;
                m_StyleScanlineColor = resolved;
                RepaintScanlines();
            }
        }

        void CheckPixelsPerPoint()
        {
            float ppp = scaledPixelsPerPoint;
            if (ppp == m_WatchedPpp)
            {
                return;
            }

            m_WatchedPpp = ppp;
            RepaintLayers();
        }

        void RepaintLayers()
        {
            RepaintScanlines();
            RepaintGrain();
        }

        void RepaintGrain()
        {
            m_GrainLayer.MarkDirtyRepaint();
        }

        void RepaintScanlines()
        {
            m_ScanlineLayer.MarkDirtyRepaint();
        }

        void ApplyGrain()
        {
            m_GrainLayer.style.opacity = m_Grain / 100f;
        }

        void ApplyScanlines()
        {
            m_ScanlineLayer.style.display = m_Scanlines ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void OnGenerateScanlines(MeshGenerationContext ctx)
        {
            Color32 tint = m_HasStyleScanlineColor ? (Color32)m_StyleScanlineColor : k_ScanlineColor;
            DrawStripes(ctx, m_ScanlineLayer, true, m_ScanlinePeriod, m_ScanlineThickness, tint);
        }

        void OnGenerateGrain(MeshGenerationContext ctx)
        {
            Color32 tint = m_HasStyleGrainColor ? (Color32)m_StyleGrainColor : k_GrainColor;
            DrawStripes(ctx, m_GrainLayer, false, m_GrainPeriod, m_GrainThickness, tint);
        }

        static void DrawStripes(MeshGenerationContext ctx, VisualElement layer, bool vertical, int periodUnits, int thicknessUnits, Color32 tint)
        {
            Rect r = layer.contentRect;
            float ppp = layer.scaledPixelsPerPoint;
            if (r.width <= 0f || r.height <= 0f || ppp <= 0f || float.IsNaN(ppp) || float.IsInfinity(ppp))
            {
                return;
            }

            // The setters keep each value at 1 or more; only the cross-check belongs here, since a
            // period shorter than the thickness would merge the stripes into one solid block.
            periodUnits = Mathf.Max(thicknessUnits, periodUnits);

            int t = Mathf.Max(1, Mathf.RoundToInt(ppp));
            int period = periodUnits * t;
            int thickness = thicknessUnits * t;

            Rect world = layer.worldBound;
            float axisWorld = vertical ? world.xMin : world.yMin;
            float axisMin = vertical ? r.xMin : r.yMin;
            float axisMax = vertical ? r.xMax : r.yMax;
            float crossMin = vertical ? r.yMin : r.xMin;
            float crossMax = vertical ? r.yMax : r.xMax;

            // Device-space alignment keeps the pattern stable across panel scaling.
            int devMin = Mathf.CeilToInt((axisWorld + axisMin) * ppp);
            int devMax = Mathf.FloorToInt((axisWorld + axisMax) * ppp);

            int count = StripeCount(devMin, devMax, period, thickness, out int start);

            while (count > k_MaxStripes)
            {
                period *= 2;
                count = StripeCount(devMin, devMax, period, thickness, out start);
            }

            if (count <= 0)
            {
                return;
            }

            MeshWriteData mesh = ctx.Allocate(count * 4, count * 6);

            for (int i = 0; i < count; i++)
            {
                int dev = start + i * period;
                float a0 = dev / ppp - axisWorld;
                float a1 = (dev + thickness) / ppp - axisWorld;

                ushort b = (ushort)(i * 4);
                if (vertical)
                {
                    AddQuad(mesh, a0, crossMin, a1, crossMax, tint, b);
                }
                else
                {
                    AddQuad(mesh, crossMin, a0, crossMax, a1, tint, b);
                }
            }
        }

        static int StripeCount(int devMin, int devMax, int period, int thickness, out int start)
        {
            int phase = ((devMin % period) + period) % period;
            start = phase == 0 ? devMin : devMin + period - phase;

            int room = devMax - start - thickness;
            return room < 0 ? 0 : room / period + 1;
        }

        static void AddQuad(MeshWriteData mesh, float x0, float y0, float x1, float y1, Color32 tint, ushort b)
        {
            Vertex v = default;
            v.tint = tint;

            v.position = new Vector3(x0, y1, Vertex.nearZ);
            mesh.SetNextVertex(v);
            v.position = new Vector3(x0, y0, Vertex.nearZ);
            mesh.SetNextVertex(v);
            v.position = new Vector3(x1, y0, Vertex.nearZ);
            mesh.SetNextVertex(v);
            v.position = new Vector3(x1, y1, Vertex.nearZ);
            mesh.SetNextVertex(v);

            mesh.SetNextIndex(b);
            mesh.SetNextIndex((ushort)(b + 1));
            mesh.SetNextIndex((ushort)(b + 2));
            mesh.SetNextIndex((ushort)(b + 2));
            mesh.SetNextIndex((ushort)(b + 3));
            mesh.SetNextIndex(b);
        }
    }
}
