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
using OpenCvSharp;
using SmartSightBase;

namespace SmartSightFrontEnd
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private static float[] mCameraMatrix = new float[9] { 612.84f, 0.0f, 326.46f, 0.0f, 612.84f, 289.42f, 0.0f, 0.0f, 1.0f };
        private static MarkerDetector mMarkerDetector = new MarkerDetector(new CameraCalibration(mCameraMatrix[0], mCameraMatrix[4], mCameraMatrix[2], mCameraMatrix[5]));
        private Mat mImg;

        public MainWindow()
        {
            this.InitializeComponent();

            mMarkerDetector.MarkerDetected += this.mMarkerDetector_MarkerDetected;

            Task.Run(() =>
            {
                using (var camera = new VideoCapture(0))
                {
                    while (true)
                    {
                        mImg = camera.RetrieveMat();
                        mMarkerDetector.FindMarkers(mImg, true);

                        this.Dispatcher.Invoke(() =>
                        {
                            var memStream = new MemoryStream();
                            var bitmapImg = new BitmapImage();

                            using (var ms = mImg.ToMemoryStream())
                            {
                                bitmapImg.BeginInit();
                                bitmapImg.CacheOption = BitmapCacheOption.OnLoad;
                                bitmapImg.StreamSource = ms;
                                bitmapImg.EndInit();

                                this.WebcamDisplay.Source = bitmapImg;
                                this.WebcamDisplay.InvalidateVisual();
                            };
                        });
                    }
                }
            });
        }

        private void mMarkerDetector_MarkerDetected(object sender, EventArgs e)
        {

            this.Dispatcher.Invoke(() =>
            {
                var memStream = new MemoryStream();
                var bitmapImg = new BitmapImage();

                using (var ms = mMarkerDetector.DetectedMarkerImg.ToMemoryStream())
                {
                    bitmapImg.BeginInit();
                    bitmapImg.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImg.StreamSource = ms;
                    bitmapImg.EndInit();

                    this.DetectionDisplay.Source = bitmapImg;
                    this.DetectionDisplay.InvalidateVisual();
                };
            });
        }
    }
}
