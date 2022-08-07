using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scrabble.Classes.Scrabble
{
    public class Game
    {
        public Player[] Players { get; set; }
        
        public int MaxPlayer { get; set; }
        public List<char> Pioche { get; set; }       

        public int WhoStart { get; set; }

        public int Round { get; set; }
        public string[,] GameBoard { get; set; }

        public bool GameFinish { get; set; }

        public Game()
        {
        }

        internal Game(Player[] players, int maxPlayer, string pseudoHost, int idHost)
        {
            GameBoard = new string[15, 15];

            Players = players;
            GameFinish = false;
            MaxPlayer = maxPlayer;

            Pioche = "EEEEEEEEEEEEEEEAAAAAAAAAIIIIIIIINNNNNNOOOOOORRRRRRSSSSSSTTTTTTUUUUUULLLLLDDDGGMMMBBCCPPFFHHVVJQKWXYZ  ".ToList();

            // On donne aux joueurs leurs pioches dans l'ordre qu'ils sont dans Players
            for (int i = 0; i < maxPlayer; i++)
            {
                Players[i] = new Player();
                Pioche = Pioche.OrderBy(x => MainWindow.rdn.Next()).ToList();
                Players[i].Lettres.AddRange(Pioche.Take(7).ToList());
                Pioche.RemoveRange(0, 7);
            }

            Players[0].Pseudo = pseudoHost;
            Players[0].Id = idHost;

            WhoStart = MainWindow.rdn.Next(0, maxPlayer);
            Round = 1;

            // DEVELOPPER
            WhoStart = 0;
            //Players[0].Lettres[0] = ' ' ;
            //Players[0].Lettres[1] = ' ' ;
            //Pioche = new List<char>();
            //Players[0].Lettres = new List<char>() { 'L', 'E' };


        }
    }

    public class Player
    {

        public string Pseudo { get; set; }
        public int Id { get; set; } = -1;
        public int Score { get; set; } = 0;
        public List<char> Lettres { get; set; } = new List<char>();
    }
}
