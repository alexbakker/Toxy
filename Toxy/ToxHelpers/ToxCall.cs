using System;
using System.Threading;
using System.Diagnostics;

using NAudio.Wave;

using SharpTox.Av;
using SharpTox.Core;

namespace Toxy.ToxHelpers
{
    class ToxGroupCall : ToxCall
    {
        public int GroupNumber;

        public ToxGroupCall(ToxAv toxav, int groupNumber)
            : base(toxav)
        {
            GroupNumber = groupNumber;
        }

        public override void Start(int input, int output, ToxAvCodecSettings settings)
        {
            WaveFormat outFormat = new WaveFormat((int)settings.AudioSampleRate, (int)settings.AudioChannels);
            wave_provider = new BufferedWaveProvider(outFormat);
            wave_provider.DiscardOnBufferOverflow = true;

            if (WaveIn.DeviceCount > 0)
            {
                wave_source = new WaveIn();

                if (input != -1)
                    wave_source.DeviceNumber = input - 1;

                WaveFormat inFormat = new WaveFormat((int)ToxAv.DefaultCodecSettings.AudioSampleRate, 1);

                wave_source.WaveFormat = inFormat;
                wave_source.DataAvailable += wave_source_DataAvailable;
                wave_source.RecordingStopped += wave_source_RecordingStopped;
                wave_source.BufferMilliseconds = ToxAv.DefaultCodecSettings.AudioFrameDuration;
                wave_source.StartRecording();
            }

            if (WaveOut.DeviceCount > 0)
            {
                wave_out = new WaveOut();

                if (output != -1)
                    wave_out.DeviceNumber = output - 1;

                wave_out.Init(wave_provider);
                wave_out.Play();
            }
        }

        public override void Stop()
        {
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

            if (timer != null)
                timer.Dispose();
        }

        protected override void wave_source_DataAvailable(object sender, WaveInEventArgs e)
        {
            short[] shorts = Array.ConvertAll(e.Buffer, b => (short)b);

            if (!toxav.GroupSendAudio(GroupNumber, shorts, wave_source.WaveFormat.Channels, wave_source.WaveFormat.SampleRate))
                Debug.WriteLine("Could not send audio to groupchat #{0}", GroupNumber);
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public override void Call(int current_number, ToxAvCodecSettings settings, int ringing_seconds)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public override void Answer()
        {
            throw new NotImplementedException();
        }
    }

    class ToxCall
    {
        protected ToxAv toxav;

        protected WaveIn wave_source;
        protected WaveOut wave_out;
        protected BufferedWaveProvider wave_provider;

        protected Timer timer;
        public int TotalSeconds = 0;

        public int CallIndex;
        public int FriendNumber;

        public ToxCall(ToxAv toxav, int callindex, int friendnumber)
        {
            this.toxav = toxav;
            this.FriendNumber = friendnumber;

            CallIndex = callindex;
        }

        /// <summary>
        /// Dummy. Don't use this.
        /// </summary>
        /// <param name="toxav"></param>
        public ToxCall(ToxAv toxav)
        {
            this.toxav = toxav;
        }

        public virtual void Start(int input, int output, ToxAvCodecSettings settings)
        {
            //who doesn't love magic numbers?!
            toxav.PrepareTransmission(CallIndex, 3, 40, false);

            WaveFormat outFormat = new WaveFormat((int)settings.AudioSampleRate, (int)settings.AudioChannels);
            wave_provider = new BufferedWaveProvider(outFormat);
            wave_provider.DiscardOnBufferOverflow = true;

            if (WaveIn.DeviceCount > 0)
            {
                wave_source = new WaveIn();

                if (input != -1)
                    wave_source.DeviceNumber = input - 1;

                WaveFormat inFormat = new WaveFormat((int)ToxAv.DefaultCodecSettings.AudioSampleRate, 1);

                wave_source.WaveFormat = inFormat;
                wave_source.DataAvailable += wave_source_DataAvailable;
                wave_source.RecordingStopped += wave_source_RecordingStopped;
                wave_source.BufferMilliseconds = ToxAv.DefaultCodecSettings.AudioFrameDuration;
                wave_source.StartRecording();
            }

            if (WaveOut.DeviceCount > 0)
            {
                wave_out = new WaveOut();

                if (output != -1)
                    wave_out.DeviceNumber = output - 1;

                wave_out.Init(wave_provider);
                wave_out.Play();
            }
        }

        public virtual void SetTimerCallback(TimerCallback callback)
        {
            timer = new Timer(callback, null, 0, 1000);
        }

        protected void wave_source_RecordingStopped(object sender, StoppedEventArgs e)
        {
            Debug.WriteLine("Recording stopped");
        }

        public virtual void ProcessAudioFrame(short[] frame)
        {
            if (wave_out == null)
                return;

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

        protected virtual void wave_source_DataAvailable(object sender, WaveInEventArgs e)
        {
            ushort[] ushorts = new ushort[e.Buffer.Length / 2];
            Buffer.BlockCopy(e.Buffer, 0, ushorts, 0, e.Buffer.Length);

            byte[] dest = new byte[65535];
            int size = toxav.PrepareAudioFrame(CallIndex, dest, 65535, ushorts);

            ToxAvError error = toxav.SendAudio(CallIndex, dest, size);
            if (error != ToxAvError.None)
                Debug.WriteLine(string.Format("Could not send audio: {0}", error));
        }

        public virtual void Stop()
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

            toxav.KillTransmission(CallIndex);
            toxav.Hangup(CallIndex);

            if (timer != null)
                timer.Dispose();
        }

        public virtual void Answer()
        {
            ToxAvError error = toxav.Answer(CallIndex, ToxAv.DefaultCodecSettings);
            if (error != ToxAvError.None)
                throw new Exception("Could not answer call " + error.ToString());
        }

        public virtual void Call(int current_number, ToxAvCodecSettings settings, int ringing_seconds)
        {
            toxav.Call(current_number, settings, ringing_seconds, out CallIndex);
        }
    }
}
