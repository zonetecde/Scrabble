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
        private const string COLOR_CURRENT_PLAYER = "#B2D0F9D5";
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
        public static List<int[]> PosLettresSelectionnees { get; set; } = new List<int[]>();
        public static bool IsHorizontalPlacement { get; set; } = true;
        internal static bool IsFixed { get; set; } = false;
        private static List<string> Words { get; set; }
        private int ActualRound { get; set; } = 1;
        private Timer Timer_whenDoIStart;
        private Timer Timer_MyTime;
        private int Timer_MyTime_TotalElapsed { get; set; } = 0;

        // res
        public static Cursor Grab { get; set; } 
        public static Cursor Grabbing { get; set; }

        // ref
        public static AlignableWrapPanel WrapPanel_Lettres { get; set; }
        public static StackPanel stackPanel_motVoulu { get; set; }

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
            Timer_MyTime = new Timer(100);
            Timer_MyTime.Elapsed += new ElapsedEventHandler(TimesUp);

            // Récupère les cursors
            Grab = ((TextBlock)Resources["CursorGrab"]).Cursor;
            Grabbing = ((TextBlock)Resources["CursorGrabbing"]).Cursor;

            // Reference
            WrapPanel_Lettres = wrapPanel_Lettre;
            stackPanel_motVoulu = StackPanel_motVoulu;

            //// à supp
            Pseudo += rdn.Next(0, 9999);
            Id = rdn.Next(100000000, 999999999);
            Grid_Menu.Visibility = Visibility.Visible;
        }

        private void TimesUp(object? sender, ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                Timer_MyTime_TotalElapsed++;

                if(Timer_MyTime_TotalElapsed >= 900)
                {
                    Timer_MyTime.Stop();
                    Timer_MyTime_TotalElapsed = 0;
                    Button_PasserSonTour_Click(this, null);
                }

                progressBar_timer.Value = Timer_MyTime_TotalElapsed;
            });
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Change la taille du gameBoard, les cases ne sont pas ajouté au lancement
            wrapPanel_gameBoard.Width = e.NewSize.Width / (double)2;
            wrapPanel_gameBoard.Height = e.NewSize.Width / (double)2;
            border_Message.Width = e.NewSize.Width / (double)2;

            //stackPanel_leftPart.Width = (e.NewSize.Width - wrapPanel_gameBoard.Width) / 2 - 20;
            stackPanel_rightPart.Width = (e.NewSize.Width - wrapPanel_gameBoard.Width) - 50;
            //progressBar_timer.Width = e.NewSize.Width - stackPanel_rightPart.Width - 20;

            if (e.NewSize.Width < 933 || e.NewSize.Height < 700)
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

            try
            {
                // Prend les parties
                List<GameFinder> gamesFinder = JsonConvert.DeserializeObject<List<GameFinder>>(await GitUtilities.FromGithub("games.sc"));

                // Pendant ce temps loading screen
                Grid_Loading.Visibility = Visibility.Visible;

                // Check si une partie est trouvé avec le nombre de joueur demandé et qui n'est pas complète
                int max = Convert.ToInt32((sender as Button).Content);

                if (gamesFinder.Any(x => x.MaxPlayer == max) && gamesFinder.Any(x => x.MaxPlayer == max && x.Players.Count < max))
                {
                    // Une partie est trouvé, on s'ajoute dedans
                    gamesFinder.Find(x => x.MaxPlayer == max && x.Players.Count < x.MaxPlayer)
                        .Players.Add(Pseudo + "," + Id);

                    // On renvois la partie avec nous dedans dans le github
                    GitUtilities.ToGitghub(JsonConvert.SerializeObject(gamesFinder), "games.sc", false);

                    GameUrl = gamesFinder.Find(x => x.MaxPlayer == max).HostId + ".sg";

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
            catch
            {
                MessageBox.Show("Une connexion internet est requise pour jouer en ligne", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Timer_IsGameFull_Elapsed(object? sender, ElapsedEventArgs e)
        {
            // Check si la game se trouvant dans GameUrl est full ou pas, si elle l'est : commence la game
            try
            {
                newGame = JsonConvert.DeserializeObject<Game>(await GitUtilities.FromGithub(GameUrl));
            }
            catch
            {
                this.Dispatcher.Invoke(() =>
                {
                    Timer_IsGameFull.Stop();
                    grid_Game.Visibility = Visibility.Hidden;
                    GameUrl = String.Empty;
                    MessageBox.Show("Un problème a été rencontré. Veuillez réessayer.", "Problème", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }

            if(newGame.Players.Count(x => !String.IsNullOrEmpty(x.Pseudo)) == newGame.MaxPlayer)
            {
                Timer_IsGameFull.Stop();

                // Supprime la game de games.sc
                List<GameFinder> gamesFinder = JsonConvert.DeserializeObject<List<GameFinder>>(await GitUtilities.FromGithub("games.sc"));
                gamesFinder.RemoveAll(x => x.HostId == newGame.Players[0].Id);
                GitUtilities.ToGitghub(JsonConvert.SerializeObject(gamesFinder), "games.sc", false);

                // Start la game
                this.Dispatcher.Invoke(() =>
                {
                    Grid_Loading.Visibility = Visibility.Hidden;
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
                                Timer_MyTime.Start();
                                Timer_MyTime_TotalElapsed = 0;
                                progressBar_timer.Value = 0;
                            }
                            else
                            {
                                // Pas moi qui commence
                                button_Valider.Visibility = Visibility.Hidden;
                                button_echangerLettre.Visibility = Visibility.Hidden;
                                Button_PasserSonTour.Visibility = Visibility.Hidden;
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
            try
            {
                newGame = JsonConvert.DeserializeObject<Game>(await GitUtilities.FromGithub(GameUrl));

                if (newGame.Round > ActualRound)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        Timer_whenDoIStart.Stop();
                        Timer_MyTime.Stop();

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
                                        Timer_MyTime.Start();
                                        Timer_MyTime_TotalElapsed = 0;
                                        button_Valider.Visibility = Visibility.Visible;
                                        Button_PasserSonTour.Visibility = Visibility.Visible;
                                        button_echangerLettre.Visibility = Visibility.Visible;
                                        progressBar_timer.Value = 0;
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
                        {
                            Timer_whenDoIStart.Start();
                        }
                    });
                }
            }
            catch
            {
                // connexion internet perdu
                this.Dispatcher.Invoke(() =>
                {
                    ShowQuickMessage(new List<string>() { "⚠️ La connexion internet a été perdu ⚠️" });
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

        public static EventHandler border_Lettres_MouseLeave;

        internal void Border_Lettres_MouseLeave(object sender, MouseEventArgs e)
        {
            LettresSelectionnees = string.Empty;

            foreach (UserControl_Lettre item in wrapPanel_Lettre.Children)
            {
                if (item.IsSelected)
                {
                    LettresSelectionnees += item.label_lettre.Content.ToString() == string.Empty ? " " : item.label_lettre.Content.ToString();
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

        // si il y a des jockers, je les met ici pour que lorsque l'on appuie sur un bouton pour choisir son mot on y ai accès
        int firstJokerLetterIndex = -1;
        int secondeJokerLetterIndex = -1;


        private void Button_ValiderSonTour_Click(object sender, RoutedEventArgs e)
        {
            if (GitUtilities.CheckForInternetConnection())
            {

                // Valide la position actuelle des lettres
                if ((IsFixed && MyTurn && PosLettresSelectionnees.Any()) || sender.Equals("goto"))
                {
                    bool isWordGood = true;
                    List<string> messages = new List<string>();

                    bool isCompletingAWord = true;
                    List<string> motsHorizontal = new List<string>();
                    List<string> motsVertical = new List<string>();
                    List<string> motsPossibles = new List<string>(); ; // jocker uniquement


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
                                motsPossibles = CheckForJocker(finalWord_horizontal, ref firstJokerLetterIndex, ref secondeJokerLetterIndex, string.Empty);

                                if (!motsPossibles.Any())
                                {
                                    messages.Add("Le mot horizontal " + finalWord_horizontal.Replace(" ", "?") + " n'existe pas.");

                                    isWordGood = false;
                                }
                            }

                            motsHorizontal.Add(finalWord_horizontal);

                            if ((finalWord_horizontal.Length == LettresSelectionnees.Length && GameBoard[7, 7].IsLetter)
                                && newGame.Round != 1 && finalWord_horizontal.Length != 1)
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
                            else if (finalWord_vertical.Contains(" ") && finalWord_vertical.Length > 1)
                            {
                                // on essaye de voir avec le jocker
                                // si le jocker est sur 2 mots il faut vérifier les 2 mots
                                string motJockerToo = string.Empty;
                                for (int i = 0; i < motsHorizontal.Count; i++)
                                {
                                    if (motsHorizontal[i].Length > 1)
                                        motJockerToo = motsHorizontal[i];
                                }

                                motsPossibles = CheckForJocker(finalWord_vertical, ref firstJokerLetterIndex, ref secondeJokerLetterIndex, motJockerToo);

                                if (!motsPossibles.Any())
                                {
                                    messages.Add("Le mot vertical " + finalWord_vertical.Replace(" ", "?") + " n'existe pas.");

                                    isWordGood = false;
                                }
                            }

                            motsVertical.Add(finalWord_vertical);

                            // Check si le mot complète bien un autre mot
                            if (((finalWord_vertical.Length == LettresSelectionnees.Length && GameBoard[7, 7].IsLetter))
                                && newGame.Round != 1 && (finalWord_vertical.Length != 1))
                            {
                                isCompletingAWord = false;

                            }

                        }

                        // si tous les mots ont une longueur de 1 == la lettre est seul quelque part, il doit être attaché à un autre
                        bool only = true;
                        for (int i = 0; i < motsHorizontal.Count; i++)
                        {
                            if (motsHorizontal[i].Length > 1)
                                only = false;
                        }
                        for (int i = 0; i < motsVertical.Count; i++)
                        {
                            if (motsVertical[i].Length > 1)
                                only = false;
                        }
                        if (only)
                            isCompletingAWord = false;
                    }

                    // isCompletingAWord bug donc double verif :
                    bool oneH = false;
                    bool oneV = false;

                    for (int i = 0; i < motsHorizontal.Count; i++)
                    {
                        if (motsHorizontal[i].Length > 1)
                            oneH = true;
                    }
                    for (int i = 0; i < motsVertical.Count; i++)
                    {
                        if (motsVertical[i].Length > 1)
                            oneV = true;
                    }
                    // si round > 1 et aucune piece encore posé
                    try
                    {
                        if (newGame.Round > 1 && (GameBoard[7, 7].Grid_Cell.Children[GameBoard[7, 7].Grid_Cell.Children.Count - 1] as UserControl_Lettre).gradientStop_contour.Color != (Color)ColorConverter.ConvertFromString(COLOR_LETTER_FIX))
                        {
                            isCompletingAWord = true;
                        }
                    }
                    catch
                    {
                        // aucune lettre à 7;7, c'est pas l'erreur de l'attachement mais de IsLetter qu'il faut
                        isCompletingAWord = true;
                    }

                    if (!isCompletingAWord && !(oneH && oneV))
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
                        // on demande quel mot est voulu
                        StackPanel_motVoulu.Children.Clear();

                        motsPossibles.ForEach(x => // pour chaque jocker possible
                        {
                            StackPanel_motVoulu.Children.Add(new Button() // on ajoute la possibilité dans un bouton
                            {
                                Margin = new Thickness(80, 0, 80, 0),
                                Height = 30,
                                FontSize = 16,
                                Content = x,

                            });

                            (StackPanel_motVoulu.Children[StackPanel_motVoulu.Children.Count - 1] as Button).Click += new RoutedEventHandler(JockerWordSelected); // lorsque cliqué on choisira le bon mot
                        });

                        if (motsPossibles.Count > 1) // plusieurs jocker possible on attend l'input
                            return;
                        else if (motsPossibles.Count == 1) // un seul jocker possible, on sait quel sera forcèment l'input
                            JockerWordSelected((StackPanel_motVoulu.Children[StackPanel_motVoulu.Children.Count - 1] as Button), null);



                        // couleur d'une lettre fixe
                        PosLettresSelectionnees.ForEach(coo =>
                        {
                            (GameBoard[coo[0], coo[1]].Grid_Cell.Children[GameBoard[coo[0], coo[1]].Grid_Cell.Children.Count - 1] as UserControl_Lettre).gradientStop_contour.Color = (Color)ColorConverter.ConvertFromString(COLOR_LETTER_FIX);
                        });


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
                        if (newGame.Pioche.Count >= toDelete.Count)
                        {
                            newGame.Players.First(x => x.Id == Id).Lettres.AddRange(newGame.Pioche.Take(toDelete.Count).ToList());
                            newGame.Pioche.RemoveRange(0, toDelete.Count);

                            // Ajoute les nouvelles lettre
                            foreach (UserControl_Lettre uc in wrapPanel_Lettre.Children)
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

                        TourSuivant(total);
                    }

                    ShowQuickMessage(messages);
                }
            }
            else
            {
                MessageBox.Show("Une connexion internet est requise pour jouer en ligne", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TourSuivant(int total = -1)
        {
            if (GitUtilities.CheckForInternetConnection())
            {
                // on ajoute son score
                if (total != -1)
                    newGame.Players.First(x => x.Id == Id).Score += total;
                bool nextPlayerIsPlaying = false;
                bool breakRightAfter = false;

            REDO:
                foreach (var item in stackPanel_players.Children)
                {
                    if (item is UserControl_Player)
                        if ((item as UserControl_Player).Id == Id && !breakRightAfter)
                        {
                            (item as UserControl_Player).gradientStop_selected.Color = (Color)ColorConverter.ConvertFromString(COLOR_TRANSPARENT); // enlève la couleur de séléction
                            (item as UserControl_Player).label_point.Content = newGame.Players.First(x => x.Id == Id).Score + " point(s)";
                            nextPlayerIsPlaying = true; // on met l'effet à la prochaine personne qui joue
                        }
                        else if (nextPlayerIsPlaying)
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


                // playerSuivant qui joue est
                if (newGame.WhoStart + 1 == newGame.MaxPlayer)
                    newGame.WhoStart = 0;
                else
                    newGame.WhoStart++;

                // new round
                newGame.Round++;

                // send les lettres à ajouté
                if (total != -1)
                    PosLettresSelectionnees.ForEach(co =>
                    {
                        newGame.GameBoard[co[0], co[1]] = GameBoard[co[0], co[1]].Letter + "," + ((SolidColorBrush)(GameBoard[co[0], co[1]].Grid_Cell.Children[GameBoard[co[0], co[1]].Grid_Cell.Children.Count - 1] as UserControl_Lettre).label_lettre.Foreground).Color.ToString();
                    });

                // et enfin envois le tous sur le git
                GitUtilities.ToGitghub(JsonConvert.SerializeObject(newGame), newGame.Players[0].Id + ".sg", false);

                // c'est plus notre tour, on patiente.
                MyTurn = false;
                button_Valider.Visibility = Visibility.Hidden;
                button_echangerLettre.Visibility = Visibility.Hidden;
                Button_PasserSonTour.Visibility = Visibility.Hidden;
                ActualRound++;
                Timer_whenDoIStart.Start();

                PosLettresSelectionnees.Clear();
                LettresSelectionnees = String.Empty;
                IsFixed = false;
                StackPanel_motVoulu.Children.Clear();
                Timer_MyTime.Stop();
                progressBar_timer.Value = 0;
            }
            else
            {
                MessageBox.Show("Une connexion internet est requise pour jouer en ligne", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void JockerWordSelected(object sender, RoutedEventArgs e)
        {
            StackPanel_motVoulu.Children.Clear();
            bool firstJockerPassed = false;
            PosLettresSelectionnees.ForEach(coordonees =>
            {
                // jocker
                if (String.IsNullOrWhiteSpace(GameBoard[coordonees[0], coordonees[1]].Letter))
                {
                    if (!firstJockerPassed)
                    {
                        GameBoard[coordonees[0], coordonees[1]].Letter = Char.ToString((sender as Button).Content.ToString()[firstJokerLetterIndex]);
                        (GameBoard[coordonees[0], coordonees[1]].Grid_Cell.Children[GameBoard[coordonees[0], coordonees[1]].Grid_Cell.Children.Count - 1] as UserControl_Lettre).label_lettre.Content = Char.ToString((sender as Button).Content.ToString()[firstJokerLetterIndex]);
                        (GameBoard[coordonees[0], coordonees[1]].Grid_Cell.Children[GameBoard[coordonees[0], coordonees[1]].Grid_Cell.Children.Count - 1] as UserControl_Lettre).label_lettre.Foreground = Brushes.Yellow;
                    }
                    else
                    {
                        GameBoard[coordonees[0], coordonees[1]].Letter = Char.ToString((sender as Button).Content.ToString()[secondeJokerLetterIndex]);
                        (GameBoard[coordonees[0], coordonees[1]].Grid_Cell.Children[GameBoard[coordonees[0], coordonees[1]].Grid_Cell.Children.Count - 1] as UserControl_Lettre).label_lettre.Content = Char.ToString((sender as Button).Content.ToString()[secondeJokerLetterIndex]);
                        (GameBoard[coordonees[0], coordonees[1]].Grid_Cell.Children[GameBoard[coordonees[0], coordonees[1]].Grid_Cell.Children.Count - 1] as UserControl_Lettre).label_lettre.Foreground = Brushes.Yellow;
                    }

                    firstJockerPassed = true;
                }
            });

            Button_ValiderSonTour_Click("goto", null);
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

        private static List<string> CheckForJocker(string word, ref int firstJokerLetter, ref int secondeJokerLetter, string secondWord)
        {
            List<string> limitedWords = Words.FindAll(x => x.Length == word.Length).ToList();
            List<string> motsPossibles = new List<string>();

            char[] alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
            int nbJoker = word.Count(x => x == ' ');
            firstJokerLetter = word.IndexOf(' ');
            secondeJokerLetter = word.LastIndexOf(' ');
            string motPossible;

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
                            motsPossibles.Add(motPossible);
                        }
                        else if (Words.Contains(motPossible) && Words.Contains(regex.Replace(secondWord, Char.ToString(alphabet[m]), 1))
                            && (Words.Contains(
                                regex.Replace(secondWord, 
                            Char.ToString(alphabet[m]), 1).Replace(' ', alphabet[m]))))
                        {
                            motsPossibles.Add(motPossible);
                        }
                    }
                }
                else
                {
                    // un seul jocker
                    motPossible = word.Replace(' ', alphabet[m]);
                    if (limitedWords.Contains(motPossible) && String.IsNullOrEmpty(secondWord))
                    {
                        motsPossibles.Add(motPossible);
                    }
                    else if (Words.Contains(motPossible) && Words.Contains(secondWord.Replace(' ', alphabet[m])))
                    {
                        motsPossibles.Add(motPossible);
                    }
                }
            }

            return motsPossibles;
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

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // s'enlève des fichiers github si il cherchait une game
            if (Timer_IsGameFull.Enabled)
            {
                List<GameFinder> gamesFinder = JsonConvert.DeserializeObject<List<GameFinder>>(await GitUtilities.FromGithub("games.sc"));
                foreach (GameFinder g in gamesFinder)
                    g.Players.RemoveAll(x => x.Equals(Pseudo + "," + Id));

                GitUtilities.ToGitghub(JsonConvert.SerializeObject(gamesFinder), "games.sc", false);
            }
        }

        private void Button_PasserSonTour_Click(object sender, RoutedEventArgs e)
        {
            if (MyTurn)
            {
                PosLettresSelectionnees.ForEach(pos =>
                {
                    GameBoard[pos[0], pos[1]].IsLetter = false;
                    GameBoard[pos[0], pos[1]].Letter = null;
                    GameBoard[pos[0], pos[1]].Grid_Cell.Children.RemoveAt(GameBoard[pos[0], pos[1]].Grid_Cell.Children.Count - 1);
                });

                TourSuivant();
            }
        }
    }
}
