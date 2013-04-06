using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Be.Windows.Forms;
using BrightIdeasSoftware;
using woanware;

namespace SessionViewer
{
    /// <summary>
    /// 
    /// </summary>
    public partial class FormMain : Form
    {
        #region Member Variables
        private HourGlass _hourGlass;
        private Parser _parser;
        private string _outputPath = string.Empty;
        //private List<Session> _sessions = null;
        #endregion

        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        public FormMain()
        {
            InitializeComponent();

            _parser = new Parser();
            _parser.Complete += OnParser_Complete;
            _parser.Error += OnParser_Error;
            _parser.Exclamation += OnParser_Exclamation;
            _parser.Message += OnParser_Message;
        }
        #endregion

        #region Parser Event Handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        private void OnParser_Exclamation(string message)
        {
            _hourGlass.Dispose();
            UserInterface.DisplayMessageBox(this, message, MessageBoxIcon.Exclamation);
            SetProcessingStatus(true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        private void OnParser_Message(string message)
        {
            UpdateStatusBar(message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        private void OnParser_Error(string message)
        {
            _hourGlass.Dispose();
            UserInterface.DisplayErrorMessageBox(this, message);
            SetProcessingStatus(true);
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnParser_Complete()
        {
            LoadSessions();
        }
        #endregion

        #region User Interface Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="enabled"></param>
        private void SetProcessingStatus(bool enabled)
        {
            MethodInvoker methodInvoker = delegate
            {
                //this.Enabled = enabled;
            };

            if (this.InvokeRequired == true)
            {
                this.BeginInvoke(methodInvoker);
            }
            else
            {
                methodInvoker.Invoke();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        private void UpdateStatusBar(string text)
        {
            MethodInvoker methodInvoker = delegate
            {
                statusLabel.Text = text;
            };

            if (this.InvokeRequired == true)
            {
                this.BeginInvoke(methodInvoker);
            }
            else
            {
                methodInvoker.Invoke();
            }
        }
        #endregion

        #region Listview Event Handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listSession_SelectedIndexChanged(object sender, EventArgs e)
        {
            Task task = Task.Factory.StartNew(() =>
            {
                MethodInvoker methodInvoker = delegate
                {
                    using (new HourGlass(this))
                    {
                        if (listSession.SelectedObjects.Count != 1)
                        {
                            return;
                        }

                        Session session = (Session)listSession.SelectedObjects[0];
                        if (session == null)
                        {
                            UserInterface.DisplayErrorMessageBox(this, "Unable to locate session");
                            return;
                        }

                        LoadSession(session);
                    }
                };

                if (this.InvokeRequired == true)
                {
                    this.BeginInvoke(methodInvoker);
                }
                else
                {
                    methodInvoker.Invoke();
                }
            });
        }
        #endregion

        #region Menu Event Handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuHelpAbout_Click(object sender, EventArgs e)
        {
            using (FormAbout formAbout = new FormAbout())
            {
                formAbout.ShowDialog(this);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuHelpHelp_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuFileExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuFileImport_Click(object sender, EventArgs e)
        {
            using (FormImport formOpen = new FormImport())
            {
                if (formOpen.ShowDialog(this) == System.Windows.Forms.DialogResult.Cancel)
                {
                    return;
                }

                _outputPath = formOpen.DatabaseFile;
                _hourGlass = new HourGlass(this);
                SetProcessingStatus(false);

                _parser.Parse(formOpen.PcapFile, formOpen.DatabaseFile);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuFileOpen_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "Select the output folder";

            if (folderBrowserDialog.ShowDialog(this) == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }

            if (File.Exists(System.IO.Path.Combine(folderBrowserDialog.SelectedPath, Global.DB_FILE)) == false)
            {
                UserInterface.DisplayErrorMessageBox(this, "A SessionViewer database cannot be located");
            }

            _outputPath = folderBrowserDialog.SelectedPath;
            LoadSessions();
        }
        #endregion

        #region Form Event Handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A)
            {
                OLVListItem next = listSession.GetNextItem(listSession.SelectedItem);
                if (next == null)
                {
                    return;
                }

                LoadSession((Session)next.RowObject);
                listSession.SelectedObject = (Session)next.RowObject;
                listSession.EnsureModelVisible((Session)next.RowObject);
            }
            else if (e.KeyCode == Keys.Q)
            {
                OLVListItem previous = listSession.GetPreviousItem(listSession.SelectedItem);
                if (previous == null)
                {
                    return;
                }

                LoadSession((Session)previous.RowObject);
                listSession.SelectedObject = (Session)previous.RowObject;
                listSession.EnsureModelVisible((Session)previous.RowObject);
            }
        }
        #endregion

        #region Misc Methods
        /// <summary>
        /// 
        /// </summary>
        private void LoadSessions()
        {
            MethodInvoker methodInvoker = delegate
            {
                using (new HourGlass(this))
                {
                    List<Session> sessions = new List<Session>();
                    using (DbConnection connection = Db.GetOpenConnection(_outputPath))
                    using (var db = new NPoco.Database(connection, NPoco.DatabaseType.SQLCe))
                    {
                        try
                        {
                            foreach (var session in db.Fetch<Session>("SELECT * FROM Sessions"))
                            {
                                sessions.Add(session);
                            }
                        }
                        catch (Exception ex)
                        {
                            UserInterface.DisplayErrorMessageBox(this, "An error occurred whilst retreiving the session records: " + ex.Message);
                        }
                    }  

                    listSession.ClearObjects();
                    listSession.SetObjects(sessions);

                    if (listSession.Items.Count > 0)
                    {
                        listSession.SelectedObject = sessions[0];
                    }

                    olvcSourceIp.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                    olvcSourcePort.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
                    olvcDestinationIp.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                    olvcDestinationPort.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
                    olvcSize.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                    olvcHttpHost.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                    olvcTimestampFirstPacket.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                    olvcTimestampLastPacket.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                }
            };

            if (this.InvokeRequired == true)
            {
                this.BeginInvoke(methodInvoker);
            }
            else
            {
                methodInvoker.Invoke();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        private void LoadSession(Session session)
        {
            Task task = Task.Factory.StartNew(() =>
            {
                MethodInvoker methodInvoker = delegate
                {
                    using (new HourGlass(this))
                    {
                        if (session == null)
                        {
                            UserInterface.DisplayErrorMessageBox(this, "Unable to locate session");
                            return;
                        }

                        // HEX
                        DynamicByteProvider dynamicByteProvider = new DynamicByteProvider(File.ReadAllBytes(System.IO.Path.Combine(_outputPath, session.Guid + ".bin")));
                        hexBox.ByteProvider = dynamicByteProvider;

                        // Colourised (HTML)
                        webControl.Url = new Uri(String.Format(@"file:///" + System.IO.Path.Combine(_outputPath, session.Guid + ".html"), _outputPath));
                        
                        // ASCII
                        txtSession.Text = File.ReadAllText(System.IO.Path.Combine(_outputPath, session.Guid + ".txt"));
                        txtSession.ScrollToTop();
                    }
                };

                if (this.InvokeRequired == true)
                {
                    this.BeginInvoke(methodInvoker);
                }
                else
                {
                    methodInvoker.Invoke();
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="field"></param>
        public void CopyDataToClipboard(Global.FieldsCopy field)
        {
            if (listSession.SelectedObjects.Count != 1)
            {
                return;
            }

            Session temp = (Session)listSession.SelectedObjects[0];
            if (temp == null)
            {
                UserInterface.DisplayErrorMessageBox(this, "Unable to locate session");
                return;
            }

            string value = string.Empty;
            switch (field)
            {
                case Global.FieldsCopy.SourceIp:
                    value = temp.SrcIpText;
                    break;
                case Global.FieldsCopy.SourcePort:
                    value = temp.SourcePort.ToString();
                    break;
                case Global.FieldsCopy.DestinationIp:
                    value = temp.DstIpText;
                    break;
                case Global.FieldsCopy.DestinationPort:
                    value = temp.DestinationPort.ToString();
                    break;
                case Global.FieldsCopy.Size:
                    value = temp.DataSize.ToString();
                    break;
                case Global.FieldsCopy.TimestampFirstPacket:
                    //value = temp.TimestampFirstPacket.ToString();
                    break;
                case Global.FieldsCopy.TimestampLastPacket:
                    //value = temp.TimestampLastPacket.ToString();
                    break;
            }

            Clipboard.SetText(value);

            UpdateStatusBar("\"" + value + "\" copied to the clipboard");
        }
        #endregion

        #region Form Event Handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormMain_Load(object sender, EventArgs e)
        {
            
        }
        #endregion

        #region Web Control Event Handlers
        /// <summary>
        /// This is used to reload the HTML content, since it is not correctly loaded the web 
        /// browser control gets into focus, which somehow triggers some unknown initialisation
        /// process. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WebControl_VisibleChanged(object sender, EventArgs e)
        {
            //if (webControl.IsDocumentReady == false)
            //{
            //    listSession_SelectedIndexChanged(this, new EventArgs());
            //}
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void webControl_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            KeyEventArgs kea = new KeyEventArgs(e.KeyCode);
            FormMain_KeyDown(this, kea);
        }
        #endregion   

        #region Context Menu Event Handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void contextCopySourceIp_Click(object sender, EventArgs e)
        {
            CopyDataToClipboard(Global.FieldsCopy.SourceIp);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void contextCopySourcePort_Click(object sender, EventArgs e)
        {
            CopyDataToClipboard(Global.FieldsCopy.SourcePort);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void contextCopyDestinationIp_Click(object sender, EventArgs e)
        {
            CopyDataToClipboard(Global.FieldsCopy.DestinationIp);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void contextCopyDestinationPort_Click(object sender, EventArgs e)
        {
            CopyDataToClipboard(Global.FieldsCopy.DestinationPort);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void contextCopySize_Click(object sender, EventArgs e)
        {
            CopyDataToClipboard(Global.FieldsCopy.Size);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void contextCopyTimestampFirstPacket_Click(object sender, EventArgs e)
        {
            CopyDataToClipboard(Global.FieldsCopy.TimestampFirstPacket);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void contextCopyTimestampLastPacket_Click(object sender, EventArgs e)
        {
            CopyDataToClipboard(Global.FieldsCopy.TimestampLastPacket);
        }
        #endregion    

        #region Toolbar Button Event Handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolBtnImport_Click(object sender, EventArgs e)
        {
            menuFileImport_Click(this, new EventArgs());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolBtnOpen_Click(object sender, EventArgs e)
        {
            menuFileOpen_Click(this, new EventArgs());
        }
        #endregion
    }
}
