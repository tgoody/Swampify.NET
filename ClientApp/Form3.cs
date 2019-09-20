using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace ClientApp {
    public partial class Form3 : Form { //This form is for the leader, who created new rooms
        private int totalNumSongs;
        private int currSongs;
        private string[] values;
        private int userindex;
        private bool buttonClicked;
        public static string[] readyUsers;

        public Form3() {
            InitializeComponent();
            checkedListBox1.CheckOnClick = true;
            totalNumSongs = 0;
            currSongs = 0;
            setSongText(0);
            label16.Hide();
            label17.Hide();
            buttonClicked = false;

        }

        public Form3(string data, int userindex) {
            InitializeComponent();
            checkedListBox1.CheckOnClick = true;
            currSongs = 0;
            this.userindex = userindex;
            buttonClicked = false;

            
            values = data.Split(',');
            label2.Text = values[0];

            for (int i = 1; i < values.Length-1; i+=2) {
                checkedListBox1.Items.Add(values[i]);
            }

            for (int i = 2; i < values.Length - 1; i += 2) {
                totalNumSongs += Int32.Parse(values[i]);
            }

            setSongText(0);
            
            label16.Hide();
            label17.Hide();
        }

        private void setSongText(int numSongs) {
            currSongs = numSongs;
            label15.Text = $"{currSongs}/{totalNumSongs}";
        }
        
        private void trackBar1_Scroll(object sender, EventArgs e) {
            label8.Text = trackBar1.Value.ToString();
        }

        private void trackBar2_Scroll(object sender, EventArgs e) {
            label9.Text = trackBar2.Value.ToString();
        }
        
        private void trackBar3_Scroll(object sender, EventArgs e) {
            label10.Text = trackBar3.Value.ToString();
        }


        private void trackBar4_Scroll(object sender, EventArgs e) {
            label11.Text = trackBar4.Value.ToString();
        }

        private void trackBar5_Scroll(object sender, EventArgs e) {
            label12.Text = trackBar5.Value.ToString();
        }

        private void Form3_FormClosing(object sender, FormClosingEventArgs e) {
            Environment.Exit(0);
        }

        private void button1_Click(object sender, EventArgs e) {

            if (buttonClicked) {
                return;
            }

            buttonClicked = true;
            button1.Text = "Loading...";
            
            string sendMessage = "5" + userindex;
            
            foreach(int index in checkedListBox1.CheckedIndices) {

                sendMessage += "," + index;

            }
            
            
            int songsShouldLoaded = currSongs;
            label16.Text = $"0/{songsShouldLoaded}";
            label16.Show();
            label17.Show();
            
            
            
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

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e) {
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
        
        
        
        void receiveMsg(object sender, DoWorkEventArgs e, Socket senderSocket, int songsShouldLoaded) {

            try {
                bool finishedLoading = false;
                int receivedSongs = 0;

                while (!finishedLoading) {
                    if (InvokeRequired)
                        Invoke(new Action(()=>Refresh()));
                    byte[] bytes = new byte[1024];
                    int bytesRec = senderSocket.Receive(bytes);
                    string data = Encoding.ASCII.GetString(bytes, 0, bytesRec);

                    var values = data.Split(',');
                    data = values[values.Length - 1];

                    Console.WriteLine("What was received: " + data);

                    if (data == "ALERT") {
                        finishedLoading = true;
                    }

                    else {
                        var songsAddedSoFar = Int64.Parse(data);
                        receivedSongs = (int) Math.Max(receivedSongs, songsAddedSoFar);
                        
                        label16.Invoke((MethodInvoker)delegate {
                            label16.Text = $"{receivedSongs}/{songsShouldLoaded}";});
                    }

                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }

        }
        
        
        private void receievingCompleted(object sender, RunWorkerCompletedEventArgs e) {
            
            int[] arr = new int[5];
            arr[0] = trackBar1.Value;
            arr[1] = trackBar2.Value;
            arr[2] = trackBar3.Value;
            arr[3] = trackBar5.Value;
            arr[4] = trackBar4.Value;
            
            Console.WriteLine("Worker finished");
            
            IPHostEntry host = Dns.GetHostEntry(Program.serverIP);
            IPAddress ipAddress = host.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

            // Create a TCP/IP  socket.    
            Socket senderSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            senderSocket.Connect(remoteEP);

            byte[] msg = Encoding.ASCII.GetBytes($"7{label2.Text}");
            int bytesSent = senderSocket.Send(msg);
                
            var totalBytes = 0;
            string loadedUsers = "";
            while (true) {
                byte[] bytes = new byte[1024];
                int bytesRec = senderSocket.Receive(bytes);
                totalBytes += bytesRec;
                loadedUsers += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                if (bytesRec < 1024 || totalBytes >= 10000) {
                    break;
                }
            }

            readyUsers = loadedUsers.Split(',');
            
            Invoke(new Action(() => {
                    Form6 form6 = new Form6(label2.Text, arr, userindex.ToString(), readyUsers);
                    form6.StartPosition = FormStartPosition.Manual;
                    form6.Location = Location;
                    form6.Show();
                    Hide();
            }));


//            if (InvokeRequired)
//                Invoke(new Action(()=>Close()));
            
        }

        
        
        
    }
}