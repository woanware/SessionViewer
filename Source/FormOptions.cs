using System.Windows.Forms;
using woanware;

namespace SessionViewer
{
    /// <summary>
    /// 
    /// </summary>
    public partial class FormOptions : Form
    {
        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        /// <param name="autoGzip"></param>
        /// <param name="bufferInterval"></param>
        /// <param name="sessionInterval"></param>
        public FormOptions(bool autoGzip, 
                           int bufferInterval, 
                           int sessionInterval)
        {
            InitializeComponent();

            chkAutoGzip.Checked = autoGzip;
            UserInterface.LocateAndSelectComboBoxValue(bufferInterval.ToString(), cboBufferInterval);
            UserInterface.LocateAndSelectComboBoxValue(sessionInterval.ToString(), cboSessionInterval);
        }
        #endregion

        #region Button Event Handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOk_Click(object sender, System.EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, System.EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
        #endregion

        #region Properties
        /// <summary>
        /// 
        /// </summary>
        public bool AutoGzip
        {
            get
            {
                return chkAutoGzip.Checked;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int BufferInterval
        {
            get
            {
                return int.Parse(cboBufferInterval.Items[cboBufferInterval.SelectedIndex].ToString());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int SessionInterval
        {
            get
            {
                return int.Parse(cboSessionInterval.Items[cboSessionInterval.SelectedIndex].ToString());
            }
        }
        #endregion
    }
}
