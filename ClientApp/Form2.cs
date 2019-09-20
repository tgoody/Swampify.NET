using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace ClientApp {
    public partial class Form2 : Form {
        
        private string userIndex;
        
        
        
        public Form2() {
            InitializeComponent();
            userIndex = "-";
            label2.Hide();
        }

        public Form2(string userIndex) {
            InitializeComponent();
            this.userIndex = userIndex;
            label2.Hide();
        }

        private void button1_Click(object sender, EventArgs e) { //create new room


            string sendMessage = "2" + userIndex;
            
            IPHostEntry host = Dns.GetHostEntry(Program.serverIP);
            IPAddress ipAddress = host.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

            // Create a TCP/IP  socket.    
            Socket senderSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Connect the socket to the remote endpoint. Catch any errors.    

            // Connect to Remote EndPoint  
            senderSocket.Connect(remoteEP);

            Console.WriteLine("Socket connected to {0}", senderSocket.RemoteEndPoint);


            byte[] msg = Encoding.ASCII.GetBytes(sendMessage);

            // Send the data through the socket.    
            int bytesSent = senderSocket.Send(msg);

            
            int totalBytes = 0;

            string data = "";
            
            while (true) {
                byte[] bytes = new byte[1024];
                int bytesRec = senderSocket.Receive(bytes);
                totalBytes += bytesRec;
                data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                if (bytesRec < 1024 || totalBytes >= 10000) {
                    break;
                }
            }

            Form3 form3 = new Form3(data, Int32.Parse(userIndex));
            form3.StartPosition = FormStartPosition.Manual;
            form3.Location = Location;
            //form3.Size = Size;
            form3.Show();
            Close();
            
        }

        private void button2_Click(object sender, EventArgs e) { //join room

            int userIndexNum;
            try {
                userIndexNum = Int32.Parse(userIndex);

                if (userIndexNum < 0) {
                    throw new Exception("negative user index");
                }
            }
            catch (Exception dumbExcp) {
                Debug.WriteLine(dumbExcp.ToString());
                throw new Exception("Your user index was bad, contact me");
            }

            int numDigits;
            if (userIndexNum < 10) {
                numDigits = 1;
            }
            else {
                numDigits = (int)Math.Floor(Math.Log10(userIndexNum) + 1);
            }

            string sendMessage = "3" + numDigits + userIndex + textBox1.Text;
            
            IPHostEntry host = Dns.GetHostEntry(Program.serverIP);
            IPAddress ipAddress = host.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

            // Create a TCP/IP  socket.    
            Socket senderSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Connect the socket to the remote endpoint. Catch any errors.    

            // Connect to Remote EndPoint  
            senderSocket.Connect(remoteEP);

            Console.WriteLine("Socket connected to {0}", senderSocket.RemoteEndPoint);


            byte[] msg = Encoding.ASCII.GetBytes(sendMessage);

            // Send the data through the socket.    
            int bytesSent = senderSocket.Send(msg);

            byte[] bytes = new byte[1024];
            int bytesRec = senderSocket.Receive(bytes);
            var data = Encoding.ASCII.GetString(bytes, 0, bytesRec);

            var values = data.Split(',');
            
            if (values[0] == "NOTFOUND") {
                label2.Show(); 
                Refresh();
            }

            else if (values[0] == "ADDED") {
                data = data.Remove(0,6);
                var userIndex = Int32.Parse(values[1]);
                data = data.Substring(data.IndexOf(',') + 1);
                data = textBox1.Text += "," + data;
                Form4 form4 = new Form4(data, userIndex);
                form4.StartPosition = FormStartPosition.Manual;
                form4.Location = Location;
                form4.Size = Size;
                form4.Show();
                Close();
            }

            else {
                throw new Exception("ERRERR likely received from server");
            }


        }
        
        
        
        
        
        
        
        
        
        
        
        
    }
}