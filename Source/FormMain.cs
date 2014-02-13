using Be.Windows.Forms;
using BrightIdeasSoftware;
using CsvHelper.Configuration;
using SessionViewer.PacketProcessors;
using SessionViewer.SessionParsers;
using SessionViewer.SessionProcessors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Trinet.Core.IO.Ntfs;
using woanware;

namespace SessionViewer
{
    /// <summary>
    /// 
    /// </summary>
    public partial class FormMain : Form
    {
        #region Member Variables
        private Settings _settings;
        private HourGlass _hourGlass;
        private Parser _parser;
        private string _outputPath = string.Empty;
        //private List<Session> _sessions = null;
        private List<InterfaceExtractor> extractors;
        private List<InterfaceParser> parsers;
        private string _selectedGuid = string.Empty;
        #endregion

        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        public FormMain()
        {
            InitializeComponent();

            ////// enable internal logging to the console
            //InternalLogger.LogToConsole = true;

            ////// enable internal logging to a file
            //InternalLogger.LogFile = "C:\\USBDeviceForensics\\log2.txt";

            ////// set internal log level
            //InternalLogger.LogLevel = LogLevel.Trace;

            LoggingExtensions.Logging.Log.InitializeWith<LoggingExtensions.NLog.NLogLog>();

            //this.Log().Debug(() => "Testing testing 123!");

            //Logger logger = LogManager.GetLogger("MyClassName");
            //logger.Debug("testing testing 123!");

            _parser = new Parser();
            _parser.Complete += OnParser_Complete;
            _parser.Error += OnParser_Error;
            _parser.Exclamation += OnParser_Exclamation;
            _parser.Message += OnParser_Message;

            this.extractors = new List<InterfaceExtractor>();
            SmtpExtractor smtp = new SmtpExtractor();
            smtp.Complete += OnProcessor_Complete;
            smtp.Error += OnProcessor_Error;
            smtp.Message += OnProcessor_Message;
            smtp.Warning += OnProcessor_Warning;
            this.extractors.Add(smtp);

            this.parsers = new List<InterfaceParser>();
            this.parsers.Add(new DnsParser());
            this.parsers.Add(new SessionViewer.SessionParsers.HttpParser());

            foreach (InterfaceParser processor in this.parsers)
            {
                ToolStripMenuItem menuItem = new ToolStripMenuItem(processor.Name);
                menuItem.Tag = processor.Name;
                menuItem.CheckOnClick = true;
                menuItem.Checked = true;
                menuItem.Click += menuParsers_Click;
                menuParsers.DropDownItems.Add(menuItem);
            }

            cboMaxSession.SelectedIndex = 0;
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

            IO.WriteTextToFile(message + Environment.NewLine, System.IO.Path.Combine(Misc.GetUserDataDirectory(), "Errors.txt"), true);
            UserInterface.DisplayErrorMessageBox(this, message);
            SetProcessingStatus(true);
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnParser_Complete()
        {
            //_parser.Complete -= OnParser_Complete;
            //_parser.Error -= OnParser_Error;
            //_parser.Exclamation -= OnParser_Exclamation;
            //_parser.Message -= OnParser_Message;
            //_parser = null;

            LoadSessions();
        }
        #endregion

        #region Processor Event Handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        private void OnProcessor_Warning(string message)
        {
            UserInterface.DisplayMessageBox(this, message, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        private void OnProcessor_Message(string message)
        {
            UserInterface.DisplayMessageBox(this, message, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        private void OnProcessor_Error(string message)
        {
            UserInterface.DisplayMessageBox(this, message, MessageBoxIcon.Error);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        private void OnProcessor_Complete(string message)
        {
            _hourGlass.Dispose();
            UserInterface.DisplayMessageBox(this, message, MessageBoxIcon.Information);
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
        private void listSession_SelectionChanged(object sender, EventArgs e)
        {
            new Thread(() =>
            {
                MethodInvoker methodInvoker = delegate
                {
                    if (listSession.SelectedObjects.Count != 1)
                    {
                        return;
                    }

                    using (new HourGlass(this))
                    {
                        Session session = (Session)listSession.SelectedObjects[0];

                        // Check to ensure that the selected item has changed
                        if (_selectedGuid == session.Guid)
                        {
                            return;
                        }

                        _selectedGuid = session.Guid;

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
            }).Start();
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
            Misc.ShellExecuteFile(System.IO.Path.Combine(Misc.GetApplicationDirectory(), "Help.pdf"));
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

                listSession.ClearObjects();

                _parser.IgnoreLocal = toolBtnRemoteOnly.Checked;
                _parser.BufferInterval = _settings.BufferInterval;
                _parser.SessionInterval = _settings.SessionInterval;

                _parser.ClearParsers();
                foreach (InterfaceParser parser in this.parsers)
                {
                    if (parser.Enabled == false)
                    {
                        continue;
                    }

                    _parser.SetParser(parser);
                }

                long maxSize = 0;
                switch (cboMaxSession.SelectedIndex)
                {
                    case 0: // None
                        break;
                    case 1: // 1 MB
                        maxSize = 1048576;
                        break;
                    case 2: // 2 MB
                        maxSize = 2097152;
                        break;
                    case 3: // 3 MB
                        maxSize = 3145728;
                        break;
                    case 4: // 4 MB
                        maxSize = 4194304;
                        break;
                    case 5: // 5 MB
                        maxSize = 5242880;
                        break;
                    case 6: // 10 MB
                        maxSize = 10485760;
                        break;
                    case 7: // 15 MB
                        maxSize = 15728640;
                        break;
                }

                _parser.Parse(formOpen.PcapFile, formOpen.DatabaseFile, maxSize);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuParsers_Click(object sender, EventArgs e)
        {
            string name = ((ToolStripMenuItem)sender).Tag.ToString();

            var parser = (from p in this.parsers where p.Name.ToLower() == name.ToLower() select p).SingleOrDefault();
            if (parser == null)
            {
                UserInterface.DisplayErrorMessageBox(this, "Unable to locate parser");
                return;
            }

            parser.Enabled = ((ToolStripMenuItem)sender).Checked;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuFileExportUrls_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "Select the export directory";

            if (fbd.ShowDialog(this) == DialogResult.Cancel)
            {
                return;
            }

            string directory = fbd.SelectedPath;
            string fileName = System.IO.Path.Combine(directory, "urls.tsv");

            new Thread(() =>
            {
                MethodInvoker methodInvoker = delegate
                {
                    using (new HourGlass(this))
                    {
                        CsvConfiguration csvConfiguration = new CsvConfiguration();
                        csvConfiguration.Delimiter = "\t";

                        using (FileStream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                        using (StreamWriter streamWriter = new StreamWriter(fileStream))
                        using (CsvHelper.CsvWriter csvWriter = new CsvHelper.CsvWriter(streamWriter, csvConfiguration))
                        {
                            csvWriter.WriteField("Host");
                            csvWriter.WriteField("Destination IP");
                            csvWriter.WriteField("URL");
                            csvWriter.NextRecord();

                            List<string> temp = new List<string>();

                            foreach (Session session in listSession.Objects)
                            {
                                if (File.Exists(System.IO.Path.Combine(_outputPath, session.Guid + ".urls")) == false)
                                {
                                    continue;
                                }

                                string[] urls = File.ReadAllLines(System.IO.Path.Combine(_outputPath, session.Guid + ".urls"));
                                foreach (string url in urls)
                                {
                                    string tempUrl = url.Trim();
                                    if (tempUrl.Length == 0)
                                    {
                                        continue;
                                    }

                                    if (temp.Contains(session.HttpHost + tempUrl) == false)
                                    {
                                        temp.Add(session.HttpHost + tempUrl);
                                    }

                                    csvWriter.WriteField(session.HttpHost);
                                    csvWriter.WriteField(session.DstIpText);
                                    csvWriter.WriteField(tempUrl);
                                    csvWriter.NextRecord();
                                }
                            }

                            temp.Sort();

                            foreach (string url in temp)
                            {
                                IO.WriteUnicodeTextToFile(url, System.IO.Path.Combine(directory, "urls.txt"), true);
                            }
                        }
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
            }).Start();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuToolsOptions_Click(object sender, EventArgs e)
        {
            using (FormOptions formOptions = new FormOptions(_settings.BufferInterval, 
                                                             _settings.SessionInterval))
            {
                if (formOptions.ShowDialog(this) == DialogResult.Cancel)
                {
                    return;
                }

                _settings.BufferInterval = formOptions.BufferInterval;
                _settings.SessionInterval = formOptions.SessionInterval;

                if (_parser != null)
                {
                    _parser.BufferInterval = formOptions.BufferInterval;
                    _parser.SessionInterval = formOptions.SessionInterval;
                }
            }
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
            _settings = new Settings();
            if (_settings.FileExists == true)
            {
                string ret = _settings.Load();
                if (ret.Length > 0)
                {
                    UserInterface.DisplayErrorMessageBox(this, ret);
                }
                else
                {
                    this.WindowState = _settings.FormState;

                    if (_settings.FormState != FormWindowState.Maximized)
                    {
                        this.Location = _settings.FormLocation;
                        this.Size = _settings.FormSize;
                    }
                }
            }

            _parser.BufferInterval = _settings.BufferInterval;
            _parser.SessionInterval = _settings.SessionInterval;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_parser.IsRunning == true)
            {
                _parser.Stop();
            }

            _settings.FormLocation = base.Location;
            _settings.FormSize = base.Size;
            _settings.FormState = base.WindowState;
            string ret = _settings.Save();
            if (ret.Length > 0)
            {
                UserInterface.DisplayErrorMessageBox(this, ret);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A)
            {
                int index = listSession.GetDisplayOrderOfItemIndex(listSession.SelectedItem.Index);
                OLVListItem next = listSession.GetNthItemInDisplayOrder(index + 1);
                //OLVListItem next = listSession.GetNextItem(listSession.SelectedItem);
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
                int index = listSession.GetDisplayOrderOfItemIndex(listSession.SelectedItem.Index);
                OLVListItem previous = listSession.GetNthItemInDisplayOrder(index - 1);

                //OLVListItem previous = listSession.GetPreviousItem(listSession.SelectedItem);
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

                    //listSession.ClearObjects();
                    listSession.SetObjects(sessions);

                    if (listSession.Items.Count > 0)
                    {
                        listSession.SelectedObject = sessions[0];
                    }

                    olvcSourceIp.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                    olvcSourcePort.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
                    olvcSourceCountry.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
                    olvcDestinationIp.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                    olvcDestinationPort.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
                    olvcDestinationCountry.AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
                    olvcSize.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                    olvcHttpHost.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                    olvcHttpMethods.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
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
            (new Thread(() =>
            {
                MethodInvoker methodInvoker = delegate
                {
                    try
                    {
                        using (new HourGlass(this))
                        {
                            if (session == null)
                            {
                                UserInterface.DisplayErrorMessageBox(this, "Unable to locate session");
                                return;
                            }

                            string filePath = System.IO.Path.Combine(_outputPath, session.Guid.Substring(0, 2), session.Guid + ".bin");

                            if (File.Exists(filePath) == false)
                            {
                                UserInterface.DisplayErrorMessageBox(this, "Session data file does not exist: " + filePath);
                                return;
                            }

                            byte[] temp = File.ReadAllBytes(filePath);
                            DynamicByteProvider dynamicByteProvider = new DynamicByteProvider(temp);
                            hexBox.ByteProvider = dynamicByteProvider;

                            temp = woanware.Text.ReplaceNulls(temp);

                            FileInfo fileInfo = new FileInfo(filePath);

                            // Colourised (HTML)
                            if (fileInfo.AlternateDataStreamExists("html") == true)
                            {
                                AlternateDataStreamInfo ads = fileInfo.GetAlternateDataStream("html", FileMode.Open);
                                using (TextReader reader = ads.OpenText())
                                {
                                    webControl.DocumentText = reader.ReadToEnd();
                                }
                            }
                            else
                            {
                                webControl.DocumentText = string.Empty;
                            }

                            // ASCII
                            if (fileInfo.AlternateDataStreamExists("txt") == true)
                            {
                                AlternateDataStreamInfo ads = fileInfo.GetAlternateDataStream("txt", FileMode.Open);
                                using (TextReader reader = ads.OpenText())
                                {

                                    txtSession.Text = reader.ReadToEnd();
                                    txtSession.ScrollToTop();
                                }
                            }
                            else
                            {
                                txtSession.Text = ASCIIEncoding.ASCII.GetString(temp);
                                txtSession.ScrollToTop();
                            }

                            // Info
                            if (fileInfo.AlternateDataStreamExists("info") == true)
                            {
                                AlternateDataStreamInfo ads = fileInfo.GetAlternateDataStream("info", FileMode.Open);
                                using (TextReader reader = ads.OpenText())
                                {
                                    txtInfo.Text = reader.ReadToEnd();
                                    txtInfo.ScrollToTop();
                                }
                            }
                            else
                            {
                                txtInfo.Text = string.Empty;
                                txtInfo.ScrollToTop();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Log().Error(ex.ToString());            
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

            })).Start();
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
                    value = temp.TimestampFirstPacket.ToString();
                    break;
                case Global.FieldsCopy.TimestampLastPacket:
                    value = temp.TimestampLastPacket.ToString();
                    break;
                case Global.FieldsCopy.SourceCountry:
                    value = temp.SourceCountry.ToString();
                    break;
                case Global.FieldsCopy.DestinationCountry:
                    value = temp.DestinationCountry.ToString();
                    break;
            }

            Clipboard.SetText(value);

            UpdateStatusBar("\"" + value + "\" copied to the clipboard");
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
        private void context_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (listSession.SelectedObjects.Count != 1)
            {
                contextDecodeGzip.Enabled = false;
                return;
            }

            Session temp = (Session)listSession.SelectedObjects[0];
            if (temp == null)
            {
                contextDecodeGzip.Enabled = false;
                return;
            }

            contextDecodeGzip.Enabled = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void contextDecodeGzip_Click(object sender, EventArgs e)
        {
            (new Thread(() =>
            {
                MethodInvoker methodInvoker = delegate
                {
                    using (new HourGlass(this))
                    {
                        Session temp = (Session)listSession.SelectedObjects[0];
                        if (temp == null)
                        {
                            contextDecodeGzip.Enabled = false;
                            return;
                        }

                        if (temp == null)
                        {
                            UserInterface.DisplayErrorMessageBox(this, "Unable to locate session");
                            return;
                        }

                        var httpParser = new HttpParser();
                        httpParser.Process(this._outputPath, temp);
                        LoadSession(temp);

                        using (DbConnection connection = Db.GetOpenConnection(this._outputPath))
                        using (var db = new NPoco.Database(connection, NPoco.DatabaseType.SQLCe))
                        {
                            try
                            {
                                var session = db.SingleOrDefaultById<Session>(temp.Id);
                                if (session != null)
                                {
                                    listSession.RefreshObject(session);
                                }
                            }
                            catch (Exception ex)
                            {
                                UserInterface.DisplayErrorMessageBox(this, "An error occurred whilst retreiving the sessions details: " + ex.Message);
                            }
                        }  
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
            })).Start();
        }

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void contextCopySourceCountry_Click(object sender, EventArgs e)
        {
            CopyDataToClipboard(Global.FieldsCopy.SourceCountry);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void contextCopyDestinationCountry_Click(object sender, EventArgs e)
        {
            CopyDataToClipboard(Global.FieldsCopy.DestinationCountry);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void contextExportUniqueSourceIp_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Select the export CSV";
            saveFileDialog.Filter = "TSV Files|*.tsv";
            if (saveFileDialog.ShowDialog(this) == DialogResult.Cancel)
            {
                return;
            }

            List<Session> sessions = listSession.Objects.Cast<Session>().ToList();
            var unique = sessions.Select(s => s.SrcIpText).Distinct();

            CsvConfiguration csvConfiguration = new CsvConfiguration();
            csvConfiguration.Delimiter = "\t";

            using (FileStream fileStream = new FileStream(saveFileDialog.FileName, FileMode.Append, FileAccess.Write))
            using (StreamWriter streamWriter = new StreamWriter(fileStream))
            using (CsvHelper.CsvWriter csvWriter = new CsvHelper.CsvWriter(streamWriter, csvConfiguration))
            {
                csvWriter.WriteField("Source IP");
                csvWriter.NextRecord();

                foreach (var ip in unique)
                {
                    csvWriter.WriteField(ip);
                    csvWriter.NextRecord();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void contextExportDestinationIp_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Select the export CSV";
            saveFileDialog.Filter = "TSV Files|*.tsv";
            if (saveFileDialog.ShowDialog(this) == DialogResult.Cancel)
            {
                return;
            }

            List<Session> sessions = listSession.Objects.Cast<Session>().ToList();
            var unique = sessions.Select(s => s.DstIpText).Distinct();

            CsvConfiguration csvConfiguration = new CsvConfiguration();
            csvConfiguration.Delimiter = "\t";

            using (FileStream fileStream = new FileStream(saveFileDialog.FileName, FileMode.Append, FileAccess.Write))
            using (StreamWriter streamWriter = new StreamWriter(fileStream))
            using (CsvHelper.CsvWriter csvWriter = new CsvHelper.CsvWriter(streamWriter, csvConfiguration))
            {
                csvWriter.WriteField("Destination IP");
                csvWriter.NextRecord();

                foreach (var ip in unique)
                {
                    csvWriter.WriteField(ip);
                    csvWriter.NextRecord();
                }
            }
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuProcessorsSmtp_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "Select the output folder";

            if (folderBrowserDialog.ShowDialog(this) == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }

            string outputFolder = folderBrowserDialog.SelectedPath;

            _hourGlass = new HourGlass(this);
            var processor = (from p in this.extractors where p.Name.ToLower() == "smtp" select p).SingleOrDefault();
            if (processor == null)
            {
                UserInterface.DisplayErrorMessageBox(this, "Unable to locate extractor");
                return;
            }
            
            processor.Run((ArrayList)listSession.Objects, _outputPath, outputFolder);
        }
    }
}
