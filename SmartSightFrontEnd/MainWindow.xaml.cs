using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
using Emgu.CV;
using Emgu.CV.Structure;
using OpenCvSharp;
using SmartSightBase;

namespace SmartSightFrontEnd
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private readonly Monitor mMonitor = new Monitor();

        public MainWindow()
        {
            this.InitializeComponent();

            mMonitor.MarkerDetected += this.mMonitor_MarkerDetected;
            mMonitor.HandDetected += this.mMonitor_HandDetected;

            this.BeginMonitoring();
        }

        /// <summary>
        /// Begins the monitoring process.
        /// </summary>
        private void BeginMonitoring()
        {
            Task.Run(() =>
            {
                if (mMonitor.StartCameraMonitoring())
                {
                    while (true)
                    {
                        if (mMonitor.HasImg)
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                this.WebcamDisplay.Source = mMonitor.CameraImgAsBitmap;
                                this.GestureThresholdDisplay.Source = mMonitor.GestureThresholdImgAsBitmap;
                                this.WebcamDisplay.InvalidateVisual();
                            });
                        }
                    }
                };
            });
        }

        /// <summary>
        /// Handles the MarkerDetected event from the <see cref="Monitor"/> class.
        /// </summary>
        /// <param name="sender">The object sending the event.</param>
        /// <param name="e">The event parameters.</param>
        private void mMonitor_MarkerDetected(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.DetectionDisplay.Source = mMonitor.DetectedMarkerImgAsBitmap;
                this.DetectionDisplay.InvalidateVisual();
            });
        }

        private void mMonitor_HandDetected(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.GestureDisplay.Source = mMonitor.GestureImgAsBitmap;
                this.GestureDisplay.InvalidateVisual();
            });
        }
    }
}
