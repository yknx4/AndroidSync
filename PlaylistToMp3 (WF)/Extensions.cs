using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PlaylistToMp3__WF_
{
    class Extensions
    {
        public static int[] CBR = { 32,40,48,56,64,80,96,112,128,160,192,224,256,320};
        public static int[] VBR = { 0,1,2,3,4,5,6,7,8,9 };
        public static string CombineWithValidate(params string[] par)
        {
            string regexSearch = new string(Path.GetInvalidPathChars())+ new string(Path.GetInvalidFileNameChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            for (int i = 0; i < par.Length;i++ )
            {
                par[i] = r.Replace(par[i], "");
                if (par[i] == string.Empty)
                {
                    par[i] = "-";
                }
            }
            return Path.Combine(par);
        }
    }
}
