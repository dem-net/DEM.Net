using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Imagery
{
    // Source : Brutile
    public class Attribution
    {
        public Attribution(string text = null, string url = null)
        {
            Text = text ?? "";
            Url = url ?? "";
        }

        public string Text { get; }
        public string Url { get; }
    }
}
