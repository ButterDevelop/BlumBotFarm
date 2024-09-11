namespace WalletConnectProxyServer
{
    public partial class MainForm : Form
    {
        private const int PROXY_SERVER_PORT = 7777;

        private readonly SimpleProxy _simpleProxy;

        public MainForm()
        {
            InitializeComponent();

            _simpleProxy = new(PROXY_SERVER_PORT);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            
        }

        private void buttonStartServer_Click(object sender, EventArgs e)
        {
            new TaskFactory().StartNew(_simpleProxy.Start);

            buttonStartServer.Enabled = false;
            buttonStopServer.Enabled  = true;
        }

        private void buttonStopServer_Click(object sender, EventArgs e)
        {
            _simpleProxy.Stop();

            buttonStartServer.Enabled = true;
            buttonStopServer.Enabled  = false;
        }

        private void buttonChangeId_Click(object sender, EventArgs e)
        {
            _simpleProxy.ChangeAccountId((int)numericUpDownAccountId.Value);
        }
    }
}
