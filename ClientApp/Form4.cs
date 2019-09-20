using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace ClientApp {
    public partial class Form4 : Form {
        
        private int totalNumSongs;
        private int currSongs;
        private string[] values;
        private int userIndex;
        private bool buttonClicked;
        
        
        
        public Form4() {
            InitializeComponent();
            label4.Hide();
            label5.Hide();
            buttonClicked = false;
            checkedListBox1.CheckOnClick = true;
            totalNumSongs = 0;
            currSongs = 0;
        }
        public Form4(string data, int userIndex) {
            InitializeComponent();
            label4.Hide();
            label5.Hide();
            this.userIndex = userIndex;
            buttonClicked = false;
            checkedListBox1.CheckOnClick = true;
            currSongs = 0;
            
            values = data.Split(',');
            label2.Text = values[0];

            for (int i = 1; i < values.Length-1; i+=2) {
                checkedListBox1.Items.Add(values[i]);
            }

            for (int i = 2; i < values.Length - 1; i += 2) {
                totalNumSongs += Int32.Parse(values[i]);
            }

            setSongText(0);
        }
        private void setSongText(int numSongs) {
            currSongs = numSongs;
            label15.Text = $"{currSongs}/{totalNumSongs}";
        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e) {

            if (buttonClicked) {
                return;
            }
            setSongText(0);
            
            foreach(int index in checkedListBox1.CheckedIndices) {

                currSongs += Int32.Parse(values[(index * 2) + 2]);
                
            }

            if (checkedListBox1.CheckedIndices.Count == 0) {
                currSongs = 0;
            }
            setSongText(currSongs);
            label15.Refresh();


        }

        private void button1_Click(object sender, EventArgs e) {

            if (buttonClicked) {
                return;
            }

            

            int songsShouldLoaded = currSongs;
            label4.Text = $"0/{songsShouldLoaded}";
            label4.Show();
            label5.Show();
            Refresh();
            
            buttonClicked = true;

            button1.Text = "Loading...";
            
            string sendMessage = "5" + userIndex;
            
            foreach(int index in checkedListBox1.CheckedIndices) {

                sendMessage += "," + index;

            }
            
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
            
            var worker = new BackgroundWorker();
            worker.DoWork += (obj, ea) => receiveMsg(obj, ea, senderSocket, songsShouldLoaded);
            worker.RunWorkerCompleted += receievingCompleted;
            worker.RunWorkerAsync();

           
        }
        
        
        
        void receiveMsg(object sender, DoWorkEventArgs e, Socket senderSocket, int songsShouldLoaded) {

            bool finishedLoading = false;
            int receivedSongs = 0;

            try {

                while (!finishedLoading) {
                    Refresh();
                    byte[] bytes = new byte[1024];
                    int bytesRec = senderSocket.Receive(bytes);
                    string data = Encoding.ASCII.GetString(bytes, 0, bytesRec);

                    var values = data.Split(',');
                    data = values[values.Length - 1];

                    //Console.WriteLine("What it should print: " + data);

                    if (data == "ALERT") {
                        finishedLoading = true;
                    }

                    else {
                        var songsAddedSoFar = Int64.Parse(data);
                        receivedSongs = (int) Math.Max(receivedSongs, songsAddedSoFar);


                        label4.Text = $"{receivedSongs}/{songsShouldLoaded}";
                        //Console.WriteLine("What it has: " + label4.Text);
                    }

                }
            }
            catch (Exception xc) {
                Console.WriteLine(xc.ToString());
            }

        }
        
        
        private void receievingCompleted(object sender, RunWorkerCompletedEventArgs e) {
            
            Form5 form5 = new Form5("Thank you, you can tell your party leader that you're ready!");
            form5.StartPosition = FormStartPosition.Manual;
            form5.Location = Location;
            form5.Size = Size;
            form5.Show();
            Close();
            
        }

       
    }
}