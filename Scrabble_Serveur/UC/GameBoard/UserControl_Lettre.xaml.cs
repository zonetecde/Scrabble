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
    /// Logique d'interaction pour UserControl_Lettre.xaml
    /// </summary>
    public partial class UserControl_Lettre : UserControl
    {
        int Score { get; set; }
        internal bool IsSelected { get; set; } = false;
        public bool IsTemp { get; }

        public UserControl_Lettre(char x, bool isTemp)
        {
            InitializeComponent();

            IsTemp = isTemp;

            label_lettre.Content = Char.ToString(x);
            Score = Convert.ToInt32(Properties.Settings.Default.letterScore.Split(';').ToList().Find(y => y.Split(',')[0].ToList().Contains(Convert.ToChar(x))).Split(',')[1]);
            label_score.Content = Score;

            if (x == '°')
            {
                label_lettre.Content = " ";
            }

            this.Cursor = MainWindow.Grab;
            if (isTemp)
                this.Cursor = Cursors.Arrow;
        }

        internal bool isGrabbing = false;
        private bool mouseHavedMove = false;

        private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // is grabbing
            isGrabbing = true;
            mouseHavedMove = false;
            this.Cursor = MainWindow.Grabbing;

        }

        private void UserControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // is not grabbing
            isGrabbing = false;
            this.Cursor = MainWindow.Grab;

            // veut séléctionner non pas bouger
            if (!mouseHavedMove && !IsTemp && this.gradientStop_contour.Color != (Color)ColorConverter.ConvertFromString(MainWindow.COLOR_LETTER_FIX))
            {
                // séléctionne / déselectionne la lettre
                gradientStop_contour.Color = gradientStop_contour.Color == (Color)ColorConverter.ConvertFromString("#FFFFC78F") ? gradientStop_contour.Color = (Color)ColorConverter.ConvertFromString("#FFEEE5FF") : gradientStop_contour.Color = (Color)ColorConverter.ConvertFromString("#FFFFC78F");

                IsSelected = !IsSelected;

                // enlève les propositions vu qu'on change les lettres
                MainWindow.stackPanel_motVoulu.Children.Clear();

                // un changement de lettre alors aussi le changement dans les lettres posés sur le gameBoard
                bool letter = false;
                MainWindow.IsFixed = false;

                int[] firstPos = new int[2];
                for (int i = 0; i < MainWindow.PosLettresSelectionnees.Count; i++)
                {
                    if (MainWindow.GameBoard[MainWindow.PosLettresSelectionnees[i][0], MainWindow.PosLettresSelectionnees[i][1]].IsLetter)
                    {
                        // si c'est une lettre donc qui fut fixé
                        letter = true;
                        firstPos = MainWindow.PosLettresSelectionnees[0];
                        MainWindow.GameBoard[MainWindow.PosLettresSelectionnees[i][0], MainWindow.PosLettresSelectionnees[i][1]].IsLetter = false;
                        MainWindow.GameBoard[MainWindow.PosLettresSelectionnees[i][0], MainWindow.PosLettresSelectionnees[i][1]].Letter = null;
                        MainWindow.GameBoard[MainWindow.PosLettresSelectionnees[i][0], MainWindow.PosLettresSelectionnees[i][1]].Grid_Cell.Children.RemoveAt
                            (MainWindow.GameBoard[MainWindow.PosLettresSelectionnees[i][0], MainWindow.PosLettresSelectionnees[i][1]].Grid_Cell.Children.Count - 1);
                    }
                }

                // trigger 
                if (letter)
                {
                    MainWindow.LettresSelectionnees = string.Empty;

                    foreach (UserControl_Lettre item in MainWindow.WrapPanel_Lettres.Children)
                    {
                        if (item.IsSelected)
                        {
                            MainWindow.LettresSelectionnees += item.label_lettre.Content.ToString() == string.Empty ? " " : item.label_lettre.Content.ToString();
                        }
                    }

                    if (!String.IsNullOrEmpty(MainWindow.LettresSelectionnees))
                    {
                        // trigger comme si souris sur pos1
                        MainWindow.GameBoard[firstPos[0], firstPos[1]].UserControl_MouseEnter(this, null);
                        MainWindow.GameBoard[firstPos[0], firstPos[1]].UserControl_MouseDown("let", e);
                    }

                }
            }
        }

        double currentPositionX = 0;
        int direction = 0;

        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            // take la direction
            if (isGrabbing && !IsTemp)
            {
                double deltaDirection = currentPositionX - e.GetPosition(this).X;
                direction = deltaDirection > 0 ? 1 : -1;
                currentPositionX = e.GetPosition(this).X;

                // il ne veut pas séléctionner la pièce mais la bouger
                mouseHavedMove = true;
            }
            else
                currentPositionX = e.GetPosition(this).X;

        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            // interchange avec l'autre lettre leur place
            if (isGrabbing && gradientStop_contour.Color != (Color)ColorConverter.ConvertFromString(MainWindow.COLOR_LETTER_FIX))
            {
                if (direction == 1)
                {
                    int i = MainWindow.WrapPanel_Lettres.Children.IndexOf(this);
                    if (i > 0)
                    {
                        MainWindow.WrapPanel_Lettres.Children.Remove(this);

                        MainWindow.WrapPanel_Lettres.Children.Insert(i - 1, this);
                    }
                }
                else if (direction == -1)
                {
                    int i = MainWindow.WrapPanel_Lettres.Children.IndexOf(this);
                    if (i < MainWindow.WrapPanel_Lettres.Children.Count - 1)
                    {
                        MainWindow.WrapPanel_Lettres.Children.Remove(this);

                        MainWindow.WrapPanel_Lettres.Children.Insert(i + 1, this);
                    }
                }
            }

        }
    }
}
