using Newtonsoft.Json;
using Scrabble.Classes.Menu;
using Scrabble.Classes.Scrabble;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Scrabble
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Random rdn = new Random();

        private string Pseudo { get; set; } = "admin";
        private int Id { get; set; } = 123_456_789;

        // le fichier de la game en cours
        private string GameUrl { get; set; }

        // Le timer pour détecter quand est-ce que la game est pleine pour la commencer
        Timer Timer_IsGameFull { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            // Le pseudo doit contenir que des lettres
            textBox_pseudo.PreviewTextInput += (sender, e) =>
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[a-zA-Z]") || e.Text.Length > 12)
                {
                    e.Handled = true;
                }
            };

            // Timer init
            Timer_IsGameFull = new Timer(3000);
            Timer_IsGameFull.Elapsed += new ElapsedEventHandler(Timer_IsGameFull_Elapsed);

            // à supp
            Pseudo += rdn.Next(0, 1000);
            Id = rdn.Next(100000000, 999999999);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Change la taille du gameBoard, les cases ne sont pas ajouté au lancement
            gameBoard.Width = e.NewSize.Width / (double)2;
            gameBoard.Height = e.NewSize.Width / (double)2;

            stackPanel_leftPart.Width = (e.NewSize.Width - gameBoard.Width) / 2 - 20;
            stackPanel_rightPart.Width = (e.NewSize.Width - gameBoard.Width) / 2 - 20;

            if(e.NewSize.Width < 933 || e.NewSize.Height < 700)
            {
                this.Width = 933;
                this.Height = 700;
            }
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Resize width & height uniformly 
            WindowAspectRatio.Register((Window)sender);
        }

        private void Button_Play_Click(object sender, RoutedEventArgs e)
        {
            // Valide le nom et affiche le menu
            if(!String.IsNullOrEmpty(textBox_pseudo.Text))
            {
                Pseudo = textBox_pseudo.Text;
                Id = rdn.Next(100_000_000, 999_999_999);

                Grid_Login.Visibility = Visibility.Hidden;
                Grid_Menu.Visibility = Visibility.Visible;
            }
            else
            {
                textBox_pseudo.BorderBrush = Brushes.Red;
            }
        }
        
        private void Button_TrouverUnePartie_Click(object sender, RoutedEventArgs e)
        {
            // Demande combien de joueur doivent-ils y avoir dans la partie
            Grid_nbJoueur.Visibility = Visibility.Visible;
        }

        private async void Button_SelectNumberOfPlayer_Click(object sender, RoutedEventArgs e)
        {
            // Trouve une partie avec le bon nombre de joueur. Si aucune partie existente, créer une partie en attendant que quelqu'un la rejoigne.

            // Prend les parties
            List<GameFinder> gamesFinder = JsonConvert.DeserializeObject<List<GameFinder>>( await GitUtilities.FromGithub("games.sc"));

            // Check si une partie est trouvé avec le nombre de joueur demandé 
            if(gamesFinder.Any(x => x.MaxPlayer == Convert.ToInt32((sender as Button).Content)))
            {
                // Une partie est trouvé, on s'ajoute dedans
                gamesFinder.Find(x => x.MaxPlayer == Convert.ToInt32((sender as Button).Content))
                .Players.Add(Pseudo + "," + Id);

                // On renvois la partie avec nous dedans dans le github
                GitUtilities.ToGitghub(JsonConvert.SerializeObject(gamesFinder), "games.sc", false) ;

                GameUrl = gamesFinder.Find(x => x.MaxPlayer == Convert.ToInt32((sender as Button).Content)).HostId + ".sg";

                // s'ajoute au fichier game
                Game game = JsonConvert.DeserializeObject<Game>(await GitUtilities.FromGithub(GameUrl));
                game.Players.Add(Pseudo + "," + Id);
                GitUtilities.ToGitghub(JsonConvert.SerializeObject(game), GameUrl, false);
            }
            else
            {
                // Créer une nouvelle game car aucune partie trouvé
                GameFinder game = new GameFinder(Pseudo, Id, Convert.ToInt32((sender as Button).Content), new List<string>() { Pseudo + "," + Id });
                gamesFinder.Add(game);

                GitUtilities.ToGitghub(JsonConvert.SerializeObject(gamesFinder), "games.sc", false);

                GitUtilities.ToGitghub(JsonConvert.SerializeObject(new Game(new List<string>() { Pseudo + "," + Id }, Convert.ToInt32((sender as Button).Content))), Id.ToString() + ".sg", true);

                GameUrl = game.HostId + ".sg";
            }

            // Lance un timer qui va détécter toutes les x secondes si la game est complète
            Timer_IsGameFull.Start();
        }

        private async void Timer_IsGameFull_Elapsed(object? sender, ElapsedEventArgs e)
        {
            // Check si la game se trouvant dans GameUrl est full ou pas, si elle l'est : commence la game
            Game game = JsonConvert.DeserializeObject<Game>(await GitUtilities.FromGithub(GameUrl));
            if(game.Players.Count == game.MaxPlayer)
            {
                Timer_IsGameFull.Stop();

                // Supprime la game de games.sc
                List<GameFinder> gamesFinder = JsonConvert.DeserializeObject<List<GameFinder>>(await GitUtilities.FromGithub("games.sc"));
                gamesFinder.RemoveAll(x => x.MaxPlayer == game.MaxPlayer);
                GitUtilities.ToGitghub(JsonConvert.SerializeObject(gamesFinder), "games.sc", false);

                // Start la game
                this.Dispatcher.Invoke(() =>
                {
                    Grid_nbJoueur.Visibility = Visibility.Hidden;
                    Grid_Menu.Visibility = Visibility.Hidden;
                    grid_Game.Visibility = Visibility.Visible;

                    // Ajoute le nom des jouerus à la game
                    game.Players.ForEach(x =>
                    {
                        stackPanel_rightPart.Children.Add(new Label()
                        {
                            Content = x.Split(',')[0],
                            FontSize = 25
                        });
                    });
                });
            }
        }
    }
}
