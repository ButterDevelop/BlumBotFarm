using System.Diagnostics;

namespace BlumBotFarm.GUIAccountManager
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void buttonOpenTelegram_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(textBoxAccountNumber.Text, out int accountNumber))
            {
                MessageBox.Show("The account number is wrong!", "BlumBotFarm TG account manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string csvFilePath = "accounts.csv";

            if (!File.Exists(csvFilePath))
            {
                MessageBox.Show("Can't find accounts.csv!", "BlumBotFarm TG account manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var csvService = new CsvService();
            var account    = csvService.GetAccountByNumber(csvFilePath, accountNumber);
            if (account == null)
            {
                MessageBox.Show("No account found associated with your number!", "BlumBotFarm TG account manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string accountName = account.AccountName;

            accountName = accountName.Replace(".zip", "");
            if (!Directory.Exists(accountName))
            {
                MessageBox.Show("No directory found associated with your account name!", "BlumBotFarm TG account manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var tdataPath = Path.Combine(accountName, "tdata");
            if (!Directory.Exists(tdataPath))
            {
                MessageBox.Show("No tdata directory found in your account folder!", "BlumBotFarm TG account manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!File.Exists("settingss_Fiddler"))
            {
                MessageBox.Show("File \"settingss_Fiddler\" does not exist!", "BlumBotFarm TG account manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var files = Directory.GetFiles(Directory.GetCurrentDirectory());
            if (!files.Any(file => file.EndsWith("Telegram.exe")))
            {
                MessageBox.Show("Found no Telegram.exe file!", "BlumBotFarm TG account manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Clipboard.SetText(account.ProxyForFidler);
            MessageBox.Show($"Proxy FOR Fiddler ({account.ProxyForFidler}) for this account was copied to your clipboard!", "BlumBotFarm TG account manager", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Closing all Telegram processes
            Process[] workers = Process.GetProcessesByName("Telegram");
            foreach (Process worker in workers)
            {
                worker.Kill();
                worker.WaitForExit();
                worker.Dispose();
            }

            string destTDataPath = Path.Combine(Directory.GetCurrentDirectory(), "tdata");
            if (!Directory.Exists(destTDataPath))
            {
                Directory.CreateDirectory(destTDataPath);
            }
            else
            {
                Directory.Delete(destTDataPath, true);
            }
            if (File.Exists("log.txt")) File.Delete("log.txt");
            CopyFilesRecursively(tdataPath, destTDataPath);

            File.Copy("settingss_Fiddler", Path.Combine(tdataPath, "settingss"), true);

            richTextBoxTelegramAddCommand.Text = $"/addaccount USERNAME REFRESH_TOKEN {account.ProxyForProgram}";

            var result = MessageBox.Show("Everything is ready to start. You agree? If yes, it will be started in 3 seconds.", "BlumBotFarm TG account manager", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.No)
            {
                MessageBox.Show("You disagree. Exiting.", "BlumBotFarm TG account manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Thread.Sleep(3000);

            Process.Start("Telegram.exe");
        }

        private static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                string dirToCreate = dirPath.Replace(sourcePath, targetPath);
                if (!Directory.Exists(dirToCreate)) Directory.CreateDirectory(dirToCreate);
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }

        private void textBoxAccountNumber_TextChanged(object sender, EventArgs e)
        {
            richTextBoxTelegramAddCommand.Clear();
        }

        private void richTextBoxTelegramAddCommand_Click(object sender, EventArgs e)
        {
            if (richTextBoxTelegramAddCommand.TextLength > 0)
            {
                Clipboard.SetText(richTextBoxTelegramAddCommand.Text);
                MessageBox.Show("Telegram Add Command was copied to your clipboard.", "BlumBotFarm TG account manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
