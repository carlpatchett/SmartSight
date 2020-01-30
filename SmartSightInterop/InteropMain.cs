using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SmartSightInterop
{
    public static class InteropMain
    {
        [DllImport("D:\\Repos\\SmartSight\\x64\\Debug\\SmartSightPlusPlus.dll")]
        public static extern IntPtr CreateNewMonitor();

        [DllImport("D:\\Repos\\SmartSight\\x64\\Debug\\SmartSightPlusPlus.dll")]
        public static extern int StartMonitor();

        [DllImport("D:\\Repos\\SmartSight\\x64\\Debug\\SmartSightPlusPlus.dll")]
        public static extern int StopMonitor();

        [DllImport("D:\\Repos\\SmartSight\\x64\\Debug\\SmartSightPlusPlus.dll")]
        public static extern int CheckMarkerDetection();
    }
}
