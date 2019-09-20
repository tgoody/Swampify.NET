using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using System.Windows.Forms;

namespace ClientApp {
    public partial class Form6 : Form {
        private String groupCode;
        private int[] attributes;
        private string userIndex;

        private System.Timers.Timer socketTimer;
        
        public Form6() {
            InitializeComponent();
            button1.Hide();
        }
        
        public Form6(String groupCode, int[] _attributes, string userIndex, string[] readyUsers) {
            InitializeComponent();
            button1.Hide();
            this.groupCode = groupCode;
            attributes = _attributes;
            this.userIndex = userIndex;
            
            listBox1.Items.Clear();

            for (int i = 0; i < readyUsers.Length-1; i+=2) {
                string readyString = "";
                bool readyVal = readyUsers[i + 1] == "1" || readyUsers[i + 1] == "True";
                if (readyVal) {
                    readyString = "Ready!";
                }
                else {
                    readyString = "Not ready.";
                }
                listBox1.Items.Add($"{readyUsers[i]}: {readyString}");
            }
            
            Refresh();

            socketTimer = new System.Timers.Timer(2000);
            socketTimer.Elapsed += new ElapsedEventHandler((sender, e) => getReadyUsers(sender, e, groupCode));
            socketTimer.Enabled = true;  
            
//            var worker = new BackgroundWorker();
//            worker.DoWork += (obj, ea) => receiveMsg(obj, ea, senderSocket);
//            worker.RunWorkerCompleted += receievingCompleted;
//            worker.RunWorkerAsync();

        }


        private void button1_Click(object sender, EventArgs e) {
            socketTimer.Enabled = false;
            IPHostEntry host = Dns.GetHostEntry(Program.serverIP);
            IPAddress ipAddress = host.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);
            
            Socket senderSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            senderSocket.Connect(remoteEP);
            Console.WriteLine("Socket connected to {0}", senderSocket.RemoteEndPoint);

            string sendMessage = "4" + $"{groupCode},{userIndex}";
            foreach (var temp in attributes) {
                sendMessage += $",{temp}";
            }
            
            byte[] msg = Encoding.ASCII.GetBytes(sendMessage);

            // Send the data through the socket.    
            int bytesSent = senderSocket.Send(msg);
            
            byte[] bytes = new byte[1024];
            int bytesRec = senderSocket.Receive(bytes);
            string data = Encoding.ASCII.GetString(bytes, 0, bytesRec);
            Form5 form5 = new Form5("Okay, check your playists!");
            form5.StartPosition = FormStartPosition.Manual;
            form5.Location = Location;
            form5.Size = Size;
            form5.Show();
            Close();

        }


        void getReadyUsers(object sender, ElapsedEventArgs e, String groupCode) {
            
            Invoke(new Action(()=>Refresh()));
            
            IPHostEntry host = Dns.GetHostEntry(Program.serverIP);
            IPAddress ipAddress = host.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);
            
            Socket senderSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            senderSocket.Connect(remoteEP);

            string sendMessage = "7" + $"{groupCode}";
            byte[] msg = Encoding.ASCII.GetBytes(sendMessage);
            // Send the data through the socket.    
            int bytesSent = senderSocket.Send(msg);
            
            byte[] bytes = new byte[1024];
            int bytesRec = senderSocket.Receive(bytes);
            string data = Encoding.ASCII.GetString(bytes, 0, bytesRec);

            Console.WriteLine($"Data before split: {data}");
            
            var manyValues = data.Split('|');

            data = manyValues[manyValues.Length - 1];
            if (data == "" && manyValues.Length > 1)
                data = manyValues[manyValues.Length - 2];
            
            var values = data.Split(',');

            listBox1.Items.Clear();

            bool allUsersReady = true;
            
            for (int i = 0; i < values.Length - 1; i += 2) {
                string readyString = "";
                bool readyVal = values[i + 1] == "1" || values[i + 1] == "True";
                if (readyVal) {
                    readyString = "Ready!";
                }
                else {
                    readyString = "Not ready.";
                    allUsersReady = false;
                }
                listBox1.Items.Add($"{values[i]}: {readyString}");
            }
            senderSocket.Shutdown(SocketShutdown.Both);
            senderSocket.Close();


            if (allUsersReady) {
                Invoke(new Action(() => {
                        button1.Show();
                        Refresh();
                }));
            }
            
            
        }
    }
}