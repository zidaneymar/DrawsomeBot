using Contoso.NoteTaker.JSON.Format;
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

    public class DrawsomeShape
    {
        public ShapeType Type { get; set; }

        public string Text { get; set; }

        public DrawsomeLine Next { get; set; }

        public DrawsomeLine NextFalse { get; set; }

        // lines contains the units have texts
        // other units contains all the units detected
        public DrawsomeShape(InkRecognitionUnit unit, List<InkLine> lines = null, List<InkRecognitionUnit> otherUnits = null, List<InkDrawing> otherShapes = null)
        {
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

            // ToDo: get the luis result for the text
            this.Text = lines.FindAll(item => unit.Contains((InkRecognitionUnit)item)).First().RecognizedText;
            var nextCandidate = otherUnits.ToList().FindAll(item => unit.IsLowerLinked(item)).OrderBy(item => item.BoundingRect.Width).FirstOrDefault();

            if (nextCandidate != null)
            {
                var nextOfNext = otherShapes.ToList().FindAll(item => nextCandidate.IsLowerLinked(item)).OrderBy(item => item.BoundingRect.Width).FirstOrDefault();

                if (nextOfNext != null)
                {
                    this.Next = new DrawsomeLine();
                    this.Next.Next = new DrawsomeShape(nextOfNext, lines, otherUnits, otherShapes);
                }
            }
        }
    }
}
