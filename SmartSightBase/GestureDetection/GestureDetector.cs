using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace SmartSightBase.GestureDetection
{
    public class GestureDetector
    {
        private readonly IMonitor mMonitor;
        private bool mEventDelay;

        private List<Point> mFingers = new List<Point>();
        private List<double> mFingerDistances = new List<double>();
        private Point mCenterMass;
        private double mAverageDefectDistance;

        private int mH = 0;
        private int mS = 0;
        private int mV = 0;

        public event EventHandler GestureDetected = (s, e) => { };
        public event EventHandler HandDetected = (s, e) => { };
        public event EventHandler FiveFingersDetected = (s, e) => { };
        public event EventHandler FourFingersDetected = (s, e) => { };
        public event EventHandler ThreeFingersDetected = (s, e) => { };
        public event EventHandler TwoFingersDetected = (s, e) => { };
        public event EventHandler OneFingerDetected = (s, e) => { };

        public GestureDetector(IMonitor monitor)
        {
            mMonitor = monitor;

            //Cv2.NamedWindow("HSV Window");

            //var h = 0;
            //var s = 0;
            //var v = 0;
            //Cv2.CreateTrackbar("H", "HSV Window", ref h, 255);
            //Cv2.CreateTrackbar("S", "HSV Window", ref s, 255);
            //Cv2.CreateTrackbar("V", "HSV Window", ref v, 255);
        }

        public bool GestureRecognitionSetup { get; set; }

        public Mat GestureImg { get; set; }

        public Mat ThreshholdImg { get; set; }

        public bool SetUpGestureRecognition()
        {
            mH = 0;
            mS = 0;
            mV = 0;

            // Start with a black image, and slowly allow more through the filter until we get a solid recognition
            for (var h = 255; h > 0; h -= 25)
            {
                for (var s = 255; s > 0; s -= 25)
                {
                    for (var v = 255; v > 0; v -= 25)
                    {
                        if (this.StartGestureRecognition(h, s, v))
                        {
                            mH = h;
                            mS = s;
                            mV = v;

                            this.GestureRecognitionSetup = true;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public bool StartGestureRecognition(int h = 0, int s = 0, int v = 0)
        {
            mFingerDistances.Clear();
            mFingers.Clear();

            var mat = mMonitor.CameraImg;
            Mat newMat = new Mat();
            mat.CopyTo(newMat);
            Mat outputMat = new Mat();
            mat.CopyTo(outputMat);

            Cv2.Blur(newMat, outputMat, new Size(3, 3));

            Mat hsvColourSpace = new Mat();
            outputMat.CopyTo(hsvColourSpace);
            Cv2.CvtColor(outputMat, hsvColourSpace, ColorConversionCodes.BGR2HSV);

            Mat mask2 = new Mat();
            hsvColourSpace.CopyTo(mask2);

            if (mH != 0 || mS != 0 || mV != 0)
            {
                // If we have a saved preset, use it
                Cv2.InRange(hsvColourSpace, InputArray.Create(new int[] { mH, mS, mV }), InputArray.Create(new int[] { 190, 255, 255 }), mask2);
            }
            else
            {
                Cv2.InRange(hsvColourSpace, InputArray.Create(new int[] { h, s, v }), InputArray.Create(new int[] { 190, 255, 255 }), mask2);
            }

            //{
            //    // Dynamic HSV value assignment
            //    Cv2.InRange(hsvColourSpace, InputArray.Create(new int[] { Cv2.GetTrackbarPos("H", "HSV Window"), Cv2.GetTrackbarPos("S", "HSV Window"), Cv2.GetTrackbarPos("V", "HSV Window") }), InputArray.Create(new int[] { 190, 255, 255 }), mask2);
            //}

            var kernal_ellipse = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(5, 5));
            var kernal_square = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(11, 11));

            Mat dilation = new Mat();
            mask2.CopyTo(dilation);
            Cv2.Dilate(mask2, dilation, kernal_ellipse);

            Mat erosion = new Mat();
            dilation.CopyTo(erosion);
            Cv2.Erode(dilation, erosion, kernal_square);

            Mat dilation2 = new Mat();
            erosion.CopyTo(dilation2);
            Cv2.Dilate(erosion, dilation2, kernal_ellipse);

            Mat filtered = new Mat();
            dilation2.CopyTo(filtered);
            Cv2.MedianBlur(dilation2, filtered, 5);

            kernal_ellipse = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(8, 8));
            Cv2.Dilate(filtered, dilation2, kernal_ellipse);

            kernal_ellipse = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(5, 5));

            Mat dilation3 = new Mat();
            dilation2.CopyTo(dilation3);
            Cv2.Dilate(filtered, dilation3, kernal_ellipse);

            Mat median = new Mat();
            filtered.CopyTo(median);
            Cv2.MedianBlur(filtered, median, 5);

            Mat ret = new Mat();
            filtered.CopyTo(ret);
            Cv2.Threshold(median, ret, 127, 255, ThresholdTypes.Binary);

            Mat thresh = new Mat();
            ret.CopyTo(thresh);
            this.ThreshholdImg = thresh;

            Cv2.FindContours(thresh, out var contours, out var hierarchy, RetrievalModes.List, ContourApproximationModes.ApproxSimple);

            double max_area = 100;
            var ci = 0;

            for (var i = 0; i < contours.Length; i++)
            {
                var cnt = contours[i];
                var area = Cv2.ContourArea(cnt);
                if (area > max_area)
                {
                    max_area = area;
                    ci = i;
                }
            }

            // Only continue if we have contours to process.
            if (contours.Length <= 0)
            {
                return false;
            }

            var cnts = contours[ci];

            // Draw the contours for debugging purposes
            Cv2.DrawContours(mat, contours, ci, Scalar.Red);

            var hull = Cv2.ConvexHull(cnts);
            var hull2 = Cv2.ConvexHullIndices(cnts);

            Mat convexityDefect = new Mat();

            Vec4i[] defects = Cv2.ConvexityDefects(cnts, hull2);
            var farDefects = new List<Tuple<Point, int>>();
            for (var i = 0; i < defects.Length; i++)
            {
                var start = new Tuple<Point, int>(cnts[i], defects[i].Item0);
                var end = new Tuple<Point, int>(cnts[i], defects[i].Item1);
                var far = new Tuple<Point, int>(cnts[i], defects[i].Item2);

                farDefects.Add(far);
            }

            var moments = Cv2.Moments(cnts);

            var cx = 0;
            var cy = 0;
            if (moments.M00 != 0)
            {
                cx = (int)moments.M10 / (int)moments.M00;
                cy = (int)moments.M01 / (int)moments.M00;
            }

            mCenterMass = new Point(cx, cy);

            var distanceBetweenDefectsToCenter = new List<double>();

            for (var i = 0; i < farDefects.Count; i++)
            {
                var x = farDefects[i];

                var distance = Math.Sqrt(Math.Pow(x.Item1.X - mCenterMass.X, 2) + Math.Pow(x.Item1.Y - mCenterMass.Y, 2));
                distanceBetweenDefectsToCenter.Add(distance);
            }

            var sortedDetectsDistances = distanceBetweenDefectsToCenter.OrderBy(val => val);

            if (sortedDetectsDistances.Count() <= 0)
            {
                return false;
            }

            mAverageDefectDistance = sortedDetectsDistances.Average();

            var finger = new List<Point>();
            for (var i = 0; i < hull.Length - 1; i++)
            {
                if ((Math.Abs(hull[i].X - hull[i + 1].X) > 20) || (Math.Abs(hull[i].Y - hull[i + 1].Y) > 20))
                {
                    // || (Math.Abs(hull[i].Y - hull[i+1].Y) > 20)
                    finger.Add(hull[i]);
                }
            }

            var sortedFinger = finger.OrderBy(val => val.Y);
            var goodFingers = new List<Point>();
            var allFingers = sortedFinger.Take(5);

            foreach (var tempFinger in allFingers)
            {
                if (tempFinger.Y < mCenterMass.Y + 40)
                {
                    goodFingers.Add(tempFinger);
                }
            }

            mFingers = goodFingers;

            for (var i = 0; i < goodFingers.Count; i++)
            {
                var distance = Math.Sqrt(Math.Pow(goodFingers[i].X - mCenterMass.X, 2) + Math.Pow(goodFingers[i].Y - mCenterMass.X, 2));
                mFingerDistances.Add(distance);
            }

            if (mFingers.Count > 0)
            {
                return this.GetRecognisedGesture(mat);
            }

            return false;
        }

        public bool GetRecognisedGesture(Mat renderMat)
        {
            Mat newMat = new Mat();
            renderMat.CopyTo(newMat);

            Cv2.Circle(newMat, mCenterMass, 3, Scalar.Red);
            Cv2.PutText(newMat, "Center", mCenterMass, HersheyFonts.HersheySimplex, 1, Scalar.White, 1);

            var result = 0;
            for (var i = 0; i < mFingers.Count; i++)
            {
                if (mFingerDistances[i] <= mAverageDefectDistance + 50)
                {
                    Cv2.Circle(newMat, mFingers[i], 3, Scalar.Blue);
                    Cv2.PutText(newMat, "Finger", mFingers[i], HersheyFonts.HersheySimplex, 1, Scalar.White, 1);

                    result++;
                }
            }

            if (result > 0 && !mEventDelay)
            {
                if (this.GestureRecognitionSetup)
                {
                    mEventDelay = true;
                    HandDetected.Invoke(this, new EventArgs());

                    switch (mFingers.Count)
                    {
                        case 0:
                            break;
                        case 1:
                            OneFingerDetected.Invoke(this, new EventArgs());
                            break;
                        case 2:
                            TwoFingersDetected.Invoke(this, new EventArgs());
                            break;
                        case 3:
                            ThreeFingersDetected.Invoke(this, new EventArgs());
                            break;
                        case 4:
                            FourFingersDetected.Invoke(this, new EventArgs());
                            break;
                        case 5:
                            FiveFingersDetected.Invoke(this, new EventArgs());
                            break;
                    }

                    Task.Run(() =>
                    {
                        System.Threading.Thread.Sleep(200);
                        mEventDelay = false;
                    });
                }
            }

            this.GestureImg = newMat;

            if (!GestureRecognitionSetup && result != 0)
            {
                if (mFingers.Count == 5)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
