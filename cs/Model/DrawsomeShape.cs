using Contoso.NoteTaker.JSON.Format;
using NoteTaker.HTTP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
namespace NoteTaker.Model
{
    public enum ShapeType
    {
        Rect,
        Diamond,
        Undetermined
    }

    public class DrawsomeShape: DrawsomeObj
    {
        public ShapeType Type { get; set; }

        public string Text { get; set; }

        // lines contains the units have texts
        // other units contains all the units detected
        //public DrawsomeShape(InkRecognitionUnit unit, List<InkLine> lines = null, List<InkRecognitionUnit> otherUnits = null, List<InkDrawing> otherShapes = null) : base(unit)
        public DrawsomeShape(InkRecognitionUnit unit): base(unit)
        {
            this.RecogUnit = unit;
            var drawing = unit as InkDrawing;
            switch (drawing.RecognizedShape)
            {
                case DrawingShapeKind.Rectangle:
                    this.Type = ShapeType.Rect;
                    break;
                case DrawingShapeKind.Diamond:
                    this.Type = ShapeType.Diamond;
                    break;
                default:
                    this.Type = ShapeType.Undetermined;
                    break;
            }

            //this.Text = lines.FindAll(item => unit.Contains(item)).FirstOrDefault()?.RecognizedText;

        }

        public float OverlapSizeWithLinesBegin(DrawsomeLine line, int take = 10)
        {
            float res = 0;
            foreach (var littleRect in line.LittleRects.Take(10))
            {
                res += this.RecogUnit.BoundingRect.OverlapSize(littleRect);
            }
            return res;
        }


        public float OverlapSizeWithLinesEnd(DrawsomeLine line, int take = 10)
        {
            float res = 0;
            foreach (var littleRect in line.LittleRects.Skip(Math.Max(0, line.LittleRects.Count() - take)))
            {
                res += this.RecogUnit.BoundingRect.OverlapSize(littleRect);
            }
            return res;
        }

        public override string ToString()
        {
            return string.Format("[Shape:{0}, Text:{1}]", Type, Text) + this.Next?.ToString();
        }

    }
}
