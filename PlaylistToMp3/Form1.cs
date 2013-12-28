using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PlaylistToMp3_DLL;

namespace PlaylistToMp3
{
    public partial class Form1 : Form
    {
        

        public Form1()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog m_open = new OpenFileDialog();
            m_open.Multiselect = false;
            m_open.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            m_open.ShowDialog();
            if (m_open.FileName != string.Empty) { 
                
            }
            var playlist = PlaylistToMp3_DLL.PlaylistLoader.GetPlaylist(m_open.FileName);
            dtgrPlaylist.DataSource = playlist;
            
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
