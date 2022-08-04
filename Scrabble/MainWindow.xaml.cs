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

namespace Scrabble
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Random rdn = new Random();

        private string Pseudo { get; set; }
        private int Id { get; set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Change la taille du gameBoard, les cases ne sont pas ajouté au lancement
            gameBoard.Width = e.NewSize.Width / (double)2;
            gameBoard.Height = e.NewSize.Width / (double)2;

            stackPanel_leftPart.Width = (e.NewSize.Width - gameBoard.Width) / 2 - 20;
            stackPanel_rightPart.Width = (e.NewSize.Width - gameBoard.Width) / 2 - 20;
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
            // Trouve une partie. Si aucune partie existente, créer une partie en attendant que quelqu'un la rejoigne.
            GitUtilities.ToGitghub("coucou", GitUtilities.githubToken, "zonetecde", "scrabble_webApi", "master", "games.sc");
        }
    }
}
