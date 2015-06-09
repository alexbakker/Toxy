using SharpTox.Av;
using System;
using System.Linq;
using Toxy.ViewModels;
using Toxy.Extensions;
using SharpTox.Core;

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
            App.ToxAv.OnAudioFrameReceived += ToxAv_OnAudioFrameReceived;
            App.ToxAv.OnCallStateChanged += ToxAv_OnCallStateChanged;
            App.ToxAv.OnCallRequestReceived += ToxAv_OnCallRequestReceived;
            App.ToxAv.OnAudioBitrateChanged += ToxAv_OnAudioBitrateChanged;
            App.ToxAv.OnVideoBitrateChanged += ToxAv_OnVideoBitrateChanged;
            App.Tox.OnFriendConnectionStatusChanged += Tox_OnFriendConnectionStatusChanged;
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
            
        }

        private void ToxAv_OnAudioBitrateChanged(object sender, ToxAvEventArgs.BitrateStatusEventArgs e)
        {
            
        }

        private void ToxAv_OnCallRequestReceived(object sender, ToxAvEventArgs.CallRequestEventArgs e)
        {
            if (_callInfo != null)
            {
                //TODO: notify the user there's yet another call incoming
                App.ToxAv.SendControl(e.FriendNumber, ToxAvCallControl.Cancel);
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

        private void AudioEngine_OnMicDataAvailable(short[] data, int sampleRate, int channels)
        {
            if (_callInfo == null)
                return;

            var error = ToxAvErrorSendFrame.Ok;
            if (!App.ToxAv.SendAudioFrame(_callInfo.FriendNumber, new ToxAvAudioFrame(data, sampleRate, channels), out error))
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

        public bool Answer(int friendNumber)
        {
            if (_callInfo != null)
            {
                Debugging.Write("Tried to answer a call but there is already one in progress");
                return false;
            }

            var error = ToxAvErrorAnswer.Ok;
            if (!App.ToxAv.Answer(friendNumber, 48, 0, out error))
            {
                Debugging.Write("Could not answer call for friend: " + error);
                return false;
            }

            _callInfo = new CallInfo(friendNumber);
            _callInfo.AudioEngine = new AudioEngine();
            _callInfo.AudioEngine.OnMicDataAvailable += AudioEngine_OnMicDataAvailable;
            _callInfo.AudioEngine.StartRecording();

            return true;
        }

        public bool Hangup(int friendNumber)
        {
            var error = ToxAvErrorCallControl.Ok;
            if (!App.ToxAv.SendControl(friendNumber, ToxAvCallControl.Cancel, out error))
            {
                Debugging.Write("Could not answer call for friend: " + error);
                return false;
            }

            _callInfo.Dispose();
            _callInfo = null;
            return true;
        }

        public bool SendRequest(int friendNumber)
        {
            if (_callInfo != null)
            {
                Debugging.Write("Tried to send a call request but there is already one in progress");
                return false;
            }

            var error = ToxAvErrorCall.Ok;
            if (!App.ToxAv.Call(friendNumber, 48, 0, out error))
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

            public CallInfo(int friendNumber)
            {
                FriendNumber = friendNumber;
            }
        
            public void Dispose()
            {
                if (AudioEngine != null)
                    AudioEngine.Dispose();
            }
        }

        private IChatObject FindFriend(int friendNumber)
        {
            return MainWindow.Instance.ViewModel.CurrentFriendListView.ChatCollection.FirstOrDefault(f => f.ChatNumber == friendNumber);
        }
    }
}
