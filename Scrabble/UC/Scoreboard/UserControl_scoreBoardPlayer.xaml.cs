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

namespace Scrabble.UC.Scoreboard
{
    /// <summary>
    /// Logique d'interaction pour UserControl_scoreBoardPlayer.xaml
    /// </summary>
    public partial class UserControl_scoreBoardPlayer : UserControl
    {
        public UserControl_scoreBoardPlayer(int index, Classes.Scrabble.Player x)
        {
            InitializeComponent();

            label_name.Content = x.Pseudo;
            label_point.Content = x.Score + " point";
            if (x.Score > 1)
                label_point.Content += "s";

            switch(index)
            {
                case 1:
                    img_gold.Visibility = Visibility.Visible;
                    break;
                case 2:
                    img_silver.Visibility = Visibility.Visible;
                    break;
                case 3:
                    img_bronze.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
