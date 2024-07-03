namespace BlumBotFarm.GUIAccountManager
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            labelAccountNumber = new Label();
            textBoxAccountNumber = new TextBox();
            buttonStart = new Button();
            richTextBoxTelegramAddCommand = new RichTextBox();
            labelTelegramAddCommand = new Label();
            labelTelegramProviderToken = new Label();
            richTextBoxTelegramProviderToken = new RichTextBox();
            checkBoxOpenTelegram = new CheckBox();
            SuspendLayout();
            // 
            // labelAccountNumber
            // 
            labelAccountNumber.AutoSize = true;
            labelAccountNumber.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 204);
            labelAccountNumber.ForeColor = Color.FromArgb(238, 238, 238);
            labelAccountNumber.Location = new Point(33, 35);
            labelAccountNumber.Name = "labelAccountNumber";
            labelAccountNumber.Size = new Size(165, 25);
            labelAccountNumber.TabIndex = 0;
            labelAccountNumber.Text = "Account number: ";
            // 
            // textBoxAccountNumber
            // 
            textBoxAccountNumber.BackColor = Color.FromArgb(39, 58, 67);
            textBoxAccountNumber.BorderStyle = BorderStyle.FixedSingle;
            textBoxAccountNumber.ForeColor = Color.White;
            textBoxAccountNumber.Location = new Point(68, 63);
            textBoxAccountNumber.Name = "textBoxAccountNumber";
            textBoxAccountNumber.Size = new Size(82, 31);
            textBoxAccountNumber.TabIndex = 0;
            textBoxAccountNumber.TextAlign = HorizontalAlignment.Center;
            textBoxAccountNumber.TextChanged += textBoxAccountNumber_TextChanged;
            // 
            // buttonStart
            // 
            buttonStart.FlatStyle = FlatStyle.Flat;
            buttonStart.Location = new Point(487, 49);
            buttonStart.Name = "buttonStart";
            buttonStart.Size = new Size(151, 34);
            buttonStart.TabIndex = 2;
            buttonStart.Text = "Start";
            buttonStart.UseVisualStyleBackColor = true;
            buttonStart.Click += buttonStart_Click;
            // 
            // richTextBoxTelegramAddCommand
            // 
            richTextBoxTelegramAddCommand.BackColor = Color.FromArgb(39, 58, 67);
            richTextBoxTelegramAddCommand.BorderStyle = BorderStyle.FixedSingle;
            richTextBoxTelegramAddCommand.Cursor = Cursors.Hand;
            richTextBoxTelegramAddCommand.DetectUrls = false;
            richTextBoxTelegramAddCommand.ForeColor = Color.White;
            richTextBoxTelegramAddCommand.Location = new Point(23, 148);
            richTextBoxTelegramAddCommand.Name = "richTextBoxTelegramAddCommand";
            richTextBoxTelegramAddCommand.ReadOnly = true;
            richTextBoxTelegramAddCommand.Size = new Size(626, 111);
            richTextBoxTelegramAddCommand.TabIndex = 3;
            richTextBoxTelegramAddCommand.Text = "";
            richTextBoxTelegramAddCommand.Click += richTextBoxTelegramAddCommand_Click;
            // 
            // labelTelegramAddCommand
            // 
            labelTelegramAddCommand.AutoSize = true;
            labelTelegramAddCommand.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 204);
            labelTelegramAddCommand.ForeColor = Color.FromArgb(238, 238, 238);
            labelTelegramAddCommand.Location = new Point(43, 120);
            labelTelegramAddCommand.Name = "labelTelegramAddCommand";
            labelTelegramAddCommand.Size = new Size(595, 25);
            labelTelegramAddCommand.TabIndex = 5;
            labelTelegramAddCommand.Text = "Telegram add command (clears after number change, click to copy): ";
            // 
            // labelTelegramProviderToken
            // 
            labelTelegramProviderToken.AutoSize = true;
            labelTelegramProviderToken.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 204);
            labelTelegramProviderToken.ForeColor = Color.FromArgb(238, 238, 238);
            labelTelegramProviderToken.Location = new Point(148, 284);
            labelTelegramProviderToken.Name = "labelTelegramProviderToken";
            labelTelegramProviderToken.Size = new Size(381, 50);
            labelTelegramProviderToken.TabIndex = 7;
            labelTelegramProviderToken.Text = "Telegram provider token update command\r\n(clears after number change, click to copy):";
            // 
            // richTextBoxTelegramProviderToken
            // 
            richTextBoxTelegramProviderToken.BackColor = Color.FromArgb(39, 58, 67);
            richTextBoxTelegramProviderToken.BorderStyle = BorderStyle.FixedSingle;
            richTextBoxTelegramProviderToken.Cursor = Cursors.Hand;
            richTextBoxTelegramProviderToken.DetectUrls = false;
            richTextBoxTelegramProviderToken.ForeColor = Color.White;
            richTextBoxTelegramProviderToken.Location = new Point(23, 337);
            richTextBoxTelegramProviderToken.Name = "richTextBoxTelegramProviderToken";
            richTextBoxTelegramProviderToken.ReadOnly = true;
            richTextBoxTelegramProviderToken.Size = new Size(626, 111);
            richTextBoxTelegramProviderToken.TabIndex = 4;
            richTextBoxTelegramProviderToken.Text = "";
            richTextBoxTelegramProviderToken.Click += richTextBoxTelegramProviderToken_Click;
            // 
            // checkBoxOpenTelegram
            // 
            checkBoxOpenTelegram.AutoSize = true;
            checkBoxOpenTelegram.Checked = true;
            checkBoxOpenTelegram.CheckState = CheckState.Checked;
            checkBoxOpenTelegram.FlatStyle = FlatStyle.Flat;
            checkBoxOpenTelegram.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 204);
            checkBoxOpenTelegram.Location = new Point(244, 54);
            checkBoxOpenTelegram.Name = "checkBoxOpenTelegram";
            checkBoxOpenTelegram.Size = new Size(170, 29);
            checkBoxOpenTelegram.TabIndex = 1;
            checkBoxOpenTelegram.Text = "Open Telegram?";
            checkBoxOpenTelegram.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(63, 81, 90);
            ClientSize = new Size(681, 466);
            Controls.Add(checkBoxOpenTelegram);
            Controls.Add(labelTelegramProviderToken);
            Controls.Add(richTextBoxTelegramProviderToken);
            Controls.Add(labelTelegramAddCommand);
            Controls.Add(richTextBoxTelegramAddCommand);
            Controls.Add(buttonStart);
            Controls.Add(textBoxAccountNumber);
            Controls.Add(labelAccountNumber);
            DoubleBuffered = true;
            ForeColor = Color.FromArgb(221, 221, 221);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "MainForm";
            Text = "BlumBotFarm by ButterDevelop - GUI TG account manager";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label labelAccountNumber;
        private TextBox textBoxAccountNumber;
        private GroupBox groupBoxIPOptions;
        private RadioButton radioButtonThroughFiddler;
        private Button buttonStart;
        private RichTextBox richTextBoxTelegramAddCommand;
        private Label labelTelegramAddCommand;
        private Label labelTelegramProviderToken;
        private RichTextBox richTextBoxTelegramProviderToken;
        private CheckBox checkBoxOpenTelegram;
    }
}
