using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using TagLib;


namespace PlaylistToMp3_DLL
{
    /// <summary>
    /// Class for loading the .m3u8 playlist.
    /// </summary>
public static class PlaylistLoader
    {
        private static string _error;
        /// <summary>
        /// Gets the error.
        /// </summary>
        /// <value>
        /// The error.
        /// </value>
        public static String Error { get { return _error != null ? _error : ""; } }
        /// <summary>
        /// Loads the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>
        /// A list with all music files within the list.
        /// </returns>
        /// 
        public static List<MusicFile> GetPlaylist(string path)
        {
            var result = new List<MusicFile>();
            foreach (var entry in Load(path))
            {
                result.Add(new MusicFile(entry));
            }
            return result;
        }
        public static List< TagLib.File> Load(string path)
        {
            var result = new List< TagLib.File>();
            try
            {
                foreach (string entry in System.IO.File.ReadAllLines(path))
                {
                    FileInfo m_Entry = new FileInfo(Path.GetFullPath(entry));
                    TagLib.File file = null;
                    if (m_Entry.Exists)
                    {
                        try
                        {
                            file = TagLib.File.Create(m_Entry.FullName);
                        }
                        catch (TagLib.UnsupportedFormatException)
                        {
                            Console.WriteLine("UNSUPPORTED FILE: " + m_Entry.FullName);
                            Console.WriteLine(String.Empty);
                            Console.WriteLine("---------------------------------------");
                            Console.WriteLine(String.Empty);
                            continue;
                        }
                        result.Add(file);
                    }


                }
            }

            catch (SystemException ex)
            {
                _error = ex.ToString();
            }

            return result;
        }
    }
}
