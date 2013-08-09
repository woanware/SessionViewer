using System;
using System.IO;
using System.Windows.Forms;
using woanware;

namespace SessionViewer
{
    /// <summary>
    /// 
    /// </summary>
    public partial class FormImport : Form
    {
        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        public FormImport()
        {
            InitializeComponent();
        }
        #endregion

        #region Button Event Handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPcap_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PCAP Files|*.pcap";
            openFileDialog.FileName = "*.pcap";
            openFileDialog.Title = "Select the PCAP file";

            if (openFileDialog.ShowDialog(this) == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }

            txtPcapFile.Text = openFileDialog.FileName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOutput_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "Select the output folder";

            if (folderBrowserDialog.ShowDialog(this) == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }

            if (File.Exists(System.IO.Path.Combine(txtOutputPath.Text, Global.DB_FILE)) == true)
            {
                DialogResult dialogResult = MessageBox.Show(this,
                                                       "A SessionViewer database already exists in the directory. Do you want to overwrite?",
                                                       Application.ProductName,
                                                       MessageBoxButtons.YesNo,
                                                       MessageBoxIcon.Question);

                if (dialogResult == System.Windows.Forms.DialogResult.No)
                {
                    return;
                }
            }

            txtOutputPath.Text = folderBrowserDialog.SelectedPath;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOk_Click(object sender, EventArgs e)
        {
            if (txtPcapFile.Text.Trim().Length == 0)
            {
                UserInterface.DisplayMessageBox(this, "The PCAP file must be selected", MessageBoxIcon.Exclamation);
                btnPcap.Select();
                return;
            }

            if (File.Exists(txtPcapFile.Text) == false)
            {
                UserInterface.DisplayMessageBox(this, "The PCAP file does not exist", MessageBoxIcon.Exclamation);
                btnPcap.Select();
                return;
            }

            if (txtOutputPath.Text.Trim().Length == 0)
            {
                UserInterface.DisplayMessageBox(this, "The output directory must be selected", MessageBoxIcon.Exclamation);
                btnOutput.Select();
                return;
            }

            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }
        #endregion

        #region Properties
        /// <summary>
        /// 
        /// </summary>
        public string DatabaseFile
        {
            get
            {
                return txtOutputPath.Text;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string PcapFile
        {
            get
            {
                return txtPcapFile.Text;
            }
        }
        #endregion
    }
}
