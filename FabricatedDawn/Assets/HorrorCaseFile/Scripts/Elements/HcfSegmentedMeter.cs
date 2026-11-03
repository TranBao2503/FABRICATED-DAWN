using UnityEngine;
using UnityEngine.UIElements;

namespace HorrorCaseFile.UI
{
    /// <summary>Draws a meter as filled, accented and empty segments.</summary>
    [UxmlElement]
    public partial class HcfSegmentedMeter : VisualElement
    {
        public static readonly string ussClassName = "hcf-segmented-meter";

        static readonly CustomStyleProperty<Color> s_FillProperty =
            new CustomStyleProperty<Color>("--hcf-meter-fill");
        static readonly CustomStyleProperty<Color> s_AccentProperty =
            new CustomStyleProperty<Color>("--hcf-meter-accent");
        static readonly CustomStyleProperty<Color> s_EmptyLineProperty =
            new CustomStyleProperty<Color>("--hcf-meter-empty-line");

        int m_Segments = 5;
        int m_Filled = 3;
        int m_AccentFrom = -1;
        float m_Gap = 4f;
        static readonly Color32 k_FillColor = new Color32(236, 230, 218, 255);      // #ECE6DA
        static readonly Color32 k_AccentColor = new Color32(168, 51, 42, 255);      // #A8332A
        static readonly Color32 k_EmptyLineColor = new Color32(236, 230, 218, 77);
        float m_EmptyLineWidth = 1f;

        Color m_StyleFillColor;
        Color m_StyleAccentColor;
        Color m_StyleEmptyLineColor;
        bool m_HasStyleFillColor;
        bool m_HasStyleAccentColor;
        bool m_HasStyleEmptyLineColor;

        Color ResolvedFillColor => m_HasStyleFillColor ? m_StyleFillColor : k_FillColor;
        Color ResolvedAccentColor => m_HasStyleAccentColor ? m_StyleAccentColor : k_AccentColor;
        Color ResolvedEmptyLineColor => m_HasStyleEmptyLineColor ? m_StyleEmptyLineColor : k_EmptyLineColor;

        [UxmlAttribute]
        public int segments
        {
            get => m_Segments;
            set { m_Segments = Mathf.Max(1, value); MarkDirtyRepaint(); }
        }

        [UxmlAttribute]
        public int filled
        {
            get => m_Filled;
            set
            {
                if (value == m_Filled)
                {
                    return;
                }

                int previous = m_Filled;
                m_Filled = value;
                MarkDirtyRepaint();

                using (ChangeEvent<int> evt = ChangeEvent<int>.GetPooled(previous, value))
                {
                    evt.target = this;
                    SendEvent(evt);
                }
            }
        }

        /// <summary>Updates the filled count without sending ChangeEvent.</summary>
        public void SetFilledWithoutNotify(int value)
        {
            m_Filled = value;
            MarkDirtyRepaint();
        }

        /// <summary>First filled segment drawn in the accent color, or -1 to disable.</summary>
        [UxmlAttribute]
        public int accentFrom
        {
            get => m_AccentFrom;
            set { m_AccentFrom = value; MarkDirtyRepaint(); }
        }

        [UxmlAttribute]
        public float gap
        {
            get => m_Gap;
            set { m_Gap = Mathf.Max(0f, value); MarkDirtyRepaint(); }
        }

        [UxmlAttribute]
        public float emptyLineWidth
        {
            get => m_EmptyLineWidth;
            set { m_EmptyLineWidth = Mathf.Max(0.1f, value); MarkDirtyRepaint(); }
        }

        public HcfSegmentedMeter()
        {
            AddToClassList(ussClassName);
            pickingMode = PickingMode.Ignore;
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            MarkDirtyRepaint();
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            bool changed = ReadStyleColor(evt, s_FillProperty,
                ref m_HasStyleFillColor, ref m_StyleFillColor);
            changed |= ReadStyleColor(evt, s_AccentProperty,
                ref m_HasStyleAccentColor, ref m_StyleAccentColor);
            changed |= ReadStyleColor(evt, s_EmptyLineProperty,
                ref m_HasStyleEmptyLineColor, ref m_StyleEmptyLineColor);

            if (changed)
            {
                MarkDirtyRepaint();
            }
        }

        static bool ReadStyleColor(CustomStyleResolvedEvent evt, CustomStyleProperty<Color> property,
            ref bool has, ref Color color)
        {
            Color resolved;
            bool found = evt.customStyle.TryGetValue(property, out resolved);
            if (found == has && resolved == color)
            {
                return false;
            }

            has = found;
            color = resolved;
            return true;
        }

        void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            Rect r = contentRect;
            if (r.width <= 0f || r.height <= 0f)
            {
                return;
            }

            int count = m_Segments; // the setter keeps it at 1 or more, so it is safe to divide by
            bool horizontal = r.width >= r.height;
            float total = horizontal ? r.width : r.height;
            float size = (total - m_Gap * (count - 1)) / count;
            if (size <= 0f)
            {
                return;
            }

            int filledCount = Mathf.Clamp(m_Filled, 0, count);
            int accentStart = m_AccentFrom < 0 ? int.MaxValue : m_AccentFrom;

            Painter2D p = ctx.painter2D;

            p.fillColor = ResolvedFillColor;
            p.BeginPath();
            int plain = 0;
            for (int i = 0; i < filledCount; i++)
            {
                if (i >= accentStart)
                {
                    continue;
                }

                AppendRect(p, SegmentRect(r, i, size, horizontal, count));
                plain++;
            }

            if (plain > 0)
            {
                p.Fill();
            }

            p.fillColor = ResolvedAccentColor;
            p.BeginPath();
            int accent = 0;
            for (int i = 0; i < filledCount; i++)
            {
                if (i < accentStart)
                {
                    continue;
                }

                AppendRect(p, SegmentRect(r, i, size, horizontal, count));
                accent++;
            }

            if (accent > 0)
            {
                p.Fill();
            }

            float half = m_EmptyLineWidth * 0.5f;
            p.strokeColor = ResolvedEmptyLineColor;
            p.lineWidth = m_EmptyLineWidth;
            p.lineCap = LineCap.Butt;
            p.lineJoin = LineJoin.Miter;
            p.BeginPath();
            int empty = 0;
            for (int i = filledCount; i < count; i++)
            {
                Rect seg = SegmentRect(r, i, size, horizontal, count);
                if (seg.width <= m_EmptyLineWidth || seg.height <= m_EmptyLineWidth)
                {
                    continue;
                }

                AppendRect(p, new Rect(seg.xMin + half, seg.yMin + half,
                    seg.width - m_EmptyLineWidth, seg.height - m_EmptyLineWidth));
                empty++;
            }

            if (empty > 0)
            {
                p.Stroke();
            }
        }

        Rect SegmentRect(Rect area, int index, float size, bool horizontal, int count)
        {
            if (horizontal)
            {
                float x = area.xMin + index * (size + m_Gap);
                return new Rect(x, area.yMin, size, area.height);
            }

            float y = area.yMax - (index + 1) * size - index * m_Gap;
            return new Rect(area.xMin, y, area.width, size);
        }

        static void AppendRect(Painter2D p, Rect rect)
        {
            p.MoveTo(new Vector2(rect.xMin, rect.yMin));
            p.LineTo(new Vector2(rect.xMax, rect.yMin));
            p.LineTo(new Vector2(rect.xMax, rect.yMax));
            p.LineTo(new Vector2(rect.xMin, rect.yMax));
            p.ClosePath();
        }
    }
}
