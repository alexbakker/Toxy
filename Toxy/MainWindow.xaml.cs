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
    public partial class MainWindow : MetroWindow
    {
        private Tox tox;
        private ToxAv toxav;
        private ToxCall call;

        private Dictionary<int, FlowDocument> convdic = new Dictionary<int, FlowDocument>();
        private Dictionary<int, FlowDocument> groupdic = new Dictionary<int, FlowDocument>();
        private List<FileTransfer> transfers = new List<FileTransfer>();

        private bool resizing;
        private bool focusTextbox;
        private bool typing;
        private bool savingSettings;
        private bool forceClose;

        private Accent oldAccent;
        private AppTheme oldAppTheme;

        private Config config;
        private AvatarStore avatarStore;

        NotifyIcon nIcon = new NotifyIcon();

        private Icon notifyIcon;
        private Icon newMessageNotifyIcon;

        private SQLiteAsyncConnection dbConnection;

        private string toxDataDir
        {
            get
            {
                if (!config.Portable)
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tox");
                else
                    return Environment.CurrentDirectory;
            }
        }

        private string toxDataFilename
        {
            get
            {
                return Path.Combine(toxDataDir, string.Format("{0}.tox", string.IsNullOrEmpty(config.ProfileName) ? tox.Id.PublicKey.GetString().Substring(0, 10) : config.ProfileName));
            }
        }

        private string toxOldDataFilename
        {
            get
            {
                return Path.Combine(toxDataDir, "tox_save");
            }
        }

        private string configFilename = "config.xml";

        private string dbFilename
        {
            get
            {
                return Path.Combine(toxDataDir, "toxy.db");
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            DataContext = new MainWindowViewModel();
            Debug.AutoFlush = true;

            if (File.Exists(configFilename))
            {
                config = ConfigTools.Load(configFilename);
            }
            else
            {
                config = new Config();
                ConfigTools.Save(config, configFilename);
            }

            ViewModel.ChangeLanguage(getShortLanguageName(config.Language));
            avatarStore = new AvatarStore(toxDataDir);
            applyConfig();
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
                    group.AdditionalInfo = string.Format("Topic set by: {0}", tox.GetGroupMemberName(e.GroupNumber, e.PeerNumber));
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
                        group.StatusMessage = string.Format("Peers online: {0}", tox.GetGroupMemberCount(group.ChatNumber));

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
                                var peer = group.PeerList.GetPeerByPublicKey(tox.GetGroupPeerPublicKey(e.GroupNumber, e.PeerNumber));
                                if (peer != null)
                                {
                                    peer.Name = tox.GetGroupMemberName(e.GroupNumber, e.PeerNumber);
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

                var peer = group.PeerList.GetPeerByPublicKey(tox.GetGroupPeerPublicKey(e.GroupNumber, e.PeerNumber));
                if (peer != null && peer.Ignored)
                    return;

                MessageData data = new MessageData(id: 0, username: "*  ", message: string.Format("{0} {1}", tox.GetGroupMemberName(e.GroupNumber, e.PeerNumber), e.Action), isAction: true, timestamp: DateTime.Now, isGroupMsg: true, isSelf: tox.PeerNumberIsOurs(e.GroupNumber, e.PeerNumber));

                if (groupdic.ContainsKey(e.GroupNumber))
                {
                    groupdic[e.GroupNumber].AddNewMessageRow(tox, data, false);
                }
                else
                {
                    FlowDocument document = FlowDocumentExtensions.CreateNewDocument();
                    groupdic.Add(e.GroupNumber, document);
                    groupdic[e.GroupNumber].AddNewMessageRow(tox, data, false);
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

                var peer = group.PeerList.GetPeerByPublicKey(tox.GetGroupPeerPublicKey(e.GroupNumber, e.PeerNumber));
                if (peer != null && peer.Ignored)
                    return;

                MessageData data = new MessageData(id: 0, username: tox.GetGroupMemberName(e.GroupNumber, e.PeerNumber), message: e.Message, timestamp: DateTime.Now, isGroupMsg: true, isSelf: tox.PeerNumberIsOurs(e.GroupNumber, e.PeerNumber), isAction: false);

                if (groupdic.ContainsKey(e.GroupNumber))
                {
                    var run = groupdic[e.GroupNumber].GetLastMessageRun();

                    if (run != null)
                    {
                        if (((MessageData)run.Tag).Username == data.Username)
                            groupdic[e.GroupNumber].AddNewMessageRow(tox, data, true);
                        else
                            groupdic[e.GroupNumber].AddNewMessageRow(tox, data, false);
                    }
                    else
                    {
                        groupdic[e.GroupNumber].AddNewMessageRow(tox, data, false);
                    }
                }
                else
                {
                    FlowDocument document = FlowDocumentExtensions.CreateNewDocument();
                    groupdic.Add(e.GroupNumber, document);
                    groupdic[e.GroupNumber].AddNewMessageRow(tox, data, false);
                }

                MessageAlertIncrement(group);

                if (group.Selected)
                    ScrollChatBox();

                if (ViewModel.MainToxyUser.ToxStatus != ToxStatus.Busy)
                    this.Flash();

                nIcon.Icon = newMessageNotifyIcon;
                ViewModel.HasNewMessage = true;
            })));
        }

        private async void tox_OnGroupInvite(object sender, ToxEventArgs.GroupInviteEventArgs e)
        {
            int number;

            if (e.GroupType == ToxGroupType.Text)
            {
                number = tox.JoinGroup(e.FriendNumber, e.Data);
            }
            else if (e.GroupType == ToxGroupType.Av)
            {
                if (call != null)
                {
                    await Dispatcher.BeginInvoke(((Action)(() =>
                    {
                        this.ShowMessageAsync("Error", "Could not join audio groupchat, there's already a call in progress.");
                    })));
                    return;
                }
                else
                {
                    number = toxav.JoinAvGroupchat(e.FriendNumber, e.Data);
                    call = new ToxGroupCall(toxav, number);
                    call.FilterAudio = config.FilterAudio;
                    call.Start(config.InputDevice, config.OutputDevice, ToxAv.DefaultCodecSettings);
                }
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

        /*private void tox_OnAvatarInfo(object sender, ToxEventArgs.AvatarInfoEventArgs e)
        {
            Debug.WriteLine(string.Format("Received avatar info from {0}", e.FriendNumber));

            var friend = Dispatcher.Invoke(() => ViewModel.GetFriendObjectByNumber(e.FriendNumber));
            if (friend == null)
                return;

            if (e.Format == ToxAvatarFormat.None)
            {
                Debug.WriteLine(string.Format("Received ToxAvatarFormat.None ({0})", e.FriendNumber));
                avatarStore.Delete(tox.GetClientId(e.FriendNumber));

                Dispatcher.BeginInvoke(((Action)(() =>
                {
                    friend.AvatarBytes = null;
                    friend.Avatar = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/profilepicture.png"));
                })));
            }
            else
            {
                Debug.WriteLine(string.Format("Received ToxAvatarFormat.Png ({0})", e.FriendNumber));

                if (friend.AvatarBytes == null || friend.AvatarBytes.Length == 0)
                {
                    if (!avatarStore.Contains(tox.GetClientId(e.FriendNumber)))
                    {
                        Debug.WriteLine(string.Format("Avatar ({0}) does not exist on disk, requesting data", e.FriendNumber));

                        if (!tox.RequestAvatarData(e.FriendNumber))
                            Debug.WriteLine(string.Format("Could not request avatar data from {0}: {1}", e.FriendNumber, getFriendName(e.FriendNumber)));
                    }
                }
                else
                {
                    Debug.WriteLine(string.Format("Comparing given hash to the avatar we already have ({0})", e.FriendNumber));

                    if (tox.GetHash(friend.AvatarBytes).SequenceEqual(e.Hash))
                    {
                        Debug.WriteLine(string.Format("We already have this avatar, ignore ({0})", e.FriendNumber));
                        return;
                    }
                    else
                    {
                        Debug.WriteLine(string.Format("Hashes don't match, requesting avatar data... ({0})", e.FriendNumber));

                        if (!tox.RequestAvatarData(e.FriendNumber))
                            Debug.WriteLine(string.Format("Could not request avatar date from {0}: {1}", e.FriendNumber, getFriendName(e.FriendNumber)));
                    }
                }
            }
        }

        private void tox_OnAvatarData(object sender, ToxEventArgs.AvatarDataEventArgs e)
        {
            Debug.WriteLine(string.Format("Received avatar data from {0}", e.FriendNumber));

            Dispatcher.BeginInvoke(((Action)(() =>
            {
                var friend = ViewModel.GetFriendObjectByNumber(e.FriendNumber);
                if (friend == null)
                    return;

                if (friend.AvatarBytes != null && tox.GetHash(friend.AvatarBytes).SequenceEqual(e.Avatar.Hash))
                {
                    Debug.WriteLine("Received avatar data unexpectedly, ignoring");
                }
                else
                {
                    try
                    {
                        friend.AvatarBytes = e.Avatar.Data;

                        Debug.WriteLine(string.Format("Starting task to apply the new avatar ({0})", e.FriendNumber));
                        applyAvatar(friend, e.Avatar.Data);
                    }
                    catch
                    {
                        Debug.WriteLine(string.Format("Received invalid avatar data ({0})", e.FriendNumber));
                    }
                }
            })));
        }*/

        private void tox_OnConnectionStatusChanged(object sender, ToxEventArgs.ConnectionStatusEventArgs e)
        {
            if (e.Status == ToxConnectionStatus.None)
            {
                SetStatus(ToxStatus.Invalid, false);
                WaitAndBootstrap(2000);
            }
            else
            {
                SetStatus((ToxStatus?)tox.Status, false);
            }
        }

        private void tox_OnReadReceiptReceived(object sender, ToxEventArgs.ReadReceiptEventArgs e)
        {
            //a flowdocument should already be created, but hey, just in case
            if (!convdic.ContainsKey(e.FriendNumber))
                return;

            Dispatcher.BeginInvoke(((Action)(() =>
            {
                Paragraph para = (Paragraph)convdic[e.FriendNumber].FindChildren<TableRow>().Where(r => !(r.Tag is FileTransfer) && ((MessageData)(r.Tag)).Id == e.Receipt).First().FindChildren<TableCell>().ToArray()[1].Blocks.FirstBlock;

                if (para == null)
                    return; //row or cell doesn't exist? odd, just return

                if (config.Theme == "BaseDark")
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

                        FileSender ft = (FileSender)transfer;
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
                        transfers.Remove(transfer);
                        break;
                    }
                case ToxFileControl.Pause:
                    {
                        var transfer = GetFileTransfer(e.FriendNumber, e.FileNumber) as FileTransfer;
                        if (transfer == null)
                            break;

                        transfer.Paused = true;
                        break;
                    }
            }

            Debug.WriteLine(string.Format("Received file control: {0} from {1}", e.Control, getFriendName(e.FriendNumber)));
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
                if (transfers.Contains(transfer))
                    transfers.Remove(transfer);

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
                            applyAvatar(friend);
                        }
                        catch
                        {
                            Debug.WriteLine(string.Format("Received invalid avatar data ({0})", e.FriendNumber));
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

            bool success = transfer.SendNextChunk(e.Position, e.Length);

            if (e.Position + e.Length >= transfer.FileSize && success)
                transfer.Kill(true);
        }

        private void tox_OnFileSendRequestReceived(object sender, ToxEventArgs.FileSendRequestEventArgs e)
        {
            if (e.FileKind == ToxFileKind.Data)
            {
                if (!convdic.ContainsKey(e.FriendNumber))
                    convdic.Add(e.FriendNumber, FlowDocumentExtensions.CreateNewDocument());

                Dispatcher.BeginInvoke(((Action)(() =>
                {
                    var transfer = new FileReceiver(tox, e.FileNumber, e.FriendNumber, e.FileKind, (long)e.FileSize, e.FileName, e.FileName);
                    var control = convdic[e.FriendNumber].AddNewFileTransfer(tox, transfer);
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
                        SaveFileDialog dialog = new SaveFileDialog();
                        dialog.FileName = e.FileName;

                        if (dialog.ShowDialog() == true)
                        {
                            ft.Path = dialog.FileName;
                            control.FilePath = dialog.FileName;
                            tox.FileControl(ft.FriendNumber, ft.FileNumber, ToxFileControl.Resume);
                        }

                        transfer.Tag.StartTransfer();
                    };

                    control.OnDecline += delegate(FileTransfer ft)
                    {
                        ft.Kill(false);

                        if (transfers.Contains(ft))
                            transfers.Remove(ft);
                    };

                    control.OnPause += delegate(FileTransfer ft)
                    {
                        if (ft.Paused)
                            tox.FileControl(ft.FriendNumber, ft.FileNumber, ToxFileControl.Pause);
                        else
                            tox.FileControl(ft.FriendNumber, ft.FileNumber, ToxFileControl.Resume);
                    };

                    control.OnFileOpen += delegate(FileTransfer ft)
                    {
                        try { Process.Start(ft.Path); }
                        catch { }
                    };

                    control.OnFolderOpen += delegate(FileTransfer ft)
                    {
                        Process.Start("explorer.exe", @"/select, " + ft.Path);
                    };

                    transfers.Add(transfer);

                    if (ViewModel.MainToxyUser.ToxStatus != ToxStatus.Busy)
                        this.Flash();
                })));
            }
            else if (e.FileKind == ToxFileKind.Avatar)
            {
                if (e.FileSize != 0)
                {
                    byte[] hash = tox.FileGetId(e.FriendNumber, e.FileNumber);
                    if (hash == null || hash.Length != ToxConstants.HashLength)
                        return;

                    Dispatcher.BeginInvoke(((Action)(() =>
                    {
                        var friend = ViewModel.GetFriendObjectByNumber(e.FriendNumber);
                        if (friend == null)
                            return;

                        if (friend.AvatarBytes == null || friend.AvatarBytes.Length == 0)
                        {
                            if (!avatarStore.Contains(tox.GetFriendPublicKey(e.FriendNumber)))
                            {
                                tox.FileControl(e.FriendNumber, e.FileNumber, ToxFileControl.Resume);
                                string filename = tox.GetFriendPublicKey(e.FriendNumber).GetString();
                                var transfer = new FileReceiver(tox, e.FileNumber, e.FriendNumber, e.FileKind, (long)e.FileSize, filename, Path.Combine(avatarStore.Dir, filename + ".png"));
                                transfers.Add(transfer);
                            }
                        }
                        else
                        {
                            if (ToxTools.Hash(friend.AvatarBytes).SequenceEqual(hash))
                                return;
                            else
                            {
                                tox.FileControl(e.FriendNumber, e.FileNumber, ToxFileControl.Resume);
                                string filename = tox.GetFriendPublicKey(e.FriendNumber).GetString();
                                var transfer = new FileReceiver(tox, e.FileNumber, e.FriendNumber, e.FileKind, (long)e.FileSize, filename, Path.Combine(avatarStore.Dir, filename + ".png"));
                                transfers.Add(transfer);
                            }
                        }
                    })));
                }
                else //friend doesn't have an avatar
                {
                    avatarStore.Delete(tox.GetFriendPublicKey(e.FriendNumber));

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
                    DateTime lastOnline = TimeZoneInfo.ConvertTime(tox.GetFriendLastOnline(e.FriendNumber), TimeZoneInfo.Utc, TimeZoneInfo.Local);

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

                var receivers = transfers.Where(t => t.GetType() == typeof(FileReceiver) && t.FriendNumber == e.FriendNumber && !t.Finished);
                if (receivers.Count() > 0)
                {
                    foreach (var transfer in receivers)
                        transfer.Broken = true;
                }

                var senders = transfers.Where(t => t.GetType() == typeof(FileSender) && t.FriendNumber == e.FriendNumber && !t.Finished);
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
                    friend.StatusMessage = getFriendStatusMessage(friend.ChatNumber);

                    if (friend.Selected)
                    {
                        CallButton.Visibility = Visibility.Visible;
                        FileButton.Visibility = Visibility.Visible;
                    }

                    if (ViewModel.MainToxyUser.AvatarBytes != null)
                    {
                        string avatarsDir = Path.Combine(toxDataDir, "avatars");
                        string selfAvatarFile = Path.Combine(avatarsDir, tox.Id.PublicKey.GetString() + ".png");

                        var fileInfo = tox.FileSend(e.FriendNumber, ToxFileKind.Avatar, ViewModel.MainToxyUser.AvatarBytes.Length, "avatar.png", ToxTools.Hash(ViewModel.MainToxyUser.AvatarBytes));
                        var transfer = new FileSender(tox, fileInfo.Number, friend.ChatNumber, ToxFileKind.Avatar, ViewModel.MainToxyUser.AvatarBytes.Length, "", selfAvatarFile);

                        transfers.Add(transfer);
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
                        TypingStatusLabel.Content = getFriendName(e.FriendNumber) + " is typing...";
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
                    friend.StatusMessage = getFriendStatusMessage(e.FriendNumber);
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

                nIcon.Icon = newMessageNotifyIcon;
                ViewModel.HasNewMessage = true;
            })));
        }

        private void tox_OnFriendMessageReceived(object sender, ToxEventArgs.FriendMessageEventArgs e)
        {
            MessageData data;
            if (e.MessageType == ToxMessageType.Message)
                data = new MessageData(id: 0, username: getFriendName(e.FriendNumber), message: e.Message, isAction: false, timestamp: DateTime.Now, isGroupMsg: false, isSelf: false);
            else
                data = new MessageData(id: 0, username: "*  ", message: string.Format("{0} {1}", getFriendName(e.FriendNumber), e.Message), isAction: true, timestamp: DateTime.Now, isGroupMsg: false, isSelf: false);

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

                nIcon.Icon = newMessageNotifyIcon;
                ViewModel.HasNewMessage = true;
            })));

            if (config.EnableChatLogging)
                dbConnection.InsertAsync(new ToxMessage() { PublicKey = tox.GetFriendPublicKey(e.FriendNumber).GetString(), Message = data.Message, Timestamp = DateTime.Now, IsAction = false, Name = data.Username, ProfilePublicKey = tox.Id.PublicKey.GetString() });
        }

        private void tox_OnFriendNameChanged(object sender, ToxEventArgs.NameChangeEventArgs e)
        {
            Dispatcher.BeginInvoke(((Action)(() =>
            {
                var friend = ViewModel.GetFriendObjectByNumber(e.FriendNumber);
                if (friend != null)
                {
                    friend.Name = getFriendName(e.FriendNumber);
                }
            })));
        }

        #endregion

        #region ToxAv EventHandlers

        private void toxav_OnReceivedVideo(object sender, ToxAvEventArgs.VideoDataEventArgs e)
        {
            if (Dispatcher.Invoke(() => (call == null || call.GetType() == typeof(ToxGroupCall) || call.Ended || ViewModel.IsGroupSelected || call.FriendNumber != ViewModel.SelectedChatNumber)))
                return;

            ProcessVideoFrame(e.Frame);
        }

        private void toxav_OnPeerCodecSettingsChanged(object sender, ToxAvEventArgs.CallStateEventArgs e)
        {
            Dispatcher.BeginInvoke(((Action)(() =>
            {
                if (call == null || call.GetType() == typeof(ToxGroupCall) || e.CallIndex != call.CallIndex)
                    return;

                if (toxav.GetPeerCodecSettings(e.CallIndex, 0).CallType != ToxAvCallType.Video)
                {
                    VideoImageRow.Height = new GridLength(0);
                    VideoGridSplitter.IsEnabled = false;
                    VideoChatImage.Source = null;
                }
                else if (ViewModel.IsFriendSelected && toxav.GetPeerID(e.CallIndex, 0) == ViewModel.SelectedChatNumber)
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

            var peer = group.PeerList.GetPeerByPublicKey(tox.GetGroupPeerPublicKey(e.GroupNumber, e.PeerNumber));
            if (peer == null || peer.Ignored || peer.Muted)
                return;

            if (call != null && call.GetType() == typeof(ToxGroupCall))
                ((ToxGroupCall)call).ProcessAudioFrame(e.Data, e.Channels);
        }

        private void toxav_OnReceivedAudio(object sender, ToxAvEventArgs.AudioDataEventArgs e)
        {
            if (call == null)
                return;

            call.ProcessAudioFrame(e.Data);
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
            var settings = toxav.GetPeerCodecSettings(e.CallIndex, 0);

            if (call != null)
                call.Start(config.InputDevice, config.OutputDevice, settings, config.VideoDevice);

            Dispatcher.BeginInvoke(((Action)(() =>
            {
                if (settings.CallType == ToxAvCallType.Video)
                {
                    VideoImageRow.Height = new GridLength(300);
                    VideoGridSplitter.IsEnabled = true;
                }

                int friendnumber = toxav.GetPeerID(e.CallIndex, 0);
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

            call.SetTimerCallback(timerCallback);
        }

        private void timerCallback(object state)
        {
            if (call == null)
                return;

            call.TotalSeconds++;
            var timeSpan = TimeSpan.FromSeconds(call.TotalSeconds);

            Dispatcher.BeginInvoke(((Action)(() => CurrentCallControl.TimerLabel.Content = string.Format("{0}:{1}:{2}", timeSpan.Hours.ToString("00"), timeSpan.Minutes.ToString("00"), timeSpan.Seconds.ToString("00")))));
        }

        private void toxav_OnInvite(object sender, ToxAvEventArgs.CallStateEventArgs e)
        {
            //TODO: notify the user of another incoming call
            if (call != null)
                return;

            Dispatcher.BeginInvoke(((Action)(() =>
            {
                var friend = ViewModel.GetFriendObjectByNumber(toxav.GetPeerID(e.CallIndex, 0));
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

            for (int i = 0; i < tox.GetGroupMemberCount(group.ChatNumber); i++)
            {
                var publicKey = tox.GetGroupPeerPublicKey(group.ChatNumber, i);
                var oldPeer = group.PeerList.GetPeerByPublicKey(publicKey);
                GroupPeer newPeer;

                if (oldPeer != null)
                    newPeer = oldPeer;
                else
                    newPeer = new GroupPeer(group.ChatNumber, publicKey) { Name = tox.GetGroupMemberName(group.ChatNumber, i) };

                peers.Add(newPeer);
            }

            group.PeerList = new GroupPeerCollection(peers.OrderBy(p => p.Name).ToList());
        }

        private void RearrangeChatList()
        {
            ViewModel.UpdateChatCollection(new ObservableCollection<IChatObject>(ViewModel.ChatCollection.OrderBy(chat => chat.GetType() == typeof(GroupControlModelView) ? 3 : getStatusPriority(tox.GetFriendConnectionStatus(chat.ChatNumber), (ToxStatus)tox.GetFriendStatus(chat.ChatNumber))).ThenBy(chat => chat.Name)));
        }

        private int getStatusPriority(ToxConnectionStatus connStatus, ToxStatus status)
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
            if (convdic.ContainsKey(friendNumber))
            {
                var run = convdic[friendNumber].GetLastMessageRun();

                if (run != null && run.Tag.GetType() == typeof(MessageData))
                {
                    if (((MessageData)run.Tag).Username == data.Username)
                        convdic[friendNumber].AddNewMessageRow(tox, data, true);
                    else
                        convdic[friendNumber].AddNewMessageRow(tox, data, false);
                }
                else
                {
                    convdic[friendNumber].AddNewMessageRow(tox, data, false);
                }
            }
            else
            {
                FlowDocument document = FlowDocumentExtensions.CreateNewDocument();
                convdic.Add(friendNumber, document);
                convdic[friendNumber].AddNewMessageRow(tox, data, false);
            }
        }

        private void AddActionToView(int friendNumber, MessageData data)
        {
            if (convdic.ContainsKey(friendNumber))
            {
                convdic[friendNumber].AddNewMessageRow(tox, data, false);
            }
            else
            {
                FlowDocument document = FlowDocumentExtensions.CreateNewDocument();
                convdic.Add(friendNumber, document);
                convdic[friendNumber].AddNewMessageRow(tox, data, false);
            }
        }

        private async void initDatabase()
        {
            dbConnection = new SQLiteAsyncConnection(dbFilename);
            await dbConnection.CreateTableAsync<ToxMessage>().ContinueWith((r) => { Console.WriteLine("Created ToxMessage table"); });

            if (config.EnableChatLogging)
            {
                await dbConnection.Table<ToxMessage>().ToListAsync().ContinueWith((task) =>
                {
                    foreach (ToxMessage msg in task.Result)
                    {
                        if (string.IsNullOrEmpty(msg.ProfilePublicKey) || msg.ProfilePublicKey != tox.Id.PublicKey.GetString())
                            continue;

                        int friendNumber = GetFriendByPublicKey(msg.PublicKey);
                        if (friendNumber == -1)
                            continue;

                        Dispatcher.BeginInvoke(((Action)(() =>
                        {
                            var messageData = new MessageData(id: 0, username: msg.Name, message: msg.Message, isAction: msg.IsAction, isSelf: msg.IsSelf, timestamp: msg.Timestamp, isGroupMsg: false);

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
            var friends = tox.Friends.Where(num => tox.GetFriendPublicKey(num).ToString() == publicKey);
            if (friends.Count() != 1)
                return -1;
            else
                return friends.First();
        }

        private void loadAvatars()
        {
            try
            {
                byte[] bytes;
                var avatar = avatarStore.Load(tox.Id.PublicKey, out bytes);
                if (avatar != null && bytes != null && bytes.Length > 0)
                {
                    //tox.SetAvatar(ToxAvatarFormat.Png, bytes);
                    ViewModel.MainToxyUser.Avatar = avatar;
                }
            }
            catch { Debug.WriteLine("Could not load our own avatar, using default"); }

            foreach (int friend in tox.Friends)
            {
                var obj = ViewModel.GetFriendObjectByNumber(friend);
                if (obj == null)
                    continue;

                try
                {
                    byte[] bytes;
                    var avatar = avatarStore.Load(tox.GetFriendPublicKey(friend), out bytes);

                    if (avatar != null && bytes != null && bytes.Length > 0)
                    {
                        obj.AvatarBytes = bytes;
                        obj.Avatar = avatar;
                    }
                }
                catch { Debug.WriteLine("Could not load avatar of friend " + friend); }
            }
        }

        private Task<bool> applyAvatar(IFriendObject friend)
        {
            return Task.Run(() =>
            {
                byte[] data;
                var img = avatarStore.Load(tox.GetFriendPublicKey(friend.ChatNumber), out data);
                Dispatcher.BeginInvoke(((Action)(() => friend.Avatar = img)));

                if (!avatarStore.Save(data, tox.GetFriendPublicKey(friend.ChatNumber)))
                    return false;

                return true;
            });
        }

        private async void Chat_Drop(object sender, DragEventArgs e)
        {
            if (ViewModel.IsGroupSelected)
                return;

            if (e.Data.GetDataPresent(DataFormats.FileDrop) && tox.IsFriendOnline(ViewModel.SelectedChatNumber))
            {
                var docPath = (string[])e.Data.GetData(DataFormats.FileDrop);
                MetroDialogOptions.ColorScheme = MetroDialogColorScheme.Theme;

                var mySettings = new MetroDialogSettings()
                {
                    AffirmativeButtonText = "Yes",
                    FirstAuxiliaryButtonText = "Cancel",
                    AnimateShow = false,
                    AnimateHide = false,
                    ColorScheme = MetroDialogColorScheme.Theme
                };

                MessageDialogResult result = await this.ShowMessageAsync("Please confirm", "Are you sure you want to send this file?",
                MessageDialogStyle.AffirmativeAndNegative, mySettings);

                if (result == MessageDialogResult.Affirmative)
                {
                    SendFile(ViewModel.SelectedChatNumber, docPath[0]);
                }
            }
        }

        private void Chat_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && !ViewModel.IsGroupSelected && tox.IsFriendOnline(ViewModel.SelectedChatNumber))
            {
                e.Effects = DragDropEffects.All;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private async Task<ToxData> loadTox()
        {
            if (!Directory.Exists(toxDataDir))
                Directory.CreateDirectory(toxDataDir);

            string[] fileNames = Directory.GetFiles(toxDataDir, "*.tox", SearchOption.TopDirectoryOnly).Where(s => s.EndsWith(".tox")).ToArray();
            if (fileNames.Length > 0)
            {
                if (!fileNames.Contains(toxDataFilename))
                {
                    SwitchProfileButton_Click(this, new RoutedEventArgs());
                }
                else
                {
                    return ToxData.FromDisk(toxDataFilename);
                }
            }
            else if (File.Exists(toxOldDataFilename))
            {
                string profileName = await this.ShowInputAsync("Old data file", "Toxy has detected an old data file. Please enter a name for your profile");
                if (!string.IsNullOrEmpty(profileName))
                    config.ProfileName = profileName;
                else
                    config.ProfileName = tox.Id.PublicKey.GetString().Substring(0, 10);

                File.Move(toxOldDataFilename, toxDataFilename);
                ConfigTools.Save(config, configFilename);

                return ToxData.FromDisk(toxDataFilename);
            }
            else
            {
                string profileName = await this.ShowInputAsync("Welcome to Toxy!", "To get started, enter a name for your first profile.");
                if (!string.IsNullOrEmpty(profileName))
                    config.ProfileName = profileName;
                else
                    config.ProfileName = tox.Id.PublicKey.GetString().Substring(0, 10);

                tox.Name = config.ProfileName;
                tox.GetData().Save(toxDataFilename);
                ConfigTools.Save(config, configFilename);
            }

            return null;
        }

        private void applyConfig()
        {
            var accent = ThemeManager.GetAccent(config.AccentColor);
            var theme = ThemeManager.GetAppTheme(config.Theme);

            if (accent != null && theme != null)
                ThemeManager.ChangeAppStyle(Application.Current, accent, theme);

            Width = config.WindowSize.Width;
            Height = config.WindowSize.Height;

	        ViewModel.SpellcheckEnabled = config.EnableSpellcheck;
	        ViewModel.SpellcheckLangCode = config.SpellcheckLanguage.ToDescription();

            ExecuteActionsOnNotifyIcon();
        }

        private void InitializeNotifyIcon()
        {
            Stream newMessageIconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Toxy;component/Resources/Icons/icon2.ico")).Stream;
            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Toxy;component/Resources/Icons/icon.ico")).Stream;

            notifyIcon = new Icon(iconStream);
            newMessageNotifyIcon = new Icon(newMessageIconStream);

            nIcon.Icon = notifyIcon;
            nIcon.MouseClick += nIcon_MouseClick;

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
            nIcon.ContextMenu = trayIconContextMenu;
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
            if (tox.IsConnected)
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
            forceClose = true;
            Close();
        }

        public MainWindowViewModel ViewModel
        {
            get { return DataContext as MainWindowViewModel; }
        }

        private FileTransfer GetFileTransfer(int friendnumber, int filenumber)
        {
            foreach (FileTransfer ft in transfers)
                if (ft.FileNumber == filenumber && ft.FriendNumber == friendnumber && !ft.Finished)
                    return ft;

            return null;
        }

        private void ScrollChatBox()
        {
            ScrollViewer viewer = ChatBox.FindScrollViewer();

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
            foreach (var friendNumber in tox.Friends)
            {
                AddFriendToView(friendNumber, false);
            }
        }

        private void AddGroupToView(int groupnumber, ToxGroupType type)
        {
            string groupname = string.Format("Groupchat #{0}", groupnumber);

            if (type == ToxGroupType.Av)
                groupname += " \uD83D\uDD0A"; /*:loud_sound:*/

            var groupMV = new GroupControlModelView();
            groupMV.ChatNumber = groupnumber;
            groupMV.Name = groupname;
            groupMV.GroupType = type;
            groupMV.StatusMessage = string.Format("Peers online: {0}", tox.GetGroupMemberCount(groupnumber));//string.Join(", ", tox.GetGroupNames(groupnumber));
            groupMV.SelectedAction = GroupSelectedAction;
            groupMV.DeleteAction = GroupDeleteAction;
            groupMV.ChangeTitleAction = ChangeTitleAction;

            ViewModel.ChatCollection.Add(groupMV);
            RearrangeChatList();
        }

        private async void ChangeTitleAction(IGroupObject groupObject)
        {
            string title = await this.ShowInputAsync("Change group title", "Enter a new title for this group.", new MetroDialogSettings() { DefaultText = tox.GetGroupTitle(groupObject.ChatNumber) });
            if (string.IsNullOrEmpty(title))
                return;

            if (tox.SetGroupTitle(groupObject.ChatNumber, title))
            {
                groupObject.Name = title;
                groupObject.AdditionalInfo = string.Format("Topic set by: {0}", tox.Name);
            }
        }

        private void GroupDeleteAction(IGroupObject groupObject)
        {
            ViewModel.ChatCollection.Remove(groupObject);
            int groupNumber = groupObject.ChatNumber;
            if (groupdic.ContainsKey(groupNumber))
            {
                groupdic.Remove(groupNumber);

                if (groupObject.Selected)
                    ChatBox.Document = null;
            }

            if (tox.GetGroupType(groupNumber) == ToxGroupType.Av && call != null)
            {
                call.Stop();
                call = null;
            }

            tox.DeleteGroupChat(groupNumber);

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

        private string getFriendName(int friendnumber)
        {
            return tox.GetFriendName(friendnumber).Replace("\n", "").Replace("\r", "");
        }

        private string getSelfStatusMessage()
        {
            return tox.StatusMessage.Replace("\n", "").Replace("\r", "");
        }

        private string getSelfName()
        {
            return tox.Name.Replace("\n", "").Replace("\r", "");
        }

        private string getFriendStatusMessage(int friendnumber)
        {
            return tox.GetFriendStatusMessage(friendnumber).Replace("\n", "").Replace("\r", "");
        }

        private void AddFriendToView(int friendNumber, bool sentRequest)
        {
            var friendStatus = "";
            var lastSeenInfo = (string) FindResource("Local_LastSeen");

            if (tox.IsFriendOnline(friendNumber))
            {
                friendStatus = getFriendStatusMessage(friendNumber);
            }
            else
            {
                var lastOnline = TimeZoneInfo.ConvertTime(tox.GetFriendLastOnline(friendNumber), TimeZoneInfo.Utc, TimeZoneInfo.Local);

                if (lastOnline.Year == 1970)
                {
                    if (sentRequest)
                        friendStatus = "Friend request sent";
                }
                else
                    friendStatus = string.Format("{0}: {1} {2}", lastSeenInfo, lastOnline.ToShortDateString(), lastOnline.ToLongTimeString());
            }
           

            var friendName = getFriendName(friendNumber);
            if (string.IsNullOrEmpty(friendName))
            {
                friendName = tox.GetFriendPublicKey(friendNumber).GetString();
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
            tox.InviteFriend(friendObject.ChatNumber, groupObject.ChatNumber);
        }

        private async void FriendDeleteAction(IFriendObject friendObject)
        {
            var result = await this.ShowMessageAsync("Remove friend", string.Format("Are you sure you want to remove {0} from your friend list?", friendObject.Name), MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
            if (result != MessageDialogResult.Affirmative)
                return;

            ViewModel.ChatCollection.Remove(friendObject);
            var friendNumber = friendObject.ChatNumber;
            if (convdic.ContainsKey(friendNumber))
            {
                convdic.Remove(friendNumber);
                if (friendObject.Selected)
                {
                    ChatBox.Document = null;
                }
            }
            tox.DeleteFriend(friendNumber);
            friendObject.SelectedAction = null;
            friendObject.DenyCallAction = null;
            friendObject.AcceptCallAction = null;
            friendObject.CopyIDAction = null;
            friendObject.DeleteAction = null;
            friendObject.GroupInviteAction = null;
            friendObject.MainViewModel = null;

            saveTox();
        }

        private void saveTox()
        {
            if (!config.Portable)
            {
                if (!Directory.Exists(toxDataDir))
                    Directory.CreateDirectory(toxDataDir);
            }

            ToxData data = tox.GetData();
            if (data != null)
                data.Save(toxDataFilename);
        }

        private void FriendCopyIdAction(IFriendObject friendObject)
        {
            Clipboard.Clear();
            Clipboard.SetText(tox.GetFriendPublicKey(friendObject.ChatNumber).GetString());
        }

        private void FriendSelectedAction(IFriendObject friendObject, bool isSelected)
        {
            MessageAlertClear(friendObject);

            if (isSelected)
            {
                if (!tox.GetFriendTypingStatus(friendObject.ChatNumber) || tox.GetFriendStatus(friendObject.ChatNumber) == ToxUserStatus.None)
                    TypingStatusLabel.Content = "";
                else
                    TypingStatusLabel.Content = getFriendName(friendObject.ChatNumber) + " is typing...";

                SelectFriendControl(friendObject);
                ScrollChatBox();
                TextToSend.Focus();
            }
        }

        private void FriendAcceptCallAction(IFriendObject friendObject)
        {
            if (call != null)
                return;

            call = new ToxCall(toxav, friendObject.CallIndex, friendObject.ChatNumber);
            call.FilterAudio = config.FilterAudio;
            call.Answer();
        }

        private void FriendDenyCallAction(IFriendObject friendObject)
        {
            if (call == null)
            {
                toxav.Reject(friendObject.CallIndex, "I'm busy...");
                friendObject.IsCalling = false;
            }
            else
            {
                call.Stop();
                call = null;
            }
        }

        private void AddFriendRequestToView(string id, string message)
        {
            var friendMV = new FriendControlModelView(ViewModel);
            friendMV.IsRequest = true;
            friendMV.Name = id;
            friendMV.ToxStatus = ToxStatus.Invalid;
            friendMV.RequestMessageData = new MessageData(id: 0, username: "Request Message", message: message, isAction: false, timestamp: DateTime.Now, isGroupMsg: false, isSelf: false);
            friendMV.RequestFlowDocument = FlowDocumentExtensions.CreateNewDocument();
            friendMV.SelectedAction = FriendRequestSelectedAction;
            friendMV.AcceptAction = FriendRequestAcceptAction;
            friendMV.DeclineAction = FriendRequestDeclineAction;

            ViewModel.ChatRequestCollection.Add(friendMV);

            if (ListViewTabControl.SelectedIndex != 1)
            {
                RequestsTabItem.Header = "Requests*";
            }
        }

        private void FriendRequestSelectedAction(IFriendObject friendObject, bool isSelected)
        {
            friendObject.RequestFlowDocument.AddNewMessageRow(tox, friendObject.RequestMessageData, false);
        }

        private void FriendRequestAcceptAction(IFriendObject friendObject)
        {
            int friendnumber = tox.AddFriendNoRequest(new ToxKey(ToxKeyType.Public, friendObject.Name));

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

            saveTox();
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

            if (tox.GetGroupType(group.ChatNumber) == ToxGroupType.Av)
                MicButton.Visibility = Visibility.Visible;
            else
                MicButton.Visibility = Visibility.Collapsed;

            if (groupdic.ContainsKey(group.ChatNumber))
            {
                ChatBox.Document = groupdic[group.ChatNumber];
            }
            else
            {
                FlowDocument document = FlowDocumentExtensions.CreateNewDocument();
                groupdic.Add(group.ChatNumber, document);
                ChatBox.Document = groupdic[group.ChatNumber];
            }

            VideoImageRow.Height = new GridLength(0);
            VideoGridSplitter.IsEnabled = false;
            VideoChatImage.Source = null;

            GroupListGrid.Visibility = Visibility.Visible;
            PeerColumn.Width = new GridLength(150);
        }

        private void EndCall()
        {
            if (call != null)
            {
                var friendnumber = toxav.GetPeerID(call.CallIndex, 0);
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
                toxav.Cancel(friend.CallIndex, friend.ChatNumber, "I'm busy...");

                friend.IsCalling = false;
                friend.IsCallingToFriend = false;
            }

            if (call != null)
            {
                call.Stop();
                call = null;

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
            int friendNumber = friend.ChatNumber;

            if (call != null && call.GetType() != typeof(ToxGroupCall))
            {
                if (call.FriendNumber != friendNumber)
                {
                    HangupButton.Visibility = Visibility.Collapsed;
                    VideoButton.Visibility = Visibility.Collapsed;

                    if (tox.IsFriendOnline(friendNumber))
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

                    if (toxav.GetPeerCodecSettings(call.CallIndex, 0).CallType == ToxAvCallType.Video)
                    {
                        VideoImageRow.Height = new GridLength(300);
                        VideoGridSplitter.IsEnabled = true;
                    }
                }
            }
            else
            {
                if (!tox.IsFriendOnline(friendNumber))
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

            if (convdic.ContainsKey(friend.ChatNumber))
            {
                ChatBox.Document = convdic[friend.ChatNumber];
            }
            else
            {
                FlowDocument document = FlowDocumentExtensions.CreateNewDocument();
                convdic.Add(friend.ChatNumber, document);
                ChatBox.Document = convdic[friend.ChatNumber];
            }

            GroupListGrid.Visibility = Visibility.Collapsed;
            PeerColumn.Width = GridLength.Auto;
        }

        private void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            if (config.HideInTray && !forceClose)
            {
                e.Cancel = true;
                ShowInTaskbar = false;
                WindowState = WindowState.Minimized;
                Hide();
            }
            else
            {
                KillTox(true);
                nIcon.Dispose();
            }
        }

        private void KillTox(bool save)
        {
            if (call != null)
            {
                call.Stop();
                call = null;
            }

            foreach (FileTransfer transfer in transfers)
                transfer.Kill(false);

            convdic.Clear();
            groupdic.Clear();
            transfers.Clear();

            if (toxav != null)
                toxav.Dispose();

            if (tox != null)
            {
                if (save)
                    saveTox();

                tox.Dispose();
            }

            if (config != null)
            {
                config.WindowSize = new Size(this.Width, this.Height);
                ConfigTools.Save(config, configFilename);
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
                SettingsUsername.Text = getSelfName();
                SettingsStatus.Text = getSelfStatusMessage();
                SettingsNospam.Text = tox.GetNospam().ToString();

                var style = ThemeManager.DetectAppStyle(Application.Current);
                var accent = ThemeManager.GetAccent(style.Item2.Name);
                oldAccent = accent;
                if (accent != null)
                    AccentComboBox.SelectedItem = AccentComboBox.Items.Cast<AccentColorMenuData>().Single(a => a.Name == style.Item2.Name);

                var theme = ThemeManager.GetAppTheme(style.Item1.Name);
                oldAppTheme = theme;
                if (theme != null)
                    AppThemeComboBox.SelectedItem = AppThemeComboBox.Items.Cast<AppThemeMenuData>().Single(a => a.Name == style.Item1.Name);

                ViewModel.UpdateDevices();

                foreach(var item in VideoDevicesComboBox.Items)
                {
                    var device = (VideoMenuData)item;
                    if (device.Name == config.VideoDevice)
                    {
                        VideoDevicesComboBox.SelectedItem = item;
                        break;
                    }
                }

                if (InputDevicesComboBox.Items.Count - 1 >= config.InputDevice)
                    InputDevicesComboBox.SelectedIndex = config.InputDevice;

                if (OutputDevicesComboBox.Items.Count - 1 >= config.OutputDevice)
                    OutputDevicesComboBox.SelectedIndex = config.OutputDevice;

                ChatLogCheckBox.IsChecked = config.EnableChatLogging;
                HideInTrayCheckBox.IsChecked = config.HideInTray;
                PortableCheckBox.IsChecked = config.Portable;
                AudioNotificationCheckBox.IsChecked = config.EnableAudioNotifications;
                AlwaysNotifyCheckBox.IsChecked = config.AlwaysNotify;
	            SpellcheckCheckBox.IsChecked = config.EnableSpellcheck;
				SpellcheckLanguageComboBox.SelectedItem = Enum.GetName(typeof(SpellcheckLanguage), config.SpellcheckLanguage);
                FilterAudioCheckbox.IsChecked = config.FilterAudio;

                if (!string.IsNullOrEmpty(config.ProxyAddress))
                    SettingsProxyAddress.Text = config.ProxyAddress;

                if (config.ProxyPort != 0)
                    SettingsProxyPort.Text = config.ProxyPort.ToString();

                foreach (ComboBoxItem item in ProxyTypeComboBox.Items)
                {
                    if ((ToxProxyType)int.Parse((string)item.Tag) == config.ProxyType)
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
            TextRange message = new TextRange(AddFriendMessage.Document.ContentStart, AddFriendMessage.Document.ContentEnd);

            if (string.IsNullOrWhiteSpace(AddFriendID.Text) || message.Text == null)
                return;

            string friendID = AddFriendID.Text.Trim();
            int tries = 0;

            if (friendID.Contains("@"))
            {
                if (config.ProxyType != ToxProxyType.None && config.RemindAboutProxy)
                {
                    MessageDialogResult result = await this.ShowMessageAsync("Warning", "You're about to submit a dns lookup query, the configured proxy will not be used for this.\nDo you wish to continue?", MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, new MetroDialogSettings() { AffirmativeButtonText = "Yes, don't remind me again", NegativeButtonText = "Yes", FirstAuxiliaryButtonText = "No" });
                    if (result == MessageDialogResult.Affirmative)
                    {
                        config.RemindAboutProxy = false;
                        ConfigTools.Save(config, configFilename);
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
                    string id = DnsTools.DiscoverToxID(friendID, config.NameServices, config.OnlyUseLocalNameServiceStore);

                    if (string.IsNullOrEmpty(id))
                        throw new Exception("The server returned an empty result");

                    AddFriendID.Text = id;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(string.Format("Could not resolve {0}: {1}", friendID, ex.ToString()));

                    if (tries < 3)
                        goto discover;

                    this.ShowMessageAsync("Could not find a tox id", ex.Message.ToString());
                }

                return;
            }

            try
            {
                var error = ToxErrorFriendAdd.Ok;
                int friendnumber = tox.AddFriend(new ToxId(friendID), message.Text, out error);

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

            saveTox();
            FriendFlyout.IsOpen = false;
        }

        private void SaveSettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            tox.Name = SettingsUsername.Text;
            tox.StatusMessage = SettingsStatus.Text;

            uint nospam;
            if (uint.TryParse(SettingsNospam.Text, out nospam))
                tox.SetNospam(nospam);

            ViewModel.MainToxyUser.Name = getSelfName();
            ViewModel.MainToxyUser.StatusMessage = getSelfStatusMessage();

            config.HideInTray = (bool)HideInTrayCheckBox.IsChecked;

            SettingsFlyout.IsOpen = false;

            if (AccentComboBox.SelectedItem != null)
            {
                string accentName = ((AccentColorMenuData)AccentComboBox.SelectedItem).Name;
                var theme = ThemeManager.DetectAppStyle(Application.Current);
                var accent = ThemeManager.GetAccent(accentName);
                ThemeManager.ChangeAppStyle(Application.Current, accent, theme.Item1);

                config.AccentColor = accentName;
            }

            if (AppThemeComboBox.SelectedItem != null)
            {
                string themeName = ((AppThemeMenuData)AppThemeComboBox.SelectedItem).Name;
                var theme = ThemeManager.DetectAppStyle(Application.Current);
                var appTheme = ThemeManager.GetAppTheme(themeName);
                ThemeManager.ChangeAppStyle(Application.Current, theme.Item2, appTheme);

                config.Theme = themeName;
            }

            int index = InputDevicesComboBox.SelectedIndex;
            if (WaveIn.DeviceCount > 0 && WaveIn.DeviceCount >= index)
            {
                if (config.InputDevice != index)
                    if (call != null)
                        call.SwitchInputDevice(index);

                config.InputDevice = index;
            }

            index = OutputDevicesComboBox.SelectedIndex;
            if (WaveOut.DeviceCount > 0 && WaveOut.DeviceCount >= index)
            {
                if (config.OutputDevice != index)
                    if (call != null)
                        call.SwitchOutputDevice(index);

                config.OutputDevice = index;
            }

            if (VideoDevicesComboBox.SelectedItem != null)
                config.VideoDevice = ((VideoMenuData)VideoDevicesComboBox.SelectedItem).Name;

            config.EnableChatLogging = (bool)ChatLogCheckBox.IsChecked;
            config.Portable = (bool)PortableCheckBox.IsChecked;
            config.EnableAudioNotifications = (bool)AudioNotificationCheckBox.IsChecked;
            config.AlwaysNotify = (bool)AlwaysNotifyCheckBox.IsChecked;
	        config.EnableSpellcheck = (bool)SpellcheckCheckBox.IsChecked;
	        config.SpellcheckLanguage = (SpellcheckLanguage)Enum.Parse(typeof (SpellcheckLanguage), SpellcheckLanguageComboBox.SelectedItem.ToString());

	        ViewModel.SpellcheckLangCode = config.SpellcheckLanguage.ToDescription();
	        ViewModel.SpellcheckEnabled = config.EnableSpellcheck;
            ExecuteActionsOnNotifyIcon();

            bool filterAudio = (bool)FilterAudioCheckbox.IsChecked;

            if (config.FilterAudio != filterAudio)
                if (call != null)
                    call.FilterAudio = filterAudio;

            config.FilterAudio = filterAudio;

            bool proxyConfigChanged = false;
            var proxyType = (ToxProxyType)int.Parse((string)((ComboBoxItem)ProxyTypeComboBox.SelectedItem).Tag);

            var language = LanguageComboBox.Text;
            

            if (config.ProxyType != proxyType || config.ProxyAddress != SettingsProxyAddress.Text || config.ProxyPort.ToString() != SettingsProxyPort.Text || config.Language != language)
                proxyConfigChanged = true;

            config.ProxyType = proxyType;
            config.ProxyAddress = SettingsProxyAddress.Text;

            if (language!="")
                config.Language = language;

            int proxyPort;
            if (int.TryParse(SettingsProxyPort.Text, out proxyPort))
                config.ProxyPort = proxyPort;

            ConfigTools.Save(config, configFilename);
            saveTox();

            savingSettings = true;

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
                if (!tox.IsFriendOnline(selectedChatNumber) && ViewModel.IsFriendSelected)
                {
                    var friendOnlineTip = (string) FindResource("Local_NotOnlineTip");
                    var data = new MessageData(id: 0, username: getSelfName(), message: friendOnlineTip, isAction: false, timestamp: DateTime.Now, isGroupMsg: false, isSelf: true);
                    AddMessageToView(selectedChatNumber, data);

                    return;
                }

                if (text.StartsWith("/me "))
                {
                    //action
                    string action = text.Substring(4);
                    int messageid = -1;

                    if (ViewModel.IsFriendSelected)
                        messageid = tox.SendMessage(selectedChatNumber, action, ToxMessageType.Action);
                    else if (ViewModel.IsGroupSelected)
                        tox.SendGroupAction(selectedChatNumber, action);

                    MessageData data = new MessageData(id: messageid, username: "*  ", message: string.Format("{0} {1}", getSelfName(), action), isAction: true, isSelf: true, isGroupMsg: ViewModel.IsGroupSelected, timestamp: DateTime.Now);

                    if (ViewModel.IsFriendSelected)
                    {
                        AddActionToView(selectedChatNumber, data);

                        if (config.EnableChatLogging)
                            dbConnection.InsertAsync(new ToxMessage() { PublicKey = tox.GetFriendPublicKey(selectedChatNumber).GetString(), Message = data.Message, Timestamp = DateTime.Now, IsAction = true, Name = data.Username, ProfilePublicKey = tox.Id.PublicKey.GetString() });
                    }
                }
                else
                {
                    //regular message
                    foreach (string message in text.WordWrap(ToxConstants.MaxMessageLength))
                    {
                        int messageid = -1;

                        if (ViewModel.IsFriendSelected)
                            messageid = tox.SendMessage(selectedChatNumber, message, ToxMessageType.Message);
                        else if (ViewModel.IsGroupSelected)
                            tox.SendGroupMessage(selectedChatNumber, message);

                        MessageData data = new MessageData(id: messageid, username: getSelfName(), message: message, isSelf: true, isGroupMsg: ViewModel.IsGroupSelected, isAction: false, timestamp: DateTime.Now);

                        if (ViewModel.IsFriendSelected)
                        {
                            AddMessageToView(selectedChatNumber, data);

                            if (config.EnableChatLogging)
                                dbConnection.InsertAsync(new ToxMessage() { PublicKey = tox.GetFriendPublicKey(selectedChatNumber).GetString(), Message = data.Message, Timestamp = DateTime.Now, IsAction = false, Name = data.Username, ProfilePublicKey = tox.Id.PublicKey.GetString() });
                        }
                    }
                }

                ScrollChatBox();

                TextToSend.Text = string.Empty;
                e.Handled = true;
            }
            else if (e.Key == Key.Tab && ViewModel.IsGroupSelected)
            {
                string[] names = tox.GetGroupNames(ViewModel.SelectedChatNumber);

                foreach (string name in names)
                {
                    string lastPart = text.Split(' ').Last();
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

            string text = TextToSend.Text;

            if (string.IsNullOrEmpty(text))
            {
                if (typing)
                {
                    typing = false;
                    tox.SetTypingStatus(ViewModel.SelectedChatNumber, typing);
                }
            }
            else
            {
                if (!typing)
                {
                    typing = true;
                    tox.SetTypingStatus(ViewModel.SelectedChatNumber, typing);
                }
            }
        }

        private void CopyIDButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetDataObject(tox.Id.ToString());
        }

        private void MetroWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!resizing && focusTextbox)
                TextToSend.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
        }

        private void TextToSend_OnGotFocus(object sender, RoutedEventArgs e)
        {
            focusTextbox = true;
        }

        private void TextToSend_OnLostFocus(object sender, RoutedEventArgs e)
        {
            focusTextbox = false;
        }

        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (resizing)
            {
                resizing = false;
                if (focusTextbox)
                {
                    TextToSend.Focus();
                    focusTextbox = false;
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
            if (!tox.IsConnected)
                return;

            System.Windows.Controls.MenuItem menuItem = (System.Windows.Controls.MenuItem)e.Source;
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
                    byte[] bytes = bmp.GetBytes();

                    if (!convdic.ContainsKey(ViewModel.SelectedChatNumber))
                        convdic.Add(ViewModel.SelectedChatNumber, FlowDocumentExtensions.CreateNewDocument());

                    var fileInfo = tox.FileSend(ViewModel.SelectedChatNumber, ToxFileKind.Data, bytes.Length, "image.bmp");

                    if (fileInfo.Number == -1)
                        return;

                    var transfer = new FileSender(tox, fileInfo.Number, ViewModel.SelectedChatNumber, ToxFileKind.Data, bytes.Length, "image.bmp", new MemoryStream(bytes));
                    var control = convdic[ViewModel.SelectedChatNumber].AddNewFileTransfer(tox, transfer);
                    transfer.Tag = control;

                    transfer.Tag.SetStatus(string.Format("Waiting for {0} to accept...", getFriendName(ViewModel.SelectedChatNumber)));
                    transfer.Tag.AcceptButton.Visibility = Visibility.Collapsed;
                    transfer.Tag.DeclineButton.Visibility = Visibility.Visible;

                    control.OnDecline += delegate(FileTransfer ft)
                    {
                        ft.Kill(false);

                        if (transfers.Contains(ft))
                            transfers.Remove(ft);
                    };

                    control.OnPause += delegate(FileTransfer ft)
                    {
                        if (ft.Paused)
                            tox.FileControl(ft.FriendNumber, ft.FileNumber, ToxFileControl.Pause);
                        else
                            tox.FileControl(ft.FriendNumber, ft.FileNumber, ToxFileControl.Resume);
                    };

                    transfers.Add(transfer);

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
                    tox.Status = (ToxUserStatus)newStatus.GetValueOrDefault();

                    if (tox.Status != (ToxUserStatus)newStatus.GetValueOrDefault())
                        return;
                }
            }

            Dispatcher.BeginInvoke(((Action)(() => ViewModel.MainToxyUser.ToxStatus = newStatus.GetValueOrDefault())));
        }

        private void CallButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.IsFriendSelected)
                return;

            if (call != null)
                return;

            var selectedChatNumber = ViewModel.SelectedChatNumber;
            if (!tox.IsFriendOnline(selectedChatNumber))
                return;

            int call_index;
            ToxAvError error = toxav.Call(selectedChatNumber, ToxAv.DefaultCodecSettings, 30, out call_index);
            if (error != ToxAvError.None)
                return;

            int friendnumber = toxav.GetPeerID(call_index, 0);
            call = new ToxCall(toxav, call_index, friendnumber);
            call.FilterAudio = config.FilterAudio;

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
            if (!tox.IsFriendOnline(selectedChatNumber))
                return;

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.InitialDirectory = Environment.CurrentDirectory;
            dialog.Multiselect = false;

            if (dialog.ShowDialog() != true)
                return;

            string filename = dialog.FileName;

            SendFile(selectedChatNumber, filename);
        }

        private void SendFile(int chatNumber, string filename)
        {
            FileInfo info = new FileInfo(filename);
            var fileInfo = tox.FileSend(chatNumber, ToxFileKind.Data, info.Length, filename.Split('\\').Last<string>());

            if (fileInfo.Number == -1)
                return;

            var transfer = new FileSender(tox, fileInfo.Number, chatNumber, ToxFileKind.Data, info.Length, filename.Split('\\').Last<string>(), filename);
            var control = convdic[chatNumber].AddNewFileTransfer(tox, transfer);
            transfer.Tag = control;

            control.SetStatus(string.Format("Waiting for {0} to accept...", getFriendName(chatNumber)));
            control.AcceptButton.Visibility = Visibility.Collapsed;
            control.DeclineButton.Visibility = Visibility.Visible;

            control.OnDecline += delegate(FileTransfer ft)
            {
                ft.Kill(false);

                if (transfers.Contains(ft))
                    transfers.Remove(ft);
            };

            control.OnPause += delegate(FileTransfer ft)
            {
                if (ft.Paused)
                    tox.FileControl(ft.FriendNumber, ft.FileNumber, ToxFileControl.Pause);
                else
                    tox.FileControl(ft.FriendNumber, ft.FileNumber, ToxFileControl.Resume);
            };

            transfers.Add(transfer);
            ScrollChatBox();
        }

        private void ExecuteActionsOnNotifyIcon()
        {
            nIcon.Visible = config.HideInTray;
        }

        private void mv_Activated(object sender, EventArgs e)
        {
            nIcon.Icon = notifyIcon;
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
            if (!SettingsFlyout.IsOpen && !savingSettings)
            {
                ThemeManager.ChangeAppStyle(Application.Current, oldAccent, oldAppTheme);
            }
            else if (savingSettings)
            {
                savingSettings = false;
            }
        }

        private void AppThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var theme = ThemeManager.DetectAppStyle(Application.Current);
            var appTheme = ThemeManager.GetAppTheme(((AppThemeMenuData)AppThemeComboBox.SelectedItem).Name);
            ThemeManager.ChangeAppStyle(Application.Current, theme.Item2, appTheme);
        }

        private void ExportDataButton_OnClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "Export Tox data";
            dialog.InitialDirectory = Environment.CurrentDirectory;

            if (dialog.ShowDialog() != true)
                return;

            try { File.WriteAllBytes(dialog.FileName, tox.GetData().Bytes); }
            catch { this.ShowMessageAsync("Error", "Could not export data."); }
        }

        private void AvatarMenuItem_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.MenuItem menuItem = (System.Windows.Controls.MenuItem)e.Source;
            AvatarMenuItem item = (AvatarMenuItem)menuItem.Tag;

            switch (item)
            {
                case AvatarMenuItem.ChangeAvatar:
                    changeAvatar();
                    break;
                case AvatarMenuItem.RemoveAvatar:
                    removeAvatar();
                    break;
            }
        }

        private void changeAvatar()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Image files (*.png, *.gif, *.jpeg, *.jpg) | *.png;*.gif;*.jpeg;*.jpg";
            dialog.InitialDirectory = Environment.CurrentDirectory;
            dialog.Multiselect = false;

            if (dialog.ShowDialog() != true)
                return;

            string filename = dialog.FileName;
            FileInfo info = new FileInfo(filename);
          
            byte[] avatarBytes = File.ReadAllBytes(filename);
            MemoryStream stream = new MemoryStream(avatarBytes);
            Bitmap bmp = new Bitmap(stream);

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
                Bitmap newBmp = new Bitmap((int)width, (int)height);

                using (Graphics g = Graphics.FromImage(newBmp))
                {
                    double ratioX = width / (double)bmp.Width;
                    double ratioY = height / (double)bmp.Height;
                    double ratio = ratioX < ratioY ? ratioX : ratioY;

                    int newWidth = (int)(bmp.Width * ratio);
                    int newHeight = (int)(bmp.Height * ratio);

                    int posX = (int)((width - (bmp.Width * ratio)) / 2);
                    int posY = (int)((height - (bmp.Height * ratio)) / 2);
                    
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(bmp, posX, posY, newWidth, newHeight);
                }

                bmp.Dispose();

                bmp = newBmp;
                avatarBytes = avatarBitmapToBytes(bmp);

                if (avatarBytes.Length > 0x4000)
                {
                    this.ShowMessageAsync("Error", "This image is bigger than 16 KB and Toxy could not resize the image.");
                    return;
                }
            }

            ViewModel.MainToxyUser.AvatarBytes = avatarBytes;
            ViewModel.MainToxyUser.Avatar = bmp.ToBitmapImage(ImageFormat.Png);
            bmp.Dispose();

            string avatarsDir = Path.Combine(toxDataDir, "avatars");
            string selfAvatarFile = Path.Combine(avatarsDir, tox.Id.PublicKey.GetString() + ".png");

            if (!Directory.Exists(avatarsDir))
                Directory.CreateDirectory(avatarsDir);

            File.WriteAllBytes(selfAvatarFile, avatarBytes);

            //let's announce our new avatar
            byte[] hash = ToxTools.Hash(avatarBytes);
            foreach (int friend in tox.Friends)
            {
                if (!tox.IsFriendOnline(friend))
                    continue;

                var fileInfo = tox.FileSend(friend, ToxFileKind.Avatar, avatarBytes.Length, "avatar.png", hash);
                var transfer = new FileSender(tox, fileInfo.Number, friend, ToxFileKind.Avatar, avatarBytes.Length, "", selfAvatarFile);
                transfers.Add(transfer);
            }
        }

        private byte[] avatarBitmapToBytes(Bitmap bmp)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                bmp.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }

        private void removeAvatar()
        {
            ViewModel.MainToxyUser.Avatar = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/profilepicture.png"));

            if (!config.Portable)
            {
                string path = Path.Combine(toxDataDir, "avatar.png");

                if (File.Exists(path))
                    File.Delete(path);
            }
            else
            {
                if (File.Exists("avatar.png"))
                    File.Delete("avatar.png");
            }

            foreach (int friend in tox.Friends)
            {
                if (!tox.IsFriendOnline(friend))
                    continue;

                var fileInfo = tox.FileSend(friend, ToxFileKind.Avatar, 0, "");
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

            if (config.EnableAudioNotifications && call == null)
            {
                if (WindowState == WindowState.Normal && config.AlwaysNotify && !chat.Selected)
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
                foreach (IChatObject chat in this.ViewModel.ChatCollection)
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
                foreach (IChatObject chat in this.ViewModel.ChatCollection)
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
            System.Windows.Controls.MenuItem menuItem = (System.Windows.Controls.MenuItem)e.Source;
            GroupMenuItem item = (GroupMenuItem)menuItem.Tag;

            if (item == GroupMenuItem.TextAudio && call != null)
            {
                await this.ShowMessageAsync("Error", "Could not create audio groupchat, there's already a call in progress.");
                return;
            }

            int groupNumber = item == GroupMenuItem.Text ? tox.NewGroup() : toxav.AddAvGroupchat();
            if (groupNumber != -1)
            {
                AddGroupToView(groupNumber, (ToxGroupType)item);
            }

            if (item == GroupMenuItem.TextAudio)
            {
                call = new ToxGroupCall(toxav, groupNumber);
                call.FilterAudio = config.FilterAudio;
                call.Start(config.InputDevice, config.OutputDevice, ToxAv.DefaultCodecSettings);
            }

            tox.SetGroupTitle(groupNumber, string.Format("Groupchat #{0}", groupNumber));
        }

        private async void mv_Loaded(object sender, RoutedEventArgs e)
        {
            ToxOptions options;
            if (config.ProxyType != ToxProxyType.None)
                options = new ToxOptions(config.Ipv6Enabled, config.ProxyType, config.ProxyAddress, config.ProxyPort);
            else
                options = new ToxOptions(config.Ipv6Enabled, !config.UdpDisabled);

            tox = new Tox(options);

            var data = await loadTox();
            if (data != null)
                tox = new Tox(options, data);

            tox.OnFriendNameChanged += tox_OnFriendNameChanged;
            tox.OnFriendMessageReceived += tox_OnFriendMessageReceived;
            tox.OnFriendRequestReceived += tox_OnFriendRequestReceived;
            tox.OnFriendStatusChanged += tox_OnFriendStatusChanged;
            tox.OnFriendStatusMessageChanged += tox_OnFriendStatusMessageChanged;
            tox.OnFriendTypingChanged += tox_OnFriendTypingChanged;
            tox.OnConnectionStatusChanged += tox_OnConnectionStatusChanged;
            tox.OnFriendConnectionStatusChanged += tox_OnFriendConnectionStatusChanged;
            tox.OnFileSendRequestReceived += tox_OnFileSendRequestReceived;
            tox.OnFileChunkReceived += tox_OnFileChunkReceived;
            tox.OnFileControlReceived += tox_OnFileControlReceived;
            tox.OnFileChunkRequested += tox_OnFileChunkRequested;
            tox.OnReadReceiptReceived += tox_OnReadReceiptReceived;
            tox.OnGroupTitleChanged += tox_OnGroupTitleChanged;

            tox.OnGroupInvite += tox_OnGroupInvite;
            tox.OnGroupMessage += tox_OnGroupMessage;
            tox.OnGroupAction += tox_OnGroupAction;
            tox.OnGroupNamelistChange += tox_OnGroupNamelistChange;

            toxav = new ToxAv(tox.Handle, 1);
            toxav.OnInvite += toxav_OnInvite;
            toxav.OnStart += toxav_OnStart;
            toxav.OnEnd += toxav_OnEnd;
            toxav.OnPeerTimeout += toxav_OnEnd;
            toxav.OnRequestTimeout += toxav_OnEnd;
            toxav.OnReject += toxav_OnEnd;
            toxav.OnCancel += toxav_OnEnd;
            toxav.OnReceivedAudio += toxav_OnReceivedAudio;
            toxav.OnReceivedVideo += toxav_OnReceivedVideo;
            toxav.OnPeerCodecSettingsChanged += toxav_OnPeerCodecSettingsChanged;
            toxav.OnReceivedGroupAudio += toxav_OnReceivedGroupAudio;

            DoBootstrap();
            tox.Start();
            toxav.Start();

            if (string.IsNullOrEmpty(getSelfName()))
                tox.Name = "Tox User";

            if (string.IsNullOrEmpty(getSelfStatusMessage()))
                tox.StatusMessage = "Toxing on Toxy";

            ViewModel.MainToxyUser.Name = getSelfName();
            ViewModel.MainToxyUser.StatusMessage = getSelfStatusMessage();

            InitializeNotifyIcon();

            SetStatus(null, false);
            InitFriends();

            TextToSend.AddHandler(DragOverEvent, new DragEventHandler(Chat_DragOver), true);
            TextToSend.AddHandler(DropEvent, new DragEventHandler(Chat_Drop), true);

            ChatBox.AddHandler(DragOverEvent, new DragEventHandler(Chat_DragOver), true);
            ChatBox.AddHandler(DropEvent, new DragEventHandler(Chat_Drop), true);

            if (tox.Friends.Length > 0)
                ViewModel.SelectedChatObject = ViewModel.ChatCollection.OfType<IFriendObject>().FirstOrDefault();

            initDatabase();
            loadAvatars();
        }

        private void DoBootstrap()
        {
            if (config.Nodes.Length >= 4)
            {
                var random = new Random();
                var indices = new List<int>();

                for (int i = 0; i < 4; )
                {
                    int index = random.Next(config.Nodes.Length);
                    if (indices.Contains(index))
                        continue;

                    var node = config.Nodes[index];
                    if (bootstrap(config.Nodes[index]))
                    {
                        indices.Add(index);
                        i++;
                    }
                }
            }
            else
            {
                foreach (var node in config.Nodes)
                    bootstrap(node);
            }

            WaitAndBootstrap(20000);
        }

        private async void WaitAndBootstrap(int delay)
        {
            await Task.Factory.StartNew(async() =>
            {
                //wait 'delay' seconds, check if we're connected, if not, bootstrap again
                await Task.Delay(delay);

                if (!tox.IsConnected)
                {
                    Debug.WriteLine("We're still not connected, bootstrapping again");
                    DoBootstrap();
                }
            });
        }

        private bool bootstrap(ToxConfigNode node)
        {
            var error = ToxErrorBootstrap.Ok;
            bool success = tox.Bootstrap(new ToxNode(node.Address, node.Port, new ToxKey(ToxKeyType.Public, node.ClientId)), out error);
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

            List<string> profiles = new List<string>();

            foreach (string profile in Directory.GetFiles(path, "*.tox", SearchOption.TopDirectoryOnly).Where(s => s.EndsWith(".tox")))
                profiles.Add(profile.Substring(0, profile.LastIndexOf(".tox")).Split('\\').Last());

            return profiles.ToArray();
        }

        private async void SwitchProfileButton_Click(object sender, RoutedEventArgs e)
        {
            string[] profiles = GetProfileNames(toxDataDir);
            if (profiles == null && profiles.Length < 1)
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
                string profile = await this.ShowInputAsync("New Profile", "Enter a name for your new profile.");
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
                ToxData data = ToxData.FromDisk(result.Input);
                Tox t = new Tox(ToxOptions.Default, data);

                if (data == null)
                {
                    await this.ShowInputAsync("Error", "Could not load tox profile.");
                }
                else
                {
                    string profile = await this.ShowInputAsync("New Profile", "Enter a name for your new profile.");
                    if (string.IsNullOrEmpty(profile))
                        await this.ShowMessageAsync("Error", "Could not create profile, you must enter a name for your profile.");
                    else
                    {
                        string path = Path.Combine(toxDataDir, profile + ".tox");
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
            string path = Path.Combine(toxDataDir, profileName + ".tox");
            if (File.Exists(path))
                return false;

            Tox t = new Tox(ToxOptions.Default);
            t.Name = profileName;

            if (!t.GetData().Save(path))
            {
                t.Dispose();
                return false;
            }

            t.Dispose();
            return LoadProfile(profileName, false);
        }

        private bool LoadProfile(string profile, bool allowReload)
        {
            if (config.ProfileName == profile && !allowReload)
                return true;

            if (!File.Exists(Path.Combine(toxDataDir, profile + ".tox")))
                return false;

            KillTox(false);
            ViewModel.ChatCollection.Clear();

            config.ProfileName = profile;
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
            if (call == null || call.GetType() != typeof(ToxGroupCall))
                return;

            var groupCall = (ToxGroupCall)call;
            groupCall.Muted = !groupCall.Muted;
        }

        private void VideoButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (call == null || call.GetType() == typeof(ToxGroupCall))
                return;

            call.ToggleVideo((bool)VideoButton.IsChecked, config.VideoDevice);
        }

        private void ProcessVideoFrame(IntPtr frame)
        {
            var vpxImage = VpxImage.FromPointer(frame);
            byte[] dest = VpxHelper.Yuv420ToRgb(vpxImage, vpxImage.d_w * vpxImage.d_h * 4);

            vpxImage.Free();

            int bytesPerPixel = (PixelFormats.Bgra32.BitsPerPixel + 7) / 8;
            int stride = 4 * (((int)vpxImage.d_w * bytesPerPixel + 3) / 4);

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
            byte[] buffer = new byte[sizeof(uint)];
            var random = new Random();

            random.NextBytes(buffer);
            SettingsNospam.Text = BitConverter.ToUInt32(buffer, 0).ToString();
        }

        private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
        {
            LanguageMenuData content = (LanguageMenuData) e.AddedItems[0];
            var fileShortCut = getShortLanguageName(content.Name);
            if (fileShortCut.Equals("fail"))
                return;

            ViewModel.ChangeLanguage(fileShortCut);
        } 


        private string getShortLanguageName(string LanguageDescriptor)
        {
            switch (LanguageDescriptor)
            {
                case "English":
                    return "Eng";

                case "Deutsch":
                    return "Ger";

                case "Dutch":
                    return "Nl";

                case "Pусский":
                    return "Ru";

                case "한글":
                    return "Kr";

            }

            return "fail";
        }
    }
}
