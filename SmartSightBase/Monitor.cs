using System;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;
using SmartSightBase.GestureDetection;
using SmartSightBase.Enumeration;

/// <summary>
/// Carl Patchett
/// 27/04/2020
/// NHE2422 Advanced Computer Games Development
/// </summary>
namespace SmartSightBase
{
    /// <summary>
    /// The Monitor class used by SmartSight to control Marker Detection, Gesture Recognition and Camera Monitoring.
    /// </summary>
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

        private VideoCapture mCapture;
        private bool mMarkerDetected;

        public event EventHandler<EMarker> MarkerDetected = (s, e) => { };
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

        /// <summary>
        /// Gets the current GestureDetector.
        /// </summary>
        public GestureDetector GestureDetector { get; }

        /// <summary>
        /// Gets the current MarkerDetector Capture Image.
        /// </summary>
        public Mat CameraImg => mCameraImg;

        /// <summary>
        /// Gets the current GestureDetector Capture Image.
        /// </summary>
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

        /// <summary>
        /// Starts the video device capturing.
        /// </summary>
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
            // Hook up events
            this.MarkerDetector.MarkerDetected += this.mMarkerDetector_MarkerDetected;
            this.MarkerDetector.MarkerAngle += this.mMarkerDetector_MarkerAngle;
            this.GestureDetector.HandDetected += this.mGestureDetector_HandDetected;
            this.GestureDetector.OneFingerDetected += this.mGestureDetector_OneFingerDetected;
            this.GestureDetector.TwoFingersDetected += this.mGestureDetector_TwoFingersDetected;
            this.GestureDetector.ThreeFingersDetected += this.mGestureDetector_ThreeFingersDetected;
            this.GestureDetector.FourFingersDetected += this.mGestureDetector_FourFingersDetected;
            this.GestureDetector.FiveFingersDetected += this.mGestureDetector_FiveFingersDetected;
            
            // Only start image capture if our capture device successfully retrieves an image
            if (!this.HasImg)
            {
                this.StartImageCapture();
            }

            // Offload expensive work to worker threads
            Task.Run(() =>
            {
                while (true)
                {
                    // No need to check detection each frame, do it periodically
                    while (!this.HasImg)
                    {
                        Thread.Sleep(100);
                    }

                    this.StartMarkerDetection();
                    Cv2.WaitKey(1);
                }
            }, mMarkerDetectionToken.Token);

            // Only start gesture detection if it's enabled
            if (GestureDetector.GestureDetectionEnabled)
            {
                Task.Run(() =>
                {
                    while (true)
                    {
                        // No need to check detection each frame, do it periodically
                        while (!this.HasImg)
                        {
                            Thread.Sleep(100);
                        }

                        this.GestureDetector.StartGestureRecognition();
                        Cv2.WaitKey(1);
                    }
                }, mGestureDetectionToken.Token);
            }

            return true;
        }

        /// <summary>
        /// Cancels camera based monitoring.
        /// </summary>
        public void StopCameraMonitoring()
        {
            // Unhook all events
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

                Task.Delay(1);

                mMarkerDetectionInProgress = false;
            });
        }

        /// <summary>
        /// Starts the one finger detection countdown - this allows 2 and a half seconds for 8 successful detections.
        /// </summary>
        private void StartOneFingerDetectionCountdown()
        {
            Task.Run(() =>
            {
                Thread.Sleep(2500);
                mOneFingerDetectionCount = 0;
            }, mOneFingerDetectionToken.Token);
        }

        /// <summary>
        /// Starts the two fingers detection countdown - this allows 2 and a half seconds for 8 successful detections.
        /// </summary>
        private void StartTwoFingersDetectionCountdown()
        {
            Task.Run(() =>
            {
                Thread.Sleep(2500);
                mTwoFingersDetectionCount = 0;
            }, mTwoFingersDetectionToken.Token);
        }

        /// <summary>
        /// Starts the three fingers detection countdown - this allows 2 and a half seconds for 8 successful detections.
        /// </summary>
        private void StartThreeFingersDetectionCountdown()
        {
            Task.Run(() =>
            {
                Thread.Sleep(2500);
                mThreeFingersDetectionCount = 0;
            }, mThreeFingersDetectionToken.Token);
        }

        /// <summary>
        /// Starts the three fingers detection countdown - this allows 2 and a half seconds for 8 successful detections.
        /// </summary>
        private void StartFourFingersDetectionCountdown()
        {
            Task.Run(() =>
            {
                Thread.Sleep(2500);
                mFourFingersDetectionCount = 0;
            }, mFourFingersDetectionToken.Token);
        }

        /// <summary>
        /// Starts the three fingers detection countdown - this allows 2 and a half seconds for 8 successful detections.
        /// </summary>
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
        private void mMarkerDetector_MarkerDetected(object sender, EMarker e)
        {
            // If we have over 30 detections, reset and raise the event.
            MarkerDetected.Invoke(this, e);

            mMarkerDetected = true;

            Task.Run(() =>
            {
                Thread.Sleep(2000);
                mMarkerDetected = false;
            });
        }

        /// <summary>
        /// Handles the MarkerAngle event from the <see cref="MarkerDetector"/> class, and re-raises it.
        /// </summary>
        private void mMarkerDetector_MarkerAngle(object sender, float f)
        {
            MarkerAngle.Invoke(this, f);
        }

        /// <summary>
        /// Handles the HandDetected event from the <see cref="GestureDetector"/> class.
        /// </summary>
        /// <remarks>
        /// Currently awaiting further implementation with custom gesture recognition.
        /// </remarks>
        private void mGestureDetector_HandDetected(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Handles the One Finger Detected event from the <see cref="GestureDetector"/> class.
        /// </summary>
        /// <param name="sender">The instance sending the event.</param>
        /// <param name="e">The event args related to the event.</param>
        private void mGestureDetector_OneFingerDetected(object sender, EventArgs e)
        {
            // If we are currently detecting a marker, ignore the gesture event
            if (mMarkerDetected)
            {
                return;
            }

            // If this is the first one finger detection, start a countdown
            if (mOneFingerDetectionCount == 0)
            {
                this.StartOneFingerDetectionCountdown();
            }

            // If we have 8 or more successful detections, raise our events and reset our countdown
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

        /// <summary>
        /// Handles the Two Fingers Detected event from the <see cref="GestureDetector"/> class.
        /// </summary>
        /// <param name="sender">The instance sending the event.</param>
        /// <param name="e">The event args related to the event.</param>
        private void mGestureDetector_TwoFingersDetected(object sender, EventArgs e)
        {
            // If we are currently detecting a marker, ignore the gesture event
            if (mMarkerDetected)
            {
                return;
            }

            // If this is the first two finger detection, start a countdown
            if (mTwoFingersDetectionCount == 0)
            {
                this.StartTwoFingersDetectionCountdown();
            }

            // If we have 8 or more successful detections, raise our events and reset our countdown
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

        /// <summary>
        /// Handles the Three Fingers Detected event from the <see cref="GestureDetector"/> class.
        /// </summary>
        /// <param name="sender">The instance sending the event.</param>
        /// <param name="e">The event args related to the event.</param>
        private void mGestureDetector_ThreeFingersDetected(object sender, EventArgs e)
        {
            // If we are currently detecting a marker, ignore the gesture event
            if (mMarkerDetected)
            {
                return;
            }

            // If this is the first three finger detection, start a countdown
            if (mThreeFingersDetectionCount == 0)
            {
                this.StartThreeFingersDetectionCountdown();
            }

            // If we have 8 or more successful detections, raise our events and reset our countdown
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

        /// <summary>
        /// Handles the Four Fingers Detected event from the <see cref="GestureDetector"/> class.
        /// </summary>
        /// <param name="sender">The instance sending the event.</param>
        /// <param name="e">The event args related to the event.</param>
        private void mGestureDetector_FourFingersDetected(object sender, EventArgs e)
        {
            // If we are currently detecting a marker, ignore the gesture event
            if (mMarkerDetected)
            {
                return;
            }

            // If this is the first four finger detection, start a countdown
            if (mFourFingersDetectionCount == 0)
            {
                this.StartFourFingersDetectionCountdown();
            }

            // If we have 8 or more successful detections, raise our events and reset our countdown
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

        /// <summary>
        /// Handles the Five Fingers Detected event from the <see cref="GestureDetector"/> class.
        /// </summary>
        /// <param name="sender">The instance sending the event.</param>
        /// <param name="e">The event args related to the event.</param>
        private void mGestureDetector_FiveFingersDetected(object sender, EventArgs e)
        {
            // If we are currently detecting a marker, ignore the gesture event
            if (mMarkerDetected)
            {
                return;
            }

            // If this is the first five finger detection, start a countdown
            if (mFiveFingersDetectionCount == 0)
            {
                this.StartFiveFingersDetectionCountdown();
            }

            // If we have 8 or more successful detections, raise our events and reset our countdown
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
