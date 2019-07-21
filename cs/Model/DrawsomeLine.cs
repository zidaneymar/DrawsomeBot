using Contoso.NoteTaker.JSON.Format;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoteTaker.Model
{
    public class DrawsomeLine: DrawsomeObj
    {
        public List<Rectangle> LittleRects { get; set; } = new List<Rectangle>();

        public InkRecognizerStroke MainStroke { get; set; }

        private Rectangle GetRectangle(Windows.UI.Input.Inking.InkPoint p, float size = 4.0f)
        {
            var res = new Contoso.NoteTaker.JSON.Format.Rectangle();
            res.TopX = (float)(p.Position.X - size / 2);
            res.TopY = (float)(p.Position.Y - size / 2);
            res.Width = size;
            res.Height = size;
            return res;
        }

        public DrawsomeLine(InkRecognitionUnit unit, List<InkRecognizerStroke> inkStrokes, int skip = 10): base(unit)
        {
            var mainStrokeId = unit.StrokeIds.OrderByDescending(id => CalPointDistance(inkStrokes.Find(item => item.Id == id))).First();
            var mainStroke = inkStrokes.Find(item => item.Id == mainStrokeId);
            this.MainStroke = mainStroke;

            long cur = 0;

            foreach (var p in mainStroke.Points.ToList())
            {
                if (cur % skip == 0)
                {
                    // use skip as size
                    this.LittleRects.Add(GetRectangle(p, skip));
                }
                cur++;
            }
        }

        private double CalPointDistance(InkRecognizerStroke stroke)
        {
            return Math.Pow(stroke.Points.First().Position.X - stroke.Points.Last().Position.X, 2)
                + Math.Pow(stroke.Points.First().Position.Y - stroke.Points.Last().Position.Y, 2);
        }

        public override string ToString()
        {
            return "->" + Next.ToString();
        }

    }
}
