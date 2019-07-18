using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoteTaker.Model
{
    public class DrawsomeLine
    {
        public DrawsomeShape Next { get; set; }

        public DrawsomeLine()
        {

        }

        public override string ToString()
        {
            return "->" + Next.ToString();
        }
    }
}
