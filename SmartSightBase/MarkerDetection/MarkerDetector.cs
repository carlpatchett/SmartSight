using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using SmartSightBase.GeometryTypes;

namespace SmartSightBase
{
    public class MarkerDetector
    {
        private float mMinContourLengthAllowed = 100.0f;
        private Size mMarkerSize = new Size(100,100);
        private int mCellSize = 100 / 7;
        private Mat camMatrix = new Mat();
        private Mat mDistCoeff = new Mat();
        private Mat mGrayscale = new Mat();
        private Mat mThresholdImg = new Mat();

        private List<List<Point2f>> mDetectedMarkers = new List<List<Point2f>>();
        private List<List<Point>> mContours = new List<List<Point>>();
        private List<Point3f> mMarkerCorners3d = new List<Point3f>();
        private List<Point2f> mMarkerCorners2d = new List<Point2f>();

        public event EventHandler MarkerDetected = (s, e) => { };

        public List<List<Point2f>> GoodMarkers { get; set; } = new List<List<Point2f>>();

        public Transformation Transformation { get; set; }

        public int[] TemplateMarker { get; set; }

        public Mat DetectedMarkerImg { get; set; } = new Mat();

        public MarkerDetector(CameraCalibration calibration, Monitor monitor)
        {
            camMatrix = new Mat(3, 3, MatType.CV_32F, calibration.GetIntrinsic().Data[0]);
            mDistCoeff = new Mat(4, 1, MatType.CV_32F, calibration.GetDistorsion().Data[0]);

            mMarkerCorners3d.Add(new Point3f(-0.5f, -0.5f, 0));
            mMarkerCorners3d.Add(new Point3f(+0.5f, -0.5f, 0));
            mMarkerCorners3d.Add(new Point3f(+0.5f, +0.5f, 0));
            mMarkerCorners3d.Add(new Point3f(-0.5f, +0.5f, 0));

            mMarkerCorners2d.Add(new Point2f(0, 0));
            mMarkerCorners2d.Add(new Point2f(((float)mMarkerSize.Width - 1), 0));
            mMarkerCorners2d.Add(new Point2f(((float)mMarkerSize.Width - 1), (float)(mMarkerSize.Height - 1)));
            mMarkerCorners2d.Add(new Point2f(0, (float)(mMarkerSize.Height - 1)));

            this.TemplateMarker = new int[25]{ 1, 1, 1, 0, 1,
                                               0, 0, 0, 1, 0,
                                               1, 1, 1, 1, 0,
                                               1, 1, 1, 1, 0,
                                               1, 1, 1, 0, 0 };
        }

        public void FindMarkers(Monitor monitor, bool showImg)
        {
            this.PrepareImage(monitor.CameraImg, mGrayscale);

            this.PerformThreshold(mGrayscale, mThresholdImg);

            this.FindContours(mThresholdImg, mContours, mGrayscale.Cols / 5);

            this.FindCandidates(mContours, mDetectedMarkers);

            this.GoodMarkers = this.RecognizedMarkers(mGrayscale, mDetectedMarkers, showImg);

            this.EstimatePosition(this.GoodMarkers);
        }

        protected void PrepareImage(Mat bgrMat, Mat grayscale)
        {
            Cv2.CvtColor(bgrMat, grayscale, ColorConversionCodes.BGR2GRAY);
        }

        protected void PerformThreshold(Mat grayscale, Mat thresholdImg)
        {
            Cv2.AdaptiveThreshold(grayscale, thresholdImg, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv, 7, 7);
        }

        protected void FindContours(Mat thresholdImg, List<List<Point>> contours, int minContourPointsAllowed)
        {
            Point[][] allContours;
            HierarchyIndex[] hierarchyIndexes;
            Cv2.FindContours(thresholdImg, out allContours, out hierarchyIndexes, RetrievalModes.List, ContourApproximationModes.ApproxNone);

            contours.Clear();

            for (var i = 0; i < allContours.Length; i++)
            {
                var contourSize = allContours[i].Length;
                if (contourSize > minContourPointsAllowed)
                {
                    contours.Add(allContours[i].ToList());
                }
            }
        }

        protected float Perimeter(List<Point2f> list)
        {
            float sum = 0, dx, dy;

            for (var i = 0; i < list.Count; i++)
            {
                var i2 = (i + 1) % list.Count;

                dx = list[i].X - list[i2].X;
                dy = list[i].Y - list[i2].Y;

                sum += (float)Math.Sqrt(dx * dx + dy * dy);
            }

            return sum;
        }

        protected void FindCandidates(List<List<Point>> contours, List<List<Point2f>> marks)
        {
            //var approxCurve = new List<Point>();
            var possibleMarkers = new List<List<Point2f>>();

            // For each contour, analyze if it is a parallelepiped likely to be the	marker
            for (var i = 0; i < contours.Count; i++)
            {
                //Approximate to a polygon
                var eps = contours[i].Count * 0.05;

                try
                {
                    var inputArray = InputArray.Create(contours[i]);
                    var outputArray = OutputArray.Create(contours[i]);
                }
                catch(ArgumentNullException)
                {
                    // Chomp chomp, no idea
                }

                var approxCurve = Cv2.ApproxPolyDP(contours[i], eps, true).ToList();

                //Cv2.ApproxPolyDP(inputArray, outputArray, eps, true);

                // We interested only in polygons that contains only four points
                if (approxCurve.Count != 4)
                    continue;

                // And they have to be convex
                if (!Cv2.IsContourConvex(approxCurve))
                    continue;

                // Ensure that the distance between consecutive points is large enough
                var minDist = float.MaxValue;

                for (var j = 0; j < 4; j++)
                {
                    var side = approxCurve[j] - approxCurve[(j + 1) % 4];
                    var squaredSideLength = (float)side.DotProduct(side);
                    minDist = Math.Min(minDist, squaredSideLength);
                }

                // Check that distance is not very small
                if (minDist < mMinContourLengthAllowed)
                    continue;

                // All tests are passed. Save marker candidate:
                var m = new List<Point2f>();
                for (var j = 0; j < 4; j++)
                    m.Add(new Point2f(approxCurve[j].X, approxCurve[j].Y));

                // Sort the points in anti-clockwise order
                // Trace a line between the first and second point.
                // If the third point is at the right side, then the points are anticlockwise
                var v1 = m[1] - m[0];
                var v2 = m[2] - m[0];

                double o = (v1.X * v2.Y) - (v1.Y * v2.X);
                if (o < 0.0) //if the third point is in the left side,	then sort in anti - clockwise order
                {
                    var temp = m[1];
                    var temp2 = m[3];
                    m[1] = temp2;
                    m[3] = temp;
                }

                possibleMarkers.Add(m);
            }

            // Remove these elements which corners are too close to each other.
            // First detect candidates for removal:
            var tooNearCandidates = new List<Tuple<int, int>>();
            for (var i = 0; i < possibleMarkers.Count; i++)
            {
                var m1 = possibleMarkers[i];

                //calculate the average distance of each corner to the nearest corner of the other marker candidate
                for (var j = i + 1; j < possibleMarkers.Count; j++)
                {
                    var m2 = possibleMarkers[j];
                    float distSquared = 0;

                    for (var c = 0; c < 4; c++)
                    {
                        var v = m1[c] - m2[c];
                        distSquared += (float)v.DotProduct(v);
                    }

                    distSquared /= 4;

                    if (distSquared < 100)
                    {
                        tooNearCandidates.Add(new Tuple<int, int>(i, j));
                    }
                }
            }

            // Mark for removal the element of the pair with smaller perimeter
            var removalMask = Enumerable.Repeat(false, possibleMarkers.Count).ToList();

            for (var i = 0; i < tooNearCandidates.Count; i++)
            {
                var p1 = this.Perimeter(possibleMarkers[tooNearCandidates[i].Item1]);
                var p2 = this.Perimeter(possibleMarkers[tooNearCandidates[i].Item2]);
                int removalIndex;

                if (p1 > p2)
                    removalIndex = tooNearCandidates[i].Item2;
                else
                    removalIndex = tooNearCandidates[i].Item1;

                removalMask[removalIndex] = true;
            }

            // Return candidates
            mDetectedMarkers.Clear();
            for (var i = 0; i < possibleMarkers.Count; i++)
            {
                if (!removalMask.ElementAt(i))
                    mDetectedMarkers.Add(possibleMarkers[i]);
            }
        }

        protected List<List<Point2f>> RecognizedMarkers(Mat gray, List<List<Point2f>> detectedMarkers, bool showImg)
        {
            this.GoodMarkers = new List<List<Point2f>>();
            for (var i = 0; i < detectedMarkers.Count; i++)
            {
                var canonicalMarker = new Mat();
                var marker = detectedMarkers[i];

                // Find the perspective transfomation that brings current marker to rectangular form
                var m = Cv2.GetPerspectiveTransform(marker, mMarkerCorners2d);

                // Transform image to get a canonical marker image
                Cv2.WarpPerspective(gray, canonicalMarker, m, mMarkerSize);

                //threshold image
                Cv2.Threshold(canonicalMarker, canonicalMarker, 125, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

                //Read the code
                Mat bitMatrix = Mat.Zeros(5, 5, MatType.CV_8UC1);

                //get information (for each innner square, determine if it is black or white)
                for (var y = 0; y < 5; y++)
                    for (var x = 0; x < 5; x++)
                    {
                        var cellX = (x + 1) * mCellSize;
                        var cellY = (y + 1) * mCellSize; 
                        var cell = canonicalMarker.SubMat(new Rect(cellX, cellY, mCellSize, mCellSize));

                        var nZ = Cv2.CountNonZero(cell);
                        if (nZ > (mCellSize * mCellSize / 2))
                            bitMatrix.Set<byte>(y, x, 1);
                    }

                //check all possible rotations
                var rotations = new Mat[4];
                var distances = new int[4];
                rotations[0] = bitMatrix;
                distances[0] = this.HammDistMarker(rotations[0]);
                var minDist = new Tuple<int, int>(distances[0], 0);

                for (var j = 1; j < 4; j++)
                {
                    //get the hamming distance to the nearest possible word
                    rotations[j] = this.Rotate(rotations[j - 1]);
                    distances[j] = this.HammDistMarker(rotations[j]);

                    if (distances[j] < minDist.Item1)
                    {
                        minDist = new Tuple<int, int>(distances[j], j);
                    }
                }

                if (minDist.Item1 == 0)
                {
                    var nRotations = minDist.Item2;
                    //sort the points so that they are always in the same order
                    //no matter the camera orientation
                    var rotated = marker.Skip(marker.IndexOf(marker.First()) + 4 - nRotations).Concat(marker.Take(marker.IndexOf(marker.First()) + 4 - nRotations)).ToList();

                    this.GoodMarkers.Add(rotated);
                }

            }

            //Marker locaiton refinement
            if (this.GoodMarkers.Count > 0)
            {
                var preciseCorners = Enumerable.Repeat(new Point2f(), 4 * this.GoodMarkers.Count).ToList();

                for (var i = 0; i < this.GoodMarkers.Count; i++)
                {
                    var marker = this.GoodMarkers[i];

                    for (var c = 0; c < 4; c++)
                    {
                        preciseCorners[i * 4 + c] = marker[c];
                    }

                    Cv2.CornerSubPix(gray, preciseCorners, new Size(5, 5), new Size(-1, -
                            1), new TermCriteria(CriteriaType.MaxIter, 30, 0.1));

                    //copy back
                    for (var j = 0; j < this.GoodMarkers.Count; j++)
                    {
                        var marker2 = this.GoodMarkers[i];
                        for (var c = 0; c < 4; c++)
                        {
                            marker2[c] = preciseCorners[i * 4 + c];
                        }
                    }
                }

                if (showImg)
                {
                    Cv2.CvtColor(gray, this.DetectedMarkerImg, ColorConversionCodes.GRAY2BGR);
                    var thickness = 2;
                    var color = new Scalar(0, 0, 255);

                    for (var i = 0; i < this.GoodMarkers.Count; i++)
                    {
                        var current_mark = this.GoodMarkers[i];
                        var int_current_mark = Enumerable.Repeat(new Point2f(), (this.GoodMarkers[i].Count)).ToArray();
                        current_mark.CopyTo(int_current_mark);
                        Cv2.Line(this.DetectedMarkerImg, (int)int_current_mark[0].X, (int)int_current_mark[0].Y, (int)int_current_mark[1].X, (int)int_current_mark[1].Y, color, thickness, LineTypes.AntiAlias);
                        Cv2.Line(this.DetectedMarkerImg, (int)int_current_mark[1].X, (int)int_current_mark[1].Y, (int)int_current_mark[2].X, (int)int_current_mark[2].Y, color, thickness, LineTypes.AntiAlias);
                        Cv2.Line(this.DetectedMarkerImg, (int)int_current_mark[2].X, (int)int_current_mark[2].Y, (int)int_current_mark[3].X, (int)int_current_mark[3].Y, color, thickness, LineTypes.AntiAlias);
                        Cv2.Line(this.DetectedMarkerImg, (int)int_current_mark[3].X, (int)int_current_mark[3].Y, (int)int_current_mark[0].X, (int)int_current_mark[0].Y, color, thickness, LineTypes.AntiAlias);
                    }
                }

                MarkerDetected.Invoke(this, new EventArgs());
            }

            return this.GoodMarkers;
        }

        protected Mat Rotate(Mat matrix)
        {
            var outMatrix = new Mat();
            matrix.CopyTo(outMatrix);

            for (var i = 0; i < matrix.Rows; i++)
            {
                for (var j = 0; j < matrix.Cols; j++)
                {
                    outMatrix.Set<byte>(i, j, matrix.At<byte>(matrix.Cols - j - 1, i));
                }
            }

            return outMatrix;
        }


        protected int HammDistMarker(Mat bits)
        {
            var dist = 0;

            for (var y = 0; y < 5; y++)
            {
                var minSum = (int)1e5; //hamming distance to each possible word

                for (var p = 0; p < 5; p++)
                {
                    var sum = 0;

                    //now, count
                    for (var x = 0; x < 5; x++)
                    {
                        sum += bits.At<byte>(y, x) == this.TemplateMarker[p * 5 + x] ? 0 : 1;
                    }

                    if (minSum > sum)
                        minSum = sum;
                }

                //do the and
                dist += minSum;
            }

            return dist;
        }

        protected void EstimatePosition(List<List<Point2f>> detectedMarkers)
        {
            for (var i = 0; i < detectedMarkers.Count; i++)
            {
                var m = detectedMarkers[i];
                Mat Rvec = new Mat();
                Mat Tvec = new Mat();
                Mat raux = new Mat(), taux = new Mat();
                var inputArrayCorners = InputArray.Create(mMarkerCorners3d);
                var inputArrayImage = InputArray.Create(m);
                var outputArrayRaux = OutputArray.Create(raux);
                var outputArrayTaux = OutputArray.Create(taux);

                Cv2.SolvePnP(inputArrayCorners, inputArrayImage, camMatrix, mDistCoeff, raux, taux);

                raux.ConvertTo(Rvec, MatType.CV_32F);
                taux.ConvertTo(Tvec, MatType.CV_32F);

                var rotMat = new Mat(3, 3, MatType.CV_32F);
                Cv2.Rodrigues(Rvec, rotMat);

                // Copy to transformation matrix
                for (var col = 0; col < 3; col++)
                {
                    for (var row = 0; row < 3; row++)
                    {
                        if (this.Transformation == null)
                            this.Transformation = new Transformation();

                        this.Transformation.Rotation().Mat[row, col] = rotMat.Get<float>(row, col); // Copy rotation component
                    }

                    this.Transformation.Translation().Data[col] = Tvec.Get<float>(col); // Copy translation component
                }

                // Since solvePnP finds camera location, w.r.t to marker pose, to get marker pose w.r.t to the camera we invert it.
                this.Transformation = this.Transformation.GetInverted();
            }
        }
    }
}
