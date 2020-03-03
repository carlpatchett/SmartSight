using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        private Tuple<string, int> mCurrentDetectionCount = new Tuple<string, int>("None", 0);
        private int mMaxDetectionCount = 10;

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
                    while (!this.HasImg)
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
            if (String.Equals(mCurrentDetectionCount.Item1, "Marker"))
            {
                // We previously detected a marker, lets see the frame count
                if (mCurrentDetectionCount.Item2 > mMaxDetectionCount)
                {
                    // If we have over 30 detections, reset and raise the event.
                    MarkerDetected.Invoke(this, new EventArgs());
                    mCurrentDetectionCount = new Tuple<string, int>("Marker", 0);
                }
                else
                {
                    // Otherwise increase the count
                    var currentCount = mCurrentDetectionCount.Item2;
                    currentCount++;
                    mCurrentDetectionCount = new Tuple<string, int>("Marker", currentCount);
                }
            }

            // This is the first time detecting a marker, set the count
            mCurrentDetectionCount = new Tuple<string, int>("Marker", 1);
        }

        private void mGestureDetector_HandDetected(object sender, EventArgs e)
        {
            //if (String.Equals(mCurrentDetectionCount.Item1, "Hand"))
            //{
            //    if (mCurrentDetectionCount.Item2 > mMaxDetectionCount)
            //    {
            //        HandDetected.Invoke(this, new EventArgs());
            //        mCurrentDetectionCount = new Tuple<string, int>("Hand", 0);
            //    }
            //    else
            //    {
            //        var currentCount = mCurrentDetectionCount.Item2;
            //        currentCount++;
            //        mCurrentDetectionCount = new Tuple<string, int>("Hand", currentCount);
            //    }
            //}

            //mCurrentDetectionCount = new Tuple<string, int>("Hand", 1);
        }
        private void mGestureDetector_OneFingerDetected(object sender, EventArgs e)
        {
            OneFingerDetected.Invoke(this, new EventArgs());

            if (String.Equals(mCurrentDetectionCount.Item1, "OneFinger"))
            {
                if (mCurrentDetectionCount.Item2 > mMaxDetectionCount)
                {
                    //OneFingerDetected.Invoke(this, new EventArgs());
                    mCurrentDetectionCount = new Tuple<string, int>("OneFinger", 0);
                }
                else
                {
                    var currentCount = mCurrentDetectionCount.Item2;
                    currentCount++;
                    mCurrentDetectionCount = new Tuple<string, int>("OneFinger", currentCount);
                }
            }

            mCurrentDetectionCount = new Tuple<string, int>("OneFinger", 1);
        }

        private void mGestureDetector_TwoFingersDetected(object sender, EventArgs e)
        {
            TwoFingersDetected.Invoke(this, new EventArgs());

            if (String.Equals(mCurrentDetectionCount.Item1, "TwoFingers"))
            {
                if (mCurrentDetectionCount.Item2 > mMaxDetectionCount)
                {
                    //TwoFingersDetected.Invoke(this, new EventArgs());
                    mCurrentDetectionCount = new Tuple<string, int>("TwoFingers", 0);
                }
                else
                {
                    var currentCount = mCurrentDetectionCount.Item2;
                    currentCount++;
                    mCurrentDetectionCount = new Tuple<string, int>("TwoFingers", currentCount);
                }
            }

            mCurrentDetectionCount = new Tuple<string, int>("TwoFingers", 1);
        }

        private void mGestureDetector_ThreeFingersDetected(object sender, EventArgs e)
        {
            ThreeFingersDetected.Invoke(this, new EventArgs());

            if (String.Equals(mCurrentDetectionCount.Item1, "ThreeFingers"))
            {
                if (mCurrentDetectionCount.Item2 > mMaxDetectionCount)
                {
                    //ThreeFingersDetected.Invoke(this, new EventArgs());
                    mCurrentDetectionCount = new Tuple<string, int>("ThreeFingers", 0);
                }
                else
                {
                    var currentCount = mCurrentDetectionCount.Item2;
                    currentCount++;
                    mCurrentDetectionCount = new Tuple<string, int>("ThreeFingers", currentCount);
                }
            }

            mCurrentDetectionCount = new Tuple<string, int>("ThreeFingers", 1);
        }

        private void mGestureDetector_FourFingersDetected(object sender, EventArgs e)
        {
            FourFingersDetected.Invoke(this, new EventArgs());

            if (String.Equals(mCurrentDetectionCount.Item1, "FourFingers"))
            {
                if (mCurrentDetectionCount.Item2 > mMaxDetectionCount)
                {
                    //FourFingersDetected.Invoke(this, new EventArgs());
                    mCurrentDetectionCount = new Tuple<string, int>("FourFingers", 0);
                }
                else
                {
                    var currentCount = mCurrentDetectionCount.Item2;
                    currentCount++;
                    mCurrentDetectionCount = new Tuple<string, int>("FourFingers", currentCount);
                }
            }

            mCurrentDetectionCount = new Tuple<string, int>("FourFingers", 1);
        }


        private void mGestureDetector_FiveFingersDetected(object sender, EventArgs e)
        {
            FiveFingersDetected.Invoke(this, new EventArgs());

            if (String.Equals(mCurrentDetectionCount.Item1, "FiveFingers"))
            {
                if (mCurrentDetectionCount.Item2 > mMaxDetectionCount)
                {
                    //FiveFingersDetected.Invoke(this, new EventArgs());
                    mCurrentDetectionCount = new Tuple<string, int>("FiveFingers", 0);
                }
                else
                {
                    var currentCount = mCurrentDetectionCount.Item2;
                    currentCount++;
                    mCurrentDetectionCount = new Tuple<string, int>("FiveFingers", currentCount);
                }
            }

            mCurrentDetectionCount = new Tuple<string, int>("FiveFingers", 1);
        }
    }
}
