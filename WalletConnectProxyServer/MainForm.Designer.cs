namespace WalletConnectProxyServer
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            buttonStartServer = new Button();
            numericUpDownAccountId = new NumericUpDown();
            buttonChangeId = new Button();
            labelAccountID = new Label();
            buttonStopServer = new Button();
            ((System.ComponentModel.ISupportInitialize)numericUpDownAccountId).BeginInit();
            SuspendLayout();
            // 
            // buttonStartServer
            // 
            buttonStartServer.Location = new Point(235, 22);
            buttonStartServer.Name = "buttonStartServer";
            buttonStartServer.Size = new Size(112, 34);
            buttonStartServer.TabIndex = 0;
            buttonStartServer.Text = "Start server";
            buttonStartServer.UseVisualStyleBackColor = true;
            buttonStartServer.Click += buttonStartServer_Click;
            // 
            // numericUpDownAccountId
            // 
            numericUpDownAccountId.Location = new Point(204, 149);
            numericUpDownAccountId.Name = "numericUpDownAccountId";
            numericUpDownAccountId.Size = new Size(180, 31);
            numericUpDownAccountId.TabIndex = 2;
            numericUpDownAccountId.TextAlign = HorizontalAlignment.Center;
            numericUpDownAccountId.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // buttonChangeId
            // 
            buttonChangeId.Location = new Point(235, 186);
            buttonChangeId.Name = "buttonChangeId";
            buttonChangeId.Size = new Size(112, 34);
            buttonChangeId.TabIndex = 3;
            buttonChangeId.Text = "Change ID";
            buttonChangeId.UseVisualStyleBackColor = true;
            buttonChangeId.Click += buttonChangeId_Click;
            // 
            // labelAccountID
            // 
            labelAccountID.AutoSize = true;
            labelAccountID.Location = new Point(239, 121);
            labelAccountID.Name = "labelAccountID";
            labelAccountID.Size = new Size(104, 25);
            labelAccountID.TabIndex = 3;
            labelAccountID.Text = "Account ID:";
            // 
            // buttonStopServer
            // 
            buttonStopServer.Enabled = false;
            buttonStopServer.Location = new Point(235, 62);
            buttonStopServer.Name = "buttonStopServer";
            buttonStopServer.Size = new Size(112, 34);
            buttonStopServer.TabIndex = 1;
            buttonStopServer.Text = "Stop server";
            buttonStopServer.UseVisualStyleBackColor = true;
            buttonStopServer.Click += buttonStopServer_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(578, 244);
            Controls.Add(buttonStopServer);
            Controls.Add(labelAccountID);
            Controls.Add(buttonChangeId);
            Controls.Add(numericUpDownAccountId);
            Controls.Add(buttonStartServer);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "MainForm";
            Text = "Blum Accounts Wallet Connector (Proxy Server)";
            Load += MainForm_Load;
            ((System.ComponentModel.ISupportInitialize)numericUpDownAccountId).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button buttonStartServer;
        private NumericUpDown numericUpDownAccountId;
        private Button buttonChangeId;
        private Label labelAccountID;
        private Button buttonStopServer;
    }
}
