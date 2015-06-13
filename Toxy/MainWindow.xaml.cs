using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MahApps.Metro;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using NAudio.Wave;
using SharpTox.Av;
using SharpTox.Core;
using SharpTox.Vpx;
using SQLite;
using Toxy.Common;
using Toxy.Common.Transfers;
using Toxy.Tables;
using Toxy.ToxHelpers;
using Toxy.ViewModels;
using Toxy.Views;
using Toxy.Extensions;
using Win32;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using Clipboard = System.Windows.Clipboard;
using ContextMenu = System.Windows.Forms.ContextMenu;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using DragEventHandler = System.Windows.DragEventHandler;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MenuItem = System.Windows.Forms.MenuItem;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Size = System.Windows.Size;

namespace Toxy
{
    public partial class MainWindow
    {
        private ToxAv _toxav;
        private ToxCall _call;

        private readonly Dictionary<int, FlowDocument> _convdic = new Dictionary<int, FlowDocument>();
        private readonly Dictionary<int, FlowDocument> _groupdic = new Dictionary<int, FlowDocument>();
        private readonly List<FileTransfer> _transfers = new List<FileTransfer>();

        private bool _resizing;
        private bool _focusTextbox;
        private bool _typing;
        private bool _savingSettings;
        private bool _forceClose;

        private Accent _oldAccent;
        private AppTheme _oldAppTheme;


        readonly NotifyIcon _nIcon = new NotifyIcon();

        private Icon _notifyIcon;
        private Icon _newMessageNotifyIcon;

        private SQLiteAsyncConnection _dbConnection;

        

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
            Debug.AutoFlush = true;
            ApplyConfig();
        }

        #region Tox EventHandlers
        private void tox_OnGroupTitleChanged(object sender, ToxEventArgs.GroupTitleEventArgs e)
        {
            Dispatcher.BeginInvoke(((Action)(() =>
            {
                var group = ViewModel.GetGroupObjectByNumber(e.GroupNumber);
                if (group == null)
                    return;

                group.Name = e.Title;

                if (e.PeerNumber != -1)
                    group.AdditionalInfo = string.Format("Topic set by: {0}", ViewModel.tox.GetGroupMemberName(e.GroupNumber, e.PeerNumber));
            })));
        }

        private void tox_OnGroupNamelistChange(object sender, ToxEventArgs.GroupNamelistChangeEventArgs e)
        {
            Dispatcher.BeginInvoke(((Action)(() =>
            {
                var group = ViewModel.GetGroupObjectByNumber(e.GroupNumber);
                if (group != null)
                {
                    if (e.Change == ToxChatChange.PeerAdd || e.Change == ToxChatChange.PeerDel)
                        group.StatusMessage = string.Format("Peers online: {0}", ViewModel.tox.GetGroupMemberCount(group.ChatNumber));

                    switch (e.Change)
                    {
                        case ToxChatChange.PeerAdd:
                            {
                                RearrangeGroupPeerList(group);
                                break;
                            }
                        case ToxChatChange.PeerDel:
                            {
                                RearrangeGroupPeerList(group);
                                break;
                            }
                        case ToxChatChange.PeerName:
                            {
                                var peer = group.PeerList.GetPeerByPublicKey(ViewModel.tox.GetGroupPeerPublicKey(e.GroupNumber, e.PeerNumber));
                                if (peer != null)
                                {
                                    peer.Name = ViewModel.tox.GetGroupMemberName(e.GroupNumber, e.PeerNumber);
                                    RearrangeGroupPeerList(group);
                                }

                                break;
                            }
                    }
                }
            })));
        }

        private void tox_OnGroupAction(object sender, ToxEventArgs.GroupActionEventArgs e)
        {
            Dispatcher.BeginInvoke(((Action)(() =>
            {
                var group = ViewModel.GetGroupObjectByNumber(e.GroupNumber);
                if (group == null)
                    return;

                var peer = group.PeerList.GetPeerByPublicKey(ViewModel.tox.GetGroupPeerPublicKey(e.GroupNumber, e.PeerNumber));
                if (peer != null && peer.Ignored)
                    return;

                var data = new MessageData
                {
                    Username = "*  ",
                    Message = string.Format("{0} {1}",
                    ViewModel.tox.GetGroupMemberName(e.GroupNumber, e.PeerNumber), e.Action),
                    IsAction = true, Timestamp = DateTime.Now,
                    IsGroupMsg = true,
                    IsSelf = ViewModel.tox.PeerNumberIsOurs(e.GroupNumber, e.PeerNumber)
                };

                if (_groupdic.ContainsKey(e.GroupNumber))
                {
                    _groupdic[e.GroupNumber].AddNewMessageRow(ViewModel.tox, data, false);
                }
                else
                {
                    var document = FlowDocumentExtensions.CreateNewDocument();
                    _groupdic.Add(e.GroupNumber, document);
                    _groupdic[e.GroupNumber].AddNewMessageRow(ViewModel.tox, data, false);
                }

                MessageAlertIncrement(group);

                if (group.Selected)
                    ScrollChatBox();

                if (ViewModel.MainToxyUser.ToxStatus != ToxStatus.Busy)
                    this.Flash();
            })));
        }

        private void tox_OnGroupMessage(object sender, ToxEventArgs.GroupMessageEventArgs e)
        {
            Dispatcher.BeginInvoke(((Action)(() =>
            {
                var group = ViewModel.GetGroupObjectByNumber(e.GroupNumber);
                if (group == null)
                    return;

                var peer = group.PeerList.GetPeerByPublicKey(ViewModel.tox.GetGroupPeerPublicKey(e.GroupNumber, e.PeerNumber));
                if (peer != null && peer.Ignored)
                    return;

                var data = new MessageData
                {
                    Username = ViewModel.tox.GetGroupMemberName(e.GroupNumber, e.PeerNumber),
                    Message = e.Message,
                    Timestamp = DateTime.Now,
                    IsGroupMsg = true,
                    IsSelf = ViewModel.tox.PeerNumberIsOurs(e.GroupNumber, e.PeerNumber)
                };

                if (_groupdic.ContainsKey(e.GroupNumber))
                {
                    var run = _groupdic[e.GroupNumber].GetLastMessageRun();

                    if (run != null)
                    {
                        if (((MessageData)run.Tag).Username == data.Username)
                            _groupdic[e.GroupNumber].AddNewMessageRow(ViewModel.tox, data, true);
                        else
                            _groupdic[e.GroupNumber].AddNewMessageRow(ViewModel.tox, data, false);
                    }
                    else
                    {
                        _groupdic[e.GroupNumber].AddNewMessageRow(ViewModel.tox, data, false);
                    }
                }
                else
                {
                    var document = FlowDocumentExtensions.CreateNewDocument();
                    _groupdic.Add(e.GroupNumber, document);
                    _groupdic[e.GroupNumber].AddNewMessageRow(ViewModel.tox, data, false);
                }

                MessageAlertIncrement(group);

                if (group.Selected)
                    ScrollChatBox();

                if (ViewModel.MainToxyUser.ToxStatus != ToxStatus.Busy)
                    this.Flash();

                _nIcon.Icon = _newMessageNotifyIcon;
                ViewModel.HasNewMessage = true;
            })));
        }

        private async void tox_OnGroupInvite(object sender, ToxEventArgs.GroupInviteEventArgs e)
        {
            int number;

            if (e.GroupType == ToxGroupType.Text)
            {
                number = ViewModel.tox.JoinGroup(e.FriendNumber, e.Data);
            }
            else if (e.GroupType == ToxGroupType.Av)
            {
                if (_call != null)
                {
                    await Dispatcher.BeginInvoke(((Action)(() =>
                    {
                        this.ShowMessageAsync("Error", "Could not join audio groupchat, there's already a call in progress.");
                    })));
                    return;
                }
                number = _toxav.JoinAvGroupchat(e.FriendNumber, e.Data);
                _call = new ToxGroupCall(_toxav, number);
                _call.FilterAudio = ViewModel.config.FilterAudio;
                _call.Start(ViewModel.config.InputDevice, ViewModel.config.OutputDevice, ToxAv.DefaultCodecSettings);
            }
            else
            {
                return;
            }

            Dispatcher.BeginInvoke(((Action)(() =>
            {
                var group = ViewModel.GetGroupObjectByNumber(number);

                if (group != null)
                    SelectGroupControl(group);
                else if (number != -1)
                    AddGroupToView(number, e.GroupType);
            })));
        }

        private void tox_OnConnectionStatusChanged(object sender, ToxEventArgs.ConnectionStatusEventArgs e)
        {
            if (e.Status == ToxConnectionStatus.None)
            {
                SetStatus(ToxStatus.Invalid, false);
                WaitAndBootstrap(2000);
            }
            else
            {
                SetStatus((ToxStatus?)ViewModel.tox.Status, false);
            }
        }

        private void tox_OnReadReceiptReceived(object sender, ToxEventArgs.ReadReceiptEventArgs e)
        {
            //a flowdocument should already be created, but hey, just in case
            if (!_convdic.ContainsKey(e.FriendNumber))
                return;

            Dispatcher.BeginInvoke(((Action)(() =>
            {
                var para = (Paragraph)_convdic[e.FriendNumber].FindChildren<TableRow>().Where(r => !(r.Tag is FileTransfer) && ((MessageData)(r.Tag)).Id == e.Receipt).First().FindChildren<TableCell>().ToArray()[1].Blocks.FirstBlock;

                if (para == null)
                    return; //row or cell doesn't exist? odd, just return

                if (ViewModel.config.Theme == "BaseDark")
                    para.Foreground = Brushes.White;
                else
                    para.Foreground = Brushes.Black;
            })));
        }

        private void tox_OnFileControlReceived(object sender, ToxEventArgs.FileControlEventArgs e)
        {
            switch (e.Control)
            {
                case ToxFileControl.Resume:
                    {
                        var transfer = GetFileTransfer(e.FriendNumber, e.FileNumber);
                        if (transfer == null)
                            break;

                        var ft = (FileSender)transfer;
                        if (ft.Tag != null)
                        {
                            ft.Tag.StartTransfer();
                            ft.Start();
                        }
                        break;
                    }

                case ToxFileControl.Cancel:
                    {
                        var transfer = GetFileTransfer(e.FriendNumber, e.FileNumber);
                        if (transfer == null)
                            break;

                        transfer.Kill(false);
                        _transfers.Remove(transfer);
                        break;
                    }
                case ToxFileControl.Pause:
                    {
                        var transfer = GetFileTransfer(e.FriendNumber, e.FileNumber);
                        if (transfer == null)
                            break;

                        transfer.Paused = true;
                        break;
                    }
            }

            Debug.WriteLine("Received file control: {0} from {1}", e.Control, GetFriendName(e.FriendNumber));
        }

        private void tox_OnFileChunkReceived(object sender, ToxEventArgs.FileChunkEventArgs e)
        {
            var transfer = GetFileTransfer(e.FriendNumber, e.FileNumber) as FileReceiver;
            if (transfer == null)
            {
                Debug.WriteLine("Hoooold your horses, we don't know about this file transfer!");
                return;
            }

            if (transfer.Broken || transfer.Paused)
                return;

            if (e.Data != null && e.Data.Length != 0)
            {
                transfer.ProcessReceivedData(e.Data);
            }
            else if (e.Position == transfer.FileSize)
            {
                transfer.Kill(true);
                if (_transfers.Contains(transfer))
                    _transfers.Remove(transfer);

                if (transfer.Kind == ToxFileKind.Avatar)
                {
                    Dispatcher.BeginInvoke(((Action)(() =>
                    {
                        var friend = ViewModel.GetFriendObjectByNumber(e.FriendNumber);
                        if (friend == null)
                            return;

                        try
                        {
                            //friend.AvatarBytes = e.Avatar.Data;
                            ApplyAvatar(friend);
                        }
                        catch
                        {
                            Debug.WriteLine("Received invalid avatar data ({0})", e.FriendNumber);
                        }
                    })));
                }
            }
        }

        private void tox_OnFileChunkRequested(object sender, ToxEventArgs.FileRequestChunkEventArgs e)
        {
            var transfer = GetFileTransfer(e.FriendNumber, e.FileNumber) as FileSender;
            if (transfer == null)
                return;

            var success = transfer.SendNextChunk(e.Position, e.Length);

            if (e.Position + e.Length >= transfer.FileSize && success)
                transfer.Kill(true);
        }

        private void tox_OnFileSendRequestReceived(object sender, ToxEventArgs.FileSendRequestEventArgs e)
        {
            if (e.FileKind == ToxFileKind.Data)
            {
                if (!_convdic.ContainsKey(e.FriendNumber))
                    _convdic.Add(e.FriendNumber, FlowDocumentExtensions.CreateNewDocument());

                Dispatcher.BeginInvoke(((Action)(() =>
                {
                    var transfer = new FileReceiver(ViewModel.tox, e.FileNumber, e.FriendNumber, e.FileKind, e.FileSize, e.FileName, e.FileName);
                    var control = _convdic[e.FriendNumber].AddNewFileTransfer(ViewModel.tox, transfer);
                    var friend = ViewModel.GetFriendObjectByNumber(e.FriendNumber);
                    transfer.Tag = control;

                    if (friend != null)
                    {
                        MessageAlertIncrement(friend);

                        if (friend.Selected)
                            ScrollChatBox();
                    }

                    control.OnAccept += delegate(FileTransfer ft)
                    {
                        var dialog = new SaveFileDialog();
                        dialog.FileName = e.FileName;

                        if (dialog.ShowDialog() == true)
                        {
                            ft.Path = dialog.FileName;
                            control.FilePath = dialog.FileName;
                            ViewModel.tox.FileControl(ft.FriendNumber, ft.FileNumber, ToxFileControl.Resume);
                        }

                        transfer.Tag.StartTransfer();
                    };

                    control.OnDecline += delegate(FileTransfer ft)
                    {
                        ft.Kill(false);

                        if (_transfers.Contains(ft))
                            _transfers.Remove(ft);
                    };

                    control.OnPause += delegate(FileTransfer ft)
                    {
                        if (ft.Paused)
                            ViewModel.tox.FileControl(ft.FriendNumber, ft.FileNumber, ToxFileControl.Pause);
                        else
                            ViewModel.tox.FileControl(ft.FriendNumber, ft.FileNumber, ToxFileControl.Resume);
                    };

                    control.OnFileOpen += delegate(FileTransfer ft)
                    {
                        try { Process.Start(ft.Path); }
                        catch
                        {
                            // ignored
                        }
                    };

                    control.OnFolderOpen += delegate(FileTransfer ft)
                    {
                        Process.Start("explorer.exe", @"/select, " + ft.Path);
                    };

                    _transfers.Add(transfer);

                    if (ViewModel.MainToxyUser.ToxStatus != ToxStatus.Busy)
                        this.Flash();
                })));
            }
            else if (e.FileKind == ToxFileKind.Avatar)
            {
                if (e.FileSize != 0)
                {
                    var hash = ViewModel.tox.FileGetId(e.FriendNumber, e.FileNumber);
                    if (hash == null || hash.Length != ToxConstants.HashLength)
                        return;

                    Dispatcher.BeginInvoke(((Action)(() =>
                    {
                        var friend = ViewModel.GetFriendObjectByNumber(e.FriendNumber);
                        if (friend == null)
                            return;

                        if (friend.AvatarBytes == null || friend.AvatarBytes.Length == 0)
                        {
                            if (!ViewModel.AvatarStore.Contains(ViewModel.tox.GetFriendPublicKey(e.FriendNumber)))
                            {
                                ViewModel.tox.FileControl(e.FriendNumber, e.FileNumber, ToxFileControl.Resume);
                                var filename = ViewModel.tox.GetFriendPublicKey(e.FriendNumber).GetString();
                                var transfer = new FileReceiver(ViewModel.tox, e.FileNumber, e.FriendNumber, e.FileKind, e.FileSize, filename, Path.Combine(ViewModel.AvatarStore.Dir, filename + ".png"));
                                _transfers.Add(transfer);
                            }
                        }
                        else
                        {
                            if (ToxTools.Hash(friend.AvatarBytes).SequenceEqual(hash))
                                return;
                            
                            ViewModel.tox.FileControl(e.FriendNumber, e.FileNumber, ToxFileControl.Resume);
                            var filename = ViewModel.tox.GetFriendPublicKey(e.FriendNumber).GetString();
                            var transfer = new FileReceiver(ViewModel.tox, e.FileNumber, e.FriendNumber, e.FileKind, (long)e.FileSize, filename, Path.Combine(ViewModel.AvatarStore.Dir, filename + ".png"));
                            _transfers.Add(transfer);
                        }
                    })));
                }
                else //friend doesn't have an avatar
                {
                    ViewModel.AvatarStore.Delete(ViewModel.tox.GetFriendPublicKey(e.FriendNumber));

                    Dispatcher.BeginInvoke(((Action)(() =>
                    {
                        var friend = ViewModel.GetFriendObjectByNumber(e.FriendNumber);
                        if (friend == null)
                            return;

                        friend.AvatarBytes = null;
                        friend.Avatar = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/profilepicture.png"));
                    })));
                }
            }
        }

        private void tox_OnFriendConnectionStatusChanged(object sender, ToxEventArgs.FriendConnectionStatusEventArgs e)
        {
            var friend = Dispatcher.Invoke(() => ViewModel.GetFriendObjectByNumber(e.FriendNumber));
            if (friend == null)
                return;

            if (e.Status == ToxConnectionStatus.None)
            {
                Dispatcher.BeginInvoke(((Action)(() =>
                {
                    var lastOnline = TimeZoneInfo.ConvertTime(ViewModel.tox.GetFriendLastOnline(e.FriendNumber), TimeZoneInfo.Utc, TimeZoneInfo.Local);

                    if (lastOnline.Year == 1970) //quick and dirty way to check if we're dealing with epoch 0
                        friend.StatusMessage = "Friend request sent";
                    else
                        friend.StatusMessage = string.Format("Last seen: {0} {1}", lastOnline.ToShortDateString(), lastOnline.ToLongTimeString());

                    friend.ToxStatus = ToxStatus.Invalid; //not the proper way to do it, I know...

                    if (friend.Selected)
                    {
                        CallButton.Visibility = Visibility.Collapsed;
                        FileButton.Visibility = Visibility.Collapsed;
                        TypingStatusLabel.Content = "";
                    }
                })));

                var receivers = _transfers.Where(t => t.GetType() == typeof(FileReceiver) && t.FriendNumber == e.FriendNumber && !t.Finished);
                if (receivers.Count() > 0)
                {
                    foreach (var transfer in receivers)
                        transfer.Broken = true;
                }

                var senders = _transfers.Where(t => t.GetType() == typeof(FileSender) && t.FriendNumber == e.FriendNumber && !t.Finished);
                if (senders.Count() > 0)
                {
                    foreach (var transfer in senders)
                        transfer.Broken = true;
                }
            }
            else if (e.Status != ToxConnectionStatus.None)
            {
                Dispatcher.BeginInvoke(((Action)(() =>
                {
                    friend.StatusMessage = GetFriendStatusMessage(friend.ChatNumber);

                    if (friend.Selected)
                    {
                        CallButton.Visibility = Visibility.Visible;
                        FileButton.Visibility = Visibility.Visible;
                    }

                    if (ViewModel.MainToxyUser.AvatarBytes != null)
                    {
                        var avatarsDir = Path.Combine(ViewModel.toxDataDir, "avatars");
                        var selfAvatarFile = Path.Combine(avatarsDir, ViewModel.tox.Id.PublicKey.GetString() + ".png");

                        var fileInfo = ViewModel.tox.FileSend(e.FriendNumber, ToxFileKind.Avatar, ViewModel.MainToxyUser.AvatarBytes.Length, "avatar.png", ToxTools.Hash(ViewModel.MainToxyUser.AvatarBytes));
                        var transfer = new FileSender(ViewModel.tox, fileInfo.Number, friend.ChatNumber, ToxFileKind.Avatar, ViewModel.MainToxyUser.AvatarBytes.Length, "", selfAvatarFile);

                        _transfers.Add(transfer);
                    }
                })));
            }

            Dispatcher.BeginInvoke(((Action)(() => RearrangeChatList())));
        }

        private void tox_OnFriendTypingChanged(object sender, ToxEventArgs.TypingStatusEventArgs e)
        {
            Dispatcher.BeginInvoke(((Action)(() =>
            {
                var friend = ViewModel.GetFriendObjectByNumber(e.FriendNumber);
                if (friend == null)
                    return;

                if (friend.Selected)
                {
                    if (e.IsTyping)
                        TypingStatusLabel.Content = GetFriendName(e.FriendNumber) + " is typing...";
                    else
                        TypingStatusLabel.Content = "";
                }
            })));
        }

        private void tox_OnFriendStatusMessageChanged(object sender, ToxEventArgs.StatusMessageEventArgs e)
        {
            Dispatcher.BeginInvoke(((Action)(() =>
            {
                var friend = ViewModel.GetFriendObjectByNumber(e.FriendNumber);
                if (friend != null)
                {
                    friend.StatusMessage = GetFriendStatusMessage(e.FriendNumber);
                }
            })));
        }

        private void tox_OnFriendStatusChanged(object sender, ToxEventArgs.StatusEventArgs e)
        {
            Dispatcher.BeginInvoke(((Action)(() =>
            {
                var friend = ViewModel.GetFriendObjectByNumber(e.FriendNumber);
                if (friend != null)
                {
                    friend.ToxStatus = (ToxStatus)e.Status;
                }

                RearrangeChatList();
            })));
        }

        private void tox_OnFriendRequestReceived(object sender, ToxEventArgs.FriendRequestEventArgs e)
        {
            Dispatcher.BeginInvoke(((Action)(() =>
            {
                try
                {
                    AddFriendRequestToView(e.PublicKey.GetString(), e.Message);
                    if (ViewModel.MainToxyUser.ToxStatus != ToxStatus.Busy)
                        this.Flash();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }

                _nIcon.Icon = _newMessageNotifyIcon;
                ViewModel.HasNewMessage = true;
            })));
        }

        private void tox_OnFriendMessageReceived(object sender, ToxEventArgs.FriendMessageEventArgs e)
        {
            MessageData data;
            if (e.MessageType == ToxMessageType.Message)
                data = new MessageData { Username = GetFriendName(e.FriendNumber), Message = e.Message, Timestamp = DateTime.Now };
            else
                data = new MessageData { Username = "*  ", Message = string.Format("{0} {1}", GetFriendName(e.FriendNumber), e.Message), IsAction = true, Timestamp = DateTime.Now };

            Dispatcher.BeginInvoke(((Action)(() =>
            {
                AddMessageToView(e.FriendNumber, data);

                var friend = ViewModel.GetFriendObjectByNumber(e.FriendNumber);
                if (friend != null)
                {
                    MessageAlertIncrement(friend);

                    if (friend.Selected)
                        ScrollChatBox();
                }
                if (ViewModel.MainToxyUser.ToxStatus != ToxStatus.Busy)
                    this.Flash();

                _nIcon.Icon = _newMessageNotifyIcon;
                ViewModel.HasNewMessage = true;
            })));

            if (ViewModel.config.EnableChatLogging)
            {
                var message = new ToxMessage
                {
                    PublicKey = ViewModel.tox.GetFriendPublicKey(e.FriendNumber).GetString(),
                    Message = data.Message,
                    Timestamp = DateTime.Now,
                    IsAction = false,
                    Name = data.Username,
                    ProfilePublicKey = ViewModel.tox.Id.PublicKey.GetString()
                };
                _dbConnection.InsertAsync(message);
            }
        }

        private void tox_OnFriendNameChanged(object sender, ToxEventArgs.NameChangeEventArgs e)
        {
            Dispatcher.BeginInvoke(((Action)(() =>
            {
                var friend = ViewModel.GetFriendObjectByNumber(e.FriendNumber);
                if (friend != null)
                {
                    friend.Name = GetFriendName(e.FriendNumber);
                }
            })));
        }

        #endregion

        #region ToxAv EventHandlers

        private void toxav_OnReceivedVideo(object sender, ToxAvEventArgs.VideoDataEventArgs e)
        {
            if (Dispatcher.Invoke(() => (_call == null || _call.GetType() == typeof(ToxGroupCall) || _call.Ended || ViewModel.IsGroupSelected || _call.FriendNumber != ViewModel.SelectedChatNumber)))
                return;

            ProcessVideoFrame(e.Frame);
        }

        private void toxav_OnPeerCodecSettingsChanged(object sender, ToxAvEventArgs.CallStateEventArgs e)
        {
            Dispatcher.BeginInvoke(((Action)(() =>
            {
                if (_call == null || _call.GetType() == typeof(ToxGroupCall) || e.CallIndex != _call.CallIndex)
                    return;

                if (_toxav.GetPeerCodecSettings(e.CallIndex, 0).CallType != ToxAvCallType.Video)
                {
                    VideoImageRow.Height = new GridLength(0);
                    VideoGridSplitter.IsEnabled = false;
                    VideoChatImage.Source = null;
                }
                else if (ViewModel.IsFriendSelected && _toxav.GetPeerID(e.CallIndex, 0) == ViewModel.SelectedChatNumber)
                {
                    VideoImageRow.Height = new GridLength(300);
                    VideoGridSplitter.IsEnabled = true;
                }
            })));
        }

        private void toxav_OnReceivedGroupAudio(object sender, ToxAvEventArgs.GroupAudioDataEventArgs e)
        {
            var group = Dispatcher.Invoke(() => ViewModel.GetGroupObjectByNumber(e.GroupNumber));
            if (group == null)
                return;

            var peer = group.PeerList.GetPeerByPublicKey(ViewModel.tox.GetGroupPeerPublicKey(e.GroupNumber, e.PeerNumber));
            if (peer == null || peer.Ignored || peer.Muted)
                return;

            if (_call != null && _call.GetType() == typeof(ToxGroupCall))
                ((ToxGroupCall)_call).ProcessAudioFrame(e.Data, e.Channels);
        }

        private void toxav_OnReceivedAudio(object sender, ToxAvEventArgs.AudioDataEventArgs e)
        {
            if (_call == null)
                return;

            _call.ProcessAudioFrame(e.Data);
        }

        private void toxav_OnEnd(object sender, ToxAvEventArgs.CallStateEventArgs e)
        {
            Dispatcher.BeginInvoke(((Action)(() =>
            {
                EndCall();

                CallButton.Visibility = Visibility.Visible;
                HangupButton.Visibility = Visibility.Collapsed;
                VideoButton.Visibility = Visibility.Collapsed;
                VideoButton.IsChecked = false;
            })));
        }

        private void toxav_OnStart(object sender, ToxAvEventArgs.CallStateEventArgs e)
        {
            var settings = _toxav.GetPeerCodecSettings(e.CallIndex, 0);

            if (_call != null)
                _call.Start(ViewModel.config.InputDevice, ViewModel.config.OutputDevice, settings, ViewModel.config.VideoDevice);

            Dispatcher.BeginInvoke(((Action)(() =>
            {
                if (settings.CallType == ToxAvCallType.Video)
                {
                    VideoImageRow.Height = new GridLength(300);
                    VideoGridSplitter.IsEnabled = true;
                }

                var friendnumber = _toxav.GetPeerID(e.CallIndex, 0);
                var callingFriend = ViewModel.GetFriendObjectByNumber(friendnumber);
                if (callingFriend != null)
                {
                    callingFriend.IsCalling = false;
                    callingFriend.IsCallingToFriend = false;
                    CallButton.Visibility = Visibility.Collapsed;
                    if (callingFriend.Selected)
                    {
                        HangupButton.Visibility = Visibility.Visible;
                        VideoButton.Visibility = Visibility.Visible;
                    }
                    ViewModel.CallingFriend = callingFriend;
                }
            })));

            _call.SetTimerCallback(TimerCallback);
        }

        private void TimerCallback(object state)
        {
            if (_call == null)
                return;

            _call.TotalSeconds++;
            var timeSpan = TimeSpan.FromSeconds(_call.TotalSeconds);

            Dispatcher.BeginInvoke(((Action)(() => CurrentCallControl.TimerLabel.Content = string.Format("{0}:{1}:{2}", timeSpan.Hours.ToString("00"), timeSpan.Minutes.ToString("00"), timeSpan.Seconds.ToString("00")))));
        }

        private void toxav_OnInvite(object sender, ToxAvEventArgs.CallStateEventArgs e)
        {
            //TODO: notify the user of another incoming call
            if (_call != null)
                return;

            Dispatcher.BeginInvoke(((Action)(() =>
            {
                var friend = ViewModel.GetFriendObjectByNumber(_toxav.GetPeerID(e.CallIndex, 0));
                if (friend != null)
                {
                    friend.CallIndex = e.CallIndex;
                    friend.IsCalling = true;
                }
            })));
        }

        #endregion

        private void RearrangeGroupPeerList(IGroupObject group)
        {
            var peers = new ObservableCollection<GroupPeer>();

            for (var i = 0; i < ViewModel.tox.GetGroupMemberCount(group.ChatNumber); i++)
            {
                var publicKey = ViewModel.tox.GetGroupPeerPublicKey(group.ChatNumber, i);
                var oldPeer = group.PeerList.GetPeerByPublicKey(publicKey);
                GroupPeer newPeer;

                if (oldPeer != null)
                    newPeer = oldPeer;
                else
                    newPeer = new GroupPeer(group.ChatNumber, publicKey) { Name = ViewModel.tox.GetGroupMemberName(group.ChatNumber, i) };

                peers.Add(newPeer);
            }

            group.PeerList = new GroupPeerCollection(peers.OrderBy(p => p.Name).ToList());
        }

        private void RearrangeChatList()
        {
            ViewModel.UpdateChatCollection(new ObservableCollection<IChatObject>(ViewModel.ChatCollection.OrderBy(chat => chat.GetType() == typeof(GroupControlModelView) ? 3 : GetStatusPriority(ViewModel.tox.GetFriendConnectionStatus(chat.ChatNumber), (ToxStatus)ViewModel.tox.GetFriendStatus(chat.ChatNumber))).ThenBy(chat => chat.Name)));
        }

        private int GetStatusPriority(ToxConnectionStatus connStatus, ToxStatus status)
        {
            if (connStatus == ToxConnectionStatus.None)
                return 4;

            switch (status)
            {
                case ToxStatus.None:
                    return 0;
                case ToxStatus.Away:
                    return 1;
                case ToxStatus.Busy:
                    return 2;
                default:
                    return 3;
            }
        }

        private void AddMessageToView(int friendNumber, MessageData data)
        {
            if (_convdic.ContainsKey(friendNumber))
            {
                var run = _convdic[friendNumber].GetLastMessageRun();

                if (run != null && run.Tag.GetType() == typeof(MessageData))
                {
                    if (((MessageData)run.Tag).Username == data.Username)
                        _convdic[friendNumber].AddNewMessageRow(ViewModel.tox, data, true);
                    else
                        _convdic[friendNumber].AddNewMessageRow(ViewModel.tox, data, false);
                }
                else
                {
                    _convdic[friendNumber].AddNewMessageRow(ViewModel.tox, data, false);
                }
            }
            else
            {
                var document = FlowDocumentExtensions.CreateNewDocument();
                _convdic.Add(friendNumber, document);
                _convdic[friendNumber].AddNewMessageRow(ViewModel.tox, data, false);
            }
        }

        private void AddActionToView(int friendNumber, MessageData data)
        {
            if (_convdic.ContainsKey(friendNumber))
            {
                _convdic[friendNumber].AddNewMessageRow(ViewModel.tox, data, false);
            }
            else
            {
                var document = FlowDocumentExtensions.CreateNewDocument();
                _convdic.Add(friendNumber, document);
                _convdic[friendNumber].AddNewMessageRow(ViewModel.tox, data, false);
            }
        }

        private async void InitDatabase()
        {
            _dbConnection = new SQLiteAsyncConnection(ViewModel.dbFilename);
            await _dbConnection.CreateTableAsync<ToxMessage>().ContinueWith((r) => { Console.WriteLine("Created ToxMessage table"); });

            if (ViewModel.config.EnableChatLogging)
            {
                await _dbConnection.Table<ToxMessage>().ToListAsync().ContinueWith((task) =>
                {
                    foreach (var msg in task.Result)
                    {
                        if (string.IsNullOrEmpty(msg.ProfilePublicKey) || msg.ProfilePublicKey != ViewModel.tox.Id.PublicKey.GetString())
                            continue;

                        var friendNumber = GetFriendByPublicKey(msg.PublicKey);
                        if (friendNumber == -1)
                            continue;

                        Dispatcher.BeginInvoke(((Action)(() =>
                        {
                            var messageData = new MessageData { Username = msg.Name, Message = msg.Message, IsAction = msg.IsAction, IsSelf = msg.IsSelf, Timestamp = msg.Timestamp };

                            if (!msg.IsAction)
                                AddMessageToView(friendNumber, messageData);
                            else
                                AddActionToView(friendNumber, messageData);
                        })));
                    }
                });
            }
        }

        private int GetFriendByPublicKey(string publicKey)
        {
            var friends = ViewModel.tox.Friends.Where(num => ViewModel.tox.GetFriendPublicKey(num).ToString() == publicKey);
            if (friends.Count() != 1)
                return -1;
            return friends.First();
        }

        private void LoadAvatars()
        {
            try
            {
                byte[] bytes;
                var avatar = ViewModel.AvatarStore.Load(ViewModel.tox.Id.PublicKey, out bytes);
                if (avatar != null && bytes != null && bytes.Length > 0)
                {
                    //tox.SetAvatar(ToxAvatarFormat.Png, bytes);
                    ViewModel.MainToxyUser.Avatar = avatar;
                }
            }
            catch { Debug.WriteLine("Could not load our own avatar, using default"); }

            foreach (var friend in ViewModel.tox.Friends)
            {
                var obj = ViewModel.GetFriendObjectByNumber(friend);
                if (obj == null)
                    continue;

                try
                {
                    byte[] bytes;
                    var avatar = ViewModel.AvatarStore.Load(ViewModel.tox.GetFriendPublicKey(friend), out bytes);

                    if (avatar != null && bytes != null && bytes.Length > 0)
                    {
                        obj.AvatarBytes = bytes;
                        obj.Avatar = avatar;
                    }
                }
                catch { Debug.WriteLine("Could not load avatar of friend " + friend); }
            }
        }

        private Task<bool> ApplyAvatar(IFriendObject friend)
        {
            return Task.Run(() =>
            {
                byte[] data;
                var img = ViewModel.AvatarStore.Load(ViewModel.tox.GetFriendPublicKey(friend.ChatNumber), out data);
                Dispatcher.BeginInvoke(((Action)(() => friend.Avatar = img)));

                if (!ViewModel.AvatarStore.Save(data, ViewModel.tox.GetFriendPublicKey(friend.ChatNumber)))
                    return false;

                return true;
            });
        }

        private async void Chat_Drop(object sender, DragEventArgs e)
        {
            if (ViewModel.IsGroupSelected)
                return;

            if (e.Data.GetDataPresent(DataFormats.FileDrop) && ViewModel.tox.IsFriendOnline(ViewModel.SelectedChatNumber))
            {
                var docPath = (string[])e.Data.GetData(DataFormats.FileDrop);
                MetroDialogOptions.ColorScheme = MetroDialogColorScheme.Theme;

                var mySettings = new MetroDialogSettings
                {
                    AffirmativeButtonText = "Yes",
                    FirstAuxiliaryButtonText = "Cancel",
                    AnimateShow = false,
                    AnimateHide = false,
                    ColorScheme = MetroDialogColorScheme.Theme
                };

                var result = await this.ShowMessageAsync("Please confirm", "Are you sure you want to send this file?",
                MessageDialogStyle.AffirmativeAndNegative, mySettings);

                if (result == MessageDialogResult.Affirmative)
                {
                    SendFile(ViewModel.SelectedChatNumber, docPath[0]);
                }
            }
        }

        private void Chat_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && !ViewModel.IsGroupSelected && ViewModel.tox.IsFriendOnline(ViewModel.SelectedChatNumber))
            {
                e.Effects = DragDropEffects.All;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private async Task<ToxData> LoadTox()
        {
            if (!Directory.Exists(ViewModel.toxDataDir))
                Directory.CreateDirectory(ViewModel.toxDataDir);

            var fileNames = Directory.GetFiles(ViewModel.toxDataDir, "*.tox", SearchOption.TopDirectoryOnly).Where(s => s.EndsWith(".tox")).ToArray();
            if (fileNames.Length > 0)
            {
                if (!fileNames.Contains(ViewModel.toxDataFilename))
                {
                    SwitchProfileButton_Click(this, new RoutedEventArgs());
                }
                else
                {
                    return ToxData.FromDisk(ViewModel.toxDataFilename);
                }
            }
            else if (File.Exists(ViewModel.toxOldDataFilename))
            {
                var profileName = await this.ShowInputAsync("Old data file", "Toxy has detected an old data file. Please enter a name for your profile");
                if (!string.IsNullOrEmpty(profileName))
                    ViewModel.config.ProfileName = profileName;
                else
                    ViewModel.config.ProfileName = ViewModel.tox.Id.PublicKey.GetString().Substring(0, 10);

                File.Move(ViewModel.toxOldDataFilename, ViewModel.toxDataFilename);
                ConfigTools.Save(ViewModel.config, ViewModel.configFilename);

                return ToxData.FromDisk(ViewModel.toxDataFilename);
            }
            else
            {
                var profileName = await this.ShowInputAsync("Welcome to Toxy!", "To get started, enter a name for your first profile.");
                if (!string.IsNullOrEmpty(profileName))
                    ViewModel.config.ProfileName = profileName;
                else
                    ViewModel.config.ProfileName = ViewModel.tox.Id.PublicKey.GetString().Substring(0, 10);

                ViewModel.tox.Name = ViewModel.config.ProfileName;
                ViewModel.tox.GetData().Save(ViewModel.toxDataFilename);
                ConfigTools.Save(ViewModel.config, ViewModel.configFilename);
            }

            return null;
        }

        private void ApplyConfig()
        {
            var accent = ThemeManager.GetAccent(ViewModel.config.AccentColor);
            var theme = ThemeManager.GetAppTheme(ViewModel.config.Theme);

            if (accent != null && theme != null)
                ThemeManager.ChangeAppStyle(Application.Current, accent, theme);

            Width = ViewModel.config.WindowSize.Width;
            Height = ViewModel.config.WindowSize.Height;

            ViewModel.SpellcheckEnabled = ViewModel.config.EnableSpellcheck;
            ViewModel.SpellcheckLangCode = ViewModel.config.SpellcheckLanguage.ToDescription();

            ExecuteActionsOnNotifyIcon();
        }

        private void InitializeNotifyIcon()
        {
            var newMessageIconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Toxy;component/Resources/Icons/icon2.ico")).Stream;
            var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Toxy;component/Resources/Icons/icon.ico")).Stream;

            _notifyIcon = new Icon(iconStream);
            _newMessageNotifyIcon = new Icon(newMessageIconStream);

            _nIcon.Icon = _notifyIcon;
            _nIcon.MouseClick += nIcon_MouseClick;

            var trayIconContextMenu = new ContextMenu();
            var closeMenuItem = new MenuItem("Exit", closeMenuItem_Click);
            var openMenuItem = new MenuItem("Open", openMenuItem_Click);

            var statusMenuItem = new MenuItem("Status");
            var setOnlineMenuItem = new MenuItem("Online", setStatusMenuItem_Click);
            var setAwayMenuItem = new MenuItem("Away", setStatusMenuItem_Click);
            var setBusyMenuItem = new MenuItem("Busy", setStatusMenuItem_Click);

            setOnlineMenuItem.Tag = 0; // Online
            setAwayMenuItem.Tag = 1; // Away
            setBusyMenuItem.Tag = 2; // Busy

            statusMenuItem.MenuItems.Add(setOnlineMenuItem);
            statusMenuItem.MenuItems.Add(setAwayMenuItem);
            statusMenuItem.MenuItems.Add(setBusyMenuItem);

            trayIconContextMenu.MenuItems.Add(openMenuItem);
            trayIconContextMenu.MenuItems.Add(statusMenuItem);
            trayIconContextMenu.MenuItems.Add(closeMenuItem);
            _nIcon.ContextMenu = trayIconContextMenu;
        }

        private void nIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            if (WindowState != WindowState.Normal)
            {
                Show();
                WindowState = WindowState.Normal;
                ShowInTaskbar = true;
            }
            else
            {
                Hide();
                WindowState = WindowState.Minimized;
                ShowInTaskbar = false;
            }
        }

        private void setStatusMenuItem_Click(object sender, EventArgs eventArgs)
        {
            if (ViewModel.tox.IsConnected)
                SetStatus((ToxStatus)((MenuItem)sender).Tag, true);
        }

        private void openMenuItem_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
            ShowInTaskbar = true;
        }

        private void closeMenuItem_Click(object sender, EventArgs eventArgs)
        {
            _forceClose = true;
            Close();
        }

        public MainWindowViewModel ViewModel
        {
            get { return DataContext as MainWindowViewModel; }
        }

        private FileTransfer GetFileTransfer(int friendnumber, int filenumber)
        {
            foreach (var ft in _transfers)
                if (ft.FileNumber == filenumber && ft.FriendNumber == friendnumber && !ft.Finished)
                    return ft;

            return null;
        }

        private void ScrollChatBox()
        {
            var viewer = ChatBox.FindScrollViewer();

            if (viewer != null)
                if (viewer.ScrollableHeight == viewer.VerticalOffset)
                    viewer.ScrollToBottom();
        }

        private void NewGroupButton_Click(object sender, RoutedEventArgs e)
        {
            GroupContextMenu.PlacementTarget = this;
            GroupContextMenu.IsOpen = true;
        }

        private void InitFriends()
        {
            //Creates a new FriendControl for every friend
            foreach (var friendNumber in ViewModel.tox.Friends)
            {
                AddFriendToView(friendNumber, false);
            }
        }

        private void AddGroupToView(int groupnumber, ToxGroupType type)
        {
            var groupname = string.Format("Groupchat #{0}", groupnumber);

            if (type == ToxGroupType.Av)
                groupname += " \uD83D\uDD0A"; /*:loud_sound:*/

            var groupMv = new GroupControlModelView();
            groupMv.ChatNumber = groupnumber;
            groupMv.Name = groupname;
            groupMv.GroupType = type;
            groupMv.StatusMessage = string.Format("Peers online: {0}", ViewModel.tox.GetGroupMemberCount(groupnumber));//string.Join(", ", tox.GetGroupNames(groupnumber));
            groupMv.SelectedAction = GroupSelectedAction;
            groupMv.DeleteAction = GroupDeleteAction;
            groupMv.ChangeTitleAction = ChangeTitleAction;

            ViewModel.ChatCollection.Add(groupMv);
            RearrangeChatList();
        }

        private async void ChangeTitleAction(IGroupObject groupObject)
        {
            var title = await this.ShowInputAsync("Change group title", "Enter a new title for this group.", new MetroDialogSettings { DefaultText = ViewModel.tox.GetGroupTitle(groupObject.ChatNumber) });
            if (string.IsNullOrEmpty(title))
                return;

            if (ViewModel.tox.SetGroupTitle(groupObject.ChatNumber, title))
            {
                groupObject.Name = title;
                groupObject.AdditionalInfo = string.Format("Topic set by: {0}", ViewModel.tox.Name);
            }
        }

        private void GroupDeleteAction(IGroupObject groupObject)
        {
            ViewModel.ChatCollection.Remove(groupObject);
            var groupNumber = groupObject.ChatNumber;
            if (_groupdic.ContainsKey(groupNumber))
            {
                _groupdic.Remove(groupNumber);

                if (groupObject.Selected)
                    ChatBox.Document = null;
            }

            if (ViewModel.tox.GetGroupType(groupNumber) == ToxGroupType.Av && _call != null)
            {
                _call.Stop();
                _call = null;
            }

            ViewModel.tox.DeleteGroupChat(groupNumber);

            groupObject.SelectedAction = null;
            groupObject.DeleteAction = null;

            MicButton.IsChecked = false;
        }

        private void GroupSelectedAction(IGroupObject groupObject, bool isSelected)
        {
            MessageAlertClear(groupObject);

            TypingStatusLabel.Content = "";

            if (isSelected)
            {
                SelectGroupControl(groupObject);
                ScrollChatBox();
                TextToSend.Focus();
            }
        }

        private string GetFriendName(int friendnumber)
        {
            return ViewModel.tox.GetFriendName(friendnumber).Replace("\n", "").Replace("\r", "");
        }

        private string GetSelfStatusMessage()
        {
            return ViewModel.tox.StatusMessage.Replace("\n", "").Replace("\r", "");
        }

        private string GetSelfName()
        {
            return ViewModel.tox.Name.Replace("\n", "").Replace("\r", "");
        }

        private string GetFriendStatusMessage(int friendnumber)
        {
            return ViewModel.tox.GetFriendStatusMessage(friendnumber).Replace("\n", "").Replace("\r", "");
        }

        private void AddFriendToView(int friendNumber, bool sentRequest)
        {
            var friendStatus = "";
            var lastSeenInfo = (string) FindResource("Local_LastSeen");

            if (ViewModel.tox.IsFriendOnline(friendNumber))
            {
                friendStatus = GetFriendStatusMessage(friendNumber);
            }
            else
            {
                var lastOnline = TimeZoneInfo.ConvertTime(ViewModel.tox.GetFriendLastOnline(friendNumber), TimeZoneInfo.Utc, TimeZoneInfo.Local);

                if (lastOnline.Year == 1970)
                {
                    if (sentRequest)
                        friendStatus = "Friend request sent";
                }
                else
                    friendStatus = string.Format("{0}: {1} {2}", lastSeenInfo, lastOnline.ToShortDateString(), lastOnline.ToLongTimeString());
            }
           

            var friendName = GetFriendName(friendNumber);
            if (string.IsNullOrEmpty(friendName))
            {
                friendName = ViewModel.tox.GetFriendPublicKey(friendNumber).GetString();
            }

            var friendMv = new FriendControlModelView(ViewModel)
            {
                ChatNumber = friendNumber,
                Name = friendName,
                StatusMessage = friendStatus,
                ToxStatus = ToxStatus.Invalid,
                SelectedAction = FriendSelectedAction,
                DenyCallAction = FriendDenyCallAction,
                AcceptCallAction = FriendAcceptCallAction,
                CopyIDAction = FriendCopyIdAction,
                DeleteAction = FriendDeleteAction,
                GroupInviteAction = GroupInviteAction,
                HangupAction = FriendHangupAction
            };

            ViewModel.ChatCollection.Add(friendMv);
            RearrangeChatList();
        }

        private void FriendHangupAction(IFriendObject friendObject)
        {
            EndCall(friendObject);
        }

        private void GroupInviteAction(IFriendObject friendObject, IGroupObject groupObject)
        {
            ViewModel.tox.InviteFriend(friendObject.ChatNumber, groupObject.ChatNumber);
        }

        private async void FriendDeleteAction(IFriendObject friendObject)
        {
            var result = await this.ShowMessageAsync("Remove friend", string.Format("Are you sure you want to remove {0} from your friend list?", friendObject.Name), MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
            if (result != MessageDialogResult.Affirmative)
                return;

            ViewModel.ChatCollection.Remove(friendObject);
            var friendNumber = friendObject.ChatNumber;
            if (_convdic.ContainsKey(friendNumber))
            {
                _convdic.Remove(friendNumber);
                if (friendObject.Selected)
                {
                    ChatBox.Document = null;
                }
            }
            ViewModel.tox.DeleteFriend(friendNumber);
            friendObject.SelectedAction = null;
            friendObject.DenyCallAction = null;
            friendObject.AcceptCallAction = null;
            friendObject.CopyIDAction = null;
            friendObject.DeleteAction = null;
            friendObject.GroupInviteAction = null;
            friendObject.MainViewModel = null;

            SaveTox();
        }

        private void SaveTox()
        {
            if (!ViewModel.config.Portable)
            {
                if (!Directory.Exists(ViewModel.toxDataDir))
                    Directory.CreateDirectory(ViewModel.toxDataDir);
            }

            var data = ViewModel.tox.GetData();
            if (data != null)
                data.Save(ViewModel.toxDataFilename);
        }

        private void FriendCopyIdAction(IFriendObject friendObject)
        {
            Clipboard.Clear();
            Clipboard.SetText(ViewModel.tox.GetFriendPublicKey(friendObject.ChatNumber).GetString());
        }

        private void FriendSelectedAction(IFriendObject friendObject, bool isSelected)
        {
            MessageAlertClear(friendObject);

            if (isSelected)
            {
                if (!ViewModel.tox.GetFriendTypingStatus(friendObject.ChatNumber) || ViewModel.tox.GetFriendStatus(friendObject.ChatNumber) == ToxUserStatus.None)
                    TypingStatusLabel.Content = "";
                else
                    TypingStatusLabel.Content = GetFriendName(friendObject.ChatNumber) + " is typing...";

                SelectFriendControl(friendObject);
                ScrollChatBox();
                TextToSend.Focus();
            }
        }

        private void FriendAcceptCallAction(IFriendObject friendObject)
        {
            if (_call != null)
                return;

            _call = new ToxCall(_toxav, friendObject.CallIndex, friendObject.ChatNumber);
            _call.FilterAudio = ViewModel.config.FilterAudio;
            _call.Answer();
        }

        private void FriendDenyCallAction(IFriendObject friendObject)
        {
            if (_call == null)
            {
                _toxav.Reject(friendObject.CallIndex, "I'm busy...");
                friendObject.IsCalling = false;
            }
            else
            {
                _call.Stop();
                _call = null;
            }
        }

        private void AddFriendRequestToView(string id, string message)
        {
            var friendMv = new FriendControlModelView(ViewModel);
            friendMv.IsRequest = true;
            friendMv.Name = id;
            friendMv.ToxStatus = ToxStatus.Invalid;
            friendMv.RequestMessageData = new MessageData { Message = message, Username = "Request Message", Timestamp = DateTime.Now };
            friendMv.RequestFlowDocument = FlowDocumentExtensions.CreateNewDocument();
            friendMv.SelectedAction = FriendRequestSelectedAction;
            friendMv.AcceptAction = FriendRequestAcceptAction;
            friendMv.DeclineAction = FriendRequestDeclineAction;

            ViewModel.ChatRequestCollection.Add(friendMv);

            if (ListViewTabControl.SelectedIndex != 1)
            {
                RequestsTabItem.Header = "Requests*";
            }
        }

        private void FriendRequestSelectedAction(IFriendObject friendObject, bool isSelected)
        {
            friendObject.RequestFlowDocument.AddNewMessageRow(ViewModel.tox, friendObject.RequestMessageData, false);
        }

        private void FriendRequestAcceptAction(IFriendObject friendObject)
        {
            var friendnumber = ViewModel.tox.AddFriendNoRequest(new ToxKey(ToxKeyType.Public, friendObject.Name));

            if (friendnumber != -1)
                AddFriendToView(friendnumber, false);
            else
                this.ShowMessageAsync("Unknown Error", "Could not accept friend request.");

            ViewModel.ChatRequestCollection.Remove(friendObject);
            friendObject.RequestFlowDocument = null;
            friendObject.SelectedAction = null;
            friendObject.AcceptAction = null;
            friendObject.DeclineAction = null;
            friendObject.MainViewModel = null;

            SaveTox();
        }

        private void FriendRequestDeclineAction(IFriendObject friendObject)
        {
            ViewModel.ChatRequestCollection.Remove(friendObject);
            friendObject.RequestFlowDocument = null;
            friendObject.SelectedAction = null;
            friendObject.AcceptAction = null;
            friendObject.DeclineAction = null;
        }

        private void SelectGroupControl(IGroupObject group)
        {
            if (group == null)
            {
                return;
            }

            CallButton.Visibility = Visibility.Collapsed;
            FileButton.Visibility = Visibility.Collapsed;
            HangupButton.Visibility = Visibility.Collapsed;
            VideoButton.Visibility = Visibility.Collapsed;

            if (ViewModel.tox.GetGroupType(group.ChatNumber) == ToxGroupType.Av)
                MicButton.Visibility = Visibility.Visible;
            else
                MicButton.Visibility = Visibility.Collapsed;

            if (_groupdic.ContainsKey(group.ChatNumber))
            {
                ChatBox.Document = _groupdic[group.ChatNumber];
            }
            else
            {
                var document = FlowDocumentExtensions.CreateNewDocument();
                _groupdic.Add(group.ChatNumber, document);
                ChatBox.Document = _groupdic[group.ChatNumber];
            }

            VideoImageRow.Height = new GridLength(0);
            VideoGridSplitter.IsEnabled = false;
            VideoChatImage.Source = null;

            GroupListGrid.Visibility = Visibility.Visible;
            PeerColumn.Width = new GridLength(150);
        }

        private void EndCall()
        {
            if (_call != null)
            {
                var friendnumber = _toxav.GetPeerID(_call.CallIndex, 0);
                var friend = ViewModel.GetFriendObjectByNumber(friendnumber);

                EndCall(friend);
            }
            else
            {
                EndCall(null);
            }
        }

        private void EndCall(IFriendObject friend)
        {
            if (friend != null)
            {
                _toxav.Cancel(friend.CallIndex, friend.ChatNumber, "I'm busy...");

                friend.IsCalling = false;
                friend.IsCallingToFriend = false;
            }

            if (_call != null)
            {
                _call.Stop();
                _call = null;

                CurrentCallControl.TimerLabel.Content = "00:00:00";
            }

            ViewModel.CallingFriend = null;
            VideoImageRow.Height = new GridLength(0);
            VideoGridSplitter.IsEnabled = false;
            VideoChatImage.Source = null;

            HangupButton.Visibility = Visibility.Collapsed;
            VideoButton.Visibility = Visibility.Collapsed;
            CallButton.Visibility = Visibility.Visible;
        }

        private void SelectFriendControl(IFriendObject friend)
        {
            if (friend == null)
            {
                return;
            }
            var friendNumber = friend.ChatNumber;

            if (_call != null && _call.GetType() != typeof(ToxGroupCall))
            {
                if (_call.FriendNumber != friendNumber)
                {
                    HangupButton.Visibility = Visibility.Collapsed;
                    VideoButton.Visibility = Visibility.Collapsed;

                    if (ViewModel.tox.IsFriendOnline(friendNumber))
                    {
                        CallButton.Visibility = Visibility.Visible;
                        FileButton.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        CallButton.Visibility = Visibility.Collapsed;
                        FileButton.Visibility = Visibility.Collapsed;
                    }

                    VideoImageRow.Height = new GridLength(0);
                    VideoGridSplitter.IsEnabled = false;
                    VideoChatImage.Source = null;
                }
                else
                {
                    HangupButton.Visibility = Visibility.Visible;
                    VideoButton.Visibility = Visibility.Visible;
                    CallButton.Visibility = Visibility.Collapsed;

                    if (_toxav.GetPeerCodecSettings(_call.CallIndex, 0).CallType == ToxAvCallType.Video)
                    {
                        VideoImageRow.Height = new GridLength(300);
                        VideoGridSplitter.IsEnabled = true;
                    }
                }
            }
            else
            {
                if (!ViewModel.tox.IsFriendOnline(friendNumber))
                {
                    CallButton.Visibility = Visibility.Collapsed;
                    FileButton.Visibility = Visibility.Collapsed;
                }
                else
                {
                    CallButton.Visibility = Visibility.Visible;
                    FileButton.Visibility = Visibility.Visible;
                }

                VideoImageRow.Height = GridLength.Auto;
            }

            MicButton.Visibility = Visibility.Collapsed;

            if (_convdic.ContainsKey(friend.ChatNumber))
            {
                ChatBox.Document = _convdic[friend.ChatNumber];
            }
            else
            {
                var document = FlowDocumentExtensions.CreateNewDocument();
                _convdic.Add(friend.ChatNumber, document);
                ChatBox.Document = _convdic[friend.ChatNumber];
            }

            GroupListGrid.Visibility = Visibility.Collapsed;
            PeerColumn.Width = GridLength.Auto;
        }

        private void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            if (ViewModel.config.HideInTray && !_forceClose)
            {
                e.Cancel = true;
                ShowInTaskbar = false;
                WindowState = WindowState.Minimized;
                Hide();
            }
            else
            {
                KillTox(true);
                _nIcon.Dispose();
            }
        }

        private void KillTox(bool save)
        {
            if (_call != null)
            {
                _call.Stop();
                _call = null;
            }

            foreach (var transfer in _transfers)
                transfer.Kill(false);

            _convdic.Clear();
            _groupdic.Clear();
            _transfers.Clear();

            if (_toxav != null)
                _toxav.Dispose();

            if (ViewModel.tox != null)
            {
                if (save)
                    SaveTox();

                ViewModel.tox.Dispose();
            }

            if (ViewModel.config != null)
            {
                ViewModel.config.WindowSize = new Size(Width, Height);
                ConfigTools.Save(ViewModel.config, ViewModel.configFilename);
            }
        }

        private void OpenAddFriend_Click(object sender, RoutedEventArgs e)
        {
            FriendFlyout.IsOpen = !FriendFlyout.IsOpen;
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            if (!SettingsFlyout.IsOpen)
            {
                SettingsUsername.Text = GetSelfName();
                SettingsStatus.Text = GetSelfStatusMessage();
                SettingsNospam.Text = ViewModel.tox.GetNospam().ToString();

                var style = ThemeManager.DetectAppStyle(Application.Current);
                var accent = ThemeManager.GetAccent(style.Item2.Name);
                _oldAccent = accent;
                if (accent != null)
                    AccentComboBox.SelectedItem = AccentComboBox.Items.Cast<AccentColorMenuData>().Single(a => a.Name == style.Item2.Name);

                var theme = ThemeManager.GetAppTheme(style.Item1.Name);
                _oldAppTheme = theme;
                if (theme != null)
                    AppThemeComboBox.SelectedItem = AppThemeComboBox.Items.Cast<AppThemeMenuData>().Single(a => a.Name == style.Item1.Name);

                ViewModel.UpdateDevices();

                foreach(var item in VideoDevicesComboBox.Items)
                {
                    var device = (VideoMenuData)item;
                    if (device.Name == ViewModel.config.VideoDevice)
                    {
                        VideoDevicesComboBox.SelectedItem = item;
                        break;
                    }
                }

                if (InputDevicesComboBox.Items.Count - 1 >= ViewModel.config.InputDevice)
                    InputDevicesComboBox.SelectedIndex = ViewModel.config.InputDevice;

                if (OutputDevicesComboBox.Items.Count - 1 >= ViewModel.config.OutputDevice)
                    OutputDevicesComboBox.SelectedIndex = ViewModel.config.OutputDevice;

                ChatLogCheckBox.IsChecked = ViewModel.config.EnableChatLogging;
                HideInTrayCheckBox.IsChecked = ViewModel.config.HideInTray;
                PortableCheckBox.IsChecked = ViewModel.config.Portable;
                AudioNotificationCheckBox.IsChecked = ViewModel.config.EnableAudioNotifications;
                AlwaysNotifyCheckBox.IsChecked = ViewModel.config.AlwaysNotify;
                SpellcheckCheckBox.IsChecked = ViewModel.config.EnableSpellcheck;
                SpellcheckLanguageComboBox.SelectedItem = Enum.GetName(typeof(SpellcheckLanguage), ViewModel.config.SpellcheckLanguage);
                FilterAudioCheckbox.IsChecked = ViewModel.config.FilterAudio;

                if (!string.IsNullOrEmpty(ViewModel.config.ProxyAddress))
                    SettingsProxyAddress.Text = ViewModel.config.ProxyAddress;

                if (ViewModel.config.ProxyPort != 0)
                    SettingsProxyPort.Text = ViewModel.config.ProxyPort.ToString();

                foreach (ComboBoxItem item in ProxyTypeComboBox.Items)
                {
                    if ((ToxProxyType)int.Parse((string)item.Tag) == ViewModel.config.ProxyType)
                    {
                        ProxyTypeComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            SettingsFlyout.IsOpen = !SettingsFlyout.IsOpen;
        }

        private async void AddFriend_Click(object sender, RoutedEventArgs e)
        {
            var message = new TextRange(AddFriendMessage.Document.ContentStart, AddFriendMessage.Document.ContentEnd);

            if (string.IsNullOrWhiteSpace(AddFriendID.Text) || message.Text == null)
                return;

            var friendId = AddFriendID.Text.Trim();
            var tries = 0;

            if (friendId.Contains("@"))
            {
                if (ViewModel.config.ProxyType != ToxProxyType.None && ViewModel.config.RemindAboutProxy)
                {
                    var result = await this.ShowMessageAsync("Warning", "You're about to submit a dns lookup query, the configured proxy will not be used for this.\nDo you wish to continue?", MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, new MetroDialogSettings { AffirmativeButtonText = "Yes, don't remind me again", NegativeButtonText = "Yes", FirstAuxiliaryButtonText = "No" });
                    if (result == MessageDialogResult.Affirmative)
                    {
                        ViewModel.config.RemindAboutProxy = false;
                        ConfigTools.Save(ViewModel.config, ViewModel.configFilename);
                    }
                    else if (result == MessageDialogResult.FirstAuxiliary)
                    {
                        return;
                    }
                }

            discover:
                try
                {
                    tries++;
                    var id = DnsTools.DiscoverToxID(friendId, ViewModel.config.NameServices, ViewModel.config.OnlyUseLocalNameServiceStore);

                    if (string.IsNullOrEmpty(id))
                        throw new Exception("The server returned an empty result");

                    AddFriendID.Text = id;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Could not resolve {0}: {1}", friendId, ex.ToString());

                    if (tries < 3)
                        goto discover;

                    this.ShowMessageAsync("Could not find a tox id", ex.Message.ToString());
                }

                return;
            }

            try
            {
                var error = ToxErrorFriendAdd.Ok;
                var friendnumber = ViewModel.tox.AddFriend(new ToxId(friendId), message.Text, out error);

                if (error != ToxErrorFriendAdd.Ok)
                {
                    if (error != ToxErrorFriendAdd.SetNewNospam)
                        this.ShowMessageAsync("An error occurred", Tools.GetAFError(error));

                    return;
                }

                FriendFlyout.IsOpen = false;
                AddFriendToView(friendnumber, true);
            }
            catch
            {
                this.ShowMessageAsync("An error occurred", "The ID you entered is not valid.");
                return;
            }

            AddFriendID.Text = string.Empty;
            AddFriendMessage.Document.Blocks.Clear();
            AddFriendMessage.Document.Blocks.Add(new Paragraph(new Run("Hello, I'd like to add you to my friends list.")));

            SaveTox();
            FriendFlyout.IsOpen = false;
        }

        private void SaveSettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModel.tox.Name = SettingsUsername.Text;
            ViewModel.tox.StatusMessage = SettingsStatus.Text;

            uint nospam;
            if (uint.TryParse(SettingsNospam.Text, out nospam))
                ViewModel.tox.SetNospam(nospam);

            ViewModel.MainToxyUser.Name = GetSelfName();
            ViewModel.MainToxyUser.StatusMessage = GetSelfStatusMessage();

            ViewModel.config.HideInTray = (bool)HideInTrayCheckBox.IsChecked;

            SettingsFlyout.IsOpen = false;

            if (AccentComboBox.SelectedItem != null)
            {
                var accentName = ((AccentColorMenuData)AccentComboBox.SelectedItem).Name;
                var theme = ThemeManager.DetectAppStyle(Application.Current);
                var accent = ThemeManager.GetAccent(accentName);
                ThemeManager.ChangeAppStyle(Application.Current, accent, theme.Item1);

                ViewModel.config.AccentColor = accentName;
            }

            if (AppThemeComboBox.SelectedItem != null)
            {
                var themeName = ((AppThemeMenuData)AppThemeComboBox.SelectedItem).Name;
                var theme = ThemeManager.DetectAppStyle(Application.Current);
                var appTheme = ThemeManager.GetAppTheme(themeName);
                ThemeManager.ChangeAppStyle(Application.Current, theme.Item2, appTheme);

                ViewModel.config.Theme = themeName;
            }

            var index = InputDevicesComboBox.SelectedIndex;
            if (WaveIn.DeviceCount > 0 && WaveIn.DeviceCount >= index)
            {
                if (ViewModel.config.InputDevice != index)
                    if (_call != null)
                        _call.SwitchInputDevice(index);

                ViewModel.config.InputDevice = index;
            }

            index = OutputDevicesComboBox.SelectedIndex;
            if (WaveOut.DeviceCount > 0 && WaveOut.DeviceCount >= index)
            {
                if (ViewModel.config.OutputDevice != index)
                    if (_call != null)
                        _call.SwitchOutputDevice(index);

                ViewModel.config.OutputDevice = index;
            }

            if (VideoDevicesComboBox.SelectedItem != null)
                ViewModel.config.VideoDevice = ((VideoMenuData)VideoDevicesComboBox.SelectedItem).Name;

            ViewModel.config.EnableChatLogging = (bool)ChatLogCheckBox.IsChecked;
            ViewModel.config.Portable = (bool)PortableCheckBox.IsChecked;
            ViewModel.config.EnableAudioNotifications = (bool)AudioNotificationCheckBox.IsChecked;
            ViewModel.config.AlwaysNotify = (bool)AlwaysNotifyCheckBox.IsChecked;
            ViewModel.config.EnableSpellcheck = (bool)SpellcheckCheckBox.IsChecked;
            ViewModel.config.SpellcheckLanguage = (SpellcheckLanguage)Enum.Parse(typeof(SpellcheckLanguage), SpellcheckLanguageComboBox.SelectedItem.ToString());

            ViewModel.SpellcheckLangCode = ViewModel.config.SpellcheckLanguage.ToDescription();
            ViewModel.SpellcheckEnabled = ViewModel.config.EnableSpellcheck;
            ExecuteActionsOnNotifyIcon();

            var filterAudio = (bool)FilterAudioCheckbox.IsChecked;

            if (ViewModel.config.FilterAudio != filterAudio)
                if (_call != null)
                    _call.FilterAudio = filterAudio;

            ViewModel.config.FilterAudio = filterAudio;

            var proxyConfigChanged = false;
            var proxyType = (ToxProxyType)int.Parse((string)((ComboBoxItem)ProxyTypeComboBox.SelectedItem).Tag);

            var language = LanguageComboBox.Text;


            if (ViewModel.config.ProxyType != proxyType || ViewModel.config.ProxyAddress != SettingsProxyAddress.Text || ViewModel.config.ProxyPort.ToString() != SettingsProxyPort.Text || ViewModel.config.Language != language)
                proxyConfigChanged = true;

            ViewModel.config.ProxyType = proxyType;
            ViewModel.config.ProxyAddress = SettingsProxyAddress.Text;

            if (language!="")
                ViewModel.config.Language = language;

            int proxyPort;
            if (int.TryParse(SettingsProxyPort.Text, out proxyPort))
                ViewModel.config.ProxyPort = proxyPort;

            ConfigTools.Save(ViewModel.config, ViewModel.configFilename);
            SaveTox();

            _savingSettings = true;

            if (proxyConfigChanged)
            {
                this.ShowMessageAsync("Alert", "You have changed your proxy configuration.\nPlease restart Toxy to apply these changes.");
            }
        }

        private void TextToSend_KeyDown(object sender, KeyEventArgs e)
        {
            var text = TextToSend.Text;

            if (e.Key == Key.Enter)
            {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    return;

                if (e.IsRepeat)
                    return;

                if (string.IsNullOrEmpty(text))
                    return;

                var selectedChatNumber = ViewModel.SelectedChatNumber;
                if (!ViewModel.tox.IsFriendOnline(selectedChatNumber) && ViewModel.IsFriendSelected)
                {
                    var friendOnlineTip = (string) FindResource("Local_NotOnlineTip");
                    var data = new MessageData
                    {
                        Username = GetSelfName(),
                        Message = friendOnlineTip,
                        Id = 0, IsSelf = true, Timestamp = DateTime.Now
                    };
                    AddMessageToView(selectedChatNumber, data);

                    return;
                }

                if (text.StartsWith("/me "))
                {
                    //action
                    var action = text.Substring(4);
                    var messageid = -1;

                    if (ViewModel.IsFriendSelected)
                        messageid = ViewModel.tox.SendMessage(selectedChatNumber, action, ToxMessageType.Action);
                    else if (ViewModel.IsGroupSelected)
                        ViewModel.tox.SendGroupAction(selectedChatNumber, action);

                    var data = new MessageData
                    {
                        Username = "*  ",
                        Message = string.Format("{0} {1}",
                        GetSelfName(), action),
                        IsAction = true,
                        Id = messageid,
                        IsSelf = true,
                        IsGroupMsg = ViewModel.IsGroupSelected,
                        Timestamp = DateTime.Now
                    };
                    
                    if (ViewModel.IsFriendSelected)
                    {
                        AddActionToView(selectedChatNumber, data);
                        if (ViewModel.config.EnableChatLogging)
                        {
                            var toxMessage = new ToxMessage
                            {
                                PublicKey = ViewModel.tox.GetFriendPublicKey(selectedChatNumber).GetString(),
                                Message = data.Message,
                                Timestamp = DateTime.Now,
                                IsAction = true,
                                Name = data.Username,
                                ProfilePublicKey = ViewModel.tox.Id.PublicKey.GetString()
                            };
                            _dbConnection.InsertAsync(toxMessage);                            
                        }
                    }
                }
                else
                {
                    //regular message
                    foreach (var message in text.WordWrap(ToxConstants.MaxMessageLength))
                    {
                        var messageid = -1;

                        if (ViewModel.IsFriendSelected)
                            messageid = ViewModel.tox.SendMessage(selectedChatNumber, message, ToxMessageType.Message);
                        else if (ViewModel.IsGroupSelected)
                            ViewModel.tox.SendGroupMessage(selectedChatNumber, message);

                        var data = new MessageData { Username = GetSelfName(), Message = message, Id = messageid, IsSelf = true, IsGroupMsg = ViewModel.IsGroupSelected, Timestamp = DateTime.Now };

                        if (ViewModel.IsFriendSelected)
                        {
                            AddMessageToView(selectedChatNumber, data);

                            if (ViewModel.config.EnableChatLogging)
                            {
                                var toxMessage = new ToxMessage
                                {
                                    PublicKey = ViewModel.tox.GetFriendPublicKey(selectedChatNumber).GetString(),
                                    Message = data.Message,
                                    Timestamp = DateTime.Now,
                                    IsAction = false,
                                    Name = data.Username,
                                    ProfilePublicKey = ViewModel.tox.Id.PublicKey.GetString()
                                };
                                _dbConnection.InsertAsync(toxMessage);

                               
                            }
                        }
                    }
                }

                ScrollChatBox();

                TextToSend.Text = string.Empty;
                e.Handled = true;
            }
            else if (e.Key == Key.Tab && ViewModel.IsGroupSelected)
            {
                var names = ViewModel.tox.GetGroupNames(ViewModel.SelectedChatNumber);

                foreach (var name in names)
                {
                    var lastPart = text.Split(' ').Last();
                    if (!name.ToLower().StartsWith(lastPart.ToLower()))
                        continue;

                    if (text.Split(' ').Length > 1)
                    {
                        if (text.Last() != ' ')
                        {
                            TextToSend.Text = string.Format("{0}{1} ", text.Substring(0, text.Length - lastPart.Length), name);
                            TextToSend.SelectionStart = TextToSend.Text.Length;
                        }
                    }
                    else
                    {
                        TextToSend.Text = string.Format("{0}, ", name);
                        TextToSend.SelectionStart = TextToSend.Text.Length;
                    }
                }

                e.Handled = true;
            }
        }

        private void GithubButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Reverp/Toxy");
        }

        private void TextToSend_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!ViewModel.IsFriendSelected)
                return;

            var text = TextToSend.Text;

            if (string.IsNullOrEmpty(text))
            {
                if (_typing)
                {
                    _typing = false;
                    ViewModel.tox.SetTypingStatus(ViewModel.SelectedChatNumber, _typing);
                }
            }
            else
            {
                if (!_typing)
                {
                    _typing = true;
                    ViewModel.tox.SetTypingStatus(ViewModel.SelectedChatNumber, _typing);
                }
            }
        }

        private void CopyIDButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetDataObject(ViewModel.tox.Id.ToString());
        }

        private void MetroWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_resizing && _focusTextbox)
                TextToSend.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
        }

        private void TextToSend_OnGotFocus(object sender, RoutedEventArgs e)
        {
            _focusTextbox = true;
        }

        private void TextToSend_OnLostFocus(object sender, RoutedEventArgs e)
        {
            _focusTextbox = false;
        }

        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_resizing)
            {
                _resizing = false;
                if (_focusTextbox)
                {
                    TextToSend.Focus();
                    _focusTextbox = false;
                }
            }
        }

        private void OnlineThumbButton_Click(object sender, EventArgs e)
        {
            SetStatus(ToxStatus.None, true);
        }

        private void AwayThumbButton_Click(object sender, EventArgs e)
        {
            SetStatus(ToxStatus.Away, true);
        }

        private void BusyThumbButton_Click(object sender, EventArgs e)
        {
            SetStatus(ToxStatus.Busy, true);
        }

        private void ListViewTabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void StatusRectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StatusContextMenu.PlacementTarget = this;
            StatusContextMenu.IsOpen = true;
        }

        private void MenuItem_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.tox.IsConnected)
                return;

            var menuItem = (System.Windows.Controls.MenuItem)e.Source;
            SetStatus((ToxStatus)int.Parse(menuItem.Tag.ToString()), true);
        }

        private void TextToSend_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (ViewModel.IsGroupSelected)
                    return;

                if (Clipboard.ContainsImage())
                {
                    var bmp = (Bitmap)System.Windows.Forms.Clipboard.GetImage();
                    var bytes = bmp.GetBytes();

                    if (!_convdic.ContainsKey(ViewModel.SelectedChatNumber))
                        _convdic.Add(ViewModel.SelectedChatNumber, FlowDocumentExtensions.CreateNewDocument());

                    var fileInfo = ViewModel.tox.FileSend(ViewModel.SelectedChatNumber, ToxFileKind.Data, bytes.Length, "image.bmp");

                    if (fileInfo.Number == -1)
                        return;

                    var transfer = new FileSender(ViewModel.tox, fileInfo.Number, ViewModel.SelectedChatNumber, ToxFileKind.Data, bytes.Length, "image.bmp", new MemoryStream(bytes));
                    var control = _convdic[ViewModel.SelectedChatNumber].AddNewFileTransfer(ViewModel.tox, transfer);
                    transfer.Tag = control;

                    transfer.Tag.SetStatus(string.Format("Waiting for {0} to accept...", GetFriendName(ViewModel.SelectedChatNumber)));
                    transfer.Tag.AcceptButton.Visibility = Visibility.Collapsed;
                    transfer.Tag.DeclineButton.Visibility = Visibility.Visible;

                    control.OnDecline += delegate(FileTransfer ft)
                    {
                        ft.Kill(false);

                        if (_transfers.Contains(ft))
                            _transfers.Remove(ft);
                    };

                    control.OnPause += delegate(FileTransfer ft)
                    {
                        if (ft.Paused)
                            ViewModel.tox.FileControl(ft.FriendNumber, ft.FileNumber, ToxFileControl.Pause);
                        else
                            ViewModel.tox.FileControl(ft.FriendNumber, ft.FileNumber, ToxFileControl.Resume);
                    };

                    _transfers.Add(transfer);

                    ScrollChatBox();
                }
            }
        }

        private void SetStatus(ToxStatus? newStatus, bool changeUserStatus)
        {
            if (newStatus == null)
            {
                newStatus = ToxStatus.Invalid;
            }
            else
            {
                if (changeUserStatus)
                {
                    ViewModel.tox.Status = (ToxUserStatus)newStatus.GetValueOrDefault();

                    if (ViewModel.tox.Status != (ToxUserStatus)newStatus.GetValueOrDefault())
                        return;
                }
            }

            Dispatcher.BeginInvoke(((Action)(() => ViewModel.MainToxyUser.ToxStatus = newStatus.GetValueOrDefault())));
        }

        private void CallButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.IsFriendSelected)
                return;

            if (_call != null)
                return;

            var selectedChatNumber = ViewModel.SelectedChatNumber;
            if (!ViewModel.tox.IsFriendOnline(selectedChatNumber))
                return;

            int callIndex;
            var error = _toxav.Call(selectedChatNumber, ToxAv.DefaultCodecSettings, 30, out callIndex);
            if (error != ToxAvError.None)
                return;

            var friendnumber = _toxav.GetPeerID(callIndex, 0);
            _call = new ToxCall(_toxav, callIndex, friendnumber);
            _call.FilterAudio = ViewModel.config.FilterAudio;

            CallButton.Visibility = Visibility.Collapsed;
            HangupButton.Visibility = Visibility.Visible;
            VideoButton.Visibility = Visibility.Visible;

            var callingFriend = ViewModel.GetFriendObjectByNumber(friendnumber);
            if (callingFriend != null)
            {
                ViewModel.CallingFriend = callingFriend;
                callingFriend.IsCallingToFriend = true;
            }
        }

        private void MainHangupButton_OnClick(object sender, RoutedEventArgs e)
        {
            EndCall();
        }

        private void FileButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.IsFriendSelected)
                return;

            var selectedChatNumber = ViewModel.SelectedChatNumber;
            if (!ViewModel.tox.IsFriendOnline(selectedChatNumber))
                return;

            var dialog = new OpenFileDialog();
            dialog.InitialDirectory = Environment.CurrentDirectory;
            dialog.Multiselect = false;

            if (dialog.ShowDialog() != true)
                return;

            var filename = dialog.FileName;

            SendFile(selectedChatNumber, filename);
        }

        private void SendFile(int chatNumber, string filename)
        {
            var info = new FileInfo(filename);
            var fileInfo = ViewModel.tox.FileSend(chatNumber, ToxFileKind.Data, info.Length, filename.Split('\\').Last<string>());

            if (fileInfo.Number == -1)
                return;

            var transfer = new FileSender(ViewModel.tox, fileInfo.Number, chatNumber, ToxFileKind.Data, info.Length, filename.Split('\\').Last<string>(), filename);
            var control = _convdic[chatNumber].AddNewFileTransfer(ViewModel.tox, transfer);
            transfer.Tag = control;

            control.SetStatus(string.Format("Waiting for {0} to accept...", GetFriendName(chatNumber)));
            control.AcceptButton.Visibility = Visibility.Collapsed;
            control.DeclineButton.Visibility = Visibility.Visible;

            control.OnDecline += delegate(FileTransfer ft)
            {
                ft.Kill(false);

                if (_transfers.Contains(ft))
                    _transfers.Remove(ft);
            };

            control.OnPause += delegate(FileTransfer ft)
            {
                if (ft.Paused)
                    ViewModel.tox.FileControl(ft.FriendNumber, ft.FileNumber, ToxFileControl.Pause);
                else
                    ViewModel.tox.FileControl(ft.FriendNumber, ft.FileNumber, ToxFileControl.Resume);
            };

            _transfers.Add(transfer);
            ScrollChatBox();
        }

        private void ExecuteActionsOnNotifyIcon()
        {
            _nIcon.Visible = ViewModel.config.HideInTray;
        }

        private void mv_Activated(object sender, EventArgs e)
        {
            _nIcon.Icon = _notifyIcon;
            ViewModel.HasNewMessage = false;
        }

        private void AccentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var theme = ThemeManager.DetectAppStyle(Application.Current);
            var accent = ThemeManager.GetAccent(((AccentColorMenuData)AccentComboBox.SelectedItem).Name);
            ThemeManager.ChangeAppStyle(Application.Current, accent, theme.Item1);
        }

        private void SettingsFlyout_IsOpenChanged(object sender, RoutedEventArgs e)
        {
            if (!SettingsFlyout.IsOpen && !_savingSettings)
            {
                ThemeManager.ChangeAppStyle(Application.Current, _oldAccent, _oldAppTheme);
            }
            else if (_savingSettings)
            {
                _savingSettings = false;
            }
        }

        private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
        {
            var content = (LanguageMenuData)e.AddedItems[0];
            var fileShortCut = ViewModel.getShortLanguageName(content.Name);
            if (fileShortCut.Equals("fail"))
                return;

            ViewModel.ChangeLanguage(fileShortCut);
        } 

        private void AppThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var theme = ThemeManager.DetectAppStyle(Application.Current);
            var appTheme = ThemeManager.GetAppTheme(((AppThemeMenuData)AppThemeComboBox.SelectedItem).Name);
            ThemeManager.ChangeAppStyle(Application.Current, theme.Item2, appTheme);
        }

        private void ExportDataButton_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.Title = "Export Tox data";
            dialog.InitialDirectory = Environment.CurrentDirectory;

            if (dialog.ShowDialog() != true)
                return;

            try { File.WriteAllBytes(dialog.FileName, ViewModel.tox.GetData().Bytes); }
            catch { this.ShowMessageAsync("Error", "Could not export data."); }
        }

        private void AvatarMenuItem_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            var menuItem = (System.Windows.Controls.MenuItem)e.Source;
            var item = (AvatarMenuItem)menuItem.Tag;

            switch (item)
            {
                case AvatarMenuItem.ChangeAvatar:
                    ChangeAvatar();
                    break;
                case AvatarMenuItem.RemoveAvatar:
                    RemoveAvatar();
                    break;
            }
        }

        private void ChangeAvatar()
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Image files (*.png, *.gif, *.jpeg, *.jpg) | *.png;*.gif;*.jpeg;*.jpg";
            dialog.InitialDirectory = Environment.CurrentDirectory;
            dialog.Multiselect = false;

            if (dialog.ShowDialog() != true)
                return;

            var filename = dialog.FileName;
            var info = new FileInfo(filename);
          
            var avatarBytes = File.ReadAllBytes(filename);
            var stream = new MemoryStream(avatarBytes);
            var bmp = new Bitmap(stream);

            if(bmp.RawFormat != ImageFormat.Png)
            {
                var memStream = new MemoryStream();
                bmp.Save(memStream, ImageFormat.Png);
                bmp.Dispose();

                bmp = new Bitmap(memStream);
                avatarBytes = memStream.ToArray();
            }
            
            if (avatarBytes.Length > 0x4000)
            {
                double width = 64, height = 64;
                var newBmp = new Bitmap((int)width, (int)height);

                using (var g = Graphics.FromImage(newBmp))
                {
                    var ratioX = width / (double)bmp.Width;
                    var ratioY = height / (double)bmp.Height;
                    var ratio = ratioX < ratioY ? ratioX : ratioY;

                    var newWidth = (int)(bmp.Width * ratio);
                    var newHeight = (int)(bmp.Height * ratio);

                    var posX = (int)((width - (bmp.Width * ratio)) / 2);
                    var posY = (int)((height - (bmp.Height * ratio)) / 2);
                    
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(bmp, posX, posY, newWidth, newHeight);
                }

                bmp.Dispose();

                bmp = newBmp;
                avatarBytes = AvatarBitmapToBytes(bmp);

                if (avatarBytes.Length > 0x4000)
                {
                    this.ShowMessageAsync("Error", "This image is bigger than 16 KB and Toxy could not resize the image.");
                    return;
                }
            }

            ViewModel.MainToxyUser.AvatarBytes = avatarBytes;
            ViewModel.MainToxyUser.Avatar = bmp.ToBitmapImage(ImageFormat.Png);
            bmp.Dispose();

            var avatarsDir = Path.Combine(ViewModel.toxDataDir, "avatars");
            var selfAvatarFile = Path.Combine(avatarsDir, ViewModel.tox.Id.PublicKey.GetString() + ".png");

            if (!Directory.Exists(avatarsDir))
                Directory.CreateDirectory(avatarsDir);

            File.WriteAllBytes(selfAvatarFile, avatarBytes);

            //let's announce our new avatar
            var hash = ToxTools.Hash(avatarBytes);
            foreach (var friend in ViewModel.tox.Friends)
            {
                if (!ViewModel.tox.IsFriendOnline(friend))
                    continue;

                var fileInfo = ViewModel.tox.FileSend(friend, ToxFileKind.Avatar, avatarBytes.Length, "avatar.png", hash);
                var transfer = new FileSender(ViewModel.tox, fileInfo.Number, friend, ToxFileKind.Avatar, avatarBytes.Length, "", selfAvatarFile);
                _transfers.Add(transfer);
            }
        }

        private byte[] AvatarBitmapToBytes(Bitmap bmp)
        {
            using (var stream = new MemoryStream())
            {
                bmp.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }

        private void RemoveAvatar()
        {
            ViewModel.MainToxyUser.Avatar = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/profilepicture.png"));

            if (!ViewModel.config.Portable)
            {
                var path = Path.Combine(ViewModel.toxDataDir, "avatar.png");

                if (File.Exists(path))
                    File.Delete(path);
            }
            else
            {
                if (File.Exists("avatar.png"))
                    File.Delete("avatar.png");
            }

            foreach (var friend in ViewModel.tox.Friends)
            {
                if (!ViewModel.tox.IsFriendOnline(friend))
                    continue;

                ViewModel.tox.FileSend(friend, ToxFileKind.Avatar, 0, "");
            }
        }

        private void AvatarImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AvatarContextMenu.PlacementTarget = this;
            AvatarContextMenu.IsOpen = true;
        }

        private void MessageAlertIncrement(IChatObject chat)
        {
            if (!chat.Selected)
            {
                chat.HasNewMessage = true;
                chat.NewMessageCount++;
            }

            if (ViewModel.config.EnableAudioNotifications && _call == null)
            {
                if (WindowState == WindowState.Normal && ViewModel.config.AlwaysNotify && !chat.Selected)
                {
                    Winmm.PlayMessageNotify();
                }
                else if (WindowState == WindowState.Minimized || !IsActive)
                {
                    Winmm.PlayMessageNotify();
                }
            }
        }

        private void MessageAlertClear(IChatObject chat)
        {
            chat.HasNewMessage = false;
            chat.NewMessageCount = 0;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SearchBox.Text))
            {
                foreach (var chat in this.ViewModel.ChatCollection)
                {
                    if (!chat.Name.ToLower().Contains(SearchBox.Text.ToLower()))
                    {
                        if (chat.GetType() == typeof(FriendControlModelView) || chat.GetType() == typeof(GroupControlModelView))
                        {
                            var view = (BaseChatModelView)chat;
                            view.Visible = false;
                        }
                    }
                }
            }
            else
            {
                foreach (var chat in this.ViewModel.ChatCollection)
                {
                    if (chat.GetType() == typeof(FriendControlModelView) || chat.GetType() == typeof(GroupControlModelView))
                    {
                        var view = (BaseChatModelView)chat;
                        view.Visible = true;
                    }
                }
            }
        }

        private async void GroupMenuItem_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            var menuItem = (System.Windows.Controls.MenuItem)e.Source;
            var item = (GroupMenuItem)menuItem.Tag;

            if (item == GroupMenuItem.TextAudio && _call != null)
            {
                await this.ShowMessageAsync("Error", "Could not create audio groupchat, there's already a call in progress.");
                return;
            }

            var groupNumber = item == GroupMenuItem.Text ? ViewModel.tox.NewGroup() : _toxav.AddAvGroupchat();
            if (groupNumber != -1)
            {
                AddGroupToView(groupNumber, (ToxGroupType)item);
            }

            if (item == GroupMenuItem.TextAudio)
            {
                _call = new ToxGroupCall(_toxav, groupNumber);
                _call.FilterAudio = ViewModel.config.FilterAudio;
                _call.Start(ViewModel.config.InputDevice, ViewModel.config.OutputDevice, ToxAv.DefaultCodecSettings);
            }

            ViewModel.tox.SetGroupTitle(groupNumber, string.Format("Groupchat #{0}", groupNumber));
        }

        private async void mv_Loaded(object sender, RoutedEventArgs e)
        {
            ToxOptions options;
            if (ViewModel.config.ProxyType != ToxProxyType.None)
                options = new ToxOptions(ViewModel.config.Ipv6Enabled, ViewModel.config.ProxyType, ViewModel.config.ProxyAddress, ViewModel.config.ProxyPort);
            else
                options = new ToxOptions(ViewModel.config.Ipv6Enabled, !ViewModel.config.UdpDisabled);

            ViewModel.tox = new Tox(options);

            var data = await LoadTox();
            if (data != null)
                ViewModel.tox = new Tox(options, data);

            ViewModel.tox.OnFriendNameChanged += tox_OnFriendNameChanged;
            ViewModel.tox.OnFriendMessageReceived += tox_OnFriendMessageReceived;
            ViewModel.tox.OnFriendRequestReceived += tox_OnFriendRequestReceived;
            ViewModel.tox.OnFriendStatusChanged += tox_OnFriendStatusChanged;
            ViewModel.tox.OnFriendStatusMessageChanged += tox_OnFriendStatusMessageChanged;
            ViewModel.tox.OnFriendTypingChanged += tox_OnFriendTypingChanged;
            ViewModel.tox.OnConnectionStatusChanged += tox_OnConnectionStatusChanged;
            ViewModel.tox.OnFriendConnectionStatusChanged += tox_OnFriendConnectionStatusChanged;
            ViewModel.tox.OnFileSendRequestReceived += tox_OnFileSendRequestReceived;
            ViewModel.tox.OnFileChunkReceived += tox_OnFileChunkReceived;
            ViewModel.tox.OnFileControlReceived += tox_OnFileControlReceived;
            ViewModel.tox.OnFileChunkRequested += tox_OnFileChunkRequested;
            ViewModel.tox.OnReadReceiptReceived += tox_OnReadReceiptReceived;
            ViewModel.tox.OnGroupTitleChanged += tox_OnGroupTitleChanged;

            ViewModel.tox.OnGroupInvite += tox_OnGroupInvite;
            ViewModel.tox.OnGroupMessage += tox_OnGroupMessage;
            ViewModel.tox.OnGroupAction += tox_OnGroupAction;
            ViewModel.tox.OnGroupNamelistChange += tox_OnGroupNamelistChange;

            _toxav = new ToxAv(ViewModel.tox.Handle, 1);
            _toxav.OnInvite += toxav_OnInvite;
            _toxav.OnStart += toxav_OnStart;
            _toxav.OnEnd += toxav_OnEnd;
            _toxav.OnPeerTimeout += toxav_OnEnd;
            _toxav.OnRequestTimeout += toxav_OnEnd;
            _toxav.OnReject += toxav_OnEnd;
            _toxav.OnCancel += toxav_OnEnd;
            _toxav.OnReceivedAudio += toxav_OnReceivedAudio;
            _toxav.OnReceivedVideo += toxav_OnReceivedVideo;
            _toxav.OnPeerCodecSettingsChanged += toxav_OnPeerCodecSettingsChanged;
            _toxav.OnReceivedGroupAudio += toxav_OnReceivedGroupAudio;

            DoBootstrap();
            ViewModel.tox.Start();
            _toxav.Start();

            if (string.IsNullOrEmpty(GetSelfName()))
                ViewModel.tox.Name = "Tox User";

            if (string.IsNullOrEmpty(GetSelfStatusMessage()))
                ViewModel.tox.StatusMessage = "Toxing on Toxy";

            ViewModel.MainToxyUser.Name = GetSelfName();
            ViewModel.MainToxyUser.StatusMessage = GetSelfStatusMessage();

            InitializeNotifyIcon();

            SetStatus(null, false);
            InitFriends();

            TextToSend.AddHandler(DragOverEvent, new DragEventHandler(Chat_DragOver), true);
            TextToSend.AddHandler(DropEvent, new DragEventHandler(Chat_Drop), true);

            ChatBox.AddHandler(DragOverEvent, new DragEventHandler(Chat_DragOver), true);
            ChatBox.AddHandler(DropEvent, new DragEventHandler(Chat_Drop), true);

            if (ViewModel.tox.Friends.Length > 0)
                ViewModel.SelectedChatObject = ViewModel.ChatCollection.OfType<IFriendObject>().FirstOrDefault();

            InitDatabase();
            LoadAvatars();
        }

        private void DoBootstrap()
        {
            if (ViewModel.config.Nodes.Length >= 4)
            {
                var random = new Random();
                var indices = new List<int>();

                for (var i = 0; i < 4; )
                {
                    var index = random.Next(ViewModel.config.Nodes.Length);
                    if (indices.Contains(index))
                        continue;

                    if (Bootstrap(ViewModel.config.Nodes[index]))
                    {
                        indices.Add(index);
                        i++;
                    }
                }
            }
            else
            {
                foreach (var node in ViewModel.config.Nodes)
                    Bootstrap(node);
            }

            WaitAndBootstrap(20000);
        }

        private async void WaitAndBootstrap(int delay)
        {
            await Task.Factory.StartNew(async() =>
            {
                //wait 'delay' seconds, check if we're connected, if not, bootstrap again
                await Task.Delay(delay);

                if (!ViewModel.tox.IsConnected)
                {
                    Debug.WriteLine("We're still not connected, bootstrapping again");
                    DoBootstrap();
                }
            });
        }

        private bool Bootstrap(ToxConfigNode node)
        {
            ToxErrorBootstrap error;
            var success = ViewModel.tox.Bootstrap(new ToxNode(node.Address, node.Port, new ToxKey(ToxKeyType.Public, node.ClientId)), out error);
            if (success)
                Debug.WriteLine("Bootstrapped off of {0}:{1}", node.Address, node.Port);
            else
                Debug.WriteLine("Could not bootstrap off of {0}:{1}, error: {2}", node.Address, node.Port, error);

            return success;
        }

        private string[] GetProfileNames(string path)
        {
            if (!Directory.Exists(path))
                return null;

            var profiles = new List<string>();

            foreach (var profile in Directory.GetFiles(path, "*.tox", SearchOption.TopDirectoryOnly).Where(s => s.EndsWith(".tox")))
                profiles.Add(profile.Substring(0, profile.LastIndexOf(".tox", StringComparison.Ordinal)).Split('\\').Last());

            return profiles.ToArray();
        }

        private async void SwitchProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var profiles = GetProfileNames(ViewModel.toxDataDir);
            
            if (profiles == null || profiles.Length < 1)
                return;

            var dialog = new SwitchProfileDialog(profiles, this);
            await this.ShowMetroDialogAsync(dialog);
            var result = await dialog.WaitForButtonPressAsync();
            await this.HideMetroDialogAsync(dialog);

            if (result == null)
                return;

            if (result.Result == SwitchProfileDialogResult.OK)
            {
                if (string.IsNullOrEmpty(result.Input))
                    return;

                if (!LoadProfile(result.Input, false))
                    await this.ShowMessageAsync("Error", "Could not load profile, make sure it exists/is accessible.");
            }
            else if (result.Result == SwitchProfileDialogResult.New)
            {
                var profile = await this.ShowInputAsync("New Profile", "Enter a name for your new profile.");
                if (string.IsNullOrEmpty(profile))
                    await this.ShowMessageAsync("Error", "Could not create profile, you must enter a name for your profile.");
                else
                {
                    if (!CreateNewProfile(profile))
                        await this.ShowMessageAsync("Error", "Could not create profile, did you enter a valid name?");
                }
            }
            else if (result.Result == SwitchProfileDialogResult.Import)
            {
                var data = ToxData.FromDisk(result.Input);
                var t = new Tox(ToxOptions.Default, data);

                if (data == null)
                {
                    await this.ShowInputAsync("Error", "Could not load tox profile.");
                }
                else
                {
                    var profile = await this.ShowInputAsync("New Profile", "Enter a name for your new profile.");
                    if (string.IsNullOrEmpty(profile))
                        await this.ShowMessageAsync("Error", "Could not create profile, you must enter a name for your profile.");
                    else
                    {
                        var path = Path.Combine(ViewModel.toxDataDir, profile + ".tox");
                        if (!File.Exists(path))
                        {
                            t.Name = profile;

                            if (t.GetData().Save(path))
                                if (!LoadProfile(profile, false))
                                    await this.ShowMessageAsync("Error", "Could not load profile, make sure it exists/is accessible.");
                        }
                        else
                        {
                            await this.ShowMessageAsync("Error", "Could not create profile, a profile with the same name already exists.");
                        }
                    }
                }
            }
        }

        private bool CreateNewProfile(string profileName)
        {
            var path = Path.Combine(ViewModel.toxDataDir, profileName + ".tox");
            if (File.Exists(path))
                return false;

            var tox = new Tox(ToxOptions.Default)
            {
                Name = profileName
            };

            if (!tox.GetData().Save(path))
            {
                tox.Dispose();
                return false;
            }

            tox.Dispose();
            return LoadProfile(profileName, false);
        }

        private bool LoadProfile(string profile, bool allowReload)
        {
            if (ViewModel.config.ProfileName == profile && !allowReload)
                return true;

            if (!File.Exists(Path.Combine(ViewModel.toxDataDir, profile + ".tox")))
                return false;

            KillTox(false);
            ViewModel.ChatCollection.Clear();

            ViewModel.config.ProfileName = profile;
            mv_Loaded(this, new RoutedEventArgs());

            return true;
        }

        public void GroupPeerCopyKey_Click(object sender, RoutedEventArgs e)
        {
            var peer = GroupListView.SelectedItem as GroupPeer;
            if (peer == null)
                return;

            Clipboard.Clear();
            Clipboard.SetText(peer.PublicKey.GetString());
        }

        private void GroupPeerMute_Click(object sender, RoutedEventArgs e)
        {
            var peer = GroupListView.SelectedItem as GroupPeer;
            if (peer == null)
                return;

            peer.Muted = !peer.Muted;
        }

        private void GroupPeerIgnore_Click(object sender, RoutedEventArgs e)
        {
            var peer = GroupListView.SelectedItem as GroupPeer;
            if (peer == null)
                return;

            peer.Ignored = !peer.Ignored;
        }

        private void MicButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_call == null || _call.GetType() != typeof(ToxGroupCall))
                return;

            var groupCall = (ToxGroupCall)_call;
            groupCall.Muted = !groupCall.Muted;
        }

        private void VideoButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_call == null || _call.GetType() == typeof(ToxGroupCall))
                return;

            _call.ToggleVideo(VideoButton.IsChecked != null && (bool)VideoButton.IsChecked, ViewModel.config.VideoDevice);
        }

        private void ProcessVideoFrame(IntPtr frame)
        {
            var vpxImage = VpxImage.FromPointer(frame);
            var dest = VpxHelper.Yuv420ToRgb(vpxImage, vpxImage.d_w * vpxImage.d_h * 4);

            vpxImage.Free();

            var bytesPerPixel = (PixelFormats.Bgra32.BitsPerPixel + 7) / 8;
            var stride = 4 * (((int)vpxImage.d_w * bytesPerPixel + 3) / 4);

            var source = BitmapSource.Create((int)vpxImage.d_w, (int)vpxImage.d_h, 96d, 96d, PixelFormats.Bgra32, null, dest, stride);
            source.Freeze();

            Dispatcher.Invoke(() => VideoChatImage.Source = source);
        }

        private void NameTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2)
                return;

            OpenSettings_Click(null, null);

            RoutedEventHandler handler = null;
            handler = (s, args) =>
            {
                if (SettingsFlyout.IsOpen)
                {
                    Keyboard.Focus(SettingsUsername);
                    SettingsUsername.SelectAll();
                    SettingsFlyout.IsOpenChanged -= handler;
                }
            };

            SettingsFlyout.IsOpenChanged += handler;
        }

        private void StatusTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2)
                return;

            OpenSettings_Click(null, null);

            RoutedEventHandler handler = null;
            handler = (s, args) =>
            {
                if (SettingsFlyout.IsOpen)
                {
                    Keyboard.Focus(SettingsStatus);
                    SettingsStatus.SelectAll();
                    SettingsFlyout.IsOpenChanged -= handler;
                }
            };

            SettingsFlyout.IsOpenChanged += handler;
        }

        private void RandomNospamButton_Click(object sender, RoutedEventArgs e)
        {
            var buffer = new byte[sizeof(uint)];
            var random = new Random();

            random.NextBytes(buffer);
            SettingsNospam.Text = BitConverter.ToUInt32(buffer, 0).ToString();
        }

      
    }
}
