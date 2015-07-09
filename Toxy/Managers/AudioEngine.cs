using System;
using System.Linq;
using SharpTox.Av;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.CoreAudioApi;

namespace Toxy.Managers
{
    public class AudioEngine : IDisposable
    {
        private WasapiCapture _waveSource;
        private BufferedWaveProvider _waveSourceProvider;
        private MeteringSampleProvider _waveSourceMeter;

        private WasapiOut _waveOut;
        private BufferedWaveProvider _waveOutProvider;

        public bool IsRecording { get; private set; }

        public WaveFormat RecordingFormat { get { return _waveOut.OutputWaveFormat; } }
        public WaveFormat PlaybackFormat { get { return _waveSource.WaveFormat; } }

        public delegate void AudioEngineRecordingVolumeChanged(float volume);
        public delegate void AudioEngineMicDataAvailable(short[] data, int sampleRate, int channels);

        public event AudioEngineRecordingVolumeChanged OnMicVolumeChanged;
        public event AudioEngineMicDataAvailable OnMicDataAvailable;

        public AudioEngine()
        {
            var deviceEnumerator = new MMDeviceEnumerator();

            if (Config.Instance.RecordingDevice != null)
            {
                var device = deviceEnumerator.GetDevice(Config.Instance.RecordingDevice.ID);
                int channels = device.AudioClient.MixFormat.Channels;

                SetRecordingSettings(device, 48000, channels > 2 ? 2 : channels);
            }

            if (Config.Instance.PlaybackDevice != null)
            {
                var device = deviceEnumerator.GetDevice(Config.Instance.PlaybackDevice.ID);
                int channels = device.AudioClient.MixFormat.Channels;

                SetPlaybackSettings(device, 48000, channels > 2 ? 2 : channels);
            }
        }

        public void SetPlaybackSettings(MMDevice device, int sampleRate, int channels)
        {
            //TODO: what if our friend is sending stereo but our output device only supports mono? write a conversion method for that
            var waveOutFormat = new WaveFormat(sampleRate, channels);

            if (_waveOut != null)
                _waveOut.Dispose();

            _waveOutProvider = new BufferedWaveProvider(waveOutFormat);
            _waveOutProvider.DiscardOnBufferOverflow = true;

            _waveOut = new WasapiOut(device, AudioClientShareMode.Shared, true, 0);
            _waveOut.Init(_waveOutProvider);
            _waveOut.Play();

            Debugging.Write(string.Format("Changed playback config to: samplingRate: {0}, channels: {1}", sampleRate, channels));
        }

        public void SetRecordingSettings(MMDevice device, int sampleRate, int channels)
        {
            var waveSourceFormat = new WaveFormat(sampleRate, channels);

            if (_waveSource != null)
                _waveSource.Dispose();

            _waveSource = new WasapiCapture(device);
            //_waveSource.BufferMilliseconds = 20;
            _waveSource.WaveFormat = waveSourceFormat;
            _waveSource.ShareMode = AudioClientShareMode.Exclusive;
            _waveSource.DataAvailable += waveSource_DataAvailable;

            _waveSourceProvider = new BufferedWaveProvider(waveSourceFormat);
            _waveSourceProvider.DiscardOnBufferOverflow = true;

            _waveSourceMeter = new MeteringSampleProvider(_waveSourceProvider.ToSampleProvider());
            _waveSourceMeter.StreamVolume += _waveSourceMeter_StreamVolume;

            Debugging.Write(string.Format("Changed recording config to: samplingRate: {0}, channels: {1}", sampleRate, channels));
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
