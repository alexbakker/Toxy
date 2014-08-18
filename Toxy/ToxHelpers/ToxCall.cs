using System;
using System.Threading;
using NAudio.Wave;
using SharpTox.Av;
using SharpTox.Core;

namespace Toxy.ToxHelpers
{
    class ToxCall
    {
        private Tox tox;
        private ToxAv toxav;

        private WaveIn wave_source;
        private WaveOut wave_out;
        private BufferedWaveProvider wave_provider;

        private Thread thread;

        private uint frame_size;

        public int CallIndex;
        public int FriendNumber;

        public ToxCall(Tox tox, ToxAv toxav, int callindex, int friendnumber)
        {
            this.tox = tox;
            this.toxav = toxav;
            this.FriendNumber = friendnumber;

            CallIndex = callindex;
        }

        public void Start(int input, int output)
        {
            if (WaveIn.DeviceCount < 1)
                throw new Exception("Insufficient input device(s)!");

            if (WaveOut.DeviceCount < 1)
                throw new Exception("Insufficient output device(s)!");

            frame_size = toxav.CodecSettings.AudioSampleRate * toxav.CodecSettings.AudioFrameDuration / 1000;

            //who doesn't love magic numbers?!
            toxav.PrepareTransmission(CallIndex, 3, 40, false);

            WaveFormat format = new WaveFormat((int)toxav.CodecSettings.AudioSampleRate, (int)toxav.CodecSettings.AudioChannels);
            wave_provider = new BufferedWaveProvider(format);
            wave_provider.DiscardOnBufferOverflow = true;

            wave_out = new WaveOut();

            if (output != -1)
                wave_out.DeviceNumber = output - 1;

            wave_out.Init(wave_provider);

            wave_source = new WaveIn();

            if (input != -1)
                wave_source.DeviceNumber = input - 1;

            wave_source.WaveFormat = format;
            wave_source.DataAvailable += wave_source_DataAvailable;
            wave_source.RecordingStopped += wave_source_RecordingStopped;
            wave_source.BufferMilliseconds = (int)toxav.CodecSettings.AudioFrameDuration;
            wave_source.StartRecording();

            wave_out.Play();
        }

        private void wave_source_RecordingStopped(object sender, StoppedEventArgs e)
        {
            Console.WriteLine("Recording stopped");
        }

        public void ProcessAudioFrame(short[] frame, int frame_size)
        {
            byte[] bytes = ShortArrayToByteArray(frame);
            wave_provider.AddSamples(bytes, 0, bytes.Length);
        }

        private byte[] ShortArrayToByteArray(short[] shorts)
        {
            byte[] bytes = new byte[shorts.Length * 2];

            for (int i = 0; i < shorts.Length; ++i)
            {
                bytes[2 * i] = (byte)shorts[i];
                bytes[2 * i + 1] = (byte)(shorts[i] >> 8);
            }

            return bytes;
        }

        private void wave_source_DataAvailable(object sender, WaveInEventArgs e)
        {
            ushort[] ushorts = new ushort[e.Buffer.Length / 2];
            Buffer.BlockCopy(e.Buffer, 0, ushorts, 0, e.Buffer.Length);

            byte[] dest = new byte[65535];
            int size = toxav.PrepareAudioFrame(CallIndex, dest, 65535, ushorts);

            ToxAvError error = toxav.SendAudio(CallIndex, dest, size);
            if (error != ToxAvError.None)
                Console.WriteLine("Could not send audio: {0}", error);
        }

        public void Stop()
        {
            //TODO: we might want to block here until RecordingStopped and PlaybackStopped are fired

            if (wave_source != null)
            {
                wave_source.StopRecording();
                wave_source.Dispose();
            }

            if (wave_out != null)
            {
                wave_out.Stop();
                wave_out.Dispose();
            }

            if (thread != null)
            {
                thread.Abort();
                thread.Join();
            }

            toxav.KillTransmission(CallIndex);
            toxav.Hangup(CallIndex);
        }

        public void Answer()
        {
            ToxAvError error = toxav.Answer(CallIndex, ToxAv.DefaultCodecSettings);
            if (error != ToxAvError.None)
                throw new Exception("Could not answer call " + error.ToString());
        }

        public void Call(int current_number, ToxAvCodecSettings settings, int ringing_seconds)
        {
            toxav.Call(current_number, settings, ringing_seconds, out CallIndex);
        }
    }
}
