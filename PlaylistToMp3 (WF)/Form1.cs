using Equin.ApplicationFramework;
using ffmpeg_convert;
using PlaylistToMp3_DLL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlaylistToMp3__WF_
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            ThreadPool.SetMinThreads(200, 200);
            InitializeComponent();
        }

        int numberOfThreads = Environment.ProcessorCount * 2;
        BindingList<MusicFile> playlist;
        int thread_no = Environment.ProcessorCount;
        Queue<Tuple<BackgroundWorker, convertArgs>> Conversions = new Queue<Tuple<BackgroundWorker, convertArgs>>();
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog m_open = new OpenFileDialog();
            m_open.Multiselect = false;
            m_open.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            m_open.ShowDialog();
            if (m_open.FileName != string.Empty)
            {
                playlist = new BindingList<MusicFile>(PlaylistToMp3_DLL.PlaylistLoader.GetPlaylist(m_open.FileName));
                BindingListView<MusicFile> view = new BindingListView<MusicFile>(playlist);
                dtgrPlaylist.DataSource = view;
                tslblStatus.Text = playlist.Count + " song loaded.";
            }
            

        }
        public FileInfo OutputPath
        {
            get
            {
                try
                {
                    return new FileInfo(txtOuput.Text);
                }
                catch (Exception)
                {
                    
                }
                return new FileInfo("output/");
            }
        }
        private void btnDeleteSelected_Click(object sender, EventArgs e)
        {
            if (dtgrPlaylist.SelectedRows.Count == 1)
            {
                playlist.Remove((MusicFile)dtgrPlaylist.SelectedRows[0].DataBoundItem);
            }
        }

        private void btnConvert_Click(object sender, EventArgs e)
        {
            pgrConvert.Value=0;
            BeginRefreshDatagrid();
            Convert();
        }

        private void BeginRefreshDatagrid()
        {
            tmr.Enabled = true;
            tmr.Start();
            tmr.Tick += (sender, args) =>
            {
                dtgrPlaylist.Refresh();
                if (pgrConvert.Value == pgrConvert.Maximum)
                {
                    ((System.Windows.Forms.Timer)sender).Stop();
                }
            };
        }

        private void Convert()
        {
            tslblStatus.Text = "Converting " + playlist.Count + " items.";
            pgrConvert.Value = 0;
            pgrConvert.Maximum = playlist.Count;
            List<Tuple<BackgroundWorker, convertArgs>> Conversions = new List<Tuple<BackgroundWorker, convertArgs>>();
            ffmpeg_convert.FFmpeg.Mp3ConversionArgs mp3Args = new FFmpeg.Mp3ConversionArgs();
            foreach (MusicFile source in playlist)
            {
                source.resetEvents();
                source.Progress = 0;
                source.isConverted = false;
                source.Converted += source_Converted;
#if (DEBUG)
                string outputFileName = Extensions.CombineWithValidate(source.Artist , source.Album , source.FileInformation.Name.Replace(source.FileInformation.Extension, "") + ".mp3");
#else
                //string outputFileName = source.Artist+"\\"+source.Album+"\\" + source.FileInformation.Name.Replace(source.FileInformation.Extension,"") + ".mp3";
                string outputFileName = Extensions.CombineWithValidate("output",source.Artist , source.Album , source.FileInformation.Name.Replace(source.FileInformation.Extension, "") + ".mp3");
#endif
                if(outputFileName.Length>200){
                    outputFileName = Extensions.CombineWithValidate(source.Artist.Substring(0, 20), source.Album.Substring(0, 20), source.FileInformation.Name.Replace(source.FileInformation.Extension, "") + ".mp3");
                }
                if(OutputPath.Directory.Exists){
                    outputFileName = Path.Combine(OutputPath.FullName,outputFileName);
                }
                FileInfo output = new FileInfo(outputFileName);
                #region IMPLEMENT_MESSAGEBOX
                if (output.Exists)
                {
                    source.isConverted = true;
                    continue;
                }
                while (output.Exists)
                {
                    int cnt = 1;
                    output = new FileInfo(Extensions.CombineWithValidate("output",source.Artist.Substring(0, 20), source.Album.Substring(0, 20), source.FileInformation.Name.Replace(source.FileInformation.Extension, "") + "("+cnt+").mp3"));
                    cnt++;
                }
                output.Directory.Create();
                #endregion

                convertArgs conversionArgs = new convertArgs
                {
                    Input = source,
                    Arguments = mp3Args,
                    Output = output
                };
                BackgroundWorker mConvert = new BackgroundWorker();
                mConvert.WorkerReportsProgress = true;
                mConvert.RunWorkerCompleted += (sender, EventArgs) =>
                {
                    if (EventArgs.Error != null) throw EventArgs.Error;
                    conversionArgs.Input.isConverted = (bool)EventArgs.Result;
                };
                mConvert.ProgressChanged += (sender, EventArgs) =>
                {
                    conversionArgs.Input.Progress = EventArgs.ProgressPercentage;
                };
                mConvert.DoWork += convertingProcess;
                Conversions.Add(new Tuple<BackgroundWorker, convertArgs>(mConvert, conversionArgs));
            }

            foreach (var task in Conversions)
            {
                task.Item1.RunWorkerCompleted += runNewConversion;
            }

            for (int threads = 0; threads < Conversions.Count; threads++)
            {
                var task = Conversions[threads];

                if (threads < thread_no) task.Item1.RunWorkerAsync(task.Item2);
                else
                {
                    this.Conversions.Enqueue(task);
                }
            }


        }

        private void runNewConversion(object sender, RunWorkerCompletedEventArgs e)
        {
            if (Conversions.Count != 0)
            {
                var task = Conversions.Dequeue();
                task.Item1.RunWorkerAsync(task.Item2);
            }

        }

        void source_Converted(object sender, EventArgs e)
        {
            //MessageBox.Show("Converted File!");
            
            pgrConvert.Value++;
            tslblStatus.Text = "Converting ("+pgrConvert.Value + "/" + playlist.Count+").";
        }

        private void convertingProcess(object sender, DoWorkEventArgs e)
        {
#if (DEBUG)
            //MessageBox.Show("Worker Called");
#endif
            convertArgs Argument = (convertArgs)e.Argument;
            FFmpeg mFFmepg = new FFmpeg();
            mFFmepg.Converter.ProgressChanged += (send, eargs) =>
            {
                BackgroundWorker origin = sender as BackgroundWorker;
                int progress = System.Convert.ToInt32(eargs.Progress * 100);
                origin.ReportProgress(progress);
            };
            e.Result = mFFmepg.Converter.ToMp3(Argument.Input.FileInformation, Argument.Output, Argument.Arguments);
            //txtLog.Text+=(mFFmepg.Converter.LastError);
        }



        private struct convertArgs
        {
            public MusicFile Input { get; set; }
            public ffmpeg_convert.FFmpeg.Mp3ConversionArgs Arguments { get; set; }

            public FileInfo Output { get; set; }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            folderDialog_output.ShowDialog();
            txtOuput.Text = folderDialog_output.SelectedPath;
        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }


    }
}
