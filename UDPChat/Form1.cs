using System.Net.Sockets;
using System.Net;
using System.Text;

namespace UDPChat
{
    public partial class Form1 : Form
    {
        private const int BroadcastPort = 8888;
        private string username;
        private UdpClient udpClient;
        private Task listenThread;
        private bool isListening = true;
        public Form1()
        {
            InitializeComponent();
            StartUdpClient();
        }
        private void StartUdpClient()
        {
            udpClient = new UdpClient(BroadcastPort);
            udpClient.EnableBroadcast = true;
            listenThread = Task.Run(ListenForMessages);
        }
        private void ListenForMessages()
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, BroadcastPort);
            while (isListening)
            {
                try
                {
                    byte[] data = udpClient.Receive(ref remoteEndPoint);
                    string message = Encoding.UTF8.GetString(data);
                    if (message.Contains("/err"))
                    {
                        AppendMessage(message.Replace("/err",string.Empty));
                    }
                    else if (message.Contains("/success"))
                    {
                        AppendMessage(message.Replace("/success", string.Empty));
                    }
                    else if (message.Contains("/msg"))
                    {
                        AppendMessage(message.Replace("/msg", string.Empty));
                    }
                    else if (message==("/unc"))
                    {
                        AppendMessage("Вы не ввели ничего, ни пароля, ни логина");
                    }
                }
                catch (Exception ex)
                {
                    if (isListening)
                    {
                        MessageBox.Show("Ошибка при получении сообщения: " + ex.Message);
                    }
                }
            }
        }
        private void AppendMessage(string message)
        {
            textBox1.AppendText(message + Environment.NewLine);
        }
        private void RegisterButtonEvent(object sender, EventArgs e)
        {
            string login = textBox2.Text.Trim();
            string password = textBox3.Text.Trim();
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль.");
                return;
            }
            BroadcastMessage($"/reg{login} {password}");
            MessageBox.Show("Регистрация успешна.");
        }
        private void LoginButtonEvent(object sender, EventArgs e)
        {
            string login = textBox2.Text.Trim();
            string password = textBox2.Text.Trim();
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль.");
                return;
            }
            BroadcastMessage($"/log{login} {password}");
            username = login;
            textBox2.Visible = false;
            textBox3.Visible = true;
        }

        private void SendMessageButtonEvent(object sender, EventArgs e)
        {
            string message = textBox4.Text.Trim();

            if (string.IsNullOrEmpty(message))
            {
                MessageBox.Show("Введите сообщение.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            BroadcastMessage($"/msg{username} {message}");
            textBox4.Clear();
        }

        private void BroadcastMessage(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, BroadcastPort);
            udpClient.Send(data, data.Length, endPoint);
        }

        private void Form1_formClose(object sender, FormClosingEventArgs e)
        {
            isListening = false;
            udpClient.Close();
        }
    }
}
