using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Web.Script.Serialization;
using Newtonsoft.Json;

namespace ServerApp {
    internal class Program {
        
        static string client_id = "b17ecc12d66441eb8749fcee579ea8a1"; // Your client id
        static string client_secret = "f2b6f115f4d54823a6782a13dd2a07ab"; // Your secret
        static string redirect_uri = "http://localhost:8888/callback/test"; // Your redirect uri
        private static List<User> users;
        private static List<UserGroup> groups;
        private static List<Track> failedSongList;
        private static int failedSongs;
        private static int succeededSongs;

        private static System.Timers.Timer socketTimer;

        //1 means generate token (code comes after)
        //2 means create room (index comes after) [warning: index comes as string]
        //3 means join room (index and group code comes after) [3,numDigits,index,groupcode]
        //4 means swampify
        //5 is ready joined user
        
        
        
        
        
        
        public static void Main(string[] args) {
            failedSongList = new List<Track>();
            failedSongs = 0;
            succeededSongs = 0;
            users = new List<User>();
            groups = new List<UserGroup>();
            var temp = new UserGroup();
            temp.groupCode = "AAAAAA";
            groups.Add(temp);
            StartServer().Wait();

            Console.WriteLine("Sever stopped");
            
        }

        public static async Task StartServer()  
        {  
            // Get Host IP Address that is used to establish a connection  
            // In this case, we get one IP address of localhost that is IP : 127.0.0.1  
            // If a host has multiple addresses, you will get a list of addresses  
            IPAddress[] host = Dns.GetHostAddresses("localhost");  
            IPAddress ipAddress = host[0];  
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);


            try {

                // Create a Socket that will use Tcp protocol      
                Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                // A Socket must be associated with an endpoint using the Bind method  
                listener.Bind(localEndPoint);
                // Specify how many requests a Socket can listen before it gives Server busy response.  
                listener.Listen(30);

                while (true) {
                    
                    Console.WriteLine("Waiting for a connection..."); 
                    var handler = listener.Accept();
                    // Incoming data from the client.    
                    string data = null;
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



                    var response = "";
                    
                    if (data[0] == '1') {
                        data = data.Remove(0,1);
                        response = await generateToken(data, handler.RemoteEndPoint as IPEndPoint);
                    }

                    if (data[0] == '2') {
                        data = data.Remove(0,1);
                        response = createRoom(data);
                    }
                    
                    if (data[0] == '3') {
                        data = data.Remove(0,1);

                        var numDigits = Int32.Parse(data.Substring(0,1));
                        data = data.Remove(0,1);

                        var userIndex = data.Substring(0, numDigits);
                        data = data.Remove(0, numDigits);
                        
                        response = joinRoom(userIndex, data);
                    }

                    if (data[0] == '4') {

                        data = data.Remove(0, 1);

                        String[] attributeValues = new String[5];

                        var values = data.Split(',');
                        string groupCode = values[0];
                        var thisUser = users[Int32.Parse(values[1])];
                        thisUser.privateSocket = handler;

                        Array.Copy(values,2,attributeValues,0,5);

                        Swampify(groupCode, attributeValues, thisUser);

                        continue;

                    }

                    if (data[0] == '5') {
                        data = data.Remove(0, 1);
                        
                        var values = data.Split(',');
                        var thisUser = users[Int32.Parse(values[0])];

                        List<int> playlistIndices = new List<int>();
                        for (int i = 1; i < values.Length; i++) {
                            playlistIndices.Add(Int32.Parse(values[i]));
                        }

                        thisUser.privateSocket = handler;
                        
                        getUserData(thisUser, playlistIndices);
                        continue;

                    }

                    if (data[0] == '7') {
                        data = data.Remove(0, 1);
                        var groupCode = data;
                        string usersReady = "";
                        UserGroup foundGroup = null;
                        foreach (UserGroup group in groups) {
                            if (groupCode == group.groupCode) {
                                foundGroup = group;
                                break;
                            }
                        }
            
                        foreach (var user in foundGroup.usersInGroup) {
                            usersReady += $"{user.userID},{user.isReady},";
                        }
                        
                        response = usersReady;
                        if (response[response.Length - 1] == ',') {
                            response = response.Remove(response.Length - 1, 1);
                        }

                    }
                    

                    var msg = Encoding.ASCII.GetBytes(response);
                    handler.Send(msg);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        public static async Task<string> generateToken(string code, IPEndPoint userIP) {


            var values = new Dictionary<string, string> {
                {"grant_type", "authorization_code"},
                {"code", code},
                {"redirect_uri", redirect_uri}
            };



            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(client_id + ':' + client_secret);
            var base64String = System.Convert.ToBase64String(plainTextBytes);


            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Add("Authorization", "Basic " + base64String);

            var content = new FormUrlEncodedContent(values);

            var response = await client.PostAsync("https://accounts.spotify.com/api/token", content);

            var responseString = await response.Content.ReadAsStringAsync();

            //Console.WriteLine(responseString);

            var jss = new JavaScriptSerializer();
            var table = jss.Deserialize<dynamic>(responseString);

            string accessToken = table["access_token"];
            int expiresIn = table["expires_in"];
            string refreshToken = table["refresh_token"];
            
            User newUser = new User();
            newUser.accessToken = accessToken;
            newUser.expiresIn = expiresIn;
            newUser.refreshToken = refreshToken;
            
            
            client = new HttpClient();

            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + newUser.accessToken);
            response = await client.GetAsync("https://api.spotify.com/v1/me");
            responseString = await response.Content.ReadAsStringAsync();
            jss = new JavaScriptSerializer();
            table = jss.Deserialize<dynamic>(responseString);

            newUser.userID = table["id"];

            int numPlaylists = await generatePlaylists(newUser);
            Debug.WriteLine($"Just added {numPlaylists} playlists for user: {table["id"]}");
            
            
            users.Add(newUser);

            return (users.Count-1).ToString();

        }
        public static string createRoom(string _userIndex) {

            int userIndex;
            try {
                userIndex = Int32.Parse(_userIndex);
            }
            catch (Exception e) {
                Debug.WriteLine(e.ToString());
                return "ERRERR";
            }



            User currUser = users[userIndex];

            var newGroupString = RandomString(6);

            while (groupStringExists(newGroupString)) {
                newGroupString = RandomString(6);
            }



            UserGroup newGroup = new UserGroup();
            newGroup.usersInGroup.Add(currUser);
            newGroup.groupCode = newGroupString;
            groups.Add(newGroup);

            return newGroupString + generatePlaylistString(userIndex);


        }
        public static string joinRoom(string _userIndex, string groupCode) {

            int userIndex;
            try {
                userIndex = Int32.Parse(_userIndex);
            }
            catch (Exception e) {
                Debug.WriteLine(e.ToString());
                return "ERRERR";
            }
            
            User currUser = users[userIndex];

            UserGroup foundGroup = null;
            
            foreach (UserGroup group in groups) {
                if (groupCode == group.groupCode) {
                    foundGroup = group;
                    break;
                }
            }

            if (foundGroup == null) {
                return "NOTFOUND";
            }


            foundGroup.usersInGroup.Add(currUser);
            return $"ADDED,{userIndex}" + generatePlaylistString(userIndex);

        }
        public static async Task Swampify(string groupCode, string[] values, User leader) {
            
            UserGroup foundGroup = null;
            
            foreach (UserGroup group in groups) {
                if (groupCode == group.groupCode) {
                    foundGroup = group;
                    break;
                }
            }

            if (foundGroup == null) {
                return;
            }

            var danceability = Decimal.Parse(values[0])/100;
            var energy = Decimal.Parse(values[1])/100;
            var acousticness = Decimal.Parse(values[2])/100;
            var valence = Decimal.Parse(values[3])/100;
            var instrumentalness = Decimal.Parse(values[4])/100;
            
            Decimal range = Convert.ToDecimal(0.2);

            List<Track> matchingTracks = new List<Track>();

            Decimal totalDanceability = 0.0m;
            Decimal totalEnergy = 0.0m;
            Decimal totalAcousticness = 0.0m;
            Decimal totalInstrumentalness = 0.0m;
            Decimal totalValence = 0.0m;
            int counter = 0;
            
            foreach (User user in foundGroup.usersInGroup) {

                foreach (var playlist in user.playlists) {

                    foreach (Track track in playlist.trackArray) {

                        totalDanceability += track.danceability;
                        totalEnergy += track.energy;
                        totalAcousticness += track.acousticness;
                        totalInstrumentalness += track.instrumentalness;
                        totalValence += track.energy;
                        counter++;
                        
                        if (
                            track.danceability <= danceability + range 
                                && track.danceability >= danceability - range 
                                    && track.energy <= energy + range
                                        && track.energy >= energy - range
                                            && track.acousticness <= acousticness + range 
                                                && track.acousticness >= acousticness - range
                                                    && track.instrumentalness <= instrumentalness + range 
                                                        && track.instrumentalness >= instrumentalness - range
                                                            && track.valence <= valence + range 
                                                                && track.valence >= valence - range
                        ) {

                            matchingTracks.Add(track);

                        }


                    }
                }
            }
            
            Console.WriteLine($"Average danceability: {totalDanceability/counter}\n" +
                              $"Average Energy: {totalEnergy/counter}\n" +
                              $"Average Acousticness: {totalAcousticness/counter}\n" +
                              $"Average Instrumentalness: {totalInstrumentalness/counter}\n" +
                              $"Average Valence: {totalValence/counter}");
            
            
            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + leader.accessToken);
            //client.DefaultRequestHeaders.Add("Content-Type","application/json");

            string playlistname = $"Swampify Playlist for: {leader.userID}";
            
            string myJson = "{\"name\": \"" + playlistname + "\"}";

            var temp = new StringContent(myJson, Encoding.UTF8, "application/json");


            var response = await client.PostAsync($"https://api.spotify.com/v1/users/{leader.userID}/playlists", temp);

            string playlistURI = response.Headers.Location.ToString();

            List<String> matchingURIs = new List<String>();
            for (int i = 0; i < matchingTracks.Count; i++) {
                matchingURIs.Add(matchingTracks[i].uri);

            }

            matchingURIs = matchingURIs.Distinct().ToList();
            int numTimes = (matchingURIs.Count / 100) + 1;

            for (int i = 0; i < numTimes; i++) {

                var numURIs = Math.Min(100, matchingURIs.Count);
                var json = JsonConvert.SerializeObject(matchingURIs.GetRange(0, numURIs));
                json = "{\"uris\":" + json;
                json += "}";

                temp = new StringContent(json, Encoding.UTF8, "application/json");

                var whatever = await client.PostAsync(playlistURI + "/tracks", temp);
                
                matchingURIs.RemoveRange(0, numURIs);


            }

            var msg = Encoding.ASCII.GetBytes("OK");
            leader.privateSocket.Send(msg);
            leader.privateSocket.Shutdown(SocketShutdown.Both);
            leader.privateSocket.Close();


        }
        public static async Task getUserData(User user, List<int> playlistIndices) {
            
            var tasks = new List<Task>();

            List<Playlist> newUserPlaylistList = new List<Playlist>();

            for (int i = 0; i < user.playlists.Count; i++) {
                if (playlistIndices.Contains(i)) {
                    newUserPlaylistList.Add(user.playlists[i]);
                }
            }

            user.playlists = newUserPlaylistList;
            
            foreach (var playlist in user.playlists) {
                tasks.Add(generateSongs(playlist, user));
            }

            await Task.WhenAll(tasks);
            
            var msg = Encoding.ASCII.GetBytes("ALERT");
            user.isReady = true;
            
            
            
            
            user.privateSocket.Send(msg);
            user.privateSocket.Shutdown(SocketShutdown.Both);
            user.privateSocket.Close();

        }
        public static async Task generateSongs(Playlist playlist, User user) {
            
            string tracksGet = $"https://api.spotify.com/v1/playlists/{playlist.id}/tracks?limit=100";
            var client = new HttpClient();

            try {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + user.accessToken);
                var response = await client.GetAsync(tracksGet);
                var responseString = await response.Content.ReadAsStringAsync();
                var jss = new JavaScriptSerializer();
                var table = jss.Deserialize<dynamic>(responseString);

                socketTimer = new System.Timers.Timer(3000);
                socketTimer.Elapsed += new ElapsedEventHandler((sender, e) => sendLoadingMessage(sender, e, user));
                socketTimer.Enabled = true;


                for (int i = 0; i < playlist.numTracks; i++) {

                    if (i != 0 && i % 100 == 0) {
                        tracksGet = table["next"];
                        response = await client.GetAsync(tracksGet);
                        responseString = await response.Content.ReadAsStringAsync();
                        jss = new JavaScriptSerializer();
                        table = jss.Deserialize<dynamic>(responseString);
                    }

                    var currTrack = table["items"][i % 100]["track"];
                    var name = currTrack["name"];
                    var id = currTrack["id"];
                    var uri = currTrack["uri"];

                    Track newTrack = new Track(name, id, uri);
                    playlist.trackArray.Add(await fillTrackFeatures(newTrack, user));
                    //Console.WriteLine($"Current number of songs: {user.numSongsAdded}");
                    user.numSongsAdded++;
                }
            }
            catch (Exception e) {
                Debug.WriteLine($"Failed when generating songs: {e}");
            }
//            var msg = Encoding.ASCII.GetBytes(numSongsAdded.ToString());
//            user.privateSocket.Send(msg);
            
            socketTimer.Enabled = false;

            Console.WriteLine($"Succeeded Songs: {succeededSongs}, Failed songs: {failedSongs}");

            Debug.WriteLine("PLAYLIST FINISHED: " + playlist.name);
            
        }
        public static async Task<Track> fillTrackFeatures(Track track, User user) {

            int timeout = 10000;

            
            try{
                string featuresGet = $"https://api.spotify.com/v1/audio-features/{track.id}";
                var client = new HttpClient();

                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + user.accessToken);
                var response = client.GetAsync(featuresGet);
                
                if (await Task.WhenAny(response, Task.Delay(timeout)) != response) {
                    
                    throw new Exception("Timeout!");
                    
                }
                
                var responseResult = response.Result;
                int numTries = 0;
                
                while ((int)responseResult.StatusCode == 429) {
                    string temp = responseResult.Headers.RetryAfter.ToString();
                    int timeToSleep = Int32.Parse(temp) * 1000;
                    Thread.Sleep(timeToSleep);
                    response = client.GetAsync(featuresGet);
                    responseResult = response.Result;
                    if (await Task.WhenAny(response, Task.Delay(timeout)) != response) {
                    
                        throw new Exception("Timeout!");
                    
                    }
                    numTries++;
                    if (numTries > 10) {
                        Console.WriteLine($"Failed song: {track.name} over 10 times.");
                    }
                }


                var responseString = await responseResult.Content.ReadAsStringAsync();
                var jss = new JavaScriptSerializer();
                var table = jss.Deserialize<dynamic>(responseString);


            
                track.acousticness = table["acousticness"];
                track.danceability = table["danceability"];
                track.energy = table["energy"];
                track.instrumentalness = table["instrumentalness"];
                track.valence = table["valence"];
            }

            catch (Exception e) {
                Console.WriteLine(e.Message);
                
                track.acousticness = 0;
                track.danceability = 0;
                track.energy = 0;
                track.instrumentalness = 0;
                track.valence = 0;

                Console.WriteLine($"TRACK FAILED!!! : {track.name}");
                failedSongList.Add(track);
                failedSongs++;
                return track;

            }


            Console.WriteLine("Track finished: " + track.name);
            succeededSongs++;
            return track;
        }
        public static async Task<int> generatePlaylists(User user) {
            
            string playlistGet = $"https://api.spotify.com/v1/users/{user.userID}/playlists?limit=50";
            var client = new HttpClient();

            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + user.accessToken);
            var response = await client.GetAsync(playlistGet);
            var responseString = await response.Content.ReadAsStringAsync();
            var jss = new JavaScriptSerializer();
            var table = jss.Deserialize<dynamic>(responseString);


            int numPlaylists = table["total"];

            for (int i = 0; i < numPlaylists; i++) {

                if (i != 0 && i % 50 == 0) {
                    playlistGet = $"https://api.spotify.com/v1/users/{user.userID}/playlists?offset={i}&limit=50";
                    response = await client.GetAsync(playlistGet);
                    responseString = await response.Content.ReadAsStringAsync();
                    jss = new JavaScriptSerializer();
                    table = jss.Deserialize<dynamic>(responseString);
                    //get next set
                }

                var currPlaylist = table["items"][i % 50];
                var name = currPlaylist["name"];
                var tracksLink = currPlaylist["tracks"]["href"];
                var id = currPlaylist["id"];
                var numTracks = currPlaylist["tracks"]["total"];
                
                Playlist newPlaylist = new Playlist(name, tracksLink, id, numTracks);
                user.playlists.Add(newPlaylist);
                
            }

            return user.playlists.Count;
        }

        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public static bool groupStringExists(string codeToCheck) {

            bool foundString = false;
            
            foreach (UserGroup group in groups) {
                if (codeToCheck == group.groupCode) {
                    foundString = true;
                    break;
                }
            }


            return foundString;
        }
        public static string generatePlaylistString(int userIndex) {

            var currUser = users[userIndex];

            string playlistString = ",";

            foreach (var playlist in currUser.playlists) {
                playlistString += playlist.name + "," + playlist.numTracks + ",";
            }


            return playlistString;
        }

        private static void sendLoadingMessage(object source, ElapsedEventArgs e, User user)
        {
            var msg = Encoding.ASCII.GetBytes("," + user.numSongsAdded);
            user.privateSocket.Send(msg);
            
        }
        
    }  
    
    
    
    
}