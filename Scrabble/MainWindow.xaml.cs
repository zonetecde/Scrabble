using Newtonsoft.Json;
using Scrabble.Classes.GameBoard;
using Scrabble.Classes.Menu;
using Scrabble.Classes.Scrabble;
using Scrabble.UC.GameBoard;
using Scrabble.UC.SP_rightPart;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
        public static Random rdn = new Random();

        private string Pseudo { get; set; } = "admin";
        private int Id { get; set; } = 123_456_789;

        // le fichier de la game en cours
        private string GameUrl { get; set; }

        // Le timer pour détecter quand est-ce que la game est pleine pour la commencer
        private Timer Timer_IsGameFull { get; set; }

        // GameBoard
        public static UserControl_Cell[,] GameBoard { get; set; }

        // Game
        private Game Game { get; set; }
        private List<char> MesLettres { get; set; }
        internal static bool MyTurn { get; set; } = false;
        public static string LettresSelectionnees { get; set; } = String.Empty;
        public static List<int> ScoreLettresSelectionnees { get; set; }
        public static List<int[]> PosLettresSelectionnees { get; set; } = new List<int[]>();
        public static bool IsHorizontalPlacement { get; set; } = true;
        internal static bool IsFixed { get; set; } = false;
        private static List<string> Words { get; set; }

        // res
        public static Cursor Grab { get; set; } 
        public static Cursor Grabbing { get; set; }

        // ref
        public static AlignableWrapPanel WrapPanel_Lettres { get; set; }

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
            Timer_IsGameFull = new Timer(2000);
            Timer_IsGameFull.Elapsed += new ElapsedEventHandler(Timer_IsGameFull_Elapsed);

            // Récupère les cursors
            Grab = ((TextBlock)Resources["CursorGrab"]).Cursor;
            Grabbing = ((TextBlock)Resources["CursorGrabbing"]).Cursor;

            // Reference
            WrapPanel_Lettres = wrapPanel_Lettre;

            //// à supp
            Pseudo += rdn.Next(0, 9999);
            Id = rdn.Next(100000000, 999999999);
            Grid_Menu.Visibility = Visibility.Visible;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Change la taille du gameBoard, les cases ne sont pas ajouté au lancement
            wrapPanel_gameBoard.Width = e.NewSize.Width / (double)2;
            wrapPanel_gameBoard.Height = e.NewSize.Width / (double)2;
            border_Message.Width = e.NewSize.Width / (double)2;

            //stackPanel_leftPart.Width = (e.NewSize.Width - wrapPanel_gameBoard.Width) / 2 - 20;
            stackPanel_rightPart.Width = (e.NewSize.Width - wrapPanel_gameBoard.Width) - 50;

            if(e.NewSize.Width < 933 || e.NewSize.Height < 700)
            {
                this.Width = 933;
                this.Height = 700;
            }

            // Change la taille des cases du gameBoard
            if(GameBoard != null)
            {
                for (int x = 0; x < 15; x++)
                {
                    for (int y = 0; y < 15; y++)
                    {
                        double cellSize = wrapPanel_gameBoard.Width / (double)15;
                        GameBoard[x, y].Width = cellSize;
                        GameBoard[x, y].Height = cellSize;
                    }
                }
            }

            // Change la taille des lettres 
            if (MesLettres != null)
            {
                for (int i = 0; i < wrapPanel_Lettre.Children.Count; i++)
                {
                    double cellSize = wrapPanel_gameBoard.Width / (double)15;
                    (wrapPanel_Lettre.Children[i] as UserControl_Lettre).Width = cellSize;
                    (wrapPanel_Lettre.Children[i] as UserControl_Lettre).Height = cellSize;
                }
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
            int max = Convert.ToInt32((sender as Button).Content);
            if (gamesFinder.Any(x => x.MaxPlayer == max) &&
                gamesFinder.First(x => x.MaxPlayer == max).Players.Count < max)
            {
                // Une partie est trouvé, on s'ajoute dedans
                gamesFinder.Find(x => x.MaxPlayer == Convert.ToInt32((sender as Button).Content))
                .Players.Add(Pseudo + "," + Id);

                // On renvois la partie avec nous dedans dans le github
                GitUtilities.ToGitghub(JsonConvert.SerializeObject(gamesFinder), "games.sc", false) ;

                GameUrl = gamesFinder.Find(x => x.MaxPlayer == Convert.ToInt32((sender as Button).Content)).HostId + ".sg";

                // s'ajoute au fichier game
                Game game = JsonConvert.DeserializeObject<Game>(await GitUtilities.FromGithub(GameUrl));
                // index vide pour s'ajouter
                int indexToAdd = 1;
                for (int i = 0; i < game.Players.Length; i++)
                {
                    if (game.Players[i].Id == -1)
                        indexToAdd = i;
                }
                game.Players[indexToAdd].Pseudo = Pseudo;
                game.Players[indexToAdd].Id = Id;
                GitUtilities.ToGitghub(JsonConvert.SerializeObject(game), GameUrl, false);
            }
            else
            {
                // Créer une nouvelle game car aucune partie trouvé
                GameFinder game = new GameFinder(Pseudo, Id, Convert.ToInt32((sender as Button).Content), new List<string>() { Pseudo + "," + Id });
                gamesFinder.Add(game);

                GitUtilities.ToGitghub(JsonConvert.SerializeObject(gamesFinder), "games.sc", false);

                Game game_added = new Game(new Player[gamesFinder.Last().MaxPlayer], gamesFinder.Last().MaxPlayer, Pseudo, Id);
                GitUtilities.ToGitghub(JsonConvert.SerializeObject(game_added), game.HostId.ToString() + ".sg", true);

                GameUrl = game.HostId + ".sg";
            }

            // Lance un timer qui va détécter toutes les x secondes si la game est complète
            Timer_IsGameFull.Start();
        }

        private async void Timer_IsGameFull_Elapsed(object? sender, ElapsedEventArgs e)
        {
            // Check si la game se trouvant dans GameUrl est full ou pas, si elle l'est : commence la game
            Game = JsonConvert.DeserializeObject<Game>(await GitUtilities.FromGithub(GameUrl));

            if(Game.Players.Count(x => !String.IsNullOrEmpty(x.Pseudo)) == Game.MaxPlayer)
            {
                Timer_IsGameFull.Stop();

                // Supprime la game de games.sc
                List<GameFinder> gamesFinder = JsonConvert.DeserializeObject<List<GameFinder>>(await GitUtilities.FromGithub("games.sc"));
                gamesFinder.RemoveAll(x => x.MaxPlayer == Game.MaxPlayer);
                GitUtilities.ToGitghub(JsonConvert.SerializeObject(gamesFinder), "games.sc", false);

                // Start la game
                this.Dispatcher.Invoke(() =>
                {
                    Grid_nbJoueur.Visibility = Visibility.Hidden;
                    Grid_Menu.Visibility = Visibility.Hidden;
                    grid_Game.Visibility = Visibility.Visible;

                    // Ajoute le nom des joueurs à la game
                    Game.Players.ToList().ForEach(x =>
                    {
                        stackPanel_players.Children.Add(new UserControl_Player(x));

                        // Lui qui commence
                        if (Game.WhoStart == Game.Players.ToList().IndexOf(x))
                        {
                            (stackPanel_players.Children[stackPanel_players.Children.Count - 1] as UserControl_Player)
                                .gradientStop_selected.Color = (Color)ColorConverter.ConvertFromString("#CCFFFFFF");

                            if(x.Id.Equals(Id))
                            {
                                // Moi qui commence
                                MyTurn = true;
                            }
                        }
                    });

                    // Créer le tableau de jeu
                    DrawGameBoard();

                    // Affiche les lettres du joueur
                    MesLettres = Game.Players.ToList().Find(x => x.Id == Id).Lettres;
                    MesLettres.ForEach(x =>
                    {
                        wrapPanel_Lettre.Children.Add(new UserControl_Lettre(x, false)
                        {
                            Width = wrapPanel_gameBoard.Width / (double)15,
                            Height = wrapPanel_gameBoard.Width / (double)15
                        });
                    });

                    // Ajoute les mots
                    var assembly = Assembly.GetExecutingAssembly();
                    string resourceName = assembly.GetManifestResourceNames()
                      .Single(str => str.EndsWith("words.txt"));

                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        Words = reader.ReadToEnd().Split(' ').ToList();
                    }
                });
            }
        }

        private void DrawGameBoard()
        {
            // Taille
            GameBoard = new UserControl_Cell[15, 15];

            // Cases ajoutées
            for (int x = 0; x < 15; x++)
            {
                for (int y = 0; y < 15; y++)
                {
                    double cellSize = wrapPanel_gameBoard.Width / (double)15;

                    GameBoard[x, y] = new UserControl_Cell(x,y)
                    {
                        Width = cellSize,
                        Height = cellSize
                    };

                    wrapPanel_gameBoard.Children.Add(GameBoard[x, y]);
                }
            }

            // Colorier les cases
            // Y; X
            Properties.Settings.Default.redCell.Split(';').ToList().ForEach(x =>
            {
                GameBoard[Convert.ToInt32(x.Split(',')[0]), Convert.ToInt32(x.Split(',')[1])].MakeIt(ScrabbleColor.RED);
            });
            Properties.Settings.Default.pinkCell.Split(';').ToList().ForEach(x =>
            {
                GameBoard[Convert.ToInt32(x.Split(',')[0]), Convert.ToInt32(x.Split(',')[1])].MakeIt(ScrabbleColor.PINK);
            });
            Properties.Settings.Default.lightBlueCell.Split(';').ToList().ForEach(x =>
            {
                GameBoard[Convert.ToInt32(x.Split(',')[0]), Convert.ToInt32(x.Split(',')[1])].MakeIt(ScrabbleColor.LIGHT_BLUE);
            });
            Properties.Settings.Default.blueCell.Split(';').ToList().ForEach(x =>
            {
                GameBoard[Convert.ToInt32(x.Split(',')[0]), Convert.ToInt32(x.Split(',')[1])].MakeIt(ScrabbleColor.BLUE);
            });
            GameBoard[7, 7].MakeIt(ScrabbleColor.CENTER);
        }

        private void wrapPanel_Lettre_MouseEnter(object sender, MouseEventArgs e)
        {
            // Si le curseur va sur le wrapPanel cela veut dire que la lettre ne doit plus être grab, lorsque l'utilisateur veut changer l'ordre de ses lettres
            foreach (UserControl_Lettre uc in wrapPanel_Lettre.Children)
                uc.isGrabbing = false;
        }

        private void Border_Lettres_MouseLeave(object sender, MouseEventArgs e)
        {
            LettresSelectionnees = string.Empty;
            ScoreLettresSelectionnees = new List<int>();

            foreach (UserControl_Lettre item in wrapPanel_Lettre.Children)
            {
                if (item.IsSelected)
                {
                    LettresSelectionnees += item.label_lettre.Content.ToString() == string.Empty ? " " : item.label_lettre.Content.ToString();
                    ScoreLettresSelectionnees.Add(Convert.ToInt16(item.label_score.Content));
                }
            }
        }

        private void Grid_rightPart_MouseEnter(object sender, MouseEventArgs e)
        {
            // enlève toutes les anciennes lettres placé car on sort du gameBoard
            MainWindow.PosLettresSelectionnees.ForEach(x =>
            {
                if (!((MainWindow.GameBoard[x[0], x[1]].IsLetter)))
                    MainWindow.GameBoard[x[0], x[1]].Grid_Cell.Children.RemoveAt(
                         MainWindow.GameBoard[x[0], x[1]].Grid_Cell.Children.Count - 1);
            });
        }

        private void Button_ValiderSonTour_Click(object sender, RoutedEventArgs e)
        {
            // Valide la position actuelle des lettres
            if(IsFixed && MyTurn && PosLettresSelectionnees.Any())
            {
                // si il y a des jockers
                char firstJokerLetter = ' ';
                char secondeJokerLetter = ' ';

                bool isWordGood = true;
                List<string> messages = new List<string>();

                // voir le mot fait
                int[] firstPos = PosLettresSelectionnees[0];

                // le mot : horizontal
                string finalWord_horizontal = string.Empty;

                // on prend toutes les lettres déjà présent à gauche du mot au plus possible
                for (int i = 1; i < 15; i++) // voir toutes les lettres présent à gauche de la première lettre ajouté           
                    if(firstPos[1] - i >= 0) // si c'est dans les limites du tableau
                        if(GameBoard[firstPos[0], firstPos[1] - i].IsLetter) // si la cellule est une lettre      
                            finalWord_horizontal.Insert(0, GameBoard[firstPos[0], firstPos[1] - i].Letter); // ajoute la lettre au début du mot
                        else // ce n'est plus une lettre alors on doit arrêter la boucle, le mot est fini
                            break;

                // on prend toutes les lettres déjà présent et nouvelle à à droite du mot au plus possible dont la lettre de la cellule actuelle
                for (int i = 0; i < 15; i++) // voir toutes les lettres présent à gauche de la première lettre ajouté           
                    if (firstPos[1] + i <= 14) // si c'est dans les limites du tableau
                        if (GameBoard[firstPos[0], firstPos[1] + i].IsLetter) // si la cellule est une lettre      
                            finalWord_horizontal += GameBoard[firstPos[0], firstPos[1] + i].Letter; // ajoute la lettre au début du mot
                        else // ce n'est plus une lettre alors on doit arrêter la boucle, le mot est fini
                            break;

                // check si le mot horizontal existe si c'est un mot :
                if (!Words.Contains(finalWord_horizontal) && finalWord_horizontal.Length > 1 && !finalWord_horizontal.Contains(" "))
                {
                    messages.Add("Le mot horizontal " + finalWord_horizontal.Replace(" ", "?") + " n'existe pas.");

                    isWordGood = false;
                }
                else if(finalWord_horizontal.Contains(" ") && finalWord_horizontal.Length > 1)
                {
                    string motPossible;

                    motPossible = CheckForJocker(finalWord_horizontal, ref firstJokerLetter, ref secondeJokerLetter);
                    
                    if(String.IsNullOrEmpty(motPossible))
                    {
                        messages.Add("Le mot horizontal " + finalWord_horizontal.Replace(" ", "?") + " n'existe pas.");

                        isWordGood = false;
                    }
                }

                // le mot : vertical
                string finalWord_vertical = string.Empty;

                // on prend toutes les lettres déjà présent à gauche du mot au plus possible
                for (int i = 1; i < 15; i++) // voir toutes les lettres présent à gauche de la première lettre ajouté           
                    if (firstPos[0] - i >= 0) // si c'est dans les limites du tableau
                        if (GameBoard[firstPos[0] - i, firstPos[1]].IsLetter) // si la cellule est une lettre      
                            finalWord_vertical.Insert(0, GameBoard[firstPos[0] - i, firstPos[1]].Letter); // ajoute la lettre au début du mot
                        else // ce n'est plus une lettre alors on doit arrêter la boucle, le mot est fini
                            break;

                // on prend toutes les lettres déjà présent et nouvelle à à droite du mot au plus possible dont la lettre de la cellule actuelle
                for (int i = 0; i < 15; i++) // voir toutes les lettres présent à gauche de la première lettre ajouté           
                    if (firstPos[0] + i <= 14) // si c'est dans les limites du tableau
                        if (GameBoard[firstPos[0] + i, firstPos[1]].IsLetter) // si la cellule est une lettre      
                            finalWord_vertical += GameBoard[firstPos[0] + i, firstPos[1]].Letter; // ajoute la lettre au début du mot
                        else // ce n'est plus une lettre alors on doit arrêter la boucle, le mot est fini
                            break;

                // check si le mot vertical existe si c'est un mot
                if ((!Words.Contains(finalWord_vertical) && finalWord_vertical.Length > 1) && !finalWord_vertical.Contains(" "))
                {
                    messages.Add("Le mot vertical " + finalWord_vertical.Replace(" ", "?") + " n'existe pas.");

                    isWordGood = false;
                }
                else if (finalWord_vertical.Contains(" ") && finalWord_vertical.Length > 1)
                {
                    // on essaye de voir avec le jocker
                    string motPossible;

                    motPossible = CheckForJocker(finalWord_vertical, ref firstJokerLetter, ref secondeJokerLetter);
                    
                    if (String.IsNullOrEmpty(motPossible))
                    {
                        messages.Add("Le mot vertical " + finalWord_vertical.Replace(" ", "?") + " n'existe pas.");

                        isWordGood = false;
                    }
                }

                // Check si le mot complète bien un autre mot
                if (((finalWord_vertical.Length == LettresSelectionnees.Length && GameBoard[7, 7].IsLetter)
                    || (finalWord_horizontal.Length == LettresSelectionnees.Length && GameBoard[7, 7].IsLetter))
                    && Game.Round != 1)
                {
                    messages.Add("Le mot doit être attaché à d'autres mots");

                }

                // si c'est le premier joueur à commencer il faut que le mot soit centré
                if (!GameBoard[7, 7].IsLetter)
                {
                    // Le mot n'est pas centré
                    messages.Add("Le mot doit être centré");
                    isWordGood = false;
                }

                // les mots existent verticalement et horizontalement
                if (isWordGood)
                {
                    int totalHorizontal = 0;
                    int totalHorizontalDoubleWord = 0;
                    int totalHorizontalTripleWord = 0;

                    int total = 0;
                    if (LettresSelectionnees.Length == 7)
                        total += 50;
                    
                    // On compte les points.
                    if(finalWord_horizontal.Length > 1)
                    {

                        for (int i = 1; i < 15; i++)
                            if (firstPos[1] - i >= 0)
                                if (GameBoard[firstPos[0], firstPos[1] - i].IsLetter)
                                {
                                    totalHorizontal += calculateScore(firstPos[0], firstPos[1] - i, ref totalHorizontalDoubleWord, ref totalHorizontalTripleWord);
                                }
                                else
                                    break;

                        for (int i = 0; i < 15; i++) 
                            if (firstPos[1] + i <= 14)
                                if (GameBoard[firstPos[0], firstPos[1] + i].IsLetter)
                                {
                                    totalHorizontal += calculateScore(firstPos[0], firstPos[1] + i, ref totalHorizontalDoubleWord, ref totalHorizontalTripleWord);
                                }
                                else
                                    break;
                    }

                    for (int i = 1; i <= totalHorizontalDoubleWord; i++)
                    {
                        totalHorizontal = totalHorizontal * 2;
                    }
                    for (int i = 1; i <= totalHorizontalTripleWord; i++)
                    {
                        totalHorizontal = totalHorizontal * 3;
                    }

                    // On compte les points.
                    int totalVertical = 0;
                    int totalVerticalDoubleWord = 0;
                    int totalVerticalTripleWord = 0;

                    if (finalWord_vertical.Length > 1)
                    {

                        for (int i = 1; i < 15; i++)    
                            if (firstPos[0] - i >= 0) 
                                if (GameBoard[firstPos[0] - i, firstPos[1]].IsLetter)
                                {
                                    totalVertical += calculateScore(firstPos[0] - i, firstPos[1] , ref totalVerticalDoubleWord, ref totalVerticalTripleWord);
                                }
                                else
                                    break;

                        for (int i = 0; i < 15; i++) 
                            if (firstPos[0] + i <= 14) 
                                if (GameBoard[firstPos[0] + i, firstPos[1]].IsLetter)
                                {
                                    totalVertical += calculateScore(firstPos[0] + i, firstPos[1] , ref totalVerticalDoubleWord, ref totalVerticalTripleWord);
                                }
                                else
                                    break;
                    }

                    for (int i = 1; i <= totalVerticalDoubleWord; i++)
                    {
                        totalVertical = totalVertical * 2;
                    }
                    for (int i = 1; i <= totalVerticalTripleWord; i++)
                    {
                        totalVertical = totalVertical * 3;
                    }

                    total += (totalVertical + totalHorizontal);

                    // on remplace les jockers
                    bool firstJockerPassed = false;
                    PosLettresSelectionnees.ForEach(coordonees =>
                    {
                        // jocker
                        if (String.IsNullOrEmpty(GameBoard[coordonees[0], coordonees[1]].Letter))
                        {
                            if (!firstJockerPassed)
                            {
                                GameBoard[coordonees[0], coordonees[1]].Letter = Char.ToString(firstJokerLetter);
                                (GameBoard[coordonees[0], coordonees[1]].Grid_Cell.Children[GameBoard[coordonees[0], coordonees[1]].Grid_Cell.Children.Count - 1] as UserControl_Lettre).label_lettre.Content = Char.ToString(firstJokerLetter);
                                (GameBoard[coordonees[0], coordonees[1]].Grid_Cell.Children[GameBoard[coordonees[0], coordonees[1]].Grid_Cell.Children.Count - 1] as UserControl_Lettre).label_lettre.Foreground = Brushes.Yellow;
                            }
                            else
                            {
                                GameBoard[coordonees[0], coordonees[1]].Letter = Char.ToString(secondeJokerLetter);
                                (GameBoard[coordonees[0], coordonees[1]].Grid_Cell.Children[GameBoard[coordonees[0], coordonees[1]].Grid_Cell.Children.Count - 1] as UserControl_Lettre).label_lettre.Content = Char.ToString(secondeJokerLetter);
                                (GameBoard[coordonees[0], coordonees[1]].Grid_Cell.Children[GameBoard[coordonees[0], coordonees[1]].Grid_Cell.Children.Count - 1] as UserControl_Lettre).label_lettre.Foreground = Brushes.Yellow;
                            }

                            firstJockerPassed = true;
                        }
                    });

                    // on ajoute son score
                    Game.Players.First(x => x.Id == Id).Score += total;

                    foreach(var item in stackPanel_players.Children)
                    {
                        if(item is UserControl_Player)
                            if((item as UserControl_Player).Id == Id)
                            {
                                (item as UserControl_Player).label_point.Content = Game.Players.First(x => x.Id == Id).Score + " point(s)";
                            }
                    }

                    // on enlève les lettres utilisées
                    List<UIElement> toDelete = new List<UIElement>();
                    LettresSelectionnees.ToList().ForEach(x =>
                    {
                        foreach (UserControl_Lettre item in wrapPanel_Lettre.Children)

                            if (item.label_lettre.Content == Char.ToString(x))
                            {
                                toDelete.Add(item);
                                break;
                            }
                            
                        
                    });

                    toDelete.ForEach(x =>
                    {
                        wrapPanel_Lettre.Children.Remove(x);
                    });

                    // on ajoute de la pioche les lettres manquantes 
                    if(Game.Pioche.Count >= toDelete.Count)
                    {
                        Game.Players.First(x => x.Id == Id).Lettres.AddRange(Game.Pioche.Take(toDelete.Count).ToList());
                        Game.Pioche.RemoveRange(0, toDelete.Count);
                    }
                    else
                    {
                        Game.Players.First(x => x.Id == Id).Lettres.AddRange(Game.Pioche.Take(Game.Pioche.Count).ToList());
                        Game.Pioche.RemoveRange(0, Game.Pioche.Count);
                    }

                    // playerSuivant qui joue est
                    if(Game.WhoStart + 1 == Game.MaxPlayer)
                    {
                        Game.WhoStart = 0;
                    }
                    else
                        Game.WhoStart++;

                    Game.Round++;

                    // send les lettres à ajouté
                    PosLettresSelectionnees.ForEach(co =>
                    {
                        Game.GameBoard[co[0], co[1]] = GameBoard[co[0], co[1]].Letter + "," + ((SolidColorBrush)(GameBoard[co[0], co[1]].Grid_Cell.Children[GameBoard[co[0], co[1]].Grid_Cell.Children.Count - 1] as UserControl_Lettre).label_lettre.Foreground).Color.ToString();
                    });

                    // et enfin envois le tous sur le git
                    GitUtilities.ToGitghub(JsonConvert.SerializeObject(Game), Game.Players[0].Id + ".sg", false);
                }

                ShowQuickMessage(messages);
            }
        }

        private static int calculateScore(int y, int x, ref int doubleWord, ref int tripleWord)
        {
            int letterScore = Convert.ToInt32(Properties.Settings.Default.letterScore.Split(';').ToList().Find(j => j.Split(',')[0].ToList().Contains(Convert.ToChar(GameBoard[y, x].Letter))).Split(',')[1]);
            
            // on regarde si la case n'a pas un bonus 
            if (PosLettresSelectionnees.Contains(new int[] { y, x }))
            {
                switch (GameBoard[y, x].ScrabbleColor)
                {
                    case ScrabbleColor.LIGHT_BLUE:
                        letterScore = letterScore * 2;
                        break;
                    case ScrabbleColor.BLUE:
                        letterScore = letterScore * 3;
                        break;
                    case ScrabbleColor.PINK:
                        doubleWord++;
                        break;
                    case ScrabbleColor.RED:
                        tripleWord++;
                        break;
                }
            }

            return letterScore;
        }

        private static string CheckForJocker(string word, ref char firstJokerLetter, ref char secondeJokerLetter)
        {
            List<string> limitedWords = Words.FindAll(x => x.Length == word.Length).ToList();

            char[] alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
            int nbJoker = word.Count(x => x == ' ');
            int posJocker1 = word.IndexOf(' ');
            int posJocker2 = word.LastIndexOf(' ');
            string motPossible;
            string motPossibleTrouver = String.Empty;
            for (int m = 0; m < 26; m++)
            {
                if (nbJoker == 2)
                {
                    for (int j = 0; j < 26; j++)
                    {
                        // 2 jocker
                        motPossible = word;
                        var regex = new Regex(Regex.Escape(" "));
                        motPossible = regex.Replace(motPossible, Char.ToString(alphabet[m]), 1);

                        motPossible = motPossible.Replace(' ', alphabet[j]);

                        if (limitedWords.Contains(motPossible))
                        {
                            motPossibleTrouver = motPossible;
                            firstJokerLetter = alphabet[m];
                            secondeJokerLetter = alphabet[j];
                            break;
                        }
                    }
                }
                else
                {
                    // un seul jocker
                    motPossible = word.Replace(' ', alphabet[m]);
                    if (limitedWords.Contains(motPossible))
                    {
                        motPossibleTrouver = motPossible;
                        firstJokerLetter = alphabet[m];
                        break;
                    }
                }
            }

            return motPossibleTrouver;
        }

        private void ShowQuickMessage(List<string> messages)
        {
            if (messages.Any())
            {
                border_Message.Visibility = Visibility.Visible;
                label_message.Text = string.Join("\n", messages);
                Timer t = new Timer(5000);
                t.Elapsed += (sender, e) =>
                {
                    t.Stop();

                    this.Dispatcher.Invoke(() =>
                    {
                        border_Message.Visibility -= Visibility.Hidden;
                    });
                };
                t.Start();
            }
        }
    }
}
