using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagLib;

namespace PlaylistToMp3_DLL
{
    public class MusicFile
    {
        public MusicFile(TagLib.File file)
        {
            _file = file;
            
        }
        private TagLib.File _file;
       
        public string Format { get { return _file.Properties.MediaTypes.ToString(); } }
        public int Bitrate
        {
            get
            {
                foreach (TagLib.ICodec codec in _file.Properties.Codecs)
                {
                    TagLib.IAudioCodec acodec = codec as TagLib.IAudioCodec;
                    TagLib.IVideoCodec vcodec = codec as TagLib.IVideoCodec;
                    if (acodec != null && (acodec.MediaTypes & TagLib.MediaTypes.Audio) != TagLib.MediaTypes.None)
                    {
                        Console.WriteLine("Audio Properties : " + acodec.Description);
                        return acodec.AudioBitrate;
                    }
                }
                return 0;

            }
        }
        public string FileName { get { return _file.Name; } }
        public TimeSpan Duration
        {
            get
            {
                if (_file.Properties.MediaTypes != TagLib.MediaTypes.None)
                    return _file.Properties.Duration;
                return new TimeSpan();
            }
        }
        public string Artist
        {
            get
            {
                return _file.Tag.Performers.First()!=null?_file.Tag.Performers.First().ToString():"";
            }
        }
        public string Album
        {
            get
            {
                return _file.Tag.Album.ToString();
            }
        }
        public string Title
        {
            get
            {
                return _file.Tag.Title.ToString();
            }
        }
    }
}
