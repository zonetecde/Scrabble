using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scrabble.Classes.Menu
{
    public class GameFinder
    {
        public GameFinder(string hostName, int hostId, int playerMax, List<string> players)
        {
            HostName = hostName;
            HostId = hostId;
            MaxPlayer = playerMax;
            Players = players;
        }

        public string HostName { get; set; }
        public int HostId { get; set; }
        public int MaxPlayer { get; set; }

        public List<string> Players { get; set; }
    }
}
