using UnityEngine;
using UnityEngine.UIElements;

namespace FabricatedDawn.CaseFile
{
    // Deliberately simple placeholder; replace only artwork later, keeping hotspot IDs.
    public sealed class StickFigureElement : VisualElement
    {
        readonly string tag;
        public StickFigureElement(string characterTag)
        {
            tag = characterTag ?? "";
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Paint;
        }
        void Paint(MeshGenerationContext context)
        {
            float w=contentRect.width, h=contentRect.height;
            if(w<=0 || h<=0) return;
            var p=context.painter2D;
            p.lineWidth=Mathf.Max(3, Mathf.Min(w,h)*.022f);
            p.strokeColor = tag == "?" ? new Color(.52f,.27f,.22f) : new Color(.15f,.14f,.12f);
            p.BeginPath();
            float cx=w*.5f, cy=h*.15f, radius=Mathf.Min(w*.18f,h*.09f);
            for(int n=0;n<=32;n++) {
                float angle=n*Mathf.PI*2/32;var point=new Vector2(cx+Mathf.Cos(angle)*radius,cy+Mathf.Sin(angle)*radius);
                if(n==0)p.MoveTo(point);else p.LineTo(point);
            }
            p.Stroke();
            p.BeginPath();p.MoveTo(new Vector2(cx,cy+radius));p.LineTo(new Vector2(cx,h*.58f));
            p.MoveTo(new Vector2(w*.18f,h*.48f));p.LineTo(new Vector2(cx,h*.33f));p.LineTo(new Vector2(w*.82f,h*.48f));
            p.MoveTo(new Vector2(w*.23f,h*.9f));p.LineTo(new Vector2(cx,h*.58f));p.LineTo(new Vector2(w*.77f,h*.9f));p.Stroke();
        }
    }
}
