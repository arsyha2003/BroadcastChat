using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;

namespace UDPChatAdmin
{
    public partial class Form1 : Form
    {
        private const int Port = 8888;
        private static UdpClient udpServer;
        private static bool isRunning = true;
        private string censureWord = string.Empty;
        private IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Broadcast, Port);
        private Action<string> log;
        public Form1()
        {
            InitializeComponent();
            log = (string msg) =>
            {
                textBox2.AppendText(msg + Environment.NewLine);
            };
            using (var db = new ChatDbContext())
            {
                db.Database.EnsureCreated();
            }
            udpServer = new UdpClient(Port);
            udpServer.EnableBroadcast = true;
            Thread listenThread = new Thread(ListenForMessages) { IsBackground = true };
            listenThread.Start();
            isRunning = false;
            udpServer.Close();
        }
        private void ListenForMessages()
        {
            while (isRunning)
            {
                byte[] data = udpServer.Receive(ref remoteEndPoint);
                string message = Encoding.UTF8.GetString(data);
                string response = HandleMessage(message);
                if (!string.IsNullOrEmpty(response))
                {
                    byte[] responseData = Encoding.UTF8.GetBytes(response);
                    udpServer.Send(responseData, responseData.Length, remoteEndPoint);
                }
            }
        }
        private string HandleMessage(string message)
        {
            if (message.Contains("/msg"))
            {
                if (message.Contains(censureWord))
                {
                    var responce = Encoding.UTF8.GetBytes("/msg" + message.Replace(censureWord, "####"));
                    udpServer.Send(responce, responce.Length, remoteEndPoint);
                    log.Invoke("Сообщение было подвержено цензуре");
                }
            }
            else
            {
                string[] parts = message.Split();
                string command = parts[0];
                switch (command.ToUpper())
                {
                    case "/reg":
                        return RegisterUser(parts);
                    case "/log":
                        return LoginUser(parts);
                    default:
                        return "/unc";
                }
            }
            return string.Empty;
        }
        private string RegisterUser(string[] parts)
        {
            string username = parts[1];
            string password = parts[2];
            using (var db = new ChatDbContext())
            {
                if (db.Users.Any(u => u.Username == username))
                {                    
                    log.Invoke("Пользователь не смог зарегистрироваться");
                    return "/errПользователь уже зарегистрирован!!!!";

                }
                db.Users.Add(new User { Username = username, Password = password });
                log.Invoke("Пользователь успещно зарегистрировался");
                db.SaveChanges();
            }
            return "/successВы успешно зарегались";
        }
        private string LoginUser(string[] parts)
        {
            string username = parts[1];
            string password = parts[2];
            using (var db = new ChatDbContext())
            {
                if (db.Users.Any(u => u.Username == username && u.Password == password))
                {
                    log.Invoke("Пользователь успешно авторизовался");
                    return "/successВы успешно авторизовались";
                }
                else
                {
                    log.Invoke("Данные введенные пользователем были неверны");
                    return "/errНеверные логин или пароль";
                }
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            censureWord = textBox1.Text;
            textBox1.Text = string.Empty;
        }

    }
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
    public class ChatDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=UserInfou;Trusted_Connection=True;");
        }
    }
    
}
