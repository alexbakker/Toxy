using System;
using System.Threading;

using NAudio.Wave;
using SharpTox.Core;
using SharpTox.Av;

namespace Toxy
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

        public void Start()
        {
            if (WaveIn.DeviceCount < 1)
                throw new Exception("Insufficient input device(s)!");

            if (WaveOut.DeviceCount < 1)
                throw new Exception("Insufficient output device(s)!");

            frame_size = toxav.CodecSettings.audio_sample_rate * toxav.CodecSettings.audio_frame_duration / 1000;

            toxav.PrepareTransmission(CallIndex, false);

            WaveFormat format = new WaveFormat((int)toxav.CodecSettings.audio_sample_rate, (int)toxav.CodecSettings.audio_channels);
            wave_provider = new BufferedWaveProvider(format);
            wave_provider.DiscardOnBufferOverflow = true;

            wave_out = new WaveOut();
            //wave_out.DeviceNumber = config["device_output"];
            wave_out.Init(wave_provider);

            wave_source = new WaveIn();
            //wave_source.DeviceNumber = config["device_input"];
            wave_source.WaveFormat = format;
            wave_source.DataAvailable += wave_source_DataAvailable;
            wave_source.RecordingStopped += wave_source_RecordingStopped;
            wave_source.BufferMilliseconds = (int)toxav.CodecSettings.audio_frame_duration;
            wave_source.StartRecording();

            thread = new Thread(receive);
            thread.Start();
        }

        private void wave_source_RecordingStopped(object sender, StoppedEventArgs e)
        {
            Console.WriteLine("Recording stopped");
        }

        private void receive()
        {
            wave_out.Play();

            while (true)
            {
                short[] pcm = new short[frame_size];

                int received = toxav.ReceiveAudio(CallIndex, (int)frame_size, pcm);
                if (received > 0)
                {
                    byte[] bytes = ShortArrayToByteArray(pcm);
                    wave_provider.AddSamples(bytes, 0, bytes.Length);
                }
                else if (received != (int)ToxAvError.None)
                {
                    Console.WriteLine("Could not receive data: {0}", (ToxAvError)received);
                }
            }
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
            int size = toxav.PrepareAudioFrame(CallIndex, dest, 65535, ushorts, ushorts.Length);

            if (toxav.SendAudio(CallIndex, ref dest, size) != ToxAvError.None)
                Console.WriteLine("Could not send audio");
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
            ToxAvError error = toxav.Answer(CallIndex, ToxAvCallType.Audio);
            if (error != ToxAvError.None)
                throw new Exception("Could not answer call " + error.ToString());
        }

        public void Call(int current_number, ToxAvCallType call_type, int ringing_seconds)
        {
            toxav.Call(current_number, call_type, ringing_seconds, out CallIndex);
        }
    }
}
