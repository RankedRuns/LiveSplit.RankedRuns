namespace LiveSplit.UI.Components
{
    partial class RunUploaderSettings
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.AuthButton = new System.Windows.Forms.Button();
            this.chkRichPresenceEnabled = new System.Windows.Forms.CheckBox();
            this.chkShowLoggedOutNotice = new System.Windows.Forms.CheckBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.lnkAuthStatus = new System.Windows.Forms.LinkLabel();
            this.RankedRunsLinkLabel = new System.Windows.Forms.LinkLabel();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // AuthButton
            // 
            this.AuthButton.Location = new System.Drawing.Point(3, 3);
            this.AuthButton.Name = "AuthButton";
            this.AuthButton.Size = new System.Drawing.Size(90, 29);
            this.AuthButton.TabIndex = 0;
            this.AuthButton.Text = "Sign in";
            this.AuthButton.UseVisualStyleBackColor = true;
            this.AuthButton.Click += new System.EventHandler(this.AuthButtonClick);
            // 
            // chkRichPresenceEnabled
            // 
            this.chkRichPresenceEnabled.AutoSize = true;
            this.chkRichPresenceEnabled.Checked = true;
            this.chkRichPresenceEnabled.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkRichPresenceEnabled.Location = new System.Drawing.Point(189, 4);
            this.chkRichPresenceEnabled.Margin = new System.Windows.Forms.Padding(4);
            this.chkRichPresenceEnabled.Name = "chkRichPresenceEnabled";
            this.chkRichPresenceEnabled.Size = new System.Drawing.Size(163, 20);
            this.chkRichPresenceEnabled.TabIndex = 1;
            this.chkRichPresenceEnabled.Text = "Enable Rich Presence";
            this.chkRichPresenceEnabled.UseVisualStyleBackColor = true;
            this.chkRichPresenceEnabled.CheckedChanged += new System.EventHandler(this.ChkRichPresenceEnabled_CheckedChanged);
            // 
            // chkShowLoggedOutNotice
            // 
            this.chkShowLoggedOutNotice.AutoSize = true;
            this.chkShowLoggedOutNotice.Location = new System.Drawing.Point(189, 42);
            this.chkShowLoggedOutNotice.Margin = new System.Windows.Forms.Padding(4);
            this.chkShowLoggedOutNotice.Name = "chkShowLoggedOutNotice";
            this.chkShowLoggedOutNotice.Size = new System.Drawing.Size(328, 20);
            this.chkShowLoggedOutNotice.TabIndex = 2;
            this.chkShowLoggedOutNotice.Text = "Show notification when sign-in is missing or expires";
            this.chkShowLoggedOutNotice.UseVisualStyleBackColor = true;
            this.chkShowLoggedOutNotice.CheckedChanged += new System.EventHandler(this.ChkShowLoggedOutNotice_CheckedChanged);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 185F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.AuthButton, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.chkRichPresenceEnabled, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.chkShowLoggedOutNotice, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.RankedRunsLinkLabel, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.lnkAuthStatus, 0, 1);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(4);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 61F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(635, 138);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // lnkAuthStatus
            // 
            this.lnkAuthStatus.AutoSize = true;
            this.lnkAuthStatus.Location = new System.Drawing.Point(3, 38);
            this.lnkAuthStatus.MaximumSize = new System.Drawing.Size(620, 0);
            this.lnkAuthStatus.Name = "lnkAuthStatus";
            this.lnkAuthStatus.Size = new System.Drawing.Size(67, 16);
            this.lnkAuthStatus.TabIndex = 3;
            this.lnkAuthStatus.TabStop = true;
            this.lnkAuthStatus.Text = "Not linked";
            this.lnkAuthStatus.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LnkAuthStatus_LinkClicked);
            // 
            // RankedRunsLinkLabel
            // 
            this.RankedRunsLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.RankedRunsLinkLabel.AutoSize = true;
            this.RankedRunsLinkLabel.Location = new System.Drawing.Point(442, 122);
            this.RankedRunsLinkLabel.Name = "RankedRunsLinkLabel";
            this.RankedRunsLinkLabel.Size = new System.Drawing.Size(190, 16);
            this.RankedRunsLinkLabel.TabIndex = 4;
            this.RankedRunsLinkLabel.TabStop = true;
            this.RankedRunsLinkLabel.Text = "Powered by RankedRuns.com";
            this.RankedRunsLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.RankedRunsLinkLabel_LinkClicked);
            // 
            // RunUploaderSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "RunUploaderSettings";
            this.Padding = new System.Windows.Forms.Padding(9);
            this.Size = new System.Drawing.Size(635, 174);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.Button AuthButton;
        private System.Windows.Forms.CheckBox chkRichPresenceEnabled;
        private System.Windows.Forms.CheckBox chkShowLoggedOutNotice;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.LinkLabel lnkAuthStatus;
        private System.Windows.Forms.LinkLabel RankedRunsLinkLabel;
    }
}
