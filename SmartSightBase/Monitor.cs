using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using SmartSightBase.GestureDetection;

namespace SmartSightBase
{
    public class Monitor
    {
        private readonly static float[] mCameraMatrix = new float[9] { 612.84f, 0.0f, 326.46f, 0.0f, 612.84f, 289.42f, 0.0f, 0.0f, 1.0f };
        private static Mat mCameraImg = new Mat();
        private bool mMarkerDetectionInProgress;

        public VideoCapture mCapture;

        public event EventHandler MarkerDetected = (s, e) => { };
        public event EventHandler HandDetected = (s, e) => { };
        public event EventHandler OneFingerDetected = (s, e) => { };
        public event EventHandler TwoFingersDetected = (s, e) => { };
        public event EventHandler ThreeFingersDetected = (s, e) => { };
        public event EventHandler FourFingersDetected = (s, e) => { };
        public event EventHandler FiveFingersDetected = (s, e) => { };

        /// <summary>
        /// Creates a new instance of the <see cref="Monitor"/> class.
        /// </summary>
        public Monitor()
        {
            this.MarkerDetector = new MarkerDetector(new CameraCalibration(mCameraMatrix[0],
                                                                           mCameraMatrix[4],
                                                                           mCameraMatrix[2],
                                                                           mCameraMatrix[5]), this);

            this.GestureDetector = new GestureDetector(this);

            mCapture = new VideoCapture(0);
        }

        #region Properties

        /// <summary>
        ///  Gets the current MarkerDetector.
        /// </summary>
        public MarkerDetector MarkerDetector { get; }

        public GestureDetector GestureDetector { get; }

        /// <summary>
        /// Gets the current MarkerDetector Capture Image.
        /// </summary>
        public Mat CameraImg => mCameraImg;

        /// <summary>
        /// Gets the current MarkerDetector Capture Image as a <see cref="BitmapImage"/>.
        /// </summary>
        public BitmapImage CameraImgAsBitmap
        {
            get
            {
                using (var ms = mCameraImg.ToMemoryStream())
                {
                    var bitmapImg = new BitmapImage();

                    bitmapImg.BeginInit();
                    bitmapImg.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImg.StreamSource = ms;
                    bitmapImg.EndInit();

                    return bitmapImg;
                };
            }
        }

        public Mat GestureImg => this.GestureDetector.GestureImg;

        public BitmapImage GestureImgAsBitmap
        {
            get
            {
                if (this.GestureImg == null)
                {
                    return null;
                }

                using (var ms = this.GestureImg.ToMemoryStream())
                {
                    var bitmapImg = new BitmapImage();

                    bitmapImg.BeginInit();
                    bitmapImg.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImg.StreamSource = ms;
                    bitmapImg.EndInit();

                    return bitmapImg;
                }
            }
        }

        /// <summary>
        /// Gets the current GestureDetector Theshold Image.
        /// </summary>
        public Mat GestureThresholdImg => this.GestureDetector.ThreshholdImg;

        /// <summary>
        /// Gets the current GestureDetector Threshold Image as a <see cref="BitmapImage"/>.
        /// </summary>
        public BitmapImage GestureThresholdImgAsBitmap
        {
            get
            {
                if (this.GestureThresholdImg == null)
                {
                    return null;
                }

                using (var ms = this.GestureThresholdImg.ToMemoryStream())
                {
                    var bitmapImg = new BitmapImage();

                    bitmapImg.BeginInit();
                    bitmapImg.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImg.StreamSource = ms;
                    bitmapImg.EndInit();

                    return bitmapImg;
                }
            }
        }

        /// <summary>
        ///  Gets the current MarkerDetector Detected Marker Image.
        /// </summary>
        public Mat DetectedMarkerImg => this.MarkerDetector.DetectedMarkerImg;

        /// <summary>
        /// Gets the current MarkerDetector Detected Marker Image as a <see cref="BitmapImage"/>.
        /// </summary>
        public BitmapImage DetectedMarkerImgAsBitmap
        {
            get
            {
                using (var ms = this.DetectedMarkerImg.ToMemoryStream())
                {
                    var bitmapImg = new BitmapImage();

                    bitmapImg.BeginInit();
                    bitmapImg.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImg.StreamSource = ms;
                    bitmapImg.EndInit();

                    return bitmapImg;
                };
            }
        }

        /// <summary>
        /// Gets/Sets whether the camera image exists.
        /// </summary>
        public bool HasImg { get; set; }

        #endregion

        /// <summary>
        /// Starts monitoring the Camera the given index.
        /// </summary>
        /// <param name="cameraIndex">The index of the camera that should be monitored.</param>
        /// <returns>True if monitoring was initialized successfully, otherwise False.</returns>
        public bool StartCameraMonitoring()
        {
            this.MarkerDetector.MarkerDetected += this.mMarkerDetector_MarkerDetected;

            this.GestureDetector.HandDetected += this.mGestureDetector_HandDetected;
            this.GestureDetector.OneFingerDetected += this.mGestureDetector_OneFingerDetected;
            this.GestureDetector.TwoFingersDetected += this.mGestureDetector_TwoFingersDetected;
            this.GestureDetector.ThreeFingersDetected += this.mGestureDetector_ThreeFingersDetected;
            this.GestureDetector.FourFingersDetected += this.mGestureDetector_FourFingersDetected;
            this.GestureDetector.FiveFingersDetected += this.mGestureDetector_FiveFingersDetected;

            Task.Run(() =>
            {
                while (true)
                {
                    mCameraImg = mCapture.RetrieveMat();
                    this.HasImg = true;
                }
            });

            Task.Run(() =>
            {
                while (true)
                {
                    while (!this.HasImg)
                    {
                        System.Threading.Thread.Sleep(100);
                    }

                    this.StartMarkerDetection();
                    Cv2.WaitKey(1);
                }
            });

            Task.Run(() =>
            {
                while (true)
                {
                    while(!this.HasImg)
                    { 
                        System.Threading.Thread.Sleep(100);
                    }

                    this.GestureDetector.StartGestureRecognition();
                    Cv2.WaitKey(1);
                }
            });

            return true;
        }

        private void StartMarkerDetection()
        {
            if (mMarkerDetectionInProgress)
            {
                return;
            }

            Task.Run(() =>
            {
                mMarkerDetectionInProgress = true;

                this.MarkerDetector.FindMarkers(this, true);

                Task.Delay(1000);

                mMarkerDetectionInProgress = false;
            });
        }

        /// <summary>
        /// Handles the MarkerDetected event from the <see cref="MarkerDetector"/> class.
        /// </summary>
        /// <param name="sender">The object sendering the event.</param>
        /// <param name="e">The event parameters.</param>
        private void mMarkerDetector_MarkerDetected(object sender, EventArgs e)
        {
            // Re-raise the event
            MarkerDetected.Invoke(this, new EventArgs());
        }

        private void mGestureDetector_HandDetected(object sender, EventArgs e)
        {
            // Re-raise the event
            HandDetected.Invoke(this, new EventArgs());
        }
        private void mGestureDetector_OneFingerDetected(object sender, EventArgs e)
        {
            // Re-raise the event
            OneFingerDetected.Invoke(this, new EventArgs());
        }

        private void mGestureDetector_TwoFingersDetected(object sender, EventArgs e)
        {
            // Re-raise the event
            TwoFingersDetected.Invoke(this, new EventArgs());
        }

        private void mGestureDetector_ThreeFingersDetected(object sender, EventArgs e)
        {
            // Re-raise the event
            ThreeFingersDetected.Invoke(this, new EventArgs());
        }

        private void mGestureDetector_FourFingersDetected(object sender, EventArgs e)
        {
            // Re-raise the event
            FourFingersDetected.Invoke(this, new EventArgs());
        }


        private void mGestureDetector_FiveFingersDetected(object sender, EventArgs e)
        {
            // Re-raise the event
            FiveFingersDetected.Invoke(this, new EventArgs());
        }


    }
}
