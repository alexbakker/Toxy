using System;
using System.Linq;
using SharpTox.Av;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Toxy.Managers
{
    public class AudioEngine : IDisposable
    {
        private WaveInEvent _waveSource;
        private BufferedWaveProvider _waveSourceProvider;
        private MeteringSampleProvider _waveSourceMeter;

        private WaveOut _waveOut;
        private BufferedWaveProvider _waveOutProvider;

        public bool IsRecording { get; private set; }

        public delegate void AudioEngineRecordingVolumeChanged(float volume);
        public delegate void AudioEngineMicDataAvailable(short[] data, int sampleRate, int channels);

        public event AudioEngineRecordingVolumeChanged OnMicVolumeChanged;
        public event AudioEngineMicDataAvailable OnMicDataAvailable;

        public AudioEngine()
        {
            if (Config.Instance.RecordingDevice != null && Config.Instance.RecordingDevice.Number <= WaveIn.DeviceCount)
            {
                var capabilities = WaveIn.GetCapabilities(Config.Instance.RecordingDevice.Number);
                var _waveSourceFormat = new WaveFormat(48000, capabilities.Channels);

                _waveSource = new WaveInEvent();
                _waveSource.BufferMilliseconds = 20; //we only want to process one frame each time
                _waveSource.WaveFormat = _waveSourceFormat;
                _waveSource.DeviceNumber = Config.Instance.RecordingDevice.Number;
                _waveSource.DataAvailable += waveSource_DataAvailable;

                _waveSourceProvider = new BufferedWaveProvider(_waveSourceFormat);
                _waveSourceProvider.DiscardOnBufferOverflow = true;

                _waveSourceMeter = new MeteringSampleProvider(_waveSourceProvider.ToSampleProvider());
                _waveSourceMeter.StreamVolume += _waveSourceMeter_StreamVolume;
            }

            if (Config.Instance.PlaybackDevice != null && Config.Instance.PlaybackDevice.Number <= WaveOut.DeviceCount)
            {
                var capabilities = WaveIn.GetCapabilities(Config.Instance.PlaybackDevice.Number);
                var _waveOutFormat = new WaveFormat(48000, capabilities.Channels);

                _waveOutProvider = new BufferedWaveProvider(_waveOutFormat);
                _waveOutProvider.DiscardOnBufferOverflow = true;

                _waveOut = new WaveOut();
                _waveOut.DeviceNumber = Config.Instance.PlaybackDevice.Number;
                _waveOut.Init(_waveOutProvider);
                _waveOut.Play();
            }
        }

        private void _waveSourceMeter_StreamVolume(object sender, StreamVolumeEventArgs e)
        {
            if (OnMicVolumeChanged != null)
                OnMicVolumeChanged(e.MaxSampleValues.Average());
        }

        private void waveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            short[] shorts = BytesToShorts(e.Buffer);

            if (OnMicDataAvailable != null)
                OnMicDataAvailable(shorts, _waveSource.WaveFormat.SampleRate, _waveSource.WaveFormat.Channels);

            _waveSourceProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);
            _waveSourceMeter.Read(new float[e.BytesRecorded], 0, e.BytesRecorded);
        }

        public void StartRecording()
        {
            if (_waveSource != null)
            {
                _waveSource.StartRecording();
                IsRecording = true;
            }
        }

        public void ProcessAudioFrame(ToxAvAudioFrame frame)
        {
            if (_waveOutProvider != null)
            {
                byte[] bytes = ShortsToBytes(frame.Data);
                _waveOutProvider.AddSamples(bytes, 0, bytes.Length);
            }
        }

        public void Dispose()
        {
            if (_waveSource != null)
            {
                if (IsRecording)
                    _waveSource.StopRecording();

                _waveSource.Dispose();
            }

            if (_waveOut != null)
            {
                _waveOut.Stop();
                _waveOut.Dispose();
            }
        }

        private static short[] BytesToShorts(byte[] bytes)
        {
            short[] shorts = new short[bytes.Length / 2];
            Buffer.BlockCopy(bytes, 0, shorts, 0, bytes.Length);

            return shorts;
        }

        private static byte[] ShortsToBytes(short[] shorts)
        {
            byte[] bytes = new byte[shorts.Length * 2];
            Buffer.BlockCopy(shorts, 0, bytes, 0, bytes.Length);

            return bytes;
        }
    }
}
