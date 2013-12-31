using Equin.ApplicationFramework;
using ffmpeg_convert;
using PlaylistToMp3_DLL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Program_Settings = PlaylistToMp3__WF_.Properties.Settings;

namespace PlaylistToMp3__WF_
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            this.SetStyle(
  ControlStyles.AllPaintingInWmPaint |
  ControlStyles.UserPaint |
  ControlStyles.DoubleBuffer, true);
            ThreadPool.SetMinThreads(200, 200);
            InitializeComponent();
        }

#if (DEBUG)
        private StreamWriter logfile;
#endif

        private void log(params string[] args)
        {
            foreach (string log in args)
            {
                string full_log = DateTime.Now.ToShortTimeString() + ": " + log + Environment.NewLine;
#if (DEBUG)
                logfile.Write(full_log);
                logfile.AutoFlush = true;
#endif

                txtLog.AppendText(full_log);
            }
        }

        private int numberOfThreads = Environment.ProcessorCount * 2;
        private BindingList<MusicFile> playlist;
        private int thread_no = Environment.ProcessorCount;
        private Queue<Tuple<BackgroundWorker, convertArgs>> Conversions = new Queue<Tuple<BackgroundWorker, convertArgs>>();

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //OpenFileDialog m_open = new OpenFileDialog();
            //m_open.Multiselect = false;
            m_open.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            m_open.ShowDialog();
            if (m_open.FileName != string.Empty)
            {
                LoadPlaylist(m_open.FileName);
            }
        }

        private void LoadPlaylist(string inputFileName)
        {
            log(inputFileName + " playlist selected.");
            playlist = new BindingList<MusicFile>(PlaylistToMp3_DLL.PlaylistLoader.GetPlaylist(inputFileName));
            log(inputFileName + " playlist loaded.");
            BindingListView<MusicFile> view = new BindingListView<MusicFile>(playlist);
            dtgrPlaylist.DataSource = view;

            tslblStatus.Text = playlist.Count + " song loaded.";
            log(playlist.Count + " song loaded.");
            PlaylistPath = new FileInfo(inputFileName);
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
                //log("Preparing " + source.FileName + ".");
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
                //log("Output: " + output.FullName);

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

                #endregion IMPLEMENT_MESSAGEBOX

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
                    log(conversionArgs.Input.ShortFileName + " => " + conversionArgs.Output.FullName + " conversion completed.");
                };
                mConvert.ProgressChanged += (sender, EventArgs) =>
                {
                    conversionArgs.Input.Progress = EventArgs.ProgressPercentage;
                };
                mConvert.DoWork += convertingProcess;
                Conversions.Add(new Tuple<BackgroundWorker, convertArgs>(mConvert, conversionArgs));
                //log("Conversion " + conversionArgs.Input.ShortFileName + " => " + conversionArgs.Output.FullName + " queued.");
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

        private void startNewConversion()
        {
            if (Conversions.Count != 0)
            {
                var task = Conversions.Dequeue();
                task.Item1.RunWorkerAsync(task.Item2);
                log("Conversion of " + task.Item2.Input.ShortFileName + " started.");
            }
        }

        private void source_Converted(object sender, EventArgs e)
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

        private void button1_Click(object sender, EventArgs e)
        {
            string fPath = txtOuput.Text;
            folderDialog_output.ShowDialog();
            txtOuput.Text = folderDialog_output.SelectedPath;
            if (txtOuput.Text != fPath)
            {
                log("Path changed to: " + txtOuput.Text);
                Program_Settings.Default.OutputPath = txtOuput.Text;
                SaveSettings();
            }
        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (PlaylistPath != null && PlaylistPath.Exists)
            {
                LoadPlaylist(PlaylistPath.FullName);
            }
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
                        Program_Settings.Default.isVariable = false;
                        break;

                    case "VBR":
                        log("VBR option selected.");
                        cmbPresets.DataSource = Extensions.VBR;
                        cmbPresets.SelectedIndex = 2;
                        Program_Settings.Default.isVariable = true;
                        break;

                    default:
                        break;
                }
                SaveSettings();
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
            int per_preset = Program_Settings.Default.Preset;
            int per_minbitrate = Program_Settings.Default.MinBitrate;
            if (Program_Settings.Default.isVariable)
            {
                cmbPresets.DataSource = Extensions.VBR;
            }
            else
            {
                cmbPresets.DataSource = Extensions.CBR;
            }

            cmbPresets.SelectedIndex = per_preset;
            string outPath = Program_Settings.Default.OutputPath;
            if (outPath != string.Empty && new FileInfo(outPath).Directory.Exists)
            {
                txtOuput.Text = outPath;
            }
            else
            {
                txtOuput.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                Program_Settings.Default.OutputPath = txtOuput.Text;
                SaveSettings();
            }

            folderDialog_output.SelectedPath = txtOuput.Text;
            cmbMinBR.DataSource = Extensions.CBR.ToArray();
            cmbMinBR.SelectedIndex = per_minbitrate;
            log("Preferences loaded.");
        }

        private void outputPresetChanged(object sender, EventArgs e)
        {
            ComboBox source = sender as ComboBox;
            if (Program_Settings.Default.Preset != source.SelectedIndex)
            {
                Program_Settings.Default.Preset = source.SelectedIndex;
                SaveSettings();
            }
            log("Output preset set to: " + source.SelectedValue);
        }

        private void minBitrateChanged(object sender, EventArgs e)
        {
            ComboBox source = sender as ComboBox;
            if (Program_Settings.Default.MinBitrate != source.SelectedIndex)
            {
                Program_Settings.Default.MinBitrate = source.SelectedIndex;
                SaveSettings();
            }
            log("Conversion minimum bitrate set to: " + source.SelectedValue);
        }

        private void SaveSettings()
        {
            Program_Settings.Default.Save();
        }

        private void whenClosing(object sender, FormClosingEventArgs e)
        {
            Program_Settings.Default.Save();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }
    }
}