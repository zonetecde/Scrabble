using Scrabble_Serveur.Classes.GameBoard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Scrabble_Serveur.UC.GameBoard
{
    /// <summary>
    /// Logique d'interaction pour UserControl_Cell.xaml
    /// </summary>
    public partial class UserControl_Cell : UserControl
    {
        internal ScrabbleColor ScrabbleColor { get; set; } = ScrabbleColor.NORMAL;
        private int X { get; set; }
        private int Y { get; set; }
        internal bool IsLetter { get; set; } = false;
        internal string Letter { get; set; }

        public UserControl_Cell(int x, int y)
        {
            InitializeComponent();
            X = x;
            Y = y;
        }

        internal void MakeIt(ScrabbleColor color)
        {
            // De base la textBlock n'est pas là
            if (color != ScrabbleColor.CENTER)
                textblock_bonus.Visibility = Visibility.Visible;

            ScrabbleColor = color;

            switch (color)
            {
                case ScrabbleColor.RED:
                    this.Background = Brushes.LightCoral;
                    textblock_bonus.Text = "TRIPLE\nWORD\nSCORE";
                    break;
                case ScrabbleColor.PINK:
                    this.Background = Brushes.Pink;
                    textblock_bonus.Text = "DOUBLE\nWORD\nSCORE";
                    break;
                case ScrabbleColor.LIGHT_BLUE:
                    this.Background = Brushes.LightBlue;
                    textblock_bonus.Text = "DOUBLE\nLETTER\nSCORE";
                    break;
                case ScrabbleColor.BLUE:
                    this.Background = Brushes.SteelBlue;
                    textblock_bonus.Text = "TRIPLE\nLETTER\nSCORE";
                    break;
                case ScrabbleColor.CENTER:
                    this.Background = Brushes.Pink;
                    img_blackStar.Visibility = Visibility.Visible;
                    break;
            }
        }

        internal void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            // MouseEnter : si c'est MainWindow.MyTurn alors on doit placer les lettres selectionner sur le GameBoard, en commençant par cette case
            if (MainWindow.MyTurn && !MainWindow.IsFixed)
            {
                // enlève toutes les anciennes lettres placé
                MainWindow.PosLettresSelectionnees.ForEach(x =>
                {
                    try
                    {
                        if (!((MainWindow.GameBoard[x[0], x[1]].IsLetter)))
                            MainWindow.GameBoard[x[0], x[1]].Grid_Cell.Children.RemoveAt(
                                 MainWindow.GameBoard[x[0], x[1]].Grid_Cell.Children.Count - 1);
                    }
                    catch
                    {

                    }
                });

                // ajoute les lettres
                MainWindow.PosLettresSelectionnees.Clear();

                if (!MainWindow.IsHorizontalPlacement)
                {
                    int x = X;
                    MainWindow.LettresSelectionnees.ToList().ForEach(v =>
                    {
                        try
                        {
                        // pour pas que ça empiète sur d'autre lettres
                        REDO:
                            if (MainWindow.GameBoard[x, Y].IsLetter)
                            {
                                x++;
                                goto REDO;
                            }
                            else
                            {
                                MainWindow.GameBoard[x, Y].Grid_Cell.Children.Add(new UserControl_Lettre(v, true)
                                {
                                    Width = MainWindow.GameBoard[x, Y].ActualWidth,
                                    Height = MainWindow.GameBoard[x, Y].ActualWidth
                                });
                                MainWindow.PosLettresSelectionnees.Add(new int[] { x, Y });
                                x++;
                            }
                        }
                        catch
                        {

                        }
                    });
                }
                else
                {
                    int y = Y;
                    MainWindow.LettresSelectionnees.ToList().ForEach(v =>
                    {

                        try
                        {
                        // pour pas que ça empiète sur d'autre lettres
                        REDO:
                            if (MainWindow.GameBoard[X, y].IsLetter)
                            {
                                y++;
                                goto REDO;
                            }
                            else
                            {
                                MainWindow.GameBoard[X, y].Grid_Cell.Children.Add(new UserControl_Lettre(v, true)
                                {
                                    Width = MainWindow.GameBoard[X, y].ActualWidth,
                                    Height = MainWindow.GameBoard[X, y].ActualWidth
                                });
                                MainWindow.PosLettresSelectionnees.Add(new int[] { X, y });
                                y++;
                            }
                        }
                        catch
                        {

                        }
                    });
                }

            }
        }

        internal void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed && MainWindow.MyTurn)
            {
                MainWindow.IsHorizontalPlacement = !MainWindow.IsHorizontalPlacement;
                UserControl_MouseEnter(this, null);
            }
            else if ((e.LeftButton == MouseButtonState.Pressed || sender.Equals("let")) && MainWindow.MyTurn) // = let lorsque on choisit des lettres alors que le mot est déjà sur la grille
            {
                // valide le mot
                // prend la pos de chaque lettre placé
                MainWindow.PosLettresSelectionnees.ToList().ForEach(v =>
                {
                    UserControl_Lettre ucLettre = (MainWindow.GameBoard[v[0], v[1]]).Grid_Cell.Children[(MainWindow.GameBoard[v[0], v[1]]).Grid_Cell.Children.Count - 1] as UserControl_Lettre;
                    // change sa couleur
                    ucLettre.gradientStop_contour.Color = ucLettre.gradientStop_contour.Color == (Color)ColorConverter.ConvertFromString("#FFFBEBFF") ? (Color)ColorConverter.ConvertFromString("#FFEEE5FF") : (Color)ColorConverter.ConvertFromString("#FFFBEBFF");
                    // dit que c'est une lettre fixe
                    MainWindow.GameBoard[v[0], v[1]].IsLetter = !MainWindow.GameBoard[v[0], v[1]].IsLetter;
                    // dit c'est quel lettre
                    MainWindow.GameBoard[v[0], v[1]].Letter = ucLettre.label_lettre.Content.ToString();

                });

                MainWindow.IsFixed = !MainWindow.IsFixed;
                Letter = (Grid_Cell.Children[Grid_Cell.Children.Count - 1] as UserControl_Lettre).label_lettre.Content.ToString();
            }
        }
    }
}
