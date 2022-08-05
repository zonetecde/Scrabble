using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scrabble.Classes.Scrabble
{
    public class Game
    {
        public List<string> Players { get;set; }
        public int MaxPlayer { get; set; }

        public Game(List<string> players, int maxPlayer)
        {
            Players = players;
            MaxPlayer = maxPlayer;
        }
    }
}
