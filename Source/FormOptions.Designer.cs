﻿namespace SessionViewer
{
    partial class FormOptions
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormOptions));
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.cboBufferInterval = new System.Windows.Forms.ComboBox();
            this.cboSessionInterval = new System.Windows.Forms.ComboBox();
            this.lblBufferInterval = new System.Windows.Forms.Label();
            this.lblSessionInterval = new System.Windows.Forms.Label();
            this.lblThreads = new System.Windows.Forms.Label();
            this.cboThreads = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(148, 98);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 9;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.Location = new System.Drawing.Point(68, 98);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 8;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // cboBufferInterval
            // 
            this.cboBufferInterval.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboBufferInterval.FormattingEnabled = true;
            this.cboBufferInterval.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5",
            "10",
            "15",
            "20"});
            this.cboBufferInterval.Location = new System.Drawing.Point(121, 37);
            this.cboBufferInterval.Name = "cboBufferInterval";
            this.cboBufferInterval.Size = new System.Drawing.Size(100, 23);
            this.cboBufferInterval.TabIndex = 11;
            // 
            // cboSessionInterval
            // 
            this.cboSessionInterval.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSessionInterval.FormattingEnabled = true;
            this.cboSessionInterval.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5",
            "10",
            "15",
            "20"});
            this.cboSessionInterval.Location = new System.Drawing.Point(121, 66);
            this.cboSessionInterval.Name = "cboSessionInterval";
            this.cboSessionInterval.Size = new System.Drawing.Size(100, 23);
            this.cboSessionInterval.TabIndex = 12;
            // 
            // lblBufferInterval
            // 
            this.lblBufferInterval.AutoSize = true;
            this.lblBufferInterval.Location = new System.Drawing.Point(14, 41);
            this.lblBufferInterval.Name = "lblBufferInterval";
            this.lblBufferInterval.Size = new System.Drawing.Size(103, 15);
            this.lblBufferInterval.TabIndex = 13;
            this.lblBufferInterval.Text = "Buffer Interval (m)";
            // 
            // lblSessionInterval
            // 
            this.lblSessionInterval.AutoSize = true;
            this.lblSessionInterval.Location = new System.Drawing.Point(7, 69);
            this.lblSessionInterval.Name = "lblSessionInterval";
            this.lblSessionInterval.Size = new System.Drawing.Size(110, 15);
            this.lblSessionInterval.TabIndex = 14;
            this.lblSessionInterval.Text = "Session Interval (m)";
            // 
            // lblThreads
            // 
            this.lblThreads.AutoSize = true;
            this.lblThreads.Location = new System.Drawing.Point(68, 12);
            this.lblThreads.Name = "lblThreads";
            this.lblThreads.Size = new System.Drawing.Size(49, 15);
            this.lblThreads.TabIndex = 16;
            this.lblThreads.Text = "Threads";
            // 
            // cboThreads
            // 
            this.cboThreads.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboThreads.FormattingEnabled = true;
            this.cboThreads.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "6",
            "7",
            "8"});
            this.cboThreads.Location = new System.Drawing.Point(122, 8);
            this.cboThreads.Name = "cboThreads";
            this.cboThreads.Size = new System.Drawing.Size(100, 23);
            this.cboThreads.TabIndex = 15;
            // 
            // FormOptions
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(233, 130);
            this.Controls.Add(this.lblThreads);
            this.Controls.Add(this.cboThreads);
            this.Controls.Add(this.lblSessionInterval);
            this.Controls.Add(this.lblBufferInterval);
            this.Controls.Add(this.cboSessionInterval);
            this.Controls.Add(this.cboBufferInterval);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormOptions";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Options";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.ComboBox cboBufferInterval;
        private System.Windows.Forms.ComboBox cboSessionInterval;
        private System.Windows.Forms.Label lblBufferInterval;
        private System.Windows.Forms.Label lblSessionInterval;
        private System.Windows.Forms.Label lblThreads;
        private System.Windows.Forms.ComboBox cboThreads;
    }
}