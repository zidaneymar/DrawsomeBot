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
            var lineStores = new List<InkRecognizerStroke>();

            foreach (var storkeId in unit.StrokeIds)
            {
                var stroke = inkStrokes.Find(item => item.Id == storkeId);
            }

            foreach (var storkeId in unit.StrokeIds)
            {
                var stroke = inkStrokes.Find(item => item.Id == storkeId);
                long cur = 0;
                foreach (var p in stroke.Points.ToList())
                {
                    if (cur % skip == 0)
                    {
                        this.LittleRects.Add(GetRectangle(p, 5));
                    }
                    cur++;
                }
            }
        }

        public override string ToString()
        {
            return "->" + Next.ToString();
        }
    }
}
