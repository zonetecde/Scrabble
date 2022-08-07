using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using Scrabble.Classes.GameBoard;
using Scrabble.Classes.Menu;
using Scrabble.Classes.Scrabble;
using Scrabble.UC.GameBoard;
using Scrabble.UC.Scoreboard;
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
		internal static int Id { get; set; } = 123_456_789;

		// le fichier de la game en cours
		private string GameUrl { get; set; }

		// Le timer pour détecter quand est-ce que la game est pleine pour la commencer
		private Timer Timer_IsGameFull { get; set; }

		// GameBoard
		public static UserControl_Cell[,] GameBoard { get; set; }

		// Game
		internal static Game newGame { get; set; }
		internal static List<char> MesLettres { get; set; }
		internal static bool MyTurn { get; set; } = false;
		public static string LettresSelectionnees { get; set; } = String.Empty;
		public static List<int[]> PosLettresSelectionnees { get; set; } = new List<int[]>();
		public static bool IsHorizontalPlacement { get; set; } = true;
		internal static bool IsFixed { get; set; } = false;
		internal static List<string> Words { get; set; }
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

			Grid_Login.Visibility = Visibility.Visible;
			//// à supp
			//Pseudo += rdn.Next(0, 9999);
			//Id = rdn.Next(100000000, 999999999);
			//Grid_Menu.Visibility = Visibility.Visible;
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
			PartiePrivée = false;
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

				int max = Convert.ToInt32((sender as Button).Content);
				label_loading.Text = string.Empty;

				// enlève les games qui datent de + de 2h
				gamesFinder.RemoveAll(x => (DateTime.Now - x.DateOfCreation).TotalHours > 2 );

				if (!PartiePrivée)
				{
					// Check si une partie est trouvé avec le nombre de joueur demandé et qui n'est pas complète et qui n'est pas une partie privé
					if (gamesFinder.Any(x => x.MaxPlayer == max && x.Players.Count < max && !x.IsPrivate))
					{
						// Une partie est trouvé, on s'ajoute dedans
						gamesFinder.Find(x => x.MaxPlayer == max && x.Players.Count < x.MaxPlayer && !x.IsPrivate)
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

						GitUtilities.DeleteFile(game.HostId + ".sg"); // si une game a deja existait sous ce nom
						Game game_added = new Game(new Player[max], max, Pseudo, Id);
						GitUtilities.ToGitghub(JsonConvert.SerializeObject(game_added), Id.ToString() + ".sg", true);

						GameUrl = game.HostId + ".sg";
					}
				}
                else
                {
					int joinCode = rdn.Next(100_000, 999999);
					label_loading.Text = "Code de la partie privée : \n" + joinCode.ToString().Insert(3, " ");
					gamesFinder.Add(new GameFinder(Pseudo, Id, max, new List<string>() { Pseudo + "," + Id }, true, joinCode));
					GitUtilities.ToGitghub(JsonConvert.SerializeObject(gamesFinder), "games.sc", false);
					
					Game game_added = new Game(new Player[max], max, Pseudo, Id);
					GitUtilities.ToGitghub(JsonConvert.SerializeObject(game_added), Id.ToString() + ".sg", true);
					GameUrl = Id + ".sg";
				}

				// Lance un timer qui va detecter toutes les x secondes si la game est complète
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

				if (newGame.Players.Count(x => !String.IsNullOrEmpty(x.Pseudo)) == newGame.MaxPlayer)
				{
					Timer_IsGameFull.Stop();

					// Supprime la game de games.sc
					List<GameFinder> gamesFinder = JsonConvert.DeserializeObject<List<GameFinder>>(await GitUtilities.FromGithub("games.sc"));
					gamesFinder.RemoveAll(x => x.Players.Any(y => y == Pseudo + "," + Id));
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

								if (x.Id.Equals(Id))
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
						GameUtilities.AfficherMesLettres(wrapPanel_gameBoard.Width / (double)15);

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
										if (!newGame.GameFinish)
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
										MyTurn = false;

										if(!newGame.GameFinish)
											Timer_whenDoIStart.Start();
									}
								}
								else
								{
									(item as UserControl_Player).gradientStop_selected.Color = (Color)ColorConverter.ConvertFromString(COLOR_TRANSPARENT);
								}

								i++;
							}
						}

						if(newGame.GameFinish)
                        {
							GameFinish();
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

        private void GameFinish()
        {
			stackPanel_ScoreBoard.Children.Clear();
			Grid_gameFinish.Visibility = Visibility.Visible;
			int index = 1;
			newGame.Players.ToList().OrderByDescending(x => x.Score).ToList().ForEach(x =>
			{
				stackPanel_ScoreBoard.Children.Add(new UserControl_scoreBoardPlayer(index, x));
				index++;
			});

			// delete the game after 10 seconds
			Grid_nbJoueur.IsEnabled = false;
			label_combienDeJoueur.Content = "Veuillez patientez un instant...";
			Timer t = new Timer(13000);
			t.Elapsed += (sender, e) =>
			{
				Dispatcher.Invoke(() =>
				{
					t.Stop();
					GitUtilities.DeleteFile(GameUrl);
					Grid_nbJoueur.IsEnabled = true;
					label_combienDeJoueur.Content = "Combien de joueurs ?";
				});
			};
			t.Start();
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
			// Valide la position actuelle des lettres

			if (GitUtilities.CheckForInternetConnection())
			{
				if ((IsFixed && MyTurn && PosLettresSelectionnees.Any()) || sender.Equals("goto")) // goto lorsque l'on clique sur un mot possible trouvé après une recherche de jocker.
				{
					bool isWordGood = true;
					List<string> messages = new List<string>();

					bool isCompletingAWord = true;
					List<string> motsHorizontal = new List<string>();
					List<string> motsVertical = new List<string>();
					List<string> motsPossibles = new List<string>(); ; // pour les jockers


					// voir le mot fait
					for (int indexLettreSelec = 0; indexLettreSelec < PosLettresSelectionnees.Count; indexLettreSelec++)
					{
						// le mot : vertical
						string finalWord_vertical = string.Empty;
						// le mot : horizontal
						string finalWord_horizontal = string.Empty;

						if (indexLettreSelec == 0) // pour que ça puisse passer le premier if
							motsHorizontal.Add("A");

						// ça sert à rien de faire toutes les lettres meme résultat partout si c'est un mot complet
						if (motsHorizontal.Last().Length == 1)
                        {
                            if (indexLettreSelec == 0)
                                motsHorizontal.Clear();

                            finalWord_horizontal = GameUtilities.MotHorizontalComplet(indexLettreSelec, finalWord_horizontal);

							// si le mot contient un jocker et que ce n'est toujours pas qu'une lettre seul
							if (finalWord_horizontal.Contains(" ") && finalWord_horizontal.Length > 1)
							{
								// on essaye de voir avec le jocker
								// si le jocker est sur 2 mots il faut vérifier les 2 mots
								string motJockerToo = string.Empty;
								for (int i = 0; i < motsVertical.Count; i++)
								{
									if (motsVertical[i].Length > 1)
										motJockerToo = motsVertical[i];
								}

								// liste les mots possible avec le jocker
								motsPossibles = GameUtilities.CheckForJocker(finalWord_horizontal, ref firstJokerLetterIndex, ref secondeJokerLetterIndex, motJockerToo);

								// si aucun mot existe avec ce jocker le mot est invalide
								if (!motsPossibles.Any())
								{
									messages.Add("Le mot horizontal " + finalWord_horizontal.Replace(" ", "?") + " n'existe pas.");

									isWordGood = false;
								}
							}

							motsHorizontal.Add(finalWord_horizontal);


                        }

                        if (indexLettreSelec == 0) // pour que ça puisse passer le premier if
							motsVertical.Add("A");

						if (motsVertical.Last().Length == 1)
                        {
                            if (indexLettreSelec == 0)
                                motsVertical.Clear();

                            finalWord_vertical = GameUtilities.MotVerticalComplet(indexLettreSelec, finalWord_vertical);

                            if (finalWord_vertical.Contains(" ") && finalWord_vertical.Length > 1)
                            {
                                // on essaye de voir avec le jocker
                                // si le jocker est sur 2 mots il faut vérifier les 2 mots
                                string motJockerToo = string.Empty;
                                for (int i = 0; i < motsHorizontal.Count; i++)
                                {
                                    if (motsHorizontal[i].Length > 1)
                                        motJockerToo = motsHorizontal[i];
                                }

                                motsPossibles = GameUtilities.CheckForJocker(finalWord_vertical, ref firstJokerLetterIndex, ref secondeJokerLetterIndex, motJockerToo);

                                if (!motsPossibles.Any())
                                {
                                    messages.Add("Le mot vertical " + finalWord_vertical.Replace(" ", "?") + " n'existe pas.");

                                    isWordGood = false;
                                }
                            }

                            motsVertical.Add(finalWord_vertical);

							// check si (horizontal & vertical)
							/*
							 - le mot à une longueure égale que les lettres séléctionner
							 - que le mot placé au milieu est bien une lettre
							 - qu'on est pas au premier round
							 - que ce n'est pas une lettre seul
							 */
							if ((finalWord_vertical.Length == LettresSelectionnees.Length && GameBoard[7, 7].IsLetter)
                                && newGame.Round != 1 && (finalWord_vertical.Length != 1)						
								||
								((finalWord_horizontal.Length == LettresSelectionnees.Length && GameBoard[7, 7].IsLetter)
								&& newGame.Round != 1 && finalWord_horizontal.Length != 1)
								)
                            {
                                isCompletingAWord = false;
                            }
						
							// ------------------------------
							// check si (pour horizontal & vertical)
							/*
							 - le mot est contenu dans le dico du scrabble
							 - le mot est plus grand qu'une lettre, sinon ce n'est pas un mot
							 - le mot ne contient pas de jocker
							 */
							if ((!Words.Contains(finalWord_horizontal) && finalWord_horizontal.Length > 1 && !finalWord_horizontal.Contains(" "))
								||
								(!Words.Contains(finalWord_vertical) && finalWord_vertical.Length > 1) && !finalWord_vertical.Contains(" ")

								)
							{
								string direction = (!Words.Contains(finalWord_horizontal) && finalWord_horizontal.Length > 1 && !finalWord_horizontal.Contains(" ")) ? "horizontal" : "vertical";
								messages.Add("Le mot " + direction + " " + finalWord_horizontal.Replace(" ", "?") + " n'existe pas.");

								isWordGood = false;
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

					// isCompletingAWord double verif pour cas spécial 
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

                        if (LettresSelectionnees.Length == 7) // si on fait un scrabble
                            total += 50;

                        // On compte les points.
                        int totalHorizontal = 0;

                        totalHorizontal = GameUtilities.CalculPoint(motsHorizontal, totalHorizontal);

                        // On compte les points.
                        int totalVertical = 0;

                        totalVertical = GameUtilities.CalculPoint(motsVertical, totalVertical);

                        total += (totalVertical + totalHorizontal);

                        // on remplace les jockers
                        // on demande quel mot est voulu
                        StackPanel_motVoulu.Children.Clear();

                        motsPossibles.Distinct().ToList().ForEach(x => // pour chaque jocker possible trouvé
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
                        else if (motsPossibles.Count == 1) // un seul jocker possible, on sait quel sera forcement l'input
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
								// et que la lettre est bien séléctionner
                                if (item.label_lettre.Content.ToString() == Char.ToString(x) 
								&& item.gradientStop_contour.Color == (Color)ColorConverter.ConvertFromString("#FFFFC78F") // vérifie que la lettre est bien selec
								&& !toDelete.Contains(item)) // il faut pas que sa supprime 2x le meme élément
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
						GameUtilities.AjouterLettresAuJoueur(toDelete.Count, wrapPanel_gameBoard.Width / (double)15);
                        
                        

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
				PASSETOUR:
				if (newGame.WhoStart + 1 == newGame.MaxPlayer)
					newGame.WhoStart = 0;
				else
					newGame.WhoStart++;

				List<int> joueurQuiOnGagne = Enumerable.Range(0, newGame.Players.ToList().Count)
					 .Where(i => newGame.Players.ToList()[i].Lettres.Count == 0)
					 .ToList();
				// calcul des scores si il reste qu'un seul joueur à qui il reste des lettres il arrete de jouer
				if (joueurQuiOnGagne.Count >= newGame.MaxPlayer - 1)
                {
					newGame.GameFinish = true;
                }
                else if(joueurQuiOnGagne.Any())
                {
					// on fait attention que ce ne soit pas un joueur qui a gagné qui ai le tour
					if (joueurQuiOnGagne.Contains(newGame.WhoStart))
					{
						goto PASSETOUR;
					}
				}

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

				PosLettresSelectionnees.Clear();
				LettresSelectionnees = String.Empty;
				IsFixed = false;
				StackPanel_motVoulu.Children.Clear();
				Timer_MyTime.Stop();
				progressBar_timer.Value = 0;

				if (!newGame.GameFinish)
					Timer_whenDoIStart.Start();
				else
				{
					GameFinish();
				}
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

		
		

		private void ShowQuickMessage(List<string> messages)
		{
			if (messages.Any()) // Affiche les messages au dessus du GameBoard pour avertir le joueur de quelque chsose
			{
				border_Message.Visibility = Visibility.Visible; // bordure
				label_message.Text = string.Join("\n", messages); // les messages

				Timer t = new Timer(5000); // vont s'afficher  secondes
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
				GameUtilities.DeleteTempLetterFromGameBoard();

				TourSuivant();
			}
		}

		private void button_echangerLettre_Click(object sender, RoutedEventArgs e)
		{
			// échange les lettres séléctionner
			if (newGame.Pioche.Any())
			{
				GameUtilities.SwitchLetter();

				// Affiche les nouvelles lettres
				GameUtilities.AfficherMesLettres(wrapPanel_gameBoard.Width / (double)15);

				TourSuivant();
			}
			else
            {
				ShowQuickMessage(new List<string>()
				{
					"Il n'y a plus de lettre dans la pioche!"
				});
            }
		}

        private void Button_EnregistrerLePlateau_Click(object sender, RoutedEventArgs e)
        {
			var image = GameUtilities.CreateBitmapSourceFromVisual(wrapPanel_gameBoard.Width, wrapPanel_gameBoard.Height, wrapPanel_gameBoard, false);

			var dialog = new CommonOpenFileDialog();
			dialog.IsFolderPicker = true;
			CommonFileDialogResult result = dialog.ShowDialog();
			
			if(result == CommonFileDialogResult.Ok)
            {
				using (var fileStream = new FileStream(dialog.FileName + @"\plateau " + DateTime.Now.ToString("dd MM yyyy") +".png", FileMode.Create))
				{
					BitmapEncoder encoder = new PngBitmapEncoder();
					encoder.Frames.Add(BitmapFrame.Create(image));
					encoder.Save(fileStream);

					MessageBox.Show("Le plateau a été enregistré!", "Information");
				}
			}


        }

        private void Button_RetournerAuMenu_Click(object sender, RoutedEventArgs e)
        {
			GameBoard = null;
			Grid_gameFinish.Visibility = Visibility.Hidden;
			Grid_nbJoueur.Visibility = Visibility.Hidden;
			Grid_Menu.Visibility = Visibility.Visible;
			wrapPanel_gameBoard.Children.Clear();
			wrapPanel_Lettre.Children.Clear();
			ActualRound = 0;
			Words.Clear();
			MesLettres = null;
			GameUrl = null;
			MyTurn = false;
			IsFixed = false;
			PosLettresSelectionnees = new List<int[]>(); 
			LettresSelectionnees = string.Empty;
			button_echangerLettre.Visibility = Visibility.Visible;
			button_Valider.Visibility = Visibility.Visible;
			Button_PasserSonTour.Visibility = Visibility.Visible;

			List<UIElement> toR = new List<UIElement>();
            foreach (var item in stackPanel_players.Children)
            
				if (item is UserControl_Player)
					toR.Add(item as UserControl_Player);
			toR.ForEach(x => { stackPanel_players.Children.Remove(x); });

		}

		private bool PartiePrivée = false;

        private void Button_PrivateGame_Click(object sender, RoutedEventArgs e)
        {
			Grid_partiePrivée.Visibility = Visibility.Visible;
			PartiePrivée = true;
		}

		private void Button_CréerPartiePrivé_Click(object sender, RoutedEventArgs e)
        {
			Grid_nbJoueur.Visibility = Visibility.Visible;
        }

        private async void Button_RejoindrePartiePrivée_Click(object sender, RoutedEventArgs e)
        {
			try
			{
				List<GameFinder> gamesFinder = JsonConvert.DeserializeObject<List<GameFinder>>(await GitUtilities.FromGithub("games.sc"));
				if (gamesFinder.Any(x => x.JoinCode == Convert.ToInt32(textBox_joinCode.Text.Replace(" ", String.Empty))))
				{
					Grid_Loading.Visibility = Visibility.Visible;
					label_loading.Text = "Connexion...";
					int index = gamesFinder.FindIndex(x => x.JoinCode == Convert.ToInt32(textBox_joinCode.Text.Replace(" ", String.Empty)));
					gamesFinder[index].Players.Add(Pseudo + "," + Id);
					GameUrl = gamesFinder[index].HostId + ".sg";

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

					Timer_IsGameFull.Start();
				}
				else
				{
					textBox_joinCode.BorderBrush = Brushes.Red;
				}
			}
            catch {
				textBox_joinCode.BorderBrush = Brushes.Red;
			}
		}

		private void TextBox_CodePartiePrivée_KeyDown(object sender, KeyEventArgs e)
        {
			if (e.Key == Key.Enter)
				Button_RejoindrePartiePrivée_Click(this, null);
        }
    }
}
