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

// 13 tall
// 13 wide
namespace EE356P1mfr
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            /*-- This function is the entry point for my logic and own subroutines
             *-- just so that it's easier to organize and read my own code.  -rienzo --*/
            CustomEntry ProgramMain = new CustomEntry();

            ProgramMain.start();

            // Really just adding this comment to test how the GitHub plugin for VisualStudio
            // handles dates/commit authors/etc.  !!! This was labeled as the `Sysfunc probe'
            // during commit/push on 8/23/2018.  -rienzo

            /*-- End ourselves --*/
            Application.Current.Shutdown(ReturnStatusCode);
        }
    }
}
