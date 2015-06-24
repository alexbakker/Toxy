using System;
using System.Linq;
using System.Drawing;

using AForge.Video.DirectShow;

namespace Toxy.Managers
{
    public class VideoEngine : IDisposable
    {
        private VideoCaptureDevice _captureDevice;

        public delegate void VideoEngineFrameAvailable(Bitmap frame);
        public event VideoEngineFrameAvailable OnFrameAvailable;

        public VideoEngine()
        {
            foreach (FilterInfo device in new FilterInfoCollection(FilterCategory.VideoInputDevice))
            {
                if (Config.Instance.VideoDevice != null && device.Name != Config.Instance.VideoDevice.Name)
                    continue;

                //just pick the setting with the highest res for now
                _captureDevice = new VideoCaptureDevice(device.MonikerString);
                _captureDevice.VideoResolution = _captureDevice.VideoCapabilities.OrderBy(c => (c.FrameSize.Width * c.FrameSize.Height)).First();
                _captureDevice.NewFrame += CaptureDevice_NewFrame;
            }
        }

        public bool DisplayPropertyWindow(IntPtr handle)
        {
            try { _captureDevice.DisplayPropertyPage(handle); }
            catch { return false; }

            return true;
        }

        private void CaptureDevice_NewFrame(object sender, AForge.Video.NewFrameEventArgs e)
        {
            if (OnFrameAvailable != null)
                OnFrameAvailable(e.Frame);
        }

        public void StartRecording()
        {
            if (_captureDevice != null && !_captureDevice.IsRunning)
                _captureDevice.Start();
        }

        public void Dispose()
        {
            if (_captureDevice != null)
            {
                if (_captureDevice.IsRunning)
                    _captureDevice.SignalToStop();
            }
        }
    }
}
