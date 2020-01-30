using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace SmartSightBase
{
    class Program
    {
        private static float[] mCameraMatrix = new float[9] { 612.84f, 0.0f, 326.46f, 0.0f, 612.84f, 289.42f, 0.0f, 0.0f, 1.0f };
        private static MarkerDetector mMarkerDetector = new MarkerDetector(new CameraCalibration(mCameraMatrix[0], mCameraMatrix[4], mCameraMatrix[2], mCameraMatrix[5]));

        static void Main(string[] args)
        {
            using (var camera = new VideoCapture(0))
            {
                using (var wind = new Window("Capture camera"))
                {
                    while (true)
                    {
                        var img = camera.RetrieveMat();
                        wind.Image = img;
                        mMarkerDetector.FindMarkers(img, true);
                        Cv2.WaitKey(1);
                    }
                }
            }
        }
    }
}
