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
        // color
        private const string COLOR_LETTER_FIX = "#FFEFFFE5";
        private const string COLOR_LETTER_JOCKER = "#FFFFFF00";
        private const string COLOR_CURRENT_PLAYER = "#CC2E9C49";
        private const string COLOR_TRANSPARENT = "#00FFFFFF";

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
        private Game newGame { get; set; }
        private List<char> MesLettres { get; set; }
        internal static bool MyTurn { get; set; } = false;
        public static string LettresSelectionnees { get; set; } = String.Empty;
        public static List<int> ScoreLettresSelectionnees { get; set; }
        public static List<int[]> PosLettresSelectionnees { get; set; } = new List<int[]>();
        public static bool IsHorizontalPlacement { get; set; } = true;
        internal static bool IsFixed { get; set; } = false;
        private static List<string> Words { get; set; }
        private int ActualRound { get; set; } = 1;
        private Timer Timer_whenDoIStart;

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
            Timer_whenDoIStart = new Timer(5000);
            Timer_whenDoIStart.Elapsed += new ElapsedEventHandler(CheckIfItsMyTurn);

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
            newGame = JsonConvert.DeserializeObject<Game>(await GitUtilities.FromGithub(GameUrl));

            if(newGame.Players.Count(x => !String.IsNullOrEmpty(x.Pseudo)) == newGame.MaxPlayer)
            {
                Timer_IsGameFull.Stop();

                // Supprime la game de games.sc
                List<GameFinder> gamesFinder = JsonConvert.DeserializeObject<List<GameFinder>>(await GitUtilities.FromGithub("games.sc"));
                gamesFinder.RemoveAll(x => x.MaxPlayer == newGame.MaxPlayer);
                GitUtilities.ToGitghub(JsonConvert.SerializeObject(gamesFinder), "games.sc", false);

                // Start la game
                this.Dispatcher.Invoke(() =>
                {
                    Grid_nbJoueur.Visibility = Visibility.Hidden;
                    Grid_Menu.Visibility = Visibility.Hidden;
                    grid_Game.Visibility = Visibility.Visible;

                    // Ajoute le nom des joueurs à la game
                    newGame.Players.ToList().ForEach(x =>
                    {
                        stackPanel_players.Children.Add(new UserControl_Player(x));

                        // Lui qui commence
                        if (newGame.WhoStart == newGame.Players.ToList().IndexOf(x))
                        {
                            (stackPanel_players.Children[stackPanel_players.Children.Count - 1] as UserControl_Player)
                                .gradientStop_selected.Color = (Color)ColorConverter.ConvertFromString(COLOR_CURRENT_PLAYER);

                            if(x.Id.Equals(Id))
                            {
                                // Moi qui commence
                                MyTurn = true;
                            }
                            else
                            {
                                // Pas moi qui commence
                                button_Valider.Visibility = Visibility.Hidden;
                                // Lance le chronomètre pour savoir quand je commence
                                Timer_whenDoIStart.Start();
                            }
                        }
                    });

                    // Créer le tableau de jeu
                    DrawGameBoard();

                    // Affiche les lettres du joueur
                    MesLettres = newGame.Players.ToList().Find(x => x.Id == Id).Lettres;
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

        private async void CheckIfItsMyTurn(object? sender, ElapsedEventArgs e)
        {
            // On regarde si le numéro de round à changé, si oui alors on update le tableau, les scores...
            newGame = JsonConvert.DeserializeObject<Game>(await GitUtilities.FromGithub(GameUrl));

            if(newGame.Round > ActualRound)
            {
                this.Dispatcher.Invoke(() =>
                {
                    Timer_whenDoIStart.Stop();

                    ActualRound = newGame.Round;
                    var converter = new System.Windows.Media.BrushConverter();

                    // Update le tableau avec les nouvelles lettres
                    for (int y = 0; y < 15; y++)
                    {
                        for (int x = 0; x < 15; x++)
                        {
                            if (!String.IsNullOrEmpty(newGame.GameBoard[y, x]))
                            {
                                // La case contient quelque chose
                                // Si ce quelque chose est déjà dans notre gameBoard on ne la rajoute pas
                                if (!GameBoard[y, x].IsLetter)
                                {
                                    // On en fait une lettre
                                    GameBoard[y, x].IsLetter = true;
                                    GameBoard[y, x].Letter = newGame.GameBoard[y, x].Split(',')[0];
                                    GameBoard[y, x].Grid_Cell.Children.Add(new UserControl_Lettre(Convert.ToChar(GameBoard[y, x].Letter), false)
                                    {
                                        Width = wrapPanel_gameBoard.Width / (double)15,
                                        Height = wrapPanel_gameBoard.Width / (double)15
                                    });
                                    (GameBoard[y, x].Grid_Cell.Children[GameBoard[y, x].Grid_Cell.Children.Count - 1] as UserControl_Lettre).label_lettre.Foreground =
                                        (Brush)converter.ConvertFromString(newGame.GameBoard[y, x].Split(',')[1]);

                                    (GameBoard[y, x].Grid_Cell.Children[GameBoard[y, x].Grid_Cell.Children.Count - 1] as UserControl_Lettre).gradientStop_contour.Color =
                                        (Color)ColorConverter.ConvertFromString(COLOR_LETTER_FIX);

                                    if ((newGame.GameBoard[y, x].Split(',')[1]) == COLOR_LETTER_JOCKER) // si c'est un jocker la lettre a 0 point.
                                        (GameBoard[y, x].Grid_Cell.Children[GameBoard[y, x].Grid_Cell.Children.Count - 1] as UserControl_Lettre).label_score.Content = "0";
                                }
                            }
                        }
                    }

                    // Update les scores et celui qui joue
                    int i = 0;
                    foreach (UIElement item in stackPanel_players.Children)
                    {
                        if (item is UserControl_Player)
                        {
                            (item as UserControl_Player).label_point.Content = newGame.Players.First(x => x.Id == (item as UserControl_Player).Id).Score + " point(s)";

                            if (newGame.WhoStart == i)
                            {
                                // Le joueur qui joue
                                (item as UserControl_Player).gradientStop_selected.Color = (Color)ColorConverter.ConvertFromString(COLOR_CURRENT_PLAYER); 
                                if ((item as UserControl_Player).Id == Id) // si c'est nous qui jouons
                                {
                                    MyTurn = true;
                                    button_Valider.Visibility = Visibility.Visible;
                                }
                            }
                            else
                            {
                                (item as UserControl_Player).gradientStop_selected.Color = (Color)ColorConverter.ConvertFromString(COLOR_TRANSPARENT);
                            }

                            i++;
                        }
                    }

                    if (!MyTurn)
                        Timer_whenDoIStart.Start();
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

                bool isCompletingAWord = true;
                List<string> motsHorizontal = new List<string>();
                List<string> motsVertical = new List<string>();

                // voir le mot fait
                for (int indexLettreSelec = 0; indexLettreSelec < PosLettresSelectionnees.Count; indexLettreSelec++)
                {
                    if (indexLettreSelec == 0) // pour que ça puisse passer le premier if
                        motsHorizontal.Add("A");

                    // ça sert à rien de faire toutes les lettres meme résultat partout si c'est un mot complet
                    if (motsHorizontal.Last().Length == 1)
                    {
                        if (indexLettreSelec == 0)
                            motsHorizontal.Clear();

                        // le mot : horizontal
                        string finalWord_horizontal = string.Empty;

                        // on prend toutes les lettres déjà présent à gauche du mot au plus possible
                        for (int i = 1; i < 15; i++) // voir toutes les lettres présent à gauche de la première lettre ajouté           
                            if (PosLettresSelectionnees[indexLettreSelec][1] - i >= 0) // si c'est dans les limites du tableau
                                if (GameBoard[PosLettresSelectionnees[indexLettreSelec][0], PosLettresSelectionnees[indexLettreSelec][1] - i].IsLetter) // si la cellule est une lettre      
                                    finalWord_horizontal = finalWord_horizontal.Insert(0, GameBoard[PosLettresSelectionnees[indexLettreSelec][0], PosLettresSelectionnees[indexLettreSelec][1] - i].Letter); // ajoute la lettre au début du mot
                                else // ce n'est plus une lettre alors on doit arrêter la boucle, le mot est fini
                                    break;

                        // on prend toutes les lettres déjà présent et nouvelle à à droite du mot au plus possible dont la lettre de la cellule actuelle
                        for (int i = 0; i < 15; i++) // voir toutes les lettres présent à gauche de la première lettre ajouté           
                            if (PosLettresSelectionnees[indexLettreSelec][1] + i <= 14) // si c'est dans les limites du tableau
                                if (GameBoard[PosLettresSelectionnees[indexLettreSelec][0], PosLettresSelectionnees[indexLettreSelec][1] + i].IsLetter) // si la cellule est une lettre      
                                    finalWord_horizontal += GameBoard[PosLettresSelectionnees[indexLettreSelec][0], PosLettresSelectionnees[indexLettreSelec][1] + i].Letter; // ajoute la lettre au début du mot
                                else // ce n'est plus une lettre alors on doit arrêter la boucle, le mot est fini
                                    break;

                        // check si le mot horizontal existe si c'est un mot :
                        if (!Words.Contains(finalWord_horizontal) && finalWord_horizontal.Length > 1 && !finalWord_horizontal.Contains(" "))
                        {
                            messages.Add("Le mot horizontal " + finalWord_horizontal.Replace(" ", "?") + " n'existe pas.");

                            isWordGood = false;
                        }
                        else if (finalWord_horizontal.Contains(" ") && finalWord_horizontal.Length > 1)
                        {
                            string motPossible;


                            motPossible = CheckForJocker(finalWord_horizontal, ref firstJokerLetter, ref secondeJokerLetter, string.Empty);

                            if (String.IsNullOrEmpty(motPossible))
                            {
                                messages.Add("Le mot horizontal " + finalWord_horizontal.Replace(" ", "?") + " n'existe pas.");

                                isWordGood = false;
                            }
                        }

                        motsHorizontal.Add(finalWord_horizontal);

                        if ((finalWord_horizontal.Length == LettresSelectionnees.Length && GameBoard[7, 7].IsLetter)
                            && newGame.Round != 1 /*&& finalWord_horizontal.Length != 1*/)
                        {
                            isCompletingAWord = false;

                        }
                    }

                    if (indexLettreSelec == 0) // pour que ça puisse passer le premier if
                        motsVertical.Add("B");

                    if (motsVertical.Last().Length == 1)
                    {
                        if (indexLettreSelec == 0)
                            motsVertical.Clear();

                        // le mot : vertical
                        string finalWord_vertical = string.Empty;

                        // on prend toutes les lettres déjà présent à gauche du mot au plus possible
                        for (int i = 1; i < 15; i++) // voir toutes les lettres présent à gauche de la première lettre ajouté           
                            if (PosLettresSelectionnees[indexLettreSelec][0] - i >= 0) // si c'est dans les limites du tableau
                                if (GameBoard[PosLettresSelectionnees[indexLettreSelec][0] - i, PosLettresSelectionnees[indexLettreSelec][1]].IsLetter) // si la cellule est une lettre      
                                    finalWord_vertical = finalWord_vertical.Insert(0, GameBoard[PosLettresSelectionnees[indexLettreSelec][0] - i, PosLettresSelectionnees[indexLettreSelec][1]].Letter); // ajoute la lettre au début du mot
                                else // ce n'est plus une lettre alors on doit arrêter la boucle, le mot est fini
                                    break;

                        // on prend toutes les lettres déjà présent et nouvelle à à droite du mot au plus possible dont la lettre de la cellule actuelle
                        for (int i = 0; i < 15; i++) // voir toutes les lettres présent à gauche de la première lettre ajouté           
                            if (PosLettresSelectionnees[indexLettreSelec][0] + i <= 14) // si c'est dans les limites du tableau
                                if (GameBoard[PosLettresSelectionnees[indexLettreSelec][0] + i, PosLettresSelectionnees[indexLettreSelec][1]].IsLetter) // si la cellule est une lettre      
                                    finalWord_vertical += GameBoard[PosLettresSelectionnees[indexLettreSelec][0] + i, PosLettresSelectionnees[indexLettreSelec][1]].Letter; // ajoute la lettre au début du mot
                                else // ce n'est plus une lettre alors on doit arrêter la boucle, le mot est fini
                                    break;

                        // check si le mot vertical existe si c'est un mot
                        if ((!Words.Contains(finalWord_vertical) && finalWord_vertical.Length > 1) && !finalWord_vertical.Contains(" "))
                        {
                            messages.Add("Le mot vertical " + finalWord_vertical.Replace(" ", "?") + " n'existe pas.");

                            isWordGood = false;
                        }
                        else if (finalWord_vertical.Contains(" ") && finalWord_vertical.Length > 1 )
                        {
                            // on essaye de voir avec le jocker
                            string motPossible;

                            // si le jocker est sur 2 mots il faut vérifier les 2 mots
                            string motJockerToo = string.Empty;
                            for (int i = 0; i < motsHorizontal.Count; i++)
                            {
                                if (motsHorizontal[i].Length > 1)
                                    motJockerToo = motsHorizontal[i];
                            }

                            motPossible = CheckForJocker(finalWord_vertical, ref firstJokerLetter, ref secondeJokerLetter, motJockerToo);

                            if (String.IsNullOrEmpty(motPossible))
                            {
                                messages.Add("Le mot vertical " + finalWord_vertical.Replace(" ", "?") + " n'existe pas.");

                                isWordGood = false;
                            }
                        }

                        motsVertical.Add(finalWord_vertical);

                        // Check si le mot complète bien un autre mot
                        if (((finalWord_vertical.Length == LettresSelectionnees.Length && GameBoard[7, 7].IsLetter))
                            && newGame.Round != 1 /*&& finalWord_vertical.Length != 1*/)
                        {
                            isCompletingAWord = false;

                        }
                    }

                   
                }

                if (!isCompletingAWord)
                {
                    messages.Add("Le mot doit être attaché à d'autres mots");
                    isWordGood = false;
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


                    int total = 0;
                    if (LettresSelectionnees.Length == 7)
                        total += 50;

                    // On compte les points.
                    int totalHorizontal = 0;

                    for (int index = 0; index < motsHorizontal.Count; index++)
                    {
                        if (motsHorizontal[index].Length > 1)
                        {
                            int totalHorizontalDoubleWord = 0;
                            int totalHorizontalTripleWord = 0;

                            for (int i = 1; i < 15; i++)
                                if (PosLettresSelectionnees[index][1] - i >= 0)
                                    if (GameBoard[PosLettresSelectionnees[index][0], PosLettresSelectionnees[index][1] - i].IsLetter)
                                    {
                                        totalHorizontal += calculateScore(PosLettresSelectionnees[index][0], PosLettresSelectionnees[index][1] - i, ref totalHorizontalDoubleWord, ref totalHorizontalTripleWord);
                                    }
                                    else
                                        break;

                            for (int i = 0; i < 15; i++)
                                if (PosLettresSelectionnees[index][1] + i <= 14)
                                    if (GameBoard[PosLettresSelectionnees[index][0], PosLettresSelectionnees[index][1] + i].IsLetter)
                                    {
                                        totalHorizontal += calculateScore(PosLettresSelectionnees[index][0], PosLettresSelectionnees[index][1] + i, ref totalHorizontalDoubleWord, ref totalHorizontalTripleWord);
                                    }
                                    else
                                        break;

                            for (int i = 1; i <= totalHorizontalDoubleWord; i++)
                            {
                                totalHorizontal = totalHorizontal * 2;
                            }
                            for (int i = 1; i <= totalHorizontalTripleWord; i++)
                            {
                                totalHorizontal = totalHorizontal * 3;
                            }

                        }
                    }



                    // On compte les points.
                    int totalVertical = 0;

                    for (int index = 0; index < motsVertical.Count; index++)
                    {
                        int totalVerticalDoubleWord = 0;
                        int totalVerticalTripleWord = 0;

                        if (motsVertical[index].Length > 1)
                        {

                            for (int i = 1; i < 15; i++)
                                if (PosLettresSelectionnees[index][0] - i >= 0)
                                    if (GameBoard[PosLettresSelectionnees[index][0] - i, PosLettresSelectionnees[index][1]].IsLetter)
                                    {
                                        totalVertical += calculateScore(PosLettresSelectionnees[index][0] - i, PosLettresSelectionnees[index][1], ref totalVerticalDoubleWord, ref totalVerticalTripleWord);
                                    }
                                    else
                                        break;

                            for (int i = 0; i < 15; i++)
                                if (PosLettresSelectionnees[index][0] + i <= 14)
                                    if (GameBoard[PosLettresSelectionnees[index][0] + i, PosLettresSelectionnees[index][1]].IsLetter)
                                    {
                                        totalVertical += calculateScore(PosLettresSelectionnees[index][0] + i, PosLettresSelectionnees[index][1], ref totalVerticalDoubleWord, ref totalVerticalTripleWord);
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
                    }

                    total += (totalVertical + totalHorizontal);

                    // on remplace les jockers
                    bool firstJockerPassed = false;
                    PosLettresSelectionnees.ForEach(coordonees =>
                    {
                        // jocker
                        if (String.IsNullOrWhiteSpace(GameBoard[coordonees[0], coordonees[1]].Letter))
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

                    // couleur d'une lettre fixe
                    PosLettresSelectionnees.ForEach(coo =>
                    {
                        (GameBoard[coo[0], coo[1]].Grid_Cell.Children[GameBoard[coo[0], coo[1]].Grid_Cell.Children.Count - 1] as UserControl_Lettre).gradientStop_contour.Color = (Color)ColorConverter.ConvertFromString(COLOR_LETTER_FIX);
                    });

                    // on ajoute son score
                    newGame.Players.First(x => x.Id == Id).Score += total;
                    bool nextPlayerIsPlaying = false;
                    bool breakRightAfter = false;

                    REDO:
                    foreach (var item in stackPanel_players.Children)
                    {
                        if(item is UserControl_Player)
                            if((item as UserControl_Player).Id == Id && !breakRightAfter)
                            {
                                (item as UserControl_Player).gradientStop_selected.Color = (Color)ColorConverter.ConvertFromString(COLOR_TRANSPARENT); // enlève la couleur de séléction
                                (item as UserControl_Player).label_point.Content = newGame.Players.First(x => x.Id == Id).Score + " point(s)";
                                nextPlayerIsPlaying = true; // on met l'effet à la prochaine personne qui joue
                            }
                            else if(nextPlayerIsPlaying)
                            {
                                nextPlayerIsPlaying = false;
                                (item as UserControl_Player).gradientStop_selected.Color = (Color)ColorConverter.ConvertFromString(COLOR_CURRENT_PLAYER);

                                if (breakRightAfter)
                                    break;
                            }
                    }
                    // si nextPlayerIsPlaying est tjr true ça veut dire que c'est la premiere personne de la liste qui commence à jouer
                    if (nextPlayerIsPlaying)
                    {
                        breakRightAfter = true;
                        goto REDO;
                    }

                    // on enlève les lettres utilisées
                    List<UIElement> toDelete = new List<UIElement>();

                    LettresSelectionnees.ToList().ForEach(x =>
                    {
                        foreach (UserControl_Lettre item in wrapPanel_Lettre.Children)

                            if (item.label_lettre.Content.ToString() == Char.ToString(x))
                            {
                                toDelete.Add(item);

                                // remove les lettres du joueur
                                newGame.Players.First(x => x.Id == Id).Lettres.Remove(x);

                                break;
                            }                                      
                    });

                    toDelete.ForEach(x =>
                    {
                        wrapPanel_Lettre.Children.Remove(x);
                    });

                    // on ajoute de la pioche les lettres manquantes 
                    if(newGame.Pioche.Count >= toDelete.Count)
                    {
                        newGame.Players.First(x => x.Id == Id).Lettres.AddRange(newGame.Pioche.Take(toDelete.Count).ToList());
                        newGame.Pioche.RemoveRange(0, toDelete.Count);

                        // Ajoute les nouvelles lettre
                        foreach(UserControl_Lettre uc in wrapPanel_Lettre.Children)
                        {
                            uc.isGrabbing = false;
                            uc.IsSelected = false;
                            uc.gradientStop_contour.Color = (Color)ColorConverter.ConvertFromString("#FFEEE5FF");
                        }
                        for (int i = 7 - toDelete.Count; i < 7; i++)
                        {
                            wrapPanel_Lettre.Children.Add(new UserControl_Lettre(newGame.Players.First(x => x.Id == Id).Lettres[i], false)
                            {
                                Width = wrapPanel_gameBoard.Width / (double)15,
                                Height = wrapPanel_gameBoard.Width / (double)15
                            });
                        }
                    }
                    else
                    {
                        newGame.Players.First(x => x.Id == Id).Lettres.AddRange(newGame.Pioche.Take(newGame.Pioche.Count).ToList());
                        newGame.Pioche.RemoveRange(0, newGame.Pioche.Count);
                    }

                    // playerSuivant qui joue est
                    if(newGame.WhoStart + 1 == newGame.MaxPlayer)
                    {
                        newGame.WhoStart = 0;
                    }
                    else
                        newGame.WhoStart++;

                    newGame.Round++;

                    // send les lettres à ajouté
                    PosLettresSelectionnees.ForEach(co =>
                    {
                        newGame.GameBoard[co[0], co[1]] = GameBoard[co[0], co[1]].Letter + "," + ((SolidColorBrush)(GameBoard[co[0], co[1]].Grid_Cell.Children[GameBoard[co[0], co[1]].Grid_Cell.Children.Count - 1] as UserControl_Lettre).label_lettre.Foreground).Color.ToString();
                    });

                    // et enfin envois le tous sur le git
                    GitUtilities.ToGitghub(JsonConvert.SerializeObject(newGame), newGame.Players[0].Id + ".sg", false);

                    // c'est plus notre tour, on patiente.
                    MyTurn = false;
                    button_Valider.Visibility = Visibility.Hidden;
                    ActualRound++;
                    Timer_whenDoIStart.Start();

                    PosLettresSelectionnees.Clear();
                    LettresSelectionnees = String.Empty;
                    ScoreLettresSelectionnees.Clear();
                    IsFixed = false;

                }

                ShowQuickMessage(messages);
            }
        }

        private static int calculateScore(int y, int x, ref int doubleWord, ref int tripleWord)
        {
            int letterScore = 0;
            if (Convert.ToInt32((GameBoard[y, x].Grid_Cell.Children[GameBoard[y, x].Grid_Cell.Children.Count - 1] as UserControl_Lettre).label_score.Content) != 0) // c'est un jocker avec 0 points
                letterScore = Convert.ToInt32(Properties.Settings.Default.letterScore.Split(';').ToList().Find(j => j.Split(',')[0].ToList().Contains(Convert.ToChar(GameBoard[y, x].Letter))).Split(',')[1]);
            
            // on regarde si la case n'a pas un bonus 
            if (PosLettresSelectionnees.Any(j => j[0] == y && j[1] == x))
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

        private static string CheckForJocker(string word, ref char firstJokerLetter, ref char secondeJokerLetter, string secondWord)
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

                        if (limitedWords.Contains(motPossible) && String.IsNullOrEmpty(secondWord))
                        {
                            motPossibleTrouver = motPossible;
                            firstJokerLetter = alphabet[m];
                            secondeJokerLetter = alphabet[j];
                            break;
                        }
                        else if (Words.Contains(motPossible) && Words.Contains(regex.Replace(secondWord, Char.ToString(alphabet[m]), 1))
                            && (Words.Contains(
                                regex.Replace(secondWord, 
                            Char.ToString(alphabet[m]), 1).Replace(' ', alphabet[m]))))
                        {
                            motPossibleTrouver = motPossible;
                            firstJokerLetter = alphabet[m];
                            secondeJokerLetter = alphabet[j];
                        }
                    }
                }
                else
                {
                    // un seul jocker
                    motPossible = word.Replace(' ', alphabet[m]);
                    if (limitedWords.Contains(motPossible) && String.IsNullOrEmpty(secondWord))
                    {
                        motPossibleTrouver = motPossible;
                        firstJokerLetter = alphabet[m];
                        break;
                    }
                    else if (Words.Contains(motPossible) && Words.Contains(secondWord.Replace(' ', alphabet[m])))
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
