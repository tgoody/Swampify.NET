using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ClientApp {
    public static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        static string client_id = "b17ecc12d66441eb8749fcee579ea8a1"; // Your client id
        static string redirect_uri = "http://localhost:8888/callback/test"; // Your redirect uri
        public static string serverIP = "68.1.116.243";
        
        [STAThread]
        public static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Form1 form1 = new Form1();
            form1.Show();
            Application.Run();
        }





        public static void OpenAuthPage() {
            
            var scope = "user-read-private user-read-email playlist-read-private playlist-modify-private playlist-modify-public";


            NameValueCollection queryString = HttpUtility.ParseQueryString(string.Empty);

            queryString["response_type"] = "code";
            queryString["client_id"] = client_id;
            queryString["scope"] = scope;
            queryString["redirect_uri"] = redirect_uri;


            var temp = queryString.ToString(); // Returns "key1=value1&key2=value2", all URL-encoded


            Process.Start("https://accounts.spotify.com/authorize?" + temp);
            
            
            var httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://localhost:8888/");
            httpListener.Start();
            Console.WriteLine("Listening...");
            HttpListenerContext context = httpListener.GetContext();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            string responseString = "<HTML><BODY> Thank you, you may now close this and go back to the application!</BODY></HTML>";
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            Stream output = response.OutputStream;
            output.Write(buffer,0,buffer.Length);
            output.Close();
            httpListener.Stop();
            
            GetData(request.Url.AbsoluteUri);
            
            
            
        }


        public static void GetData(string url) {


            //Change form to say connecting to server...
            Uri myUri = new Uri(url);
            string code = HttpUtility.ParseQueryString(myUri.Query).Get("code");


            var sendMessage = "1" + code;
            //Debug.WriteLine(code);

            byte[] bytes = new byte[1024];

            IPHostEntry host = Dns.GetHostEntry(serverIP);
            IPAddress ipAddress = host.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

            // Create a TCP/IP  socket.    
            Socket sender = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Connect the socket to the remote endpoint. Catch any errors.    
            try {
                // Connect to Remote EndPoint  
                sender.Connect(remoteEP);

                Console.WriteLine("Socket connected to {0}", sender.RemoteEndPoint);


                byte[] msg = Encoding.ASCII.GetBytes(sendMessage);

                // Send the data through the socket.    
                int bytesSent = sender.Send(msg);



                
                // Receive the response from the remote device.    
                int bytesRec = sender.Receive(bytes);

                string numIndex = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                
                Debug.WriteLine("User added at index: " + numIndex);
                
                sender.Shutdown(SocketShutdown.Both);
                sender.Close();
                
                
                
                //Now connect to local form
                
                sender = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                sender.Connect("localhost", 11001);
                
                Console.WriteLine("Socket connected to {0}", sender.RemoteEndPoint);

                msg = Encoding.ASCII.GetBytes(numIndex);

                bytesSent = sender.Send(msg);
                
                bytesRec = sender.Receive(bytes);

                
                // Release the socket.    
                sender.Shutdown(SocketShutdown.Both);
                sender.Close();

            }
            catch (Exception e) {
                Console.WriteLine("Unexpected exception : {0}", e);
            }

        }


        
        public static void sendSwampify(string groupCode){}
        

    }
}