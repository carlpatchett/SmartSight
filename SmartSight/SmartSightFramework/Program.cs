using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;

namespace SmartSightFramework
{
    class Program
    {

        static void Main(string[] args)
        {
            Mat Image = CvInvoke.Imread("C:\\Users\\carlp\\Desktop\\unknown.png", ImreadModes.AnyColor);

            CvInvoke.NamedWindow("Display Window", NamedWindowType.AutoSize);

            CvInvoke.Imshow("Display Window", Image);

            CvInvoke.WaitKey(0);
            return;
        }
    }
}
