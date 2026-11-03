using UnityEngine;
using UnityEngine.UIElements;

namespace HorrorCaseFile.UI
{
    /// <summary>Draws a filled plate with independently cut corners.</summary>
    [UxmlElement]
    public partial class HcfTornPlate : VisualElement
    {
        public static readonly string ussClassName = "hcf-torn-plate";

        static readonly CustomStyleProperty<Color> s_FillProperty =
            new CustomStyleProperty<Color>("--hcf-plate-fill");

        Color m_Fill = new Color32(236, 230, 218, 255);
        Color m_StyleFill;
        bool m_HasStyleFill;
        float m_CutTop = 4f;
        float m_CutRight = 8f;
        float m_CutBottom = 4f;
        float m_CutLeft = 8f;

        [UxmlAttribute]
        public Color fill
        {
            get => m_Fill;
            set { m_Fill = value; MarkDirtyRepaint(); }
        }

        [UxmlAttribute]
        public float cutTop
        {
            get => m_CutTop;
            set { m_CutTop = value; MarkDirtyRepaint(); }
        }

        [UxmlAttribute]
        public float cutRight
        {
            get => m_CutRight;
            set { m_CutRight = value; MarkDirtyRepaint(); }
        }

        [UxmlAttribute]
        public float cutBottom
        {
            get => m_CutBottom;
            set { m_CutBottom = value; MarkDirtyRepaint(); }
        }

        [UxmlAttribute]
        public float cutLeft
        {
            get => m_CutLeft;
            set { m_CutLeft = value; MarkDirtyRepaint(); }
        }

        public HcfTornPlate()
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
            bool has = evt.customStyle.TryGetValue(s_FillProperty, out resolved);
            if (has == m_HasStyleFill && resolved == m_StyleFill)
            {
                return;
            }

            m_HasStyleFill = has;
            m_StyleFill = resolved;
            MarkDirtyRepaint();
        }

        void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            Rect r = contentRect;
            if (r.width <= 0f || r.height <= 0f)
            {
                return;
            }

            float ct = Mathf.Clamp(m_CutTop, 0f, r.height);
            float cb = Mathf.Clamp(m_CutBottom, 0f, r.height);
            float cl = Mathf.Clamp(m_CutLeft, 0f, r.width);
            float cr = Mathf.Clamp(m_CutRight, 0f, r.width);

            Painter2D p = ctx.painter2D;
            p.fillColor = m_HasStyleFill ? m_StyleFill : m_Fill;
            p.BeginPath();
            p.MoveTo(new Vector2(r.xMin, r.yMin + ct));
            p.LineTo(new Vector2(r.xMax - cr, r.yMin));
            p.LineTo(new Vector2(r.xMax, r.yMax - cb));
            p.LineTo(new Vector2(r.xMin + cl, r.yMax));
            p.ClosePath();
            p.Fill();
        }
    }
}
