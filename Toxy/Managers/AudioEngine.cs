using System;
using System.Linq;
using SharpTox.Av;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Collections.Generic;

namespace Toxy.Managers
{
    public class AudioEngine : IDisposable
    {
        private WaveInEvent _waveSource;
        private BufferedWaveProvider _waveSourceProvider;
        private MeteringSampleProvider _waveSourceMeter;

        private WaveOutEvent _waveOut;
        private BufferedWaveProvider _waveOutProvider;
        private List<short> _receivedAudioBuffer = new List<short>();

        public bool IsRecording { get; private set; }

        public WaveFormat RecordingFormat { get { return _waveOut.OutputWaveFormat; } }
        public WaveFormat PlaybackFormat { get { return _waveSource.WaveFormat; } }

        public delegate void AudioEngineRecordingVolumeChanged(float volume);
        public delegate void AudioEngineMicDataAvailable(short[] data, int sampleRate, int channels);

        public event AudioEngineRecordingVolumeChanged OnMicVolumeChanged;
        public event AudioEngineMicDataAvailable OnMicDataAvailable;

        public AudioEngine()
        {
            if (Config.Instance.RecordingDevice != null && WaveIn.DeviceCount != 0 && Config.Instance.RecordingDevice.Number <= WaveIn.DeviceCount)
            {
                var capabilities = WaveIn.GetCapabilities(Config.Instance.RecordingDevice.Number);
                SetRecordingSettings(48000, capabilities.Channels > 2 ? 2 : capabilities.Channels);
            }

            if (Config.Instance.PlaybackDevice != null && WaveOut.DeviceCount != 0 && Config.Instance.PlaybackDevice.Number <= WaveOut.DeviceCount)
            {
                var capabilities = WaveOut.GetCapabilities(Config.Instance.PlaybackDevice.Number);
                SetPlaybackSettings(48000, capabilities.Channels > 2 ? 2 : capabilities.Channels);
            }
        }

        public void SetPlaybackSettings(int sampleRate, int channels)
        {
            //TODO: what if our friend is sending stereo but our output device only supports mono? write a conversion method for that
            var capabilities = WaveOut.GetCapabilities(Config.Instance.PlaybackDevice.Number);
            var waveOutFormat = new WaveFormat(sampleRate, channels);

            if (_waveOut != null)
                _waveOut.Dispose();

            _waveOutProvider = new BufferedWaveProvider(waveOutFormat);
            _waveOutProvider.DiscardOnBufferOverflow = true;

            _waveOut = new WaveOutEvent();
            _waveOut.DeviceNumber = Config.Instance.PlaybackDevice.Number;
            _waveOut.Init(_waveOutProvider);
            _waveOut.Play();

            Debugging.Write(string.Format("Changed playback config to: samplingRate: {0}, channels: {1}", sampleRate, channels));
        }

        public void SetRecordingSettings(int sampleRate, int channels)
        {
            var capabilities = WaveIn.GetCapabilities(Config.Instance.RecordingDevice.Number);
            var waveSourceFormat = new WaveFormat(sampleRate, channels);

            if (_waveSource != null)
                _waveSource.Dispose();

            _waveSource = new WaveInEvent();
            _waveSource.BufferMilliseconds = 20;
            _waveSource.WaveFormat = waveSourceFormat;
            _waveSource.DeviceNumber = Config.Instance.RecordingDevice.Number;
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
            if (_waveOutProvider == null)
                return;

            //what is the length of this audio frame?
            int audioLength = ((frame.Data.Length / frame.Channels) * 1000) / frame.SamplingRate;

            //what should the length of this frame have been? (we want 20ms to send to the provider)
            int wantedDataLength = ((20 * frame.SamplingRate) / 1000) * frame.Channels;

            if (wantedDataLength != frame.Data.Length)
            {
                //if we didn't get the amount of data we wanted, we need to buffer it
                _receivedAudioBuffer.AddRange(frame.Data);
                if (_receivedAudioBuffer.Count == wantedDataLength)
                {
                    short[] shorts = _receivedAudioBuffer.ToArray();
                    byte[] bytes = ShortsToBytes(shorts);

                    _waveOutProvider.AddSamples(bytes, 0, bytes.Length);
                    _receivedAudioBuffer.Clear();
                }
            }
            else
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
