using Contoso.NoteTaker.JSON.Format;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Input.Inking;

namespace NoteTaker.Model
{
    public class DrawsomeObj
    {
        public InkRecognitionUnit RecogUnit { get; set; }

        //public List<InkStroke> Strokes { get; set; }

        public List<DrawsomeObj> Next { get; set; } = new List<DrawsomeObj>();

        public DrawsomeObj(InkRecognitionUnit unit)
        {
            this.RecogUnit = unit;
        }

    }
}
