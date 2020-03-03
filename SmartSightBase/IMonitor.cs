using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSightBase
{
    public interface IMonitor
    {
        Mat CameraImg { get; }
    }
}
