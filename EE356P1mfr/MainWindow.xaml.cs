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
using Microsoft.Win32;

// 13 tall
// 13 wide
namespace EE356P1mfr
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CustomEntry ProgramMain;
        private int ReturnStatusCode = 0xff;
        public MainWindow()
        {
            InitializeComponent();
            

            // Really just adding this comment to test how the GitHub plugin for VisualStudio
            // handles dates/commit authors/etc.  !!! This was labeled as the `Sysfunc probe'
            // during commit/push on 8/23/2018.  -rienzo

            /*-- End ourselves --*/
            Application.Current.Shutdown(this.ReturnStatusCode);
        }


        private void btnImgLoad_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnuFileOpen_Click(object sender, RoutedEventArgs e)
        { }

        private void mnuFileSave_Click(object sender, RoutedEventArgs e)
        { }

        private void mnuFileClose_Click(object sender, RoutedEventArgs e)
        { }

        private void cmboFonts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
