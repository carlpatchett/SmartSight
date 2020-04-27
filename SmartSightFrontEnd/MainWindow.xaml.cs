using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using SmartSightBase;
using SmartSightBase.Enumeration;

/// <summary>
/// Carl Patchett
/// 27/04/2020
/// NHE2422 Advanced Computer Games Development
/// </summary>
namespace SmartSightFrontEnd
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private readonly Monitor mMonitor = new Monitor();
        private bool mMonitoringStarted;

        /// <summary>
        /// Creates a new instance of <see cref="MainWindow"/>.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();

            // Hook up all events from our monitor
            mMonitor.MarkerDetected += this.mMonitor_MarkerDetected;
            mMonitor.MarkerAngle += this.mMonitor_MarkerAngleDetected;

            mMonitor.HandDetected += this.mMonitor_HandDetected;
            mMonitor.OneFingerDetected += this.mMonitor_OneFingerDetected;
            mMonitor.TwoFingersDetected += this.mMonitor_TwoFingersDetected;
            mMonitor.ThreeFingersDetected += this.mMonitor_ThreeFingersDetected;
            mMonitor.FourFingersDetected += this.mMonitor_FourFingersDetected;
            mMonitor.FiveFingersDetected += this.mMonitor_FiveFingersDetected;

            mMonitor.StartImageCapture();
            this.BeginMonitoring();
        }

        /// <summary>
        /// Set up Gesture Recognition.
        /// </summary>
        /// <param name="automatic">Whether Gesture Recognition should be automatic or not.</param>
        private void SetupGestureRecognition(bool automatic)
        {
            var setupSuccessful = mMonitor.GestureDetector.SetUpGestureRecognition(automatic);

            if (!setupSuccessful)
            {
                this.SetupGestureRecognition(automatic);
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

                            // Offload UI work to the UI Thread
                            // This is because most events will be raised through worker threads
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

                                if (mMonitor.GestureThresholdImg != null)
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

                                if (mMonitor.GestureImg != null)
                                {
                                    using (var ms = mMonitor.GestureImg.ToMemoryStream())
                                    {
                                        var bitmapImg = new BitmapImage();

                                        bitmapImg.BeginInit();
                                        bitmapImg.CacheOption = BitmapCacheOption.OnLoad;
                                        bitmapImg.StreamSource = ms;
                                        bitmapImg.EndInit();

                                        this.GestureDisplay.Source = bitmapImg;
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
        private void mMonitor_MarkerDetected(object sender, EMarker e)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (mMonitor.CameraImg == null)
                {
                    return;
                }

                using (var ms = mMonitor.DetectedMarkerImg.ToMemoryStream())
                {
                    var bitmapImg = new BitmapImage();

                    bitmapImg.BeginInit();
                    bitmapImg.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImg.StreamSource = ms;
                    bitmapImg.EndInit();

                    this.DetectionDisplay.Source = bitmapImg;
                };

                MarkerDetectedLabel.Text = $"Detected Marker: {e.ToString()}";

                //this.DetectionDisplay.InvalidateVisual();
            });
        }

        /// <summary>
        /// Handles the MarkerAngle event from the <see cref="Monitor"/> class.
        /// </summary>
        /// <param name="sender">The object sending the event.</param>
        /// <param name="e">The float angle detected.</param>
        private void mMonitor_MarkerAngleDetected(object sender, float f)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.MarkerDetectedAngleLabel.Text = $"Marker Angle: {f}";
            });
        }

        /// <summary>
        /// Handles the HandDetected event from the <see cref="Monitor"/> class.
        /// </summary>
        /// <param name="sender">The object sending the event.</param>
        /// <param name="e">The event parameters.</param>
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

        /// <summary>
        /// Handles the OneFingerDetected event from the <see cref="Monitor"/> class.
        /// </summary>
        /// <param name="sender">The object sending the event.</param>
        /// <param name="e">The event parameters.</param>
        private void mMonitor_OneFingerDetected(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.FingersDetectedDisplay.Text = "One Finger Detected";
                this.GestureDisplay.InvalidateVisual();
            });
        }

        /// <summary>
        /// Handles the TwoFingersDetected event from the <see cref="Monitor"/> class.
        /// </summary>
        /// <param name="sender">The object sending the event.</param>
        /// <param name="e">The event parameters.</param>
        private void mMonitor_TwoFingersDetected(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.FingersDetectedDisplay.Text = "Two Fingers Detected";
                this.GestureDisplay.InvalidateVisual();
            });
        }

        /// <summary>
        /// Handles the ThreeFingersDetected event from the <see cref="Monitor"/> class.
        /// </summary>
        /// <param name="sender">The object sending the event.</param>
        /// <param name="e">The event parameters.</param>
        private void mMonitor_ThreeFingersDetected(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.FingersDetectedDisplay.Text = "Three Fingers Detected";
                this.GestureDisplay.InvalidateVisual();
            });
        }

        /// <summary>
        /// Handles the FourFingersDetected event from the <see cref="Monitor"/> class.
        /// </summary>
        /// <param name="sender">The object sending the event.</param>
        /// <param name="e">The event parameters.</param>
        private void mMonitor_FourFingersDetected(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.FingersDetectedDisplay.Text = "Four Fingers Detected";
                this.GestureDisplay.InvalidateVisual();
            });
        }

        /// <summary>
        /// Handles the FiveFingersDetected event from the <see cref="Monitor"/> class.
        /// </summary>
        /// <param name="sender">The object sending the event.</param>
        /// <param name="e">The event parameters.</param>
        private void mMonitor_FiveFingersDetected(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.FingersDetectedDisplay.Text = "Five Fingers Detected";
                this.GestureDisplay.InvalidateVisual();
            });
        }

        /// <summary>
        /// Handles the Automatic Gesture Recognition button click event.
        /// </summary>
        /// <param name="sender">The object sending the event.</param>
        /// <param name="e">The event parameters.</param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (mMonitoringStarted)
            {
                mMonitor.StopCameraMonitoring();
            }

            SetupGestureRecognition(true);
        }

        /// <summary>
        /// Handles the Manual Gesture Recognition button click event.
        /// </summary>
        /// <param name="sender">The object sending the event.</param>
        /// <param name="e">The event parameters.</param>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (mMonitoringStarted)
            {
                mMonitor.StopCameraMonitoring();
            }

            SetupGestureRecognition(false);
        }
    }
}
