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

            this.Text = lines.FindAll(item => unit.Contains((InkRecognitionUnit)item)).FirstOrDefault()?.RecognizedText;

            // we need to get the enclosed line for the else condition
            //var elseDepth = 0.0;

            var nextFalseCandidate = otherUnits.ToList().FindAll(item => unit.IsRightMidAligned(item)).OrderBy(item => unit.DistanceToRight(item)).FirstOrDefault();
            if (nextFalseCandidate != null)
            {
                var nextOfFalseNext = otherShapes.Except(new List<InkDrawing>() { drawing }).ToList().FindAll(item => nextFalseCandidate.IsRightLowerLinked(item)).OrderBy(item => nextFalseCandidate.DistanceToRight(item)).FirstOrDefault();
                if (nextOfFalseNext != null)
                {
                    //elseDepth = GetElseBranchDepth(nextOfFalseNext, otherUnits, otherShapes);
                    this.NextFalse = new DrawsomeLine();
                    this.NextFalse.Next = new DrawsomeShape(nextOfFalseNext, lines, otherUnits, otherShapes);
                }
            }


            // Get the next line
            var nextCandidate = otherUnits.ToList().FindAll(item => unit.IsLowerLinked(item)).OrderBy(item => unit.DistanceToLower(item)).FirstOrDefault();

            if (nextCandidate != null)
            {
                // Recursivly get the next shape
                var nextOfNext = otherShapes.Except(new List<InkDrawing>() { drawing }).ToList().FindAll(item => nextCandidate.IsLowerLinked(item)).OrderBy(item => nextCandidate.DistanceToLower(item)).FirstOrDefault();
                if (nextOfNext != null)
                {
                    this.Next = new DrawsomeLine();
                    this.Next.Next = new DrawsomeShape(nextOfNext, lines, otherUnits, otherShapes);
                }
            }


        }

        public float GetElseBranchDepth(InkRecognitionUnit unit, List<InkRecognitionUnit> otherUnits = null, List<InkDrawing> otherShapes = null)
        {
            var nextCandidate = otherUnits.ToList().FindAll(item => unit.IsLowerLinked(item)).OrderBy(item => unit.DistanceToLower(item)).FirstOrDefault();

            if (nextCandidate != null)
            {
                // Recursivly get the next shape
                var nextOfNext = otherShapes.ToList().FindAll(item => nextCandidate.IsLowerLinked(item)).OrderBy(item => nextCandidate.DistanceToLower(item)).FirstOrDefault();

                if (nextOfNext != null)
                {
                    return GetElseBranchDepth(nextOfNext, otherUnits, otherShapes);
                }

                // return the last arrow depth
                return nextCandidate.BoundingRect.TopY + nextCandidate.BoundingRect.Height;
            }


            // the last one doensn't have enclosed arrow so we don't have constraints on the if true condition
            return 0;
        }


        public override string ToString()
        {
            return string.Format("[Shape:{0}, Text:{1}]", Type, Text) + this.Next?.ToString();
        }

    }
}
