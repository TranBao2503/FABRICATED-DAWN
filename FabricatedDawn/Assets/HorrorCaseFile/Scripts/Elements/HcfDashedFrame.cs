using UnityEngine;
using UnityEngine.UIElements;

namespace HorrorCaseFile.UI
{
    /// <summary>Draws a configurable dashed border around its content box.</summary>
    [UxmlElement]
    public partial class HcfDashedFrame : VisualElement
    {
        public static readonly string ussClassName = "hcf-dashed-frame";

        static readonly CustomStyleProperty<Color> s_LineProperty =
            new CustomStyleProperty<Color>("--hcf-dash-color");

        Color m_LineColor = new Color32(236, 230, 218, 41); // #ECE6DA29
        Color m_StyleLineColor;
        bool m_HasStyleLineColor;
        float m_LineWidth = 1f;
        float m_Dash = 6f;
        float m_Gap = 5f;

        [UxmlAttribute]
        public Color lineColor
        {
            get => m_LineColor;
            set { m_LineColor = value; MarkDirtyRepaint(); }
        }

        [UxmlAttribute]
        public float lineWidth
        {
            get => m_LineWidth;
            set { m_LineWidth = Mathf.Max(0.1f, value); MarkDirtyRepaint(); }
        }

        [UxmlAttribute]
        public float dash
        {
            get => m_Dash;
            set { m_Dash = Mathf.Max(0.5f, value); MarkDirtyRepaint(); }
        }

        [UxmlAttribute]
        public float gap
        {
            get => m_Gap;
            set { m_Gap = Mathf.Max(0f, value); MarkDirtyRepaint(); }
        }

        public HcfDashedFrame()
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
            Color resolved;
            bool has = evt.customStyle.TryGetValue(s_LineProperty, out resolved);
            if (has == m_HasStyleLineColor && resolved == m_StyleLineColor)
            {
                return;
            }

            m_HasStyleLineColor = has;
            m_StyleLineColor = resolved;
            MarkDirtyRepaint();
        }

        void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            Rect r = contentRect;
            if (r.width <= m_LineWidth || r.height <= m_LineWidth)
            {
                return;
            }

            float half = m_LineWidth * 0.5f;
            float x0 = r.xMin + half;
            float y0 = r.yMin + half;
            float x1 = r.xMax - half;
            float y1 = r.yMax - half;

            Painter2D p = ctx.painter2D;
            p.lineWidth = m_LineWidth;
            p.strokeColor = m_HasStyleLineColor ? m_StyleLineColor : m_LineColor;
            p.lineCap = LineCap.Butt;
            p.lineJoin = LineJoin.Miter;

            p.BeginPath();
            AppendDashes(p, new Vector2(x0, y0), new Vector2(x1, y0));
            AppendDashes(p, new Vector2(x1, y0), new Vector2(x1, y1));
            AppendDashes(p, new Vector2(x1, y1), new Vector2(x0, y1));
            AppendDashes(p, new Vector2(x0, y1), new Vector2(x0, y0));
            p.Stroke();
        }

        void AppendDashes(Painter2D p, Vector2 from, Vector2 to)
        {
            float len = Vector2.Distance(from, to);
            if (len <= 0.001f)
            {
                return;
            }

            Vector2 dir = (to - from) / len;
            // The setters keep dash at 0.5 or more and gap at 0 or more, so the step never stalls.
            float step = m_Dash + m_Gap;
            for (float t = 0f; t < len; t += step)
            {
                float end = Mathf.Min(t + m_Dash, len);
                p.MoveTo(from + dir * t);
                p.LineTo(from + dir * end);
            }
        }
    }
}
