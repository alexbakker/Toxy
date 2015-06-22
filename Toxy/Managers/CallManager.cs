using SharpTox.Av;
using System;
using System.Linq;
using Toxy.ViewModels;
using Toxy.Extensions;
using SharpTox.Core;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Toxy.Managers
{
    public class CallManager
    {
        private volatile CallInfo _callInfo;
        private static CallManager _instance;

        public static CallManager Get()
        {
            if (_instance == null)
                _instance = new CallManager();

            return _instance;
        }

        public ToxAvCallState CallState { get; private set; }

        private CallManager() 
        {
            ProfileManager.Instance.ToxAv.OnAudioFrameReceived += ToxAv_OnAudioFrameReceived;
            ProfileManager.Instance.ToxAv.OnVideoFrameReceived += ToxAv_OnVideoFrameReceived;
            ProfileManager.Instance.ToxAv.OnCallStateChanged += ToxAv_OnCallStateChanged;
            ProfileManager.Instance.ToxAv.OnCallRequestReceived += ToxAv_OnCallRequestReceived;
            ProfileManager.Instance.ToxAv.OnAudioBitrateChanged += ToxAv_OnAudioBitrateChanged;
            ProfileManager.Instance.ToxAv.OnVideoBitrateChanged += ToxAv_OnVideoBitrateChanged;
            ProfileManager.Instance.Tox.OnFriendConnectionStatusChanged += Tox_OnFriendConnectionStatusChanged;
        }

        private void Tox_OnFriendConnectionStatusChanged(object sender, SharpTox.Core.ToxEventArgs.FriendConnectionStatusEventArgs e)
        {
            if (e.Status != ToxConnectionStatus.None)
                return;

            if (_callInfo != null && _callInfo.FriendNumber == e.FriendNumber)
            {
                ToxAv_OnCallStateChanged(null, new ToxAvEventArgs.CallStateEventArgs(e.FriendNumber, ToxAvCallState.Finished));
            }
        }

        private void ToxAv_OnVideoBitrateChanged(object sender, ToxAvEventArgs.BitrateStatusEventArgs e)
        {
            Debugging.Write(string.Format("Changed video bitrate to {1}, stable: {2} friend: {0}", e.FriendNumber, e.Bitrate, e.Stable));
        }

        private void ToxAv_OnAudioBitrateChanged(object sender, ToxAvEventArgs.BitrateStatusEventArgs e)
        {
            Debugging.Write(string.Format("Changed audio bitrate to {1}, stable: {2}, friend: {0}", e.FriendNumber, e.Bitrate, e.Stable));
        }

        public void ToggleVideo(bool enableVideo)
        {
            if (_callInfo == null)
                return;

            if (!enableVideo && _callInfo.VideoEngine != null)
                _callInfo.VideoEngine.Dispose();
            else if (enableVideo)
            {
                if (_callInfo.VideoEngine != null)
                    _callInfo.VideoEngine.Dispose();

                _callInfo.VideoEngine = new VideoEngine();
                _callInfo.VideoEngine.OnFrameAvailable += VideoEngine_OnFrameAvailable;
                _callInfo.VideoEngine.StartRecording();
            }
        }

        private void ToxAv_OnCallRequestReceived(object sender, ToxAvEventArgs.CallRequestEventArgs e)
        {
            if (_callInfo != null)
            {
                //TODO: notify the user there's yet another call incoming
                ProfileManager.Instance.ToxAv.SendControl(e.FriendNumber, ToxAvCallControl.Cancel);
                return;
            }

            MainWindow.Instance.UInvoke(() =>
            {
                var friend = FindFriend(e.FriendNumber);
                if (friend == null)
                {
                    Debugging.Write("Received a call request from a friend we don't know about!");
                    return;
                }

                friend.IsCalling = true;
                friend.IsInVideoCall = true;
            });
        }

        private void ToxAv_OnCallStateChanged(object sender, ToxAvEventArgs.CallStateEventArgs e)
        {
            bool isCalling = false;
            bool isCallInProgress = true;
            bool isRinging = false;

            if ((e.State & ToxAvCallState.Finished) != 0 || (e.State & ToxAvCallState.Error) != 0)
            {
                if (_callInfo != null)
                {
                    _callInfo.Dispose();
                    _callInfo = null;
                }

                isCallInProgress = false;
            }
            else if ((e.State & ToxAvCallState.ReceivingAudio) != 0 ||
                (e.State & ToxAvCallState.ReceivingVideo) != 0 ||
                (e.State & ToxAvCallState.SendingAudio) != 0 ||
                (e.State & ToxAvCallState.SendingVideo) != 0)
            {
                //start sending whatever from here
                if (_callInfo.AudioEngine == null)
                {
                    _callInfo.AudioEngine = new AudioEngine();
                    _callInfo.AudioEngine.OnMicDataAvailable += AudioEngine_OnMicDataAvailable;
                    _callInfo.AudioEngine.StartRecording();

                    _callInfo.VideoEngine = new VideoEngine();
                    _callInfo.VideoEngine.OnFrameAvailable += VideoEngine_OnFrameAvailable;
                    _callInfo.VideoEngine.StartRecording();
                }
                else
                {
                    if (!_callInfo.AudioEngine.IsRecording)
                        _callInfo.AudioEngine.StartRecording();
                }
            }

            MainWindow.Instance.UInvoke(() =>
            {
                var friend = FindFriend(e.FriendNumber);
                if (friend == null)
                {
                    Debugging.Write("Received a call state change from a friend we don't know about!");
                    return;
                }

                friend.IsCalling = isCalling;
                friend.IsRinging = isRinging;
                friend.IsCallInProgress = isCallInProgress;
                //friend.ChangeCallState(e.State);
            });
        }

        private unsafe void ToxAv_OnVideoFrameReceived(object sender, ToxAvEventArgs.VideoFrameEventArgs e)
        {
            byte[] data = new byte[e.Frame.Width * e.Frame.Height * 4];

            fixed (byte* rgb = data)
            fixed (byte* y = e.Frame.Y)
            fixed (byte* u = e.Frame.U)
            fixed (byte* v = e.Frame.V)
                yuv420tobgr((ushort)e.Frame.Width, (ushort)e.Frame.Height, y, u, v, (uint)e.Frame.YStride, (uint)e.Frame.UStride, (uint)e.Frame.VStride, rgb);

            int bytesPerPixel = (PixelFormats.Bgra32.BitsPerPixel + 7) / 8;
            int stride = 4 * ((e.Frame.Width * bytesPerPixel + 3) / 4);

            var source = BitmapSource.Create(e.Frame.Width, e.Frame.Height, 96d, 96d, PixelFormats.Bgra32, null, data, stride);
            source.Freeze();

            MainWindow.Instance.UInvoke(() =>
            {
                var friend = FindFriend(e.FriendNumber);
                if (friend == null)
                    return;

                friend.ConversationView.CurrentFrame = source;
            });
        }

        private unsafe void VideoEngine_OnFrameAvailable(Bitmap bmp)
        {
            if (_callInfo == null)
                return;

            var bitmapData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
            byte[] bytes = new byte[bitmapData.Stride * bmp.Height];

            Marshal.Copy(bitmapData.Scan0, bytes, 0, bytes.Length);

            byte[] y = new byte[bmp.Height * bmp.Width];
            byte[] u = new byte[(bmp.Height / 2) * (bmp.Width / 2)];
            byte[] v = new byte[(bmp.Height / 2) * (bmp.Width / 2)];

            fixed (byte* rgb = bytes)
            fixed (byte* ny = y)
            fixed (byte* nu = u)
            fixed (byte* nv = v)
                bgrtoyuv420(ny, nu, nv, rgb, (ushort)bmp.Width, (ushort)bmp.Height);

            var frame = new ToxAvVideoFrame(bmp.Width, bmp.Height, y, u, v);
            var error = ToxAvErrorSendFrame.Ok;

            //ToxAv_OnVideoFrameReceived(null, new ToxAvEventArgs.VideoFrameEventArgs(_callInfo.FriendNumber, frame));

            if (!ProfileManager.Instance.ToxAv.SendVideoFrame(_callInfo.FriendNumber, frame))
                Debugging.Write("Could not send video frame: " + error);
        }

        //the following conversion functions were taken from utox source code and are also licensed under GPLv3
        static byte rgb_to_y(int r, int g, int b)
        {
            int y = ((9798 * r + 19235 * g + 3736 * b) >> 15);
            return (byte)(y > 255 ? 255 : y < 0 ? 0 : y);
        }

        static byte rgb_to_u(int r, int g, int b)
        {
            int u = ((-5538 * r + -10846 * g + 16351 * b) >> 15) + 128;
            return (byte)(u > 255 ? 255 : u < 0 ? 0 : u);
        }

        static byte rgb_to_v(int r, int g, int b)
        {
            int v = ((16351 * r + -13697 * g + -2664 * b) >> 15) + 128;
            return (byte)(v > 255 ? 255 : v < 0 ? 0 : v);
        }

        private unsafe void bgrtoyuv420(byte* plane_y, byte* plane_u, byte* plane_v, byte* rgb, ushort width, ushort height)
        {
            ushort x, y;
            byte* p;
            byte r, g, b;

            for (y = 0; y != height; y += 2)
            {
                p = rgb;
                for (x = 0; x != width; x++)
                {
                    b = *rgb++;
                    g = *rgb++;
                    r = *rgb++;
                    *plane_y++ = rgb_to_y(r, g, b);
                }

                for (x = 0; x != width / 2; x++)
                {
                    b = *rgb++;
                    g = *rgb++;
                    r = *rgb++;
                    *plane_y++ = rgb_to_y(r, g, b);

                    b = *rgb++;
                    g = *rgb++;
                    r = *rgb++;
                    *plane_y++ = rgb_to_y(r, g, b);

                    b = (byte)(((int)b + (int)*(rgb - 6) + (int)*p + (int)*(p + 3) + 2) / 4); p++;
                    g = (byte)(((int)g + (int)*(rgb - 5) + (int)*p + (int)*(p + 3) + 2) / 4); p++;
                    r = (byte)(((int)r + (int)*(rgb - 4) + (int)*p + (int)*(p + 3) + 2) / 4); p++;

                    *plane_u++ = rgb_to_u(r, g, b);
                    *plane_v++ = rgb_to_v(r, g, b);

                    p += 3;
                }
            }
        }

        private unsafe void yuv420tobgr(ushort width, ushort height, byte *y, byte *u, byte *v, uint ystride, uint ustride, uint vstride, byte *result)
        {
            ulong i, j;
            for (i = 0; i < height; ++i) {
                for (j = 0; j < width; ++j) {
                    byte *point = result + 4 * ((i * width) + j);
                    int t_y = y[((i * ystride) + j)];
                    int t_u = u[(((i / 2) * ustride) + (j / 2))];
                    int t_v = v[(((i / 2) * vstride) + (j / 2))];
                    t_y = t_y < 16 ? 16 : t_y;

                    int r = (298 * (t_y - 16) + 409 * (t_v - 128) + 128) >> 8;
                    int g = (298 * (t_y - 16) - 100 * (t_u - 128) - 208 * (t_v - 128) + 128) >> 8;
                    int b = (298 * (t_y - 16) + 516 * (t_u - 128) + 128) >> 8;

                    point[2] = (byte)(r>255? 255 : r<0 ? 0 : r);
                    point[1] = (byte)(g>255? 255 : g<0 ? 0 : g);
                    point[0] = (byte)(b>255? 255 : b<0 ? 0 : b);
                    point[3] = byte.MaxValue;
                }
            }
        }

        private void AudioEngine_OnMicDataAvailable(short[] data, int sampleRate, int channels)
        {
            if (_callInfo == null)
                return;

            var error = ToxAvErrorSendFrame.Ok;
            if (!ProfileManager.Instance.ToxAv.SendAudioFrame(_callInfo.FriendNumber, new ToxAvAudioFrame(data, sampleRate, channels), out error))
            {
                Debugging.Write("Failed to send audio frame: " + error);
            }
        }

        private void ToxAv_OnAudioFrameReceived(object sender, SharpTox.Av.ToxAvEventArgs.AudioFrameEventArgs e)
        {
            if (_callInfo != null && _callInfo.AudioEngine != null)
            {
                _callInfo.AudioEngine.ProcessAudioFrame(e.Frame);
            }
            //Debugging.Write(string.Format("Received frame: length: {0}, channels: {1}, sampling rate: {2}", e.Frame.Data.Length, e.Frame.Channels, e.Frame.SamplingRate));
        }

        public bool Answer(int friendNumber, bool enableVideo)
        {
            if (_callInfo != null)
            {
                Debugging.Write("Tried to answer a call but there is already one in progress");
                return false;
            }

            var error = ToxAvErrorAnswer.Ok;
            if (!ProfileManager.Instance.ToxAv.Answer(friendNumber, 48, enableVideo ? 3000 : 0, out error))
            {
                Debugging.Write("Could not answer call for friend: " + error);
                return false;
            }

            _callInfo = new CallInfo(friendNumber);
            _callInfo.AudioEngine = new AudioEngine();
            _callInfo.AudioEngine.OnMicDataAvailable += AudioEngine_OnMicDataAvailable;
            _callInfo.AudioEngine.StartRecording();

            if (enableVideo)
            {
                _callInfo.VideoEngine = new VideoEngine();
                _callInfo.VideoEngine.OnFrameAvailable += VideoEngine_OnFrameAvailable;
                _callInfo.VideoEngine.StartRecording();
            }

            return true;
        }

        public bool Hangup(int friendNumber)
        {
            var error = ToxAvErrorCallControl.Ok;
            if (!ProfileManager.Instance.ToxAv.SendControl(friendNumber, ToxAvCallControl.Cancel, out error))
            {
                Debugging.Write("Could not answer call for friend: " + error);
                return false;
            }

            _callInfo.Dispose();
            _callInfo = null;
            return true;
        }

        public bool SendRequest(int friendNumber, bool enableVideo)
        {
            if (_callInfo != null)
            {
                Debugging.Write("Tried to send a call request but there is already one in progress");
                return false;
            }

            var error = ToxAvErrorCall.Ok;
            if (!ProfileManager.Instance.ToxAv.Call(friendNumber, 48, enableVideo ? 3000 : 0, out error))
            {
                Debugging.Write("Could not send call request to friend: " + error);
                return false;
            } 
            
            _callInfo = new CallInfo(friendNumber);
            return true;
        }

        private class CallInfo : IDisposable
        {
            public readonly int FriendNumber;
            public AudioEngine AudioEngine { get; set; }
            public VideoEngine VideoEngine { get; set; }

            public CallInfo(int friendNumber)
            {
                FriendNumber = friendNumber;
            }
        
            public void Dispose()
            {
                if (AudioEngine != null)
                    AudioEngine.Dispose();

                if (VideoEngine != null)
                    VideoEngine.Dispose();
            }
        }

        private IChatObject FindFriend(int friendNumber)
        {
            return MainWindow.Instance.ViewModel.CurrentFriendListView.ChatCollection.FirstOrDefault(f => f.ChatNumber == friendNumber);
        }
    }
}
