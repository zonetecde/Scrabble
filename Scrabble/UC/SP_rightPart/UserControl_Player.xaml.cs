using Scrabble.Classes.Scrabble;
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

namespace Scrabble.UC.SP_rightPart
{
    /// <summary>
    /// Logique d'interaction pour UserControl_Player.xaml
    /// </summary>
    public partial class UserControl_Player : UserControl
    {
        public UserControl_Player()
        {
            InitializeComponent();
        }

        internal int Id { get; set; }

        public UserControl_Player(Player x)
        {
            InitializeComponent();

            Id = x.Id;
            try
            {
                label_nom.Content = x.Pseudo.Split(',')[0];
            }
            catch { }
        }
    }
}
