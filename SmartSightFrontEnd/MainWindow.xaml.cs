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
        private bool mMonitoringStarted;

        public MainWindow()
        {
            this.InitializeComponent();

            mMonitor.MarkerDetected += this.mMonitor_MarkerDetected;

            mMonitor.HandDetected += this.mMonitor_HandDetected;
            mMonitor.OneFingerDetected += this.mMonitor_OneFingerDetected;
            mMonitor.TwoFingersDetected += this.mMonitor_TwoFingersDetected;
            mMonitor.ThreeFingersDetected += this.mMonitor_ThreeFingersDetected;
            mMonitor.FourFingersDetected += this.mMonitor_FourFingersDetected;
            mMonitor.FiveFingersDetected += this.mMonitor_FiveFingersDetected;

            mMonitor.StartImageCapture();
        }

        private void SetupGestureRecognition()
        {
            var setupSuccessful = mMonitor.GestureDetector.SetUpGestureRecognition();

            if (!setupSuccessful)
            {
                this.SetupGestureRecognition();
            }
            else
            {
                this.BeginMonitoring();
                mMonitoringStarted = true;
            }
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
                    var delay = false;
                    while (true)
                    {
                        if (mMonitor.HasImg)
                        {
                            if (delay)
                                return;

                            delay = true;

                            this.Dispatcher.Invoke(() =>
                            {
                                if (mMonitor.CameraImg != null)
                                {
                                    using (var ms = mMonitor.CameraImg.ToMemoryStream())
                                    {
                                        var bitmapImg = new BitmapImage();

                                        bitmapImg.BeginInit();
                                        bitmapImg.CacheOption = BitmapCacheOption.OnLoad;
                                        bitmapImg.StreamSource = ms;
                                        bitmapImg.EndInit();

                                        this.WebcamDisplay.Source = bitmapImg;
                                    };
                                }

                                if (mMonitor.GestureImg != null)
                                {
                                    using (var ms = mMonitor.GestureThresholdImg.ToMemoryStream())
                                    {
                                        var bitmapImg = new BitmapImage();

                                        bitmapImg.BeginInit();
                                        bitmapImg.CacheOption = BitmapCacheOption.OnLoad;
                                        bitmapImg.StreamSource = ms;
                                        bitmapImg.EndInit();

                                        this.GestureThresholdDisplay.Source = bitmapImg;
                                    };
                                }

                                this.WebcamDisplay.InvalidateVisual();
                            });

                            System.Threading.Thread.Sleep(100);
                            delay = false;
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
                if (mMonitor.CameraImg == null)
                {
                    return;
                }

                using (var ms = mMonitor.CameraImg.ToMemoryStream())
                {
                    var bitmapImg = new BitmapImage();

                    bitmapImg.BeginInit();
                    bitmapImg.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImg.StreamSource = ms;
                    bitmapImg.EndInit();

                    this.DetectionDisplay.Source = bitmapImg;
                };

                this.DetectionDisplay.InvalidateVisual();
            });
        }

        private void mMonitor_HandDetected(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (mMonitor.GestureImg == null)
                {
                    return;
                }

                using (var ms = mMonitor.GestureImg.ToMemoryStream())
                {
                    var bitmapImg = new BitmapImage();

                    bitmapImg.BeginInit();
                    bitmapImg.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImg.StreamSource = ms;
                    bitmapImg.EndInit();

                    this.GestureDisplay.Source = bitmapImg;
                };

                this.GestureDisplay.InvalidateVisual();
            });
        }

        private void mMonitor_OneFingerDetected(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.FingersDetectedDisplay.Text = "One Finger Detected";
                this.GestureDisplay.InvalidateVisual();
            });
        }

        private void mMonitor_TwoFingersDetected(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.FingersDetectedDisplay.Text = "Two Fingers Detected";
                this.GestureDisplay.InvalidateVisual();
            });
        }

        private void mMonitor_ThreeFingersDetected(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.FingersDetectedDisplay.Text = "Three Fingers Detected";
                this.GestureDisplay.InvalidateVisual();
            });
        }

        private void mMonitor_FourFingersDetected(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.FingersDetectedDisplay.Text = "Four Fingers Detected";
                this.GestureDisplay.InvalidateVisual();
            });
        }

        private void mMonitor_FiveFingersDetected(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.FingersDetectedDisplay.Text = "Five Fingers Detected";
                this.GestureDisplay.InvalidateVisual();
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (mMonitoringStarted)
            {
                mMonitor.StopCameraMonitoring();
            }

            SetupGestureRecognition();
        }
    }
}
