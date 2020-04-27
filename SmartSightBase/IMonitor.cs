using OpenCvSharp;

namespace SmartSightBase
{
    public interface IMonitor
    {
        Mat CameraImg { get; }
    }
}
