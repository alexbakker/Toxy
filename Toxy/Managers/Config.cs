using System;
using NAudio.Wave;
using Toxy.ViewModels;

namespace Toxy.Managers
{
    public class Config
    {
        private static Config _instance;
        public static Config Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Config();

                return _instance;
            }
        }

        public DeviceInfo RecordingDevice { get; set; }
        public DeviceInfo PlaybackDevice { get; set; }
    }
}
