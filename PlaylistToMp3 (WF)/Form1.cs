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
            this.SetStyle(
  ControlStyles.AllPaintingInWmPaint |
  ControlStyles.UserPaint |
  ControlStyles.DoubleBuffer, true);
            ThreadPool.SetMinThreads(200, 200);
            InitializeComponent();
        }
#if (DEBUG)
        StreamWriter logfile;
#endif
        private void log(params string[] args)
        {
            foreach (string log in args)
            {
#if (DEBUG)
                logfile.Write(log+Environment.NewLine);
                logfile.AutoFlush = true;
#endif
                //txtLog.Text += DateTime.Now.ToShortTimeString() + ": " + log;
                //txtLog.Text += Environment.NewLine;
            }
        }

        int numberOfThreads = Environment.ProcessorCount * 2;
        BindingList<MusicFile> playlist;
        int thread_no = Environment.ProcessorCount;
        Queue<Tuple<BackgroundWorker, convertArgs>> Conversions = new Queue<Tuple<BackgroundWorker, convertArgs>>();
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //OpenFileDialog m_open = new OpenFileDialog();
            //m_open.Multiselect = false;
            m_open.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            m_open.ShowDialog();
            if (m_open.FileName != string.Empty)
            {

                log(m_open.FileName + " playlist selected.");
                playlist = new BindingList<MusicFile>(PlaylistToMp3_DLL.PlaylistLoader.GetPlaylist(m_open.FileName));
                log(m_open.FileName + " playlist loaded.");
                BindingListView<MusicFile> view = new BindingListView<MusicFile>(playlist);
                dtgrPlaylist.DataSource = view;
                
                tslblStatus.Text = playlist.Count + " song loaded.";
                log(playlist.Count + " song loaded.");
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
                log(((MusicFile)dtgrPlaylist.SelectedRows[0].DataBoundItem).ShortFileName + " song removed from playlist.");
            }
        }

        private void btnConvert_Click(object sender, EventArgs e)
        {
            log("Conversions started");
            pgrConvert.Value = 0;
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
            log("Converting " + playlist.Count + " items.");
            tslblStatus.Text = "Converting " + playlist.Count + " items.";
            pgrConvert.Value = 0;
            pgrConvert.Maximum = playlist.Count;
            List<Tuple<BackgroundWorker, convertArgs>> Conversions = new List<Tuple<BackgroundWorker, convertArgs>>();
            ffmpeg_convert.FFmpeg.Mp3ConversionArgs mp3Args = new FFmpeg.Mp3ConversionArgs
            {
                isVariable = rdbVBR.Checked,
                Preset = (int)cmbPresets.SelectedItem,
                MinBitrate = (int)cmbMinBR.SelectedItem
            };
            log("FFmpeg mp3 conversion args:" +
                Environment.NewLine +
                "VBR: " + mp3Args.isVariable +
                Environment.NewLine +
                "Preset: " + mp3Args.Preset +
                Environment.NewLine +
                "Minimum Bitrate: " + mp3Args.MinBitrate);
            foreach (MusicFile source in playlist)
            {
                source.resetEvents();
                source.Progress = 0;
                source.isConverted = false;
                source.Converted += source_Converted;
                log("Preparing " + source.FileName + ".");
#if (DEBUG)
                string outputFileName = Extensions.CombineWithValidate(source.Artist, source.Album, source.FileInformation.Name.Replace(source.FileInformation.Extension, "") + ".mp3");
#else
                //string outputFileName = source.Artist+"\\"+source.Album+"\\" + source.FileInformation.Name.Replace(source.FileInformation.Extension,"") + ".mp3";
                string outputFileName = Extensions.CombineWithValidate("output",source.Artist , source.Album , source.FileInformation.Name.Replace(source.FileInformation.Extension, "") + ".mp3");
#endif
                if (outputFileName.Length > 200)
                {
                    outputFileName = Extensions.CombineWithValidate(source.Artist.Substring(0, 20), source.Album.Substring(0, 20), source.FileInformation.Name.Replace(source.FileInformation.Extension, "") + ".mp3");
                }
                if (OutputPath.Directory.Exists)
                {
                    outputFileName = Path.Combine(OutputPath.FullName, outputFileName);
                }
                FileInfo output = new FileInfo(outputFileName);
                log("Output: " + output.FullName);
                #region IMPLEMENT_MESSAGEBOX
                if (output.Exists)
                {
                    log(output.Name + " already exists.", "Skipping");
                    source.isConverted = true;
                    continue;
                }
                //while (output.Exists)
                //{
                //    int cnt = 1;
                //    output = new FileInfo(Extensions.CombineWithValidate("output", source.Artist.Substring(0, 20), source.Album.Substring(0, 20), source.FileInformation.Name.Replace(source.FileInformation.Extension, "") + "(" + cnt + ").mp3"));
                //    cnt++;
                //}
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
                    if (EventArgs.Error != null)
                    {
                        log(conversionArgs.Output.Name + "conversion failed.", "Exception: " + EventArgs.Error.ToString());
                    }
                    conversionArgs.Input.isConverted = (bool)EventArgs.Result;
                    log(conversionArgs.Output.Name + " conversion succesfull.");
                };
                mConvert.ProgressChanged += (sender, EventArgs) =>
                {
                    conversionArgs.Input.Progress = EventArgs.ProgressPercentage;
                };
                mConvert.DoWork += convertingProcess;
                Conversions.Add(new Tuple<BackgroundWorker, convertArgs>(mConvert, conversionArgs));
                log("Conversion " + conversionArgs.Input.ShortFileName + " => " + conversionArgs.Output.FullName + " queued.");
            }

            foreach (var task in Conversions)
            {
                task.Item1.RunWorkerCompleted += runNewConversion;
            }

            for (int threads = 0; threads < Conversions.Count; threads++)
            {
                var task = Conversions[threads];
                this.Conversions.Enqueue(task);
                if (threads < thread_no) startNewConversion();

            }



        }

        private void runNewConversion(object sender, RunWorkerCompletedEventArgs e)
        {
            startNewConversion();
        }
        void startNewConversion()
        {
            if (Conversions.Count != 0)
            {
                var task = Conversions.Dequeue();
                task.Item1.RunWorkerAsync(task.Item2);
                log("Conversion of " + task.Item2.Input.ShortFileName + " started.");
            }


        }

        void source_Converted(object sender, EventArgs e)
        {
            //MessageBox.Show("Converted File!");

            pgrConvert.Value++;
            tslblStatus.Text = "Converting (" + pgrConvert.Value + "/" + playlist.Count + ").";
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
            string fPath = txtOuput.Text;
            folderDialog_output.ShowDialog();
            txtOuput.Text = folderDialog_output.SelectedPath;
            if (txtOuput.Text != fPath)
            {
                log("Path changed to: " + txtOuput.Text);
            }

        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void rdbCBR_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rdbSource = sender as RadioButton;
            if (rdbSource.Checked)
            {
                switch (rdbSource.Tag.ToString())
                {
                    case "CBR":
                        log("CBR option selected.");
                        cmbPresets.DataSource = Extensions.CBR;
                        cmbPresets.SelectedItem = Extensions.CBR.Last();
                        break;
                    case "VBR":
                        log("VBR option selected.");
                        cmbPresets.DataSource = Extensions.VBR;
                        cmbPresets.SelectedIndex = 2;
                        break;
                    default:
                        break;
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
#if (DEBUG)
            string log_path = DateTime.Now.Year + "." + DateTime.Now.Ticks + " log.txt";
            File.Create(log_path).Close();
            logfile = File.AppendText(log_path);
#endif
            log("pConverter main window Loaded.");
            cmbPresets.DataSource = Extensions.VBR;
            cmbPresets.SelectedIndex = 2;
            txtOuput.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            folderDialog_output.SelectedPath = txtOuput.Text;
            cmbMinBR.DataSource = Extensions.CBR.ToArray();
            log("Default preferences loaded.");
        }


    }
}
