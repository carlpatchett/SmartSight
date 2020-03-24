using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;
using SmartSightBase.GestureDetection;

namespace SmartSightBase
{
    public class Monitor : IMonitor
    {
        private readonly static float[] mCameraMatrix = new float[9] { 612.84f, 0.0f, 326.46f, 0.0f, 612.84f, 289.42f, 0.0f, 0.0f, 1.0f };
        private static Mat mCameraImg = new Mat();
        private bool mMarkerDetectionInProgress;

        private int mOneFingerDetectionCount = 0;
        private int mTwoFingersDetectionCount = 0;
        private int mThreeFingersDetectionCount = 0;
        private int mFourFingersDetectionCount = 0;
        private int mFiveFingersDetectionCount = 0;

        private CancellationTokenSource mMarkerDetectionToken = new CancellationTokenSource();
        private CancellationTokenSource mGestureDetectionToken = new CancellationTokenSource();

        private CancellationTokenSource mOneFingerDetectionToken = new CancellationTokenSource();
        private CancellationTokenSource mTwoFingersDetectionToken = new CancellationTokenSource();
        private CancellationTokenSource mThreeFingersDetectionToken = new CancellationTokenSource();
        private CancellationTokenSource mFourFingersDetectionToken = new CancellationTokenSource();
        private CancellationTokenSource mFiveFingersDetectionToken = new CancellationTokenSource();

        public VideoCapture mCapture;

        private bool mMarkerDetected;

        public event EventHandler MarkerDetected = (s, e) => { };
        public event EventHandler<float> MarkerAngle = (s, e) => { };
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
                                                                           mCameraMatrix[5]));

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

        public Mat GestureImg => this.GestureDetector.GestureImg;

        /// <summary>
        /// Gets the current GestureDetector Theshold Image.
        /// </summary>
        public Mat GestureThresholdImg => this.GestureDetector.ThreshholdImg;

        /// <summary>
        ///  Gets the current MarkerDetector Detected Marker Image.
        /// </summary>
        public Mat DetectedMarkerImg => this.MarkerDetector.DetectedMarkerImg;

        /// <summary>
        /// Gets/Sets whether the camera image exists.
        /// </summary>
        public bool HasImg { get; set; }

        #endregion

        public void StartImageCapture()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    mCameraImg = mCapture.RetrieveMat();
                    this.HasImg = true;
                }
            });
        }

        /// <summary>
        /// Starts monitoring the Camera the given index.
        /// </summary>
        /// <param name="cameraIndex">The index of the camera that should be monitored.</param>
        /// <returns>True if monitoring was initialized successfully, otherwise False.</returns>
        public bool StartCameraMonitoring()
        {
            this.MarkerDetector.MarkerDetected += this.mMarkerDetector_MarkerDetected;
            this.MarkerDetector.MarkerAngle += this.mMarkerDetector_MarkerAngle;
            this.GestureDetector.HandDetected += this.mGestureDetector_HandDetected;
            this.GestureDetector.OneFingerDetected += this.mGestureDetector_OneFingerDetected;
            this.GestureDetector.TwoFingersDetected += this.mGestureDetector_TwoFingersDetected;
            this.GestureDetector.ThreeFingersDetected += this.mGestureDetector_ThreeFingersDetected;
            this.GestureDetector.FourFingersDetected += this.mGestureDetector_FourFingersDetected;
            this.GestureDetector.FiveFingersDetected += this.mGestureDetector_FiveFingersDetected;
            
            if (!this.HasImg)
            {
                this.StartImageCapture();
            }

            Task.Run(() =>
            {
                while (true)
                {
                    while (!this.HasImg)
                    {
                        Thread.Sleep(100);
                    }

                    this.StartMarkerDetection();
                    Cv2.WaitKey(1);
                }
            }, mMarkerDetectionToken.Token);

            Task.Run(() =>
            {
                while (true)
                {
                    while (!this.HasImg)
                    {
                        Thread.Sleep(100);
                    }

                    this.GestureDetector.StartGestureRecognition();
                    Cv2.WaitKey(1);
                }
            }, mGestureDetectionToken.Token);

            return true;
        }

        /// <summary>
        /// Cancels camera based monitoring.
        /// </summary>
        public void StopCameraMonitoring()
        {
            this.MarkerDetector.MarkerDetected -= this.mMarkerDetector_MarkerDetected;

            this.GestureDetector.HandDetected -= this.mGestureDetector_HandDetected;
            this.GestureDetector.OneFingerDetected -= this.mGestureDetector_OneFingerDetected;
            this.GestureDetector.TwoFingersDetected -= this.mGestureDetector_TwoFingersDetected;
            this.GestureDetector.ThreeFingersDetected -= this.mGestureDetector_ThreeFingersDetected;
            this.GestureDetector.FourFingersDetected -= this.mGestureDetector_FourFingersDetected;
            this.GestureDetector.FiveFingersDetected -= this.mGestureDetector_FiveFingersDetected;

            mMarkerDetectionToken.Cancel();
            mGestureDetectionToken.Cancel();

            GestureDetector.GestureRecognitionSetup = false;
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

        private void StartOneFingerDetectionCountdown()
        {
            Task.Run(() =>
            {
                Thread.Sleep(2500);
                mOneFingerDetectionCount = 0;
            }, mOneFingerDetectionToken.Token);
        }

        private void StartTwoFingersDetectionCountdown()
        {
            Task.Run(() =>
            {
                Thread.Sleep(2500);
                mTwoFingersDetectionCount = 0;
            }, mTwoFingersDetectionToken.Token);
        }

        private void StartThreeFingersDetectionCountdown()
        {
            Task.Run(() =>
            {
                Thread.Sleep(2500);
                mThreeFingersDetectionCount = 0;
            }, mThreeFingersDetectionToken.Token);
        }

        private void StartFourFingersDetectionCountdown()
        {
            Task.Run(() =>
            {
                Thread.Sleep(2500);
                mFourFingersDetectionCount = 0;
            }, mFourFingersDetectionToken.Token);
        }

        private void StartFiveFingersDetectionCountdown()
        {
            Task.Run(() =>
            {
                Thread.Sleep(2500);
                mFiveFingersDetectionCount = 0;
            }, mFiveFingersDetectionToken.Token);
        }

        /// <summary>
        /// Handles the MarkerDetected event from the <see cref="MarkerDetector"/> class.
        /// </summary>
        /// <param name="sender">The object sendering the event.</param>
        /// <param name="e">The event parameters.</param>
        private void mMarkerDetector_MarkerDetected(object sender, EventArgs e)
        {
            // If we have over 30 detections, reset and raise the event.
            MarkerDetected.Invoke(this, new EventArgs());

            mMarkerDetected = true;

            Task.Run(() =>
            {
                Thread.Sleep(2000);
                mMarkerDetected = false;
            });
        }

        private void mMarkerDetector_MarkerAngle(object sender, float f)
        {
            MarkerAngle.Invoke(this, f);
        }

        private void mGestureDetector_HandDetected(object sender, EventArgs e)
        {
            //To do: Implement
        }

        private void mGestureDetector_OneFingerDetected(object sender, EventArgs e)
        {
            if (mMarkerDetected)
            {
                return;
            }

            if (mOneFingerDetectionCount == 0)
            {
                this.StartOneFingerDetectionCountdown();
            }

            if (mOneFingerDetectionCount >= 8)
            {
                OneFingerDetected.Invoke(this, new EventArgs());
                HandDetected.Invoke(this, new EventArgs());
                mOneFingerDetectionCount = 0;
                this.CancelAllThreadTokens();
            }
            else
            {
                mOneFingerDetectionCount++;
            }
        }

        private void mGestureDetector_TwoFingersDetected(object sender, EventArgs e)
        {
            if (mMarkerDetected)
            {
                return;
            }

            if (mTwoFingersDetectionCount == 0)
            {
                this.StartTwoFingersDetectionCountdown();
            }

            if (mTwoFingersDetectionCount >= 8)
            {
                TwoFingersDetected.Invoke(this, new EventArgs());
                HandDetected.Invoke(this, new EventArgs());
                mTwoFingersDetectionCount = 0;
                this.CancelAllThreadTokens();
            }
            else
            {
                mTwoFingersDetectionCount++;
            }
        }

        private void mGestureDetector_ThreeFingersDetected(object sender, EventArgs e)
        {
            if (mMarkerDetected)
            {
                return;
            }

            if (mThreeFingersDetectionCount == 0)
            {
                this.StartThreeFingersDetectionCountdown();
            }

            if (mThreeFingersDetectionCount >= 8)
            {
                ThreeFingersDetected.Invoke(this, new EventArgs());
                HandDetected.Invoke(this, new EventArgs());
                mThreeFingersDetectionCount = 0;
                this.CancelAllThreadTokens();
            }
            else
            {
                mThreeFingersDetectionCount++;
            }
        }

        private void mGestureDetector_FourFingersDetected(object sender, EventArgs e)
        {
            if (mMarkerDetected)
            {
                return;
            }

            if (mFourFingersDetectionCount == 0)
            {
                this.StartFourFingersDetectionCountdown();
            }

            if (mFourFingersDetectionCount >= 8)
            {
                FourFingersDetected.Invoke(this, new EventArgs());
                HandDetected.Invoke(this, new EventArgs());
                mFourFingersDetectionCount = 0;
                this.CancelAllThreadTokens();
            }
            else
            {
                mFourFingersDetectionCount++;
            }
        }


        private void mGestureDetector_FiveFingersDetected(object sender, EventArgs e)
        {
            if (mMarkerDetected)
            {
                return;
            }

            if (mFiveFingersDetectionCount == 0)
            {
                this.StartFiveFingersDetectionCountdown();
            }

            if (mFiveFingersDetectionCount >= 8)
            {
                FiveFingersDetected.Invoke(this, new EventArgs());
                HandDetected.Invoke(this, new EventArgs());
                mFiveFingersDetectionCount = 0;
                this.CancelAllThreadTokens();
            }
            else
            {
                mFiveFingersDetectionCount++;
            }
        }

        private void CancelAllThreadTokens()
        {
            mOneFingerDetectionToken.Cancel();
            mTwoFingersDetectionToken.Cancel();
            mThreeFingersDetectionToken.Cancel();
            mFourFingersDetectionToken.Cancel();
            mFiveFingersDetectionToken.Cancel();
        }
    }
}
