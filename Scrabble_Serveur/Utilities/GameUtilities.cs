using Scrabble_Serveur.Classes.GameBoard;
using Scrabble_Serveur.UC.GameBoard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Scrabble_Serveur.Utilities
{
    internal class GameUtilities
    {
        internal static void SwitchLetter()
        {
            MainWindow.newGame.Pioche.AddRange(MainWindow.LettresSelectionnees.ToList()); // ajoute les lettres à changer à la pioche
            MainWindow.newGame.Pioche = MainWindow.newGame.Pioche.OrderBy(x => MainWindow.rdn.Next()).ToList(); // mélange la pioche
            MainWindow.LettresSelectionnees.ToList().ForEach(x =>
            {
                MainWindow.newGame.Players.ToList().Find(x => x.Id == MainWindow.Id).Lettres.Remove(x);  // enlève les lettres que le joueur veut changer
            });
            MainWindow.newGame.Players.ToList().Find(x => x.Id == MainWindow.Id).Lettres.AddRange(MainWindow.newGame.Pioche.Take(MainWindow.LettresSelectionnees.Length).ToList()); // ajoute les nouvelles lettre random
            MainWindow.newGame.Pioche.RemoveRange(0, MainWindow.LettresSelectionnees.Length);
        }

        internal static void AfficherMesLettres(double size)
        {
            MainWindow.WrapPanel_Lettres.Children.Clear();
            MainWindow.MesLettres = MainWindow.newGame.Players.ToList().Find(x => x.Id == MainWindow.Id).Lettres;
            MainWindow.MesLettres.ForEach(x =>
            {
                MainWindow.WrapPanel_Lettres.Children.Add(new UserControl_Lettre(x, false)
                {
                    Width = size,
                    Height = size
                });
            });
        }

        internal static void DeleteTempLetterFromGameBoard()
        {
            MainWindow.PosLettresSelectionnees.ForEach(pos =>
            {
                MainWindow.GameBoard[pos[0], pos[1]].IsLetter = false;
                MainWindow.GameBoard[pos[0], pos[1]].Letter = null;
                MainWindow.GameBoard[pos[0], pos[1]].Grid_Cell.Children.RemoveAt(MainWindow.GameBoard[pos[0], pos[1]].Grid_Cell.Children.Count - 1);
            });
        }

        internal static List<string> CheckForJocker(string word, ref int firstJokerLetterIndex, ref int secondeJokerLetterIndex, string secondWord)
        {
            List<string> limitedWords = MainWindow.Words.FindAll(x => x.Length == word.Length).ToList();
            List<string> motsPossibles = new List<string>();

            char[] alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
            int nbJoker = word.Count(x => x == ' ');
            firstJokerLetterIndex = word.IndexOf(' ');
            secondeJokerLetterIndex = word.LastIndexOf(' ');
            string motPossible;

            for (int m = 0; m < 26; m++) // les 26 lettres de l'alphabet
            {
                if (nbJoker == 2)
                {
                    for (int j = 0; j < 26; j++) // les 26 lettres de l'alphabet (2 jockers)
                    {
                        // 2 jocker dans le mot à trouvé
                        motPossible = word;
                        var regex = new Regex(Regex.Escape(" ")); // on remplace le premier jocker
                        motPossible = regex.Replace(motPossible, char.ToString(alphabet[m]), 1); // on remplace le premier jocker

                        motPossible = motPossible.Replace(' ', alphabet[j]); // on remplace le deuxieme jocker

                        try
                        {
                            if (limitedWords.Contains(motPossible) && string.IsNullOrEmpty(secondWord)) // si le mot existe
                            {
                                motsPossibles.Add(motPossible);
                            }
                            else if (MainWindow.Words.Contains(motPossible) && MainWindow.Words.Contains(regex.Replace(secondWord, char.ToString(alphabet[m]), 1))
                                && MainWindow.Words.Contains(
                                    regex.Replace(secondWord,
                                char.ToString(alphabet[m]), 1).Replace(' ', alphabet[m]))) // si tous les mots verticalement horizontalement sont good
                            {
                                motsPossibles.Add(motPossible);
                            }
                        }
                        catch
                        {
                            // erreur jocker - on affiche motsPossibles avec ce qu'on a réussi à trouver
                        }
                    }
                }
                else
                {
                    // un seul jocker
                    motPossible = word.Replace(' ', alphabet[m]); // remplace le jocker par une lettre
                    if (limitedWords.Contains(motPossible) && string.IsNullOrEmpty(secondWord)) // si le mot existe
                    {
                        motsPossibles.Add(motPossible);
                    }
                    else if (MainWindow.Words.Contains(motPossible) && MainWindow.Words.Contains(secondWord.Replace(' ', alphabet[m]))) // vérifie pour les mots verticalement et horizontalement que le mot est possible
                    {
                        motsPossibles.Add(motPossible);
                    }
                }
            }

            return motsPossibles;
        }

        internal static string MotHorizontalComplet(int indexLettreSelec, string finalWord_horizontal)
        {
            // on prend toutes les lettres déjà présent à gauche du mot au plus possible
            for (int i = 1; i < 15; i++) // voir toutes les lettres présent à gauche de la première lettre ajouté           
                if (MainWindow.PosLettresSelectionnees[indexLettreSelec][1] - i >= 0) // si c'est dans les limites du tableau
                    if (MainWindow.GameBoard[MainWindow.PosLettresSelectionnees[indexLettreSelec][0], MainWindow.PosLettresSelectionnees[indexLettreSelec][1] - i].IsLetter) // si la cellule est une lettre      
                        finalWord_horizontal = finalWord_horizontal.Insert(0, MainWindow.GameBoard[MainWindow.PosLettresSelectionnees[indexLettreSelec][0], MainWindow.PosLettresSelectionnees[indexLettreSelec][1] - i].Letter); // ajoute la lettre au début du mot
                    else // ce n'est plus une lettre alors on doit arrêter la boucle, le mot est fini
                        break;

            // on prend toutes les lettres déjà présent et nouvelle à à droite du mot au plus possible dont la lettre de la cellule actuelle
            for (int i = 0; i < 15; i++) // voir toutes les lettres présent à gauche de la première lettre ajouté           
                if (MainWindow.PosLettresSelectionnees[indexLettreSelec][1] + i <= 14) // si c'est dans les limites du tableau
                    if (MainWindow.GameBoard[MainWindow.PosLettresSelectionnees[indexLettreSelec][0], MainWindow.PosLettresSelectionnees[indexLettreSelec][1] + i].IsLetter) // si la cellule est une lettre      
                        finalWord_horizontal += MainWindow.GameBoard[MainWindow.PosLettresSelectionnees[indexLettreSelec][0], MainWindow.PosLettresSelectionnees[indexLettreSelec][1] + i].Letter; // ajoute la lettre au début du mot
                    else // ce n'est plus une lettre alors on doit arrêter la boucle, le mot est fini
                        break;
            return finalWord_horizontal;
        }

        internal static string MotVerticalComplet(int indexLettreSelec, string finalWord_vertical)
        {
            // on prend toutes les lettres déjà présent à gauche du mot au plus possible
            for (int i = 1; i < 15; i++) // voir toutes les lettres présent à gauche de la première lettre ajouté           
                if (MainWindow.PosLettresSelectionnees[indexLettreSelec][0] - i >= 0) // si c'est dans les limites du tableau
                    if (MainWindow.GameBoard[MainWindow.PosLettresSelectionnees[indexLettreSelec][0] - i, MainWindow.PosLettresSelectionnees[indexLettreSelec][1]].IsLetter) // si la cellule est une lettre      
                        finalWord_vertical = finalWord_vertical.Insert(0, MainWindow.GameBoard[MainWindow.PosLettresSelectionnees[indexLettreSelec][0] - i, MainWindow.PosLettresSelectionnees[indexLettreSelec][1]].Letter); // ajoute la lettre au début du mot
                    else // ce n'est plus une lettre alors on doit arrêter la boucle, le mot est fini
                        break;

            // on prend toutes les lettres déjà présent et nouvelle à à droite du mot au plus possible dont la lettre de la cellule actuelle
            for (int i = 0; i < 15; i++) // voir toutes les lettres présent à gauche de la première lettre ajouté           
                if (MainWindow.PosLettresSelectionnees[indexLettreSelec][0] + i <= 14) // si c'est dans les limites du tableau
                    if (MainWindow.GameBoard[MainWindow.PosLettresSelectionnees[indexLettreSelec][0] + i, MainWindow.PosLettresSelectionnees[indexLettreSelec][1]].IsLetter) // si la cellule est une lettre      
                        finalWord_vertical += MainWindow.GameBoard[MainWindow.PosLettresSelectionnees[indexLettreSelec][0] + i, MainWindow.PosLettresSelectionnees[indexLettreSelec][1]].Letter; // ajoute la lettre au début du mot
                    else // ce n'est plus une lettre alors on doit arrêter la boucle, le mot est fini
                        break;
            return finalWord_vertical;
        }

        internal static int CalculPoint(List<string> mots, int total)
        {
            for (int index = 0; index < mots.Count; index++)
            {
                if (mots[index].Length > 1)
                {
                    int totalHorizontalDoubleWord = 0;
                    int totalHorizontalTripleWord = 0;

                    for (int i = 1; i < 15; i++)
                        if (MainWindow.PosLettresSelectionnees[index][1] - i >= 0)
                            if (MainWindow.GameBoard[MainWindow.PosLettresSelectionnees[index][0], MainWindow.PosLettresSelectionnees[index][1] - i].IsLetter)
                            {
                                total += CalculateScore(MainWindow.PosLettresSelectionnees[index][0], MainWindow.PosLettresSelectionnees[index][1] - i, ref totalHorizontalDoubleWord, ref totalHorizontalTripleWord);
                            }
                            else
                                break;

                    for (int i = 0; i < 15; i++)
                        if (MainWindow.PosLettresSelectionnees[index][1] + i <= 14)
                            if (MainWindow.GameBoard[MainWindow.PosLettresSelectionnees[index][0], MainWindow.PosLettresSelectionnees[index][1] + i].IsLetter)
                            {
                                total += CalculateScore(MainWindow.PosLettresSelectionnees[index][0], MainWindow.PosLettresSelectionnees[index][1] + i, ref totalHorizontalDoubleWord, ref totalHorizontalTripleWord);
                            }
                            else
                                break;

                    for (int i = 1; i <= totalHorizontalDoubleWord; i++)
                    {
                        total = total * 2;
                    }
                    for (int i = 1; i <= totalHorizontalTripleWord; i++)
                    {
                        total = total * 3;
                    }

                }
            }

            return total;
        }

        private static int CalculateScore(int y, int x, ref int doubleWord, ref int tripleWord)
        {
            int letterScore = 0;
            if (Convert.ToInt32((MainWindow.GameBoard[y, x].Grid_Cell.Children[MainWindow.GameBoard[y, x].Grid_Cell.Children.Count - 1] as UserControl_Lettre).label_score.Content) != 0) // c'est un jocker avec 0 points
                letterScore = Convert.ToInt32(Properties.Settings.Default.letterScore.Split(';').ToList().Find(j => j.Split(',')[0].ToList().Contains(Convert.ToChar(MainWindow.GameBoard[y, x].Letter))).Split(',')[1]);

            // on regarde si la case n'a pas un bonus 
            if (MainWindow.PosLettresSelectionnees.Any(j => j[0] == y && j[1] == x))
            {
                switch (MainWindow.GameBoard[y, x].ScrabbleColor)
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


        internal static void AjouterLettresAuJoueur(int toAdd, double size)
        {
            if (MainWindow.newGame.Pioche.Count >= toAdd)
            {
                MainWindow.newGame.Players.First(x => x.Id == MainWindow.Id).Lettres.AddRange(MainWindow.newGame.Pioche.Take(toAdd).ToList());
                MainWindow.newGame.Pioche.RemoveRange(0, toAdd);

                // Ajoute les nouvelles lettre
                foreach (UserControl_Lettre uc in MainWindow.WrapPanel_Lettres.Children)
                {
                    uc.isGrabbing = false;
                    uc.IsSelected = false;
                    uc.gradientStop_contour.Color = (Color)ColorConverter.ConvertFromString("#FFEEE5FF");
                }
                for (int i = 7 - toAdd; i < 7; i++)
                {
                    MainWindow.WrapPanel_Lettres.Children.Add(new UserControl_Lettre(MainWindow.newGame.Players.First(x => x.Id == MainWindow.Id).Lettres[i], false)
                    {
                        Width = size,
                        Height = size
                    });
                }
            }
            else
            {
                try
                {
                    MainWindow.newGame.Players.First(x => x.Id == MainWindow.Id).Lettres.AddRange(MainWindow.newGame.Pioche.Take(MainWindow.newGame.Pioche.Count).ToList());
                    MainWindow.newGame.Pioche.RemoveRange(0, MainWindow.newGame.Pioche.Count);
                }
                catch
                {
                    // il n'y a plus aucune pièce dans la pioche
                }
            }
        }

        internal static BitmapSource CreateBitmapSourceFromVisual(
                double width,
                double height,
                Visual visualToRender,
                bool undoTransformation)
        {
            if (visualToRender == null)
            {
                return null;
            }
            RenderTargetBitmap bmp = new RenderTargetBitmap((int)Math.Ceiling(width),
                (int)Math.Ceiling(height), 96, 96, PixelFormats.Pbgra32);

            if (undoTransformation)
            {
                DrawingVisual dv = new DrawingVisual();
                using (DrawingContext dc = dv.RenderOpen())
                {
                    VisualBrush vb = new VisualBrush(visualToRender);
                    dc.DrawRectangle(vb, null, new Rect(new Point(), new Size(width, height)));
                }
                bmp.Render(dv);
            }
            else
            {
                bmp.Render(visualToRender);
            }
            return bmp;
        }
    }
}