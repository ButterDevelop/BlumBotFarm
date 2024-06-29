using System.Diagnostics;
using System.Text.RegularExpressions;

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

            var workingPath = $"{accountNumber}";
            if (!Directory.Exists(workingPath))
            {
                MessageBox.Show("No directory found associated with your account number!", "BlumBotFarm TG account manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var tdataPath = Path.Combine(workingPath, "tdata");
            if (!Directory.Exists(workingPath))
            {
                MessageBox.Show("No tdata directory found in account directory associated with your account number!", "BlumBotFarm TG account manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string telegramExeName = "";
            bool foundTelegramExe  = false;
            var regexTelegramExe   = new Regex("Telegram\\d{0,4}\\.exe");
            foreach (var dir in Directory.GetDirectories(Directory.GetCurrentDirectory()))
            {
                var files = Directory.GetFiles(dir);
                foreach (var file in files)
                {
                    if (regexTelegramExe.IsMatch(file))
                    {
                        telegramExeName  = file;
                        foundTelegramExe = true;
                        break;
                    }
                }
            }

            if (!foundTelegramExe)
            {
                MessageBox.Show("Found no Telegram.exe file!", "BlumBotFarm TG account manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //Clipboard.SetText(account.Proxy);
            //MessageBox.Show($"Proxy FOR Fiddler ({account.Proxy}) for this account was copied to your clipboard!", "BlumBotFarm TG account manager", MessageBoxButtons.OK, MessageBoxIcon.Information);

            List<string> postfixTelegramNames = [""];
            for (int i = 0; i <= 150; i++) postfixTelegramNames.Add(i.ToString());

            foreach (var postfix in postfixTelegramNames)
            {
                // Closing all original Telegram processes
                Process[] workers = Process.GetProcessesByName("Telegram" + postfix);
                foreach (Process worker in workers)
                {
                    worker.Kill();
                    worker.WaitForExit();
                    worker.Dispose();
                }
            }

            string logFilePath = Path.Combine(workingPath, "log.txt");
            if (File.Exists(logFilePath)) File.Delete(logFilePath);

            string username     = string.IsNullOrEmpty(account.TelegramName) ? "USERNAME"      : account.TelegramName;
            string refreshToken = string.IsNullOrEmpty(account.RefreshToken) ? "REFRESH_TOKEN" : account.RefreshToken;
            string proxy        = string.IsNullOrEmpty(account.Proxy)        ? "PROXY"         : account.Proxy;
            richTextBoxTelegramAddCommand.Text = $"/addaccount {username} {refreshToken} socks5://{proxy}";

            // Rename TelegramN.exe as necessary
            string newTelegramExeName = Path.Combine(workingPath, $"Telegram{accountNumber}.exe");
            File.Move(telegramExeName, newTelegramExeName);

            var result = MessageBox.Show("Everything is ready to start. You agree? If yes, it will be started in 3 seconds.", "BlumBotFarm TG account manager", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.No)
            {
                MessageBox.Show("You disagree. Exiting.", "BlumBotFarm TG account manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Thread.Sleep(3000);

            Process.Start(newTelegramExeName);
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
