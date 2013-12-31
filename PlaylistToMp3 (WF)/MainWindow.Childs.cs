using PlaylistToMp3_DLL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlaylistToMp3__WF_
{
    public partial class MainWindow : Form
    {
        private struct convertArgs
        {
            public MusicFile Input { get; set; }

            public ffmpeg_convert.FFmpeg.Mp3ConversionArgs Arguments { get; set; }

            public FileInfo Output { get; set; }
        }
    }
}
