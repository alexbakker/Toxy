using System;
using System.Drawing;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

using NAudio.Wave;

using SharpTox.Core;
using SharpTox.Av;
using SharpTox.Av.Filter;
using SharpTox.Vpx;

using Toxy.Common;
using Toxy.ViewModels;
using AForge.Video.DirectShow;
using AForge.Video;

namespace Toxy.ToxHelpers
{
    class ToxGroupCall : ToxCall
    {
        public int GroupNumber { get; private set; }

        private WaveOut wave_out_single;
        private BufferedWaveProvider wave_provider_single;

        private bool muted = true;
        public bool Muted
        {
            get { return muted; }
            set 
            {
                if (wave_source != null)
                {
                    if (value)
                        wave_source.StopRecording();
                    else
                        wave_source.StartRecording();
                }

                muted = value;
            }
        }

        public ToxGroupCall(ToxAv toxav, int groupNumber)
            : base(toxav)
        {
            GroupNumber = groupNumber;
        }

        public override void Start(int input, int output, ToxAvCodecSettings settings, string videoDevice = "")
        {
            WaveFormat outFormat = new WaveFormat((int)settings.AudioSampleRate, 2);
            WaveFormat outFormatSingle = new WaveFormat((int)settings.AudioSampleRate, 1);

            filterAudio = new FilterAudio((int)settings.AudioSampleRate);

            wave_provider = new BufferedWaveProvider(outFormat);
            wave_provider.DiscardOnBufferOverflow = true;
            wave_provider_single = new BufferedWaveProvider(outFormatSingle);
            wave_provider_single.DiscardOnBufferOverflow = true;

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
                //wave_source.StartRecording();
            }

            if (WaveOut.DeviceCount > 0)
            {
                wave_out = new WaveOut();

                if (output != -1)
                    wave_out.DeviceNumber = output - 1;

                wave_out.Init(wave_provider);
                wave_out.Play();

                wave_out_single = new WaveOut();

                if (output != -1)
                    wave_out.DeviceNumber = output - 1;

                wave_out.Init(wave_provider_single);
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
            short[] shorts = BytesToShorts(e.Buffer);

            if (filterAudio != null && FilterAudio)
                if (!filterAudio.Filter(shorts, shorts.Length / wave_source.WaveFormat.Channels))
                    Debug.WriteLine("Could not filter audio");

            if (!toxav.GroupSendAudio(GroupNumber, shorts, ((int)ToxAv.DefaultCodecSettings.AudioFrameDuration * (int)wave_source.WaveFormat.SampleRate) / 1000, wave_source.WaveFormat.Channels, wave_source.WaveFormat.SampleRate))
                Debug.WriteLine("Could not send audio to groupchat #{0}", GroupNumber);
        }

        public void ProcessAudioFrame(short[] frame, int channels)
        {
            var waveOut = channels == 2 ? wave_out : wave_out_single;
            var waveProvider = channels == 2 ? wave_provider : wave_provider_single;

            if (waveOut != null && waveProvider != null)
            {
                byte[] bytes = ShortArrayToByteArray(frame);
                waveProvider.AddSamples(bytes, 0, bytes.Length);
            }
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
        protected FilterAudio filterAudio;

        protected WaveIn wave_source;
        protected WaveOut wave_out;
        protected BufferedWaveProvider wave_provider;
        protected Timer timer;

        private VideoCaptureDevice videoSource;
        private VideoWindow videoWindow;

        public bool FilterAudio { get; set; }

        private int totalSeconds = 0;

        public int TotalSeconds
        {
            get { return totalSeconds; }
            set { totalSeconds = value; }
        }

        private int callIndex;

        public int CallIndex
        {
            get { return callIndex; }
        }
        
        public int FriendNumber { get; private set; }

        public ToxCall(ToxAv toxav, int callindex, int friendnumber)
        {
            this.toxav = toxav;
            this.FriendNumber = friendnumber;

            callIndex = callindex;
        }

        /// <summary>
        /// Dummy. Don't use this.
        /// </summary>
        /// <param name="toxav"></param>
        public ToxCall(ToxAv toxav)
        {
            this.toxav = toxav;
        }

        public virtual void Start(int input, int output, ToxAvCodecSettings settings, string videoDevice = "")
        {
            toxav.PrepareTransmission(callIndex, true);

            WaveFormat outFormat = new WaveFormat((int)settings.AudioSampleRate, (int)settings.AudioChannels);
            wave_provider = new BufferedWaveProvider(outFormat);
            wave_provider.DiscardOnBufferOverflow = true;

            filterAudio = new FilterAudio((int)settings.AudioSampleRate);

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

            if (settings.CallType == ToxAvCallType.Video)
            {
                var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                foreach (FilterInfo device in videoDevices)
                {
                    if (device.Name == videoDevice)
                    {
                        videoSource = new VideoCaptureDevice(device.MonikerString);
                        videoSource.NewFrame += video_source_NewFrame;
                        videoSource.Start();
                        break;
                    }
                }

                videoWindow = new VideoWindow();
                videoWindow.Show();
            }
        }

        private void video_source_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            SendVideoFrame(eventArgs.Frame);
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

        protected byte[] ShortArrayToByteArray(short[] shorts)
        {
            byte[] bytes = new byte[shorts.Length * 2];

            for (int i = 0; i < shorts.Length; ++i)
            {
                bytes[2 * i] = (byte)shorts[i];
                bytes[2 * i + 1] = (byte)(shorts[i] >> 8);
            }

            return bytes;
        }

        protected short[] BytesToShorts(byte[] buffer)
        {
            short[] shorts = new short[buffer.Length / 2];
            int index = 0;

            for (int i = 0; i < buffer.Length; i += 2)
            {
                byte[] bytes = new byte[] { buffer[i], buffer[i + 1] };
                shorts[index] = BitConverter.ToInt16(bytes, 0);

                index++;
            }

            return shorts;
        }

        protected virtual void wave_source_DataAvailable(object sender, WaveInEventArgs e)
        {
            short[] shorts = BytesToShorts(e.Buffer);

            if (filterAudio != null && FilterAudio)
                if (!filterAudio.Filter(shorts, shorts.Length / wave_source.WaveFormat.Channels))
                    Debug.WriteLine("Could not filter audio");

            byte[] dest = new byte[(((int)ToxAv.DefaultCodecSettings.AudioFrameDuration * wave_source.WaveFormat.SampleRate) / 1000) * 2 * wave_source.WaveFormat.Channels];
            int size = toxav.PrepareAudioFrame(callIndex, dest, dest.Length, shorts, ((int)wave_source.BufferMilliseconds * (int)wave_source.WaveFormat.SampleRate) / 1000);

            ToxAvError error = toxav.SendAudio(callIndex, dest, size);
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

            toxav.KillTransmission(callIndex);
            toxav.Hangup(callIndex);

            if (timer != null)
                timer.Dispose();

            if (videoSource != null)
            {
                videoSource.SignalToStop();
                videoSource.NewFrame -= video_source_NewFrame;
                videoSource = null;
            }

            if (videoWindow != null)
                videoWindow.Close();
        }

        public virtual void Answer()
        {
            var settings = ToxAv.DefaultCodecSettings;
            settings.CallType = ToxAvCallType.Video;

            ToxAvError error = toxav.Answer(callIndex, settings);
            if (error != ToxAvError.None)
                throw new Exception("Could not answer call " + error.ToString());
        }

        public virtual void Call(int current_number, ToxAvCodecSettings settings, int ringing_seconds)
        {
            toxav.Call(current_number, settings, ringing_seconds, out callIndex);
        }

        public void ProcessVideoFrame(IntPtr frame)
        {
            VpxImage image = VpxImage.FromPointer(frame);

            if (videoWindow == null)
            {
                image.Free();
                return;
            }

            byte[] dest = VpxHelper.Yuv420ToRgb(image, image.d_w * image.d_h * 4);

            image.Free();

            GCHandle handle = GCHandle.Alloc(dest, GCHandleType.Pinned);
            Bitmap bitmap = Bitmap.FromHbitmap(GdiWrapper.CreateBitmap((int)image.d_w, (int)image.d_h, 1, 32, handle.AddrOfPinnedObject()));
            handle.Free();

            videoWindow.PushVideoFrame(bitmap);
        }

        private void SendVideoFrame(Bitmap frame)
        {
            GdiWrapper.BITMAPINFO info = new GdiWrapper.BITMAPINFO()
            {
                bmiHeader =
                {
                    biWidth = frame.Width,
                    biHeight = -frame.Height,
                    biPlanes = 1,
                    biBitCount = 24,
                    biCompression = GdiWrapper.BitmapCompressionMode.BI_RGB
                }
            };

            info.bmiHeader.Init();

            byte[] bytes = new byte[frame.Width * frame.Height * 3];
            IntPtr context = GdiWrapper.CreateCompatibleDC(IntPtr.Zero);
            IntPtr hbitmap = frame.GetHbitmap();

            GdiWrapper.GetDIBits(context, hbitmap, 0, (uint)frame.Height, bytes, ref info, GdiWrapper.DIB_Color_Mode.DIB_RGB_COLORS);
            GdiWrapper.DeleteObject(hbitmap);
            GdiWrapper.DeleteDC(context);

            byte[] dest = new byte[frame.Width * frame.Height * 4];

            try
            {
                VpxImage img = VpxImage.Create(VpxImageFormat.VPX_IMG_FMT_I420, (ushort)frame.Width, (ushort)frame.Height, 1);

                //fixed (byte* b = bytes)
                VpxHelper.RgbToYuv420(img, bytes, (ushort)frame.Width, (ushort)frame.Height);

                int length = ToxAvFunctions.PrepareVideoFrame(toxav.Handle, CallIndex, dest, dest.Length, (IntPtr)img.Pointer);
                img.Free();

                if (length > 0)
                {
                    byte[] bytesToSend = new byte[length];
                    Array.Copy(dest, bytesToSend, length);

                    ToxAvError error = ToxAvFunctions.SendVideo(toxav.Handle, CallIndex, bytesToSend, (uint)bytesToSend.Length);
                    if (error != ToxAvError.None)
                        Debug.WriteLine(string.Format("Could not send video frame: {0}, {1}", error, length));
                }
                else
                {
                    Debug.WriteLine(string.Format("Could not prepare frame: {0}", (ToxAvError)length));
                }
            }
            catch
            {
                Debug.WriteLine(string.Format("Could not convert frame"));
            }

            frame.Dispose();
        }

        public void ToggleVideo(bool enableVideo, string videoDevice)
        {
            if (enableVideo && videoSource != null)
                return;

            if (!enableVideo && videoSource == null)
                return;

            if (!enableVideo)
            {
                videoSource.SignalToStop();
                videoSource.NewFrame -= video_source_NewFrame;
                videoSource = null;

                var settings = ToxAv.DefaultCodecSettings;
                settings.CallType = ToxAvCallType.Audio;

                toxav.ChangeSettings(CallIndex, settings);
            }
            else
            {
                var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                foreach (FilterInfo device in videoDevices)
                {
                    if (device.Name == videoDevice)
                    {
                        videoSource = new VideoCaptureDevice(device.MonikerString);
                        videoSource.NewFrame += video_source_NewFrame;
                        videoSource.Start();

                        var settings = ToxAv.DefaultCodecSettings;
                        settings.CallType = ToxAvCallType.Video;

                        toxav.ChangeSettings(CallIndex, settings);
                        break;
                    }
                }
            }
        }

        public void ApplyCallType(ToxAvCallType callType)
        {
            if (videoWindow == null && callType == ToxAvCallType.Video)
            {
                videoWindow = new VideoWindow();
                videoWindow.Show();
            }
            else if (videoWindow != null && callType == ToxAvCallType.Audio)
            {
                videoWindow.Close();
                videoWindow = null;
            }
        }
    }
}
