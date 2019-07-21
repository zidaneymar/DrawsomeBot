using Contoso.NoteTaker.JSON.Format;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Contoso.NoteTaker.Services.Ink;

namespace NoteTaker.Model
{
    public class DrawsomePic
    {
        public DrawsomeShape Root { get; set; }

        public InkRecognitionRoot InkRoot { get; set; }

        public List<DrawsomeShape> AllShapes { get; set; } = new List<DrawsomeShape>();

        public List<DrawsomeObj> AllUnits { get; set; } = new List<DrawsomeObj>();

        public List<DrawsomeLine> AllLines { get; set; } = new List<DrawsomeLine>();

        public DrawsomePic(InkRecognitionRoot root, List<InkRecognizerStroke> inkStrokes)
        {
            this.InkRoot = root;
            if (root.GetShapes().Count() == 0)
            {
                throw new Exception("The Graph is Empty");
            }

            //this.Root = new DrawsomeShape(root.GetShapes().ToList().OrderBy(item => item.BoundingRect.TopY).First());

            // get all recognized shapes and set the text belonged to them
            foreach (var shape in root.GetShapes())
            {
                var shapeToAdd = new DrawsomeShape(shape)
                {
                    Text = root.GetLines().ToList().FindAll(item => shape.Contains(item)).FirstOrDefault()?.RecognizedText
                };
                if (shapeToAdd.Type != ShapeType.Undetermined)
                {
                    this.AllShapes.Add(shapeToAdd);
                }
            }

            // units include lines and shapes and texts
            foreach (var unit in root.GetUnits())
            {
                var unitToAdd = new DrawsomeObj(unit);
                this.AllUnits.Add(unitToAdd);
            }

            var inkLines = root.GetUnits().Where(item => (item as InkDrawing)?.RecognizedShape == DrawingShapeKind.Drawing);

            foreach (var line in inkLines)
            {
                this.AllLines.Add(new DrawsomeLine(line, inkStrokes, 5));
            }

            // we iterate each shape to detect the next and previous line
            foreach (var dShape in this.AllShapes)
            {
                var nextLines = this.AllLines.OrderByDescending(item => dShape.OverlapSizeWithLinesBegin(item)).Where(item => dShape.OverlapSizeWithLinesBegin(item) != 0);
                foreach (var nextLine in nextLines)
                {
                    dShape.Next.Add(nextLine);
                }

                var prevLines = this.AllLines.OrderByDescending(item => dShape.OverlapSizeWithLinesEnd(item)).Where(item => dShape.OverlapSizeWithLinesEnd(item) != 0);
                foreach (var prevLine in prevLines)
                {
                    prevLine.Next.Add(dShape);
                }
            }

            // reorder the next of each shape to ensure the colored lines behind
            foreach (var dShape in this.AllShapes)
            {
                dShape.Next.Sort((A, B) => (A as DrawsomeLine)?.MainStroke.inkStrokeInternal.DrawingAttributes.Color.R > (B as DrawsomeLine)?.MainStroke.inkStrokeInternal.DrawingAttributes.Color.R ? 1 : -1);
            }

            this.Root = this.AllShapes.OrderBy(item => item.RecogUnit.BoundingRect.TopY).FirstOrDefault();
        }


        public DrawsomePic(DrawsomeShape root)
        {
            this.Root = root;
        }

        public override string ToString()
        {
            return this.Root.ToString();
        }
    }
}
