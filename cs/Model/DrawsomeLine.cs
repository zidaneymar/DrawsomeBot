using Contoso.NoteTaker.JSON.Format;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoteTaker.Model
{
    public class DrawsomeObj
    {
        public Rectangle BoundingRect { get; set; }

    }
    public class DrawsomeLine: DrawsomeObj
    {
        public DrawsomeShape Next { get; set; }

        public DrawsomeLine(InkRecognitionUnit unit)
        {
            this.BoundingRect = unit.BoundingRect;
        }

        public override string ToString()
        {
            return "->" + Next.ToString();
        }
    }
}
