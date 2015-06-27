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
using Toxy.Tools;

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
                ToxAv_OnCallStateChanged(null, new ToxAvEventArgs.CallStateEventArgs(e.FriendNumber, ToxAvFriendCallState.Finished));
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

            ProfileManager.Instance.ToxAv.SetVideoBitrate(_callInfo.FriendNumber, enableVideo ? 3000 : 0, false);

            if (!enableVideo && _callInfo.VideoEngine != null)
            {
                _callInfo.VideoEngine.Dispose();
                _callInfo.VideoEngine = null;
            }
            else if (enableVideo)
            {
                if (_callInfo.VideoEngine != null)
                {
                    _callInfo.VideoEngine.Dispose();
                    _callInfo.VideoEngine = null;
                }

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

                friend.CallState = CallState.Calling;

                if (e.VideoEnabled)
                    friend.CallState |= CallState.SendingVideo;
            });
        }

        private void ToxAv_OnCallStateChanged(object sender, ToxAvEventArgs.CallStateEventArgs e)
        {
            var state = CallState.InProgress;

            if ((e.State & ToxAvFriendCallState.Finished) != 0 || (e.State & ToxAvFriendCallState.Error) != 0)
            {
                if (_callInfo != null)
                {
                    _callInfo.Dispose();
                    _callInfo = null;
                }

                state = CallState.None;
            }
            else if ((e.State & ToxAvFriendCallState.ReceivingAudio) != 0 ||
                (e.State & ToxAvFriendCallState.ReceivingVideo) != 0 ||
                (e.State & ToxAvFriendCallState.SendingAudio) != 0 ||
                (e.State & ToxAvFriendCallState.SendingVideo) != 0)
            {
                //start sending whatever from here
                if (_callInfo.AudioEngine != null)
                {
                    if (!_callInfo.AudioEngine.IsRecording)
                        _callInfo.AudioEngine.StartRecording();
                }

                if (_callInfo.VideoEngine != null)
                {
                    if (!_callInfo.VideoEngine.IsRecording)
                        _callInfo.VideoEngine.StartRecording();
                }

                if (e.State.HasFlag(ToxAvFriendCallState.SendingVideo))
                    state |= CallState.SendingVideo;

                if (_callInfo.VideoEngine != null)
                    state |= CallState.ReceivingVideo;
            }

            MainWindow.Instance.UInvoke(() =>
            {
                var friend = FindFriend(e.FriendNumber);
                if (friend == null)
                {
                    Debugging.Write("Received a call state change from a friend we don't know about!");
                    return;
                }

                friend.CallState = state;
            });
        }

        private void ToxAv_OnVideoFrameReceived(object sender, ToxAvEventArgs.VideoFrameEventArgs e)
        {
            var source = VideoUtils.ToxAvFrameToBitmap(e.Frame);

            MainWindow.Instance.UInvoke(() =>
            {
                var friend = FindFriend(e.FriendNumber);
                if (friend == null)
                    return;

                friend.ConversationView.CurrentFrame = source;
            });
        }

        private void VideoEngine_OnFrameAvailable(Bitmap bmp)
        {
            if (_callInfo == null)
                return;

            var frame = VideoUtils.BitmapToToxAvFrame(bmp);
            bmp.Dispose();

            //yes, check again
            if (_callInfo == null)
                return;

            var error = ToxAvErrorSendFrame.Ok;
            if (!ProfileManager.Instance.ToxAv.SendVideoFrame(_callInfo.FriendNumber, frame))
                Debugging.Write("Could not send video frame: " + error);
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
            _callInfo.AudioEngine = new AudioEngine();
            _callInfo.AudioEngine.OnMicDataAvailable += AudioEngine_OnMicDataAvailable;

            if (enableVideo)
            {
                _callInfo.VideoEngine = new VideoEngine();
                _callInfo.VideoEngine.OnFrameAvailable += VideoEngine_OnFrameAvailable;
            }

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

    public enum CallState
    {
        None = 1 << 0,
        Ringing = 1 << 1,
        Calling = 1 << 2,
        InProgress = 1 << 3,
        SendingVideo = 1 << 4,
        ReceivingVideo = 1 << 5
    }
}
