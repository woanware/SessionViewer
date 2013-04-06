namespace SessionViewer
{
    partial class FormMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            this.menu = new System.Windows.Forms.MenuStrip();
            this.menuFile = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileImport = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFileSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.menuFileExit = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHelpHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHelpSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.menuHelpAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.toolBtnImport = new System.Windows.Forms.ToolStripButton();
            this.toolBtnOpen = new System.Windows.Forms.ToolStripButton();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.listSession = new BrightIdeasSoftware.FastObjectListView();
            this.olvcSourceIp = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.olvcSourcePort = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.olvcDestinationIp = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.olvcDestinationPort = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.olvcHttpHost = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.olvcSize = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.olvcTimestampFirstPacket = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.olvcTimestampLastPacket = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabPageAscii = new System.Windows.Forms.TabPage();
            this.txtSession = new System.Windows.Forms.RichTextBox();
            this.tabPageHex = new System.Windows.Forms.TabPage();
            this.hexBox = new Be.Windows.Forms.HexBox();
            this.tabPageColourised = new System.Windows.Forms.TabPage();
            this.context = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.contextCopy = new System.Windows.Forms.ToolStripMenuItem();
            this.contextCopySourceIp = new System.Windows.Forms.ToolStripMenuItem();
            this.contextCopySourcePort = new System.Windows.Forms.ToolStripMenuItem();
            this.contextCopyDestinationIp = new System.Windows.Forms.ToolStripMenuItem();
            this.contextCopyDestinationPort = new System.Windows.Forms.ToolStripMenuItem();
            this.contextCopySize = new System.Windows.Forms.ToolStripMenuItem();
            this.contextCopyTimestampFirstPacket = new System.Windows.Forms.ToolStripMenuItem();
            this.contextCopyTimestampLastPacket = new System.Windows.Forms.ToolStripMenuItem();
            this.webControl = new System.Windows.Forms.WebBrowser();
            this.menu.SuspendLayout();
            this.toolStrip.SuspendLayout();
            this.statusStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.listSession)).BeginInit();
            this.tabControl.SuspendLayout();
            this.tabPageAscii.SuspendLayout();
            this.tabPageHex.SuspendLayout();
            this.tabPageColourised.SuspendLayout();
            this.context.SuspendLayout();
            this.SuspendLayout();
            // 
            // menu
            // 
            this.menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuFile,
            this.menuHelp});
            this.menu.Location = new System.Drawing.Point(0, 0);
            this.menu.Name = "menu";
            this.menu.Padding = new System.Windows.Forms.Padding(7, 2, 0, 2);
            this.menu.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.menu.Size = new System.Drawing.Size(545, 24);
            this.menu.TabIndex = 0;
            this.menu.Text = "menuStrip1";
            // 
            // menuFile
            // 
            this.menuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuFileImport,
            this.menuFileOpen,
            this.menuFileSep1,
            this.menuFileExit});
            this.menuFile.Name = "menuFile";
            this.menuFile.Size = new System.Drawing.Size(37, 20);
            this.menuFile.Text = "&File";
            // 
            // menuFileImport
            // 
            this.menuFileImport.Name = "menuFileImport";
            this.menuFileImport.Size = new System.Drawing.Size(110, 22);
            this.menuFileImport.Text = "Import";
            this.menuFileImport.Click += new System.EventHandler(this.menuFileImport_Click);
            // 
            // menuFileOpen
            // 
            this.menuFileOpen.Name = "menuFileOpen";
            this.menuFileOpen.Size = new System.Drawing.Size(110, 22);
            this.menuFileOpen.Text = "Open";
            this.menuFileOpen.Click += new System.EventHandler(this.menuFileOpen_Click);
            // 
            // menuFileSep1
            // 
            this.menuFileSep1.Name = "menuFileSep1";
            this.menuFileSep1.Size = new System.Drawing.Size(107, 6);
            // 
            // menuFileExit
            // 
            this.menuFileExit.Name = "menuFileExit";
            this.menuFileExit.Size = new System.Drawing.Size(110, 22);
            this.menuFileExit.Text = "Exit";
            this.menuFileExit.Click += new System.EventHandler(this.menuFileExit_Click);
            // 
            // menuHelp
            // 
            this.menuHelp.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuHelpHelp,
            this.menuHelpSep1,
            this.menuHelpAbout});
            this.menuHelp.Name = "menuHelp";
            this.menuHelp.Size = new System.Drawing.Size(44, 20);
            this.menuHelp.Text = "Help";
            // 
            // menuHelpHelp
            // 
            this.menuHelpHelp.Name = "menuHelpHelp";
            this.menuHelpHelp.Size = new System.Drawing.Size(107, 22);
            this.menuHelpHelp.Text = "Help";
            this.menuHelpHelp.Click += new System.EventHandler(this.menuHelpHelp_Click);
            // 
            // menuHelpSep1
            // 
            this.menuHelpSep1.Name = "menuHelpSep1";
            this.menuHelpSep1.Size = new System.Drawing.Size(104, 6);
            // 
            // menuHelpAbout
            // 
            this.menuHelpAbout.Name = "menuHelpAbout";
            this.menuHelpAbout.Size = new System.Drawing.Size(107, 22);
            this.menuHelpAbout.Text = "About";
            this.menuHelpAbout.Click += new System.EventHandler(this.menuHelpAbout_Click);
            // 
            // toolStrip
            // 
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolBtnImport,
            this.toolBtnOpen});
            this.toolStrip.Location = new System.Drawing.Point(0, 24);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.toolStrip.Size = new System.Drawing.Size(545, 25);
            this.toolStrip.TabIndex = 1;
            this.toolStrip.Text = "toolStrip1";
            // 
            // toolBtnImport
            // 
            this.toolBtnImport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolBtnImport.Image = ((System.Drawing.Image)(resources.GetObject("toolBtnImport.Image")));
            this.toolBtnImport.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolBtnImport.Name = "toolBtnImport";
            this.toolBtnImport.Size = new System.Drawing.Size(23, 22);
            this.toolBtnImport.ToolTipText = "Import/New";
            this.toolBtnImport.Click += new System.EventHandler(this.toolBtnImport_Click);
            // 
            // toolBtnOpen
            // 
            this.toolBtnOpen.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolBtnOpen.Image = ((System.Drawing.Image)(resources.GetObject("toolBtnOpen.Image")));
            this.toolBtnOpen.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolBtnOpen.Name = "toolBtnOpen";
            this.toolBtnOpen.Size = new System.Drawing.Size(23, 22);
            this.toolBtnOpen.ToolTipText = "Open";
            this.toolBtnOpen.Click += new System.EventHandler(this.toolBtnOpen_Click);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 339);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Padding = new System.Windows.Forms.Padding(1, 0, 16, 0);
            this.statusStrip.Size = new System.Drawing.Size(545, 22);
            this.statusStrip.TabIndex = 2;
            this.statusStrip.Text = "statusStrip1";
            // 
            // statusLabel
            // 
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(0, 17);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 49);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.listSession);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.tabControl);
            this.splitContainer1.Size = new System.Drawing.Size(545, 290);
            this.splitContainer1.SplitterDistance = 270;
            this.splitContainer1.SplitterWidth = 5;
            this.splitContainer1.TabIndex = 3;
            // 
            // listSession
            // 
            this.listSession.AllColumns.Add(this.olvcSourceIp);
            this.listSession.AllColumns.Add(this.olvcSourcePort);
            this.listSession.AllColumns.Add(this.olvcDestinationIp);
            this.listSession.AllColumns.Add(this.olvcDestinationPort);
            this.listSession.AllColumns.Add(this.olvcHttpHost);
            this.listSession.AllColumns.Add(this.olvcSize);
            this.listSession.AllColumns.Add(this.olvcTimestampFirstPacket);
            this.listSession.AllColumns.Add(this.olvcTimestampLastPacket);
            this.listSession.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.olvcSourceIp,
            this.olvcSourcePort,
            this.olvcDestinationIp,
            this.olvcDestinationPort,
            this.olvcHttpHost,
            this.olvcSize,
            this.olvcTimestampFirstPacket,
            this.olvcTimestampLastPacket});
            this.listSession.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listSession.FullRowSelect = true;
            this.listSession.HideSelection = false;
            this.listSession.Location = new System.Drawing.Point(0, 0);
            this.listSession.MultiSelect = false;
            this.listSession.Name = "listSession";
            this.listSession.ShowGroups = false;
            this.listSession.Size = new System.Drawing.Size(270, 290);
            this.listSession.TabIndex = 0;
            this.listSession.UseCompatibleStateImageBehavior = false;
            this.listSession.UseFiltering = true;
            this.listSession.View = System.Windows.Forms.View.Details;
            this.listSession.VirtualMode = true;
            this.listSession.SelectedIndexChanged += new System.EventHandler(this.listSession_SelectedIndexChanged);
            // 
            // olvcSourceIp
            // 
            this.olvcSourceIp.AspectName = "SrcIpText";
            this.olvcSourceIp.CellPadding = null;
            this.olvcSourceIp.Text = "Source IP";
            // 
            // olvcSourcePort
            // 
            this.olvcSourcePort.AspectName = "SourcePort";
            this.olvcSourcePort.CellPadding = null;
            this.olvcSourcePort.Text = "Source Port";
            // 
            // olvcDestinationIp
            // 
            this.olvcDestinationIp.AspectName = "DstIpText";
            this.olvcDestinationIp.CellPadding = null;
            this.olvcDestinationIp.Text = "Destination IP";
            // 
            // olvcDestinationPort
            // 
            this.olvcDestinationPort.AspectName = "DestinationPort";
            this.olvcDestinationPort.CellPadding = null;
            this.olvcDestinationPort.Text = "Destination Port";
            // 
            // olvcHttpHost
            // 
            this.olvcHttpHost.AspectName = "HttpHost";
            this.olvcHttpHost.CellPadding = null;
            this.olvcHttpHost.Text = "Host";
            // 
            // olvcSize
            // 
            this.olvcSize.AspectName = "DataSize";
            this.olvcSize.CellPadding = null;
            this.olvcSize.Text = "Size";
            // 
            // olvcTimestampFirstPacket
            // 
            this.olvcTimestampFirstPacket.AspectName = "TimestampFirstPacket";
            this.olvcTimestampFirstPacket.CellPadding = null;
            this.olvcTimestampFirstPacket.Text = "First Packet";
            this.olvcTimestampFirstPacket.Width = 90;
            // 
            // olvcTimestampLastPacket
            // 
            this.olvcTimestampLastPacket.AspectName = "TimestampLastPacket";
            this.olvcTimestampLastPacket.CellPadding = null;
            this.olvcTimestampLastPacket.Text = "Last Packet";
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.tabPageAscii);
            this.tabControl.Controls.Add(this.tabPageHex);
            this.tabControl.Controls.Add(this.tabPageColourised);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(270, 290);
            this.tabControl.TabIndex = 1;
            // 
            // tabPageAscii
            // 
            this.tabPageAscii.Controls.Add(this.txtSession);
            this.tabPageAscii.Location = new System.Drawing.Point(4, 24);
            this.tabPageAscii.Name = "tabPageAscii";
            this.tabPageAscii.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageAscii.Size = new System.Drawing.Size(262, 262);
            this.tabPageAscii.TabIndex = 0;
            this.tabPageAscii.Text = "ASCII";
            this.tabPageAscii.UseVisualStyleBackColor = true;
            // 
            // txtSession
            // 
            this.txtSession.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSession.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtSession.Location = new System.Drawing.Point(3, 3);
            this.txtSession.Name = "txtSession";
            this.txtSession.Size = new System.Drawing.Size(256, 256);
            this.txtSession.TabIndex = 0;
            this.txtSession.Text = "";
            // 
            // tabPageHex
            // 
            this.tabPageHex.Controls.Add(this.hexBox);
            this.tabPageHex.Location = new System.Drawing.Point(4, 22);
            this.tabPageHex.Name = "tabPageHex";
            this.tabPageHex.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageHex.Size = new System.Drawing.Size(262, 264);
            this.tabPageHex.TabIndex = 1;
            this.tabPageHex.Text = "HEX";
            this.tabPageHex.UseVisualStyleBackColor = true;
            // 
            // hexBox
            // 
            this.hexBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.hexBox.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.hexBox.InfoForeColor = System.Drawing.Color.Gray;
            this.hexBox.LineInfoVisible = true;
            this.hexBox.Location = new System.Drawing.Point(3, 3);
            this.hexBox.Name = "hexBox";
            this.hexBox.ReadOnly = true;
            this.hexBox.ShadowSelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(60)))), ((int)(((byte)(188)))), ((int)(((byte)(255)))));
            this.hexBox.Size = new System.Drawing.Size(256, 258);
            this.hexBox.StringViewVisible = true;
            this.hexBox.TabIndex = 0;
            this.hexBox.UseFixedBytesPerLine = true;
            this.hexBox.VScrollBarVisible = true;
            // 
            // tabPageColourised
            // 
            this.tabPageColourised.Controls.Add(this.webControl);
            this.tabPageColourised.Location = new System.Drawing.Point(4, 24);
            this.tabPageColourised.Name = "tabPageColourised";
            this.tabPageColourised.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageColourised.Size = new System.Drawing.Size(262, 262);
            this.tabPageColourised.TabIndex = 2;
            this.tabPageColourised.Text = "Colourised";
            this.tabPageColourised.UseVisualStyleBackColor = true;
            // 
            // context
            // 
            this.context.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.contextCopy});
            this.context.Name = "context";
            this.context.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.context.Size = new System.Drawing.Size(103, 26);
            // 
            // contextCopy
            // 
            this.contextCopy.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.contextCopySourceIp,
            this.contextCopySourcePort,
            this.contextCopyDestinationIp,
            this.contextCopyDestinationPort,
            this.contextCopySize,
            this.contextCopyTimestampFirstPacket,
            this.contextCopyTimestampLastPacket});
            this.contextCopy.Name = "contextCopy";
            this.contextCopy.Size = new System.Drawing.Size(102, 22);
            this.contextCopy.Text = "Copy";
            // 
            // contextCopySourceIp
            // 
            this.contextCopySourceIp.Name = "contextCopySourceIp";
            this.contextCopySourceIp.Size = new System.Drawing.Size(197, 22);
            this.contextCopySourceIp.Text = "Source IP";
            this.contextCopySourceIp.Click += new System.EventHandler(this.contextCopySourceIp_Click);
            // 
            // contextCopySourcePort
            // 
            this.contextCopySourcePort.Name = "contextCopySourcePort";
            this.contextCopySourcePort.Size = new System.Drawing.Size(197, 22);
            this.contextCopySourcePort.Text = "Source Port";
            this.contextCopySourcePort.Click += new System.EventHandler(this.contextCopySourcePort_Click);
            // 
            // contextCopyDestinationIp
            // 
            this.contextCopyDestinationIp.Name = "contextCopyDestinationIp";
            this.contextCopyDestinationIp.Size = new System.Drawing.Size(197, 22);
            this.contextCopyDestinationIp.Text = "Destination IP";
            this.contextCopyDestinationIp.Click += new System.EventHandler(this.contextCopyDestinationIp_Click);
            // 
            // contextCopyDestinationPort
            // 
            this.contextCopyDestinationPort.Name = "contextCopyDestinationPort";
            this.contextCopyDestinationPort.Size = new System.Drawing.Size(197, 22);
            this.contextCopyDestinationPort.Text = "Destination Port";
            this.contextCopyDestinationPort.Click += new System.EventHandler(this.contextCopyDestinationPort_Click);
            // 
            // contextCopySize
            // 
            this.contextCopySize.Name = "contextCopySize";
            this.contextCopySize.Size = new System.Drawing.Size(197, 22);
            this.contextCopySize.Text = "Size";
            this.contextCopySize.Click += new System.EventHandler(this.contextCopySize_Click);
            // 
            // contextCopyTimestampFirstPacket
            // 
            this.contextCopyTimestampFirstPacket.Name = "contextCopyTimestampFirstPacket";
            this.contextCopyTimestampFirstPacket.Size = new System.Drawing.Size(197, 22);
            this.contextCopyTimestampFirstPacket.Text = "First Packet Timestamp";
            this.contextCopyTimestampFirstPacket.Click += new System.EventHandler(this.contextCopyTimestampFirstPacket_Click);
            // 
            // contextCopyTimestampLastPacket
            // 
            this.contextCopyTimestampLastPacket.Name = "contextCopyTimestampLastPacket";
            this.contextCopyTimestampLastPacket.Size = new System.Drawing.Size(197, 22);
            this.contextCopyTimestampLastPacket.Text = "Last Packet Timestamp";
            this.contextCopyTimestampLastPacket.Click += new System.EventHandler(this.contextCopyTimestampLastPacket_Click);
            // 
            // webControl
            // 
            this.webControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webControl.IsWebBrowserContextMenuEnabled = false;
            this.webControl.Location = new System.Drawing.Point(3, 3);
            this.webControl.MinimumSize = new System.Drawing.Size(20, 20);
            this.webControl.Name = "webControl";
            this.webControl.ScriptErrorsSuppressed = true;
            this.webControl.Size = new System.Drawing.Size(256, 256);
            this.webControl.TabIndex = 1;
            this.webControl.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.webControl_PreviewKeyDown);
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(545, 361);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.toolStrip);
            this.Controls.Add(this.menu);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MainMenuStrip = this.menu;
            this.Name = "FormMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SessionViewer";
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FormMain_KeyDown);
            this.menu.ResumeLayout(false);
            this.menu.PerformLayout();
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.listSession)).EndInit();
            this.tabControl.ResumeLayout(false);
            this.tabPageAscii.ResumeLayout(false);
            this.tabPageHex.ResumeLayout(false);
            this.tabPageColourised.ResumeLayout(false);
            this.context.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menu;
        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.RichTextBox txtSession;
        private BrightIdeasSoftware.FastObjectListView listSession;
        private System.Windows.Forms.ToolStripMenuItem menuFile;
        private System.Windows.Forms.ToolStripMenuItem menuFileImport;
        private System.Windows.Forms.ToolStripSeparator menuFileSep1;
        private System.Windows.Forms.ToolStripMenuItem menuFileExit;
        private System.Windows.Forms.ToolStripMenuItem menuHelp;
        private System.Windows.Forms.ToolStripMenuItem menuHelpHelp;
        private System.Windows.Forms.ToolStripSeparator menuHelpSep1;
        private System.Windows.Forms.ToolStripMenuItem menuHelpAbout;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabPageAscii;
        private System.Windows.Forms.TabPage tabPageHex;
        private Be.Windows.Forms.HexBox hexBox;
        private BrightIdeasSoftware.OLVColumn olvcSize;
        private System.Windows.Forms.TabPage tabPageColourised;
        private BrightIdeasSoftware.OLVColumn olvcTimestampFirstPacket;
        private BrightIdeasSoftware.OLVColumn olvcTimestampLastPacket;
        private BrightIdeasSoftware.OLVColumn olvcSourceIp;
        private BrightIdeasSoftware.OLVColumn olvcSourcePort;
        private BrightIdeasSoftware.OLVColumn olvcDestinationIp;
        private BrightIdeasSoftware.OLVColumn olvcDestinationPort;
        private System.Windows.Forms.ToolStripMenuItem menuFileOpen;
        private System.Windows.Forms.ContextMenuStrip context;
        private System.Windows.Forms.ToolStripMenuItem contextCopy;
        private System.Windows.Forms.ToolStripMenuItem contextCopySourceIp;
        private System.Windows.Forms.ToolStripMenuItem contextCopySourcePort;
        private System.Windows.Forms.ToolStripMenuItem contextCopyDestinationIp;
        private System.Windows.Forms.ToolStripMenuItem contextCopyDestinationPort;
        private System.Windows.Forms.ToolStripMenuItem contextCopySize;
        private System.Windows.Forms.ToolStripMenuItem contextCopyTimestampFirstPacket;
        private System.Windows.Forms.ToolStripMenuItem contextCopyTimestampLastPacket;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.ToolStripButton toolBtnImport;
        private System.Windows.Forms.ToolStripButton toolBtnOpen;
        private BrightIdeasSoftware.OLVColumn olvcHttpHost;
        private System.Windows.Forms.WebBrowser webControl;
    }
}

