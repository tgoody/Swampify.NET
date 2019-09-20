using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClientApp {
    public partial class Form1 : Form {

        public Form1() {
            InitializeComponent(); 
            label1.Hide();
        }

        private void button1_Click(object sender, EventArgs e) {
            button1.Hide();
            label1.Show();

            Refresh();
            
            Task.Run(() => Program.OpenAuthPage());
            
            IPHostEntry host = Dns.GetHostEntry("localhost");  
            IPAddress ipAddress = host.AddressList[0];

            foreach (IPAddress ip in host.AddressList)
            {
                AddressFamily af = ip.AddressFamily;
                if (af == AddressFamily.InterNetwork)
                {
                    ipAddress = ip;
                    break;
                }
            }            
            
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11001);
            
            Socket listener = new Socket(ipAddress.AddressFamily ,SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(localEndPoint);
            listener.Listen(2);

            Debug.WriteLine("waiting");
            var handler = listener.Accept();



            string data = "";
            int totalBytes = 0;

            while (true) {
                byte[] bytes = new byte[1024];
                int bytesRec = handler.Receive(bytes);
                totalBytes += bytesRec;
                data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                if (bytesRec < 1024 || totalBytes >= 10000) {
                    break;
                }
            }
            
            var msg = Encoding.ASCII.GetBytes("ok");
            handler.Send(msg);
            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
            Form2 form2 = new Form2(data);
            form2.StartPosition = FormStartPosition.Manual;
            form2.Location = Location;
            form2.Size = Size;
            form2.Show();
            Close();


        }

        private void label1_Click(object sender, EventArgs e) {
            
        }
    }
}