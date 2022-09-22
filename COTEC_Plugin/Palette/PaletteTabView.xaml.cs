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

namespace COTEC_Plugin.PaletteWpf
{
    /// <summary>
    /// Logique d'interaction pour PaletteTabView.xaml
    /// </summary>
    public partial class PaletteTabView : UserControl
    {
        public PaletteTabView()
        {
            InitializeComponent();

            // définit la liaison de données avec la partie ViewModel
            DataContext = new PaletteTabViewModel();
        }
    }
}

