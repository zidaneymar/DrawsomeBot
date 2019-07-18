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

        public DrawsomePic(InkRecognitionRoot root)
        {
            this.Root = new DrawsomeShape(root.GetShapes().ToList().OrderBy(item => item.BoundingRect.TopY).First(), root.GetLines().ToList(), root.GetUnits().ToList(), root.GetShapes().ToList());
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
