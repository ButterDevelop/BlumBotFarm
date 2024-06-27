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
            groupBoxIPOptions = new GroupBox();
            radioButtonThroughFiddler = new RadioButton();
            buttonOpenTelegram = new Button();
            richTextBoxTelegramAddCommand = new RichTextBox();
            labelTelegramAddCommand = new Label();
            groupBoxIPOptions.SuspendLayout();
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
            textBoxAccountNumber.TabIndex = 1;
            textBoxAccountNumber.TextAlign = HorizontalAlignment.Center;
            textBoxAccountNumber.TextChanged += textBoxAccountNumber_TextChanged;
            // 
            // groupBoxIPOptions
            // 
            groupBoxIPOptions.Controls.Add(radioButtonThroughFiddler);
            groupBoxIPOptions.ForeColor = Color.FromArgb(221, 221, 221);
            groupBoxIPOptions.Location = new Point(245, 12);
            groupBoxIPOptions.Name = "groupBoxIPOptions";
            groupBoxIPOptions.Size = new Size(193, 95);
            groupBoxIPOptions.TabIndex = 2;
            groupBoxIPOptions.TabStop = false;
            groupBoxIPOptions.Text = "IP options";
            groupBoxIPOptions.Visible = false;
            // 
            // radioButtonThroughFiddler
            // 
            radioButtonThroughFiddler.AutoSize = true;
            radioButtonThroughFiddler.Checked = true;
            radioButtonThroughFiddler.FlatStyle = FlatStyle.Flat;
            radioButtonThroughFiddler.Location = new Point(17, 42);
            radioButtonThroughFiddler.Name = "radioButtonThroughFiddler";
            radioButtonThroughFiddler.Size = new Size(161, 29);
            radioButtonThroughFiddler.TabIndex = 0;
            radioButtonThroughFiddler.TabStop = true;
            radioButtonThroughFiddler.Text = "Through Fiddler";
            radioButtonThroughFiddler.UseVisualStyleBackColor = true;
            // 
            // buttonOpenTelegram
            // 
            buttonOpenTelegram.FlatStyle = FlatStyle.Flat;
            buttonOpenTelegram.Location = new Point(487, 49);
            buttonOpenTelegram.Name = "buttonOpenTelegram";
            buttonOpenTelegram.Size = new Size(151, 34);
            buttonOpenTelegram.TabIndex = 3;
            buttonOpenTelegram.Text = "Open Telegram";
            buttonOpenTelegram.UseVisualStyleBackColor = true;
            buttonOpenTelegram.Click += buttonOpenTelegram_Click;
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
            richTextBoxTelegramAddCommand.TabIndex = 4;
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
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(63, 81, 90);
            ClientSize = new Size(681, 271);
            Controls.Add(labelTelegramAddCommand);
            Controls.Add(richTextBoxTelegramAddCommand);
            Controls.Add(buttonOpenTelegram);
            Controls.Add(groupBoxIPOptions);
            Controls.Add(textBoxAccountNumber);
            Controls.Add(labelAccountNumber);
            DoubleBuffered = true;
            ForeColor = Color.FromArgb(221, 221, 221);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "MainForm";
            Text = "BlumBotFarm by ButterDevelop - GUI TG account manager";
            groupBoxIPOptions.ResumeLayout(false);
            groupBoxIPOptions.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label labelAccountNumber;
        private TextBox textBoxAccountNumber;
        private GroupBox groupBoxIPOptions;
        private RadioButton radioButtonThroughFiddler;
        private Button buttonOpenTelegram;
        private RichTextBox richTextBoxTelegramAddCommand;
        private Label labelTelegramAddCommand;
    }
}
