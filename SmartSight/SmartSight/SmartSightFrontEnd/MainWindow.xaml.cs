using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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

namespace SmartSightFrontEnd
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("SmartSightPlusPlus.dll")]
        private static extern IntPtr CreateNewMonitor();

        [DllImport("SmartSightPlusPlus.dll")]
        private static extern int CheckMarkerDetection();

        [DllImport("SmartSightPlusPlus.dll")]
        private static extern int StartMonitor();

        [DllImport("SmartSightPlusPlus.dll")]
        private static extern int StopMonitor();

        public MainWindow()
        {
            InitializeComponent();

            CreateNewMonitor();
            StartMonitor();

            while(true)
            {
                if (CheckMarkerDetection())
                {
                      
                }
            }


        }
    }
}
