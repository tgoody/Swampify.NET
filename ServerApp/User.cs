using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace ServerApp {
    public class User {
        public string userID;
        public string accessToken;
        public int expiresIn;
        public string refreshToken;
        public string groupCode;
        public bool isReady;
        public Socket privateSocket;
        public List<Playlist> playlists;
        public int numSongsAdded;

        public User() {
            playlists = new List<Playlist>();
            numSongsAdded = 0;
        }
    }



    public class UserGroup {
        
        public List<User> usersInGroup;
        public string groupCode;
        
        public UserGroup() {
            usersInGroup = new List<User>();
        }
        
        
    }







    public class Track {
        public string name;
        public string id;
        public string uri;
        public decimal danceability;
        public decimal energy;
        public decimal acousticness;
        public decimal instrumentalness;
        public decimal valence;
        
        

        public Track(string name, string id, string uri) {
            this.name = name;
            this.id = id;
            this.uri = uri;
            danceability = 0;
            energy = 0;
            acousticness = 0;
            valence = 0;
        }
        
    }

    public class Playlist {


        public string name, tracksLink, id;
        public List<Track> trackArray;
        public bool complete;
        public int numTracks;
        


        public Playlist(string name, string tracksLink, string id, int numTracks) {
            this.name = name;
            this.id = id;
            this.tracksLink = tracksLink;
            trackArray = new List<Track>();
            complete = false;
            this.numTracks = numTracks;
        }
    }
    
    
    
    
    
}