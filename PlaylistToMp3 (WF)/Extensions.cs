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
