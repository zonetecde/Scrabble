using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scrabble_Serveur.Classes.Menu
{
    public class GameFinder
    {
        public GameFinder(string hostName, int hostId, int playerMax, List<string> players, bool isPrivate = false, int joinCode = -1)
        {
            HostName = hostName;
            HostId = hostId;
            MaxPlayer = playerMax;
            Players = players;
            IsPrivate = isPrivate;
            JoinCode = joinCode;
            DateOfCreation = DateTime.Now;
        }

        public string HostName { get; set; }
        public int HostId { get; set; }
        public int MaxPlayer { get; set; }
        public bool IsPrivate { get; set; }
        public int JoinCode { get; set; }
        public DateTime DateOfCreation { get; set; }

        public List<string> Players { get; set; }
    }
}
