using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

using SharpTox.Core;
using Toxy.ViewModels;
using Toxy.Extensions;

namespace Toxy.Managers
{
    public class TransferManager
    {
        private static TransferManager _instance;
        private Dictionary<FileTransfer, Stream> _transfers = new Dictionary<FileTransfer, Stream>();

        private TransferManager()
        {
            ProfileManager.Instance.Tox.OnFileChunkReceived += Tox_OnFileChunkReceived;
            ProfileManager.Instance.Tox.OnFileChunkRequested += Tox_OnFileChunkRequested;
            ProfileManager.Instance.Tox.OnFileControlReceived += Tox_OnFileControlReceived;
            ProfileManager.Instance.Tox.OnFileSendRequestReceived += Tox_OnFileSendRequestReceived;
            ProfileManager.Instance.Tox.OnFriendConnectionStatusChanged += Tox_OnFriendConnectionStatusChanged;
        }

        public static TransferManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new TransferManager();

                return _instance;
            }
        }

        public static TransferManager Init()
        {
            return Instance;
        }

        public bool CancelTransfer(FileTransfer transfer)
        {
            if (!_transfers.ContainsKey(transfer))
            {
                Debugging.Write("Could not cancel file transfer, we don't know about this file transfer");
                return false;
            }

            var error = ToxErrorFileControl.Ok;
            ProfileManager.Instance.Tox.FileControl(transfer.FriendNumber, transfer.FileNumber, ToxFileControl.Cancel);

            RemoveTransfer(transfer, true);

            if (error != ToxErrorFileControl.Ok)
            {
                Debugging.Write("Could not cancel file transfer, error: " + error);
                return false;
            }

            return true;
        }

        public bool PauseTransfer(FileTransfer fileTransfer)
        {
            if (fileTransfer.IsPaused)
                return false;

            var error = ToxErrorFileControl.Ok;
            if (!ProfileManager.Instance.Tox.FileControl(fileTransfer.FriendNumber, fileTransfer.FileNumber, ToxFileControl.Pause, out error))
                Debugging.Write("Could not pause file transfer, error: " + error);
            else
                fileTransfer.Pause(true);

            return error == ToxErrorFileControl.Ok;
        }
        
        public bool ResumeTransfer(FileTransfer fileTransfer)
        {
            if (!fileTransfer.IsPaused)
                return false;

            var error = ToxErrorFileControl.Ok;
            if (!ProfileManager.Instance.Tox.FileControl(fileTransfer.FriendNumber, fileTransfer.FileNumber, ToxFileControl.Resume, out error))
                Debugging.Write("Could not resume file transfer, error: " + error);
            else
                fileTransfer.Resume();

            return error == ToxErrorFileControl.Ok;
        }

        public void SendAvatar(int friendNumber, byte[] avatar)
        {
            //cancel any existing avatar file transfers with this friend first
            for (int i = _transfers.Count() - 1; i >= 0; i--)
            {
                //reverse loop to be able to delete entries while iterating through them, fun stuff
                var entry = _transfers.ElementAt(i);
                if (entry.Key.FriendNumber == friendNumber && entry.Key.Direction == FileTransferDirection.Outgoing)
                    CancelTransfer(entry.Key);
            }

            var error = ToxErrorFileSend.Ok;
            ToxFileInfo fileInfo;

            if (avatar != null)
                fileInfo = ProfileManager.Instance.Tox.FileSend(friendNumber, ToxFileKind.Avatar, avatar.Length, "avatar.png", ToxTools.Hash(avatar), out error);
            else
                fileInfo = ProfileManager.Instance.Tox.FileSend(friendNumber, ToxFileKind.Avatar, 0, "avatar.png");

            if (error != ToxErrorFileSend.Ok)
            {
                Debugging.Write("Could not send file transfer request: " + error.ToString());
                return;
            }

            //no point in adding a 'dummy' transfer to our list
            if (avatar == null)
                return;

            var tr = new FileTransfer(fileInfo.Number, friendNumber, ToxFileKind.Avatar, FileTransferDirection.Outgoing);

            if (!_transfers.ContainsKey(tr))
                _transfers.Add(tr, new MemoryStream(avatar));
            else
                Debugging.Write("Tried to add a filetransfer that's already in the list, panic!");
        }

        public FileTransfer SendFile(int friendNumber, string path)
        {
            string fileName = path.Split(new[] { "\\" }, StringSplitOptions.None).Last();
            FileStream stream;

            try { stream = new FileStream(path, FileMode.Open, FileAccess.Read); }
            catch { return null; }

            long size = stream.Length;
            var error = ToxErrorFileSend.Ok;

            ToxFileInfo fileInfo = ProfileManager.Instance.Tox.FileSend(friendNumber, ToxFileKind.Data, size, fileName, out error);

            if (error != ToxErrorFileSend.Ok)
            {
                Debugging.Write("Could not send file transfer request: " + error.ToString());
                return null;
            }

            var tr = new FileTransfer(fileInfo.Number, friendNumber, ToxFileKind.Avatar, FileTransferDirection.Outgoing);
            tr.Name = fileName;
            tr.Size = size;

            if (!_transfers.ContainsKey(tr))
                _transfers.Add(tr, stream);
            else
            {
                Debugging.Write("Tried to add a filetransfer that's already in the list, panic!");
                return null;
            }

            return tr;
        }

        public FileTransfer FindTransfer(int friendNumber, int fileNumber)
        {
            return _transfers.FirstOrDefault(t => t.Key.FriendNumber == friendNumber && t.Key.FileNumber == fileNumber).Key;
        }

        public bool AcceptTransfer(FileTransfer transfer, string path)
        {
            var error = ToxErrorFileControl.Ok;
            ProfileManager.Instance.Tox.FileControl(transfer.FriendNumber, transfer.FileNumber, ToxFileControl.Resume, out error);
            if (error != ToxErrorFileControl.Ok)
            {
                Debugging.Write("Could not accept file transfer, error: " + error);
                return false;
            }

            try
            {
                _transfers[transfer] = new FileStream(path, FileMode.Create);
                transfer.Path = path;
                transfer.Start();
                return true;
            }
            catch (Exception ex)
            {
                Debugging.Write("Could not accept file transfer, exception: " + ex.ToString());
                return false;
            }
        }

        private void Tox_OnFriendConnectionStatusChanged(object sender, ToxEventArgs.FriendConnectionStatusEventArgs e)
        {
            if (e.Status == ToxConnectionStatus.None)
            {
                //TODO: refactor
                for (int i = _transfers.Count() - 1; i >= 0; i--)
                {
                    var entry = _transfers.ElementAt(i);
                    if (entry.Key.FriendNumber == e.FriendNumber)
                        continue;

                    RemoveTransfer(entry.Key, true);
                }

                Debugging.Write("A friend went offline, purged all file transfers");
            }
        }

        private void Tox_OnFileChunkReceived(object sender, ToxEventArgs.FileChunkEventArgs e)
        {
            var transfer = FindTransfer(e.FriendNumber, e.FileNumber);
            Stream stream = null;

            if (transfer == null || !_transfers.TryGetValue(transfer, out stream))
            {
                Debugging.Write("Got a chunk for a transfer we don't know about, ignoring");
                return;
            }

            //TODO: remember the total file size in case the client isn't nice enough to send an empty chunk
            if (e.Data == null || e.Data.Length == 0)
            {
                //looks like we're done!
                RemoveTransfer(transfer, false);

                if (transfer.Kind == ToxFileKind.Avatar)
                    AvatarManager.Instance.Rehash(e.FriendNumber);

                Debugging.Write("File transfer finished");
                return;
            }

            if (stream.Position != e.Position)
            {
                Debugging.Write("Position doesn't equal ours, rewinding");
                stream.Seek(e.Position, SeekOrigin.Begin);
                transfer.TransferredBytes += e.Position;
            }

            stream.Write(e.Data, 0, e.Data.Length);
            transfer.TransferredBytes += e.Data.Length;
        }

        private void Tox_OnFileChunkRequested(object sender, ToxEventArgs.FileRequestChunkEventArgs e)
        {
            var transfer = FindTransfer(e.FriendNumber, e.FileNumber);
            Stream stream = null;

            if (transfer == null || !_transfers.TryGetValue(transfer, out stream))
            {
                Debugging.Write("Chunk requested for a transfer we don't know about, ignoring");
                return;
            }

            if (e.Length == 0)
            {
                Debugging.Write("Transfer finished");

                //time to clean up
                RemoveTransfer(transfer, false);

                //let's be nice and send an empty chunk to let our friend know we're done sending the file
                ProfileManager.Instance.Tox.FileSendChunk(e.FriendNumber, e.FileNumber, e.Position, new byte[0]);
                return;
            }

            if (stream.Position != e.Position)
            {
                Debugging.Write("Position doesn't equal ours, rewinding");

                try { stream.Seek(e.Position, SeekOrigin.Begin); }
                catch (Exception ex) 
                {
                    Debugging.Write("Error while seeking in stream, exception: " + ex.ToString());
                    return; //TODO: should probably cancel the transfer here
                }

                transfer.TransferredBytes = e.Position;
            }

            byte[] buffer = new byte[e.Length];

            try { stream.Read(buffer, 0, buffer.Length); }
            catch (Exception ex) 
            { 
                Debugging.Write("Error while reading from stream, exception: " + ex.ToString());
                return; //TODO: should probably cancel the transfer here
            }

            var error = ToxErrorFileSendChunk.Ok;
            if (!ProfileManager.Instance.Tox.FileSendChunk(e.FriendNumber, e.FileNumber, e.Position, buffer, out error))
                Debugging.Write("Failed to send chunk: " + error);
            else
                transfer.TransferredBytes += e.Length;
        }

        private void RemoveTransfer(FileTransfer transfer, bool force)
        {
            transfer.Stop(force);

            if (_transfers.ContainsKey(transfer))
            {
                var stream = _transfers[transfer];
                if (stream != null)
                    stream.Dispose();

                _transfers.Remove(transfer);
            }
            else
            {
                Debugging.Write("Attempted to remove a transfer we don't know about");
            }
        }

        private void Tox_OnFileControlReceived(object sender, ToxEventArgs.FileControlEventArgs e)
        {
            var transfer = FindTransfer(e.FriendNumber, e.FileNumber);
            if (transfer != null)
            {
                switch (e.Control)
                {
                    case ToxFileControl.Cancel:
                        {
                            RemoveTransfer(transfer, true);
                            break;
                        }
                    case ToxFileControl.Resume:
                        {
                            if (!transfer.IsPaused)
                                transfer.Start();
                            else
                                transfer.Resume();

                            break;
                        }
                    case ToxFileControl.Pause:
                        {
                            if (!transfer.IsPaused)
                                transfer.Pause(false);
                            else
                                Debugging.Write("Friend tried to pause a transfer that was paused already!");

                            break;
                        }
                }
            }

            Debugging.Write(string.Format("Received control: {0} for transfer: {1}. Transfer known?: {2}", e.Control, e.FileNumber, transfer != null));
        }

        private void Tox_OnFileSendRequestReceived(object sender, ToxEventArgs.FileSendRequestEventArgs e)
        {
            var transfer = new FileTransfer(e.FileNumber, e.FriendNumber, e.FileKind, FileTransferDirection.Incoming);
            transfer.Name = e.FileName;
            transfer.Size = e.FileSize;

            switch (e.FileKind)
            {
                case ToxFileKind.Avatar:
                    {
                        if (e.FileSize == 0)
                        {
                            //friend removed avatar, remove it from our store too
                            AvatarManager.Instance.RemoveAvatar(e.FriendNumber);
                            ProfileManager.Instance.Tox.FileControl(e.FriendNumber, e.FileNumber, ToxFileControl.Cancel);

                            Debugging.Write("Friend removed avatar");
                            break;
                        }

                        if (e.FileSize > 1 << 16)
                        {
                            //we don't like this avatar, it's too big
                            ProfileManager.Instance.Tox.FileControl(e.FriendNumber, e.FileNumber, ToxFileControl.Cancel);

                            Debugging.Write("Avatar too big, ignoring");
                            break;
                        }

                        if (AvatarManager.Instance.Contains(e.FriendNumber))
                        {
                            //compare hashes to see if we already have this avatar
                            byte[] hash = ProfileManager.Instance.Tox.FileGetId(e.FriendNumber, e.FileNumber);
                            if (hash != null && AvatarManager.Instance.HashMatches(e.FriendNumber, hash))
                            {
                                //we already have this avatar, cancel the transfer
                                ProfileManager.Instance.Tox.FileControl(e.FriendNumber, e.FileNumber, ToxFileControl.Cancel);
                                break;
                            }
                        }

                        var error = ToxErrorFileControl.Ok;
                        if (!ProfileManager.Instance.Tox.FileControl(e.FriendNumber, e.FileNumber, ToxFileControl.Resume, out error))
                        {
                            Debugging.Write("Failed to accept avatar transfer request: " + error);
                            break;
                        }

                        var avatarPath = AvatarManager.Instance.GetAvatarFilename(e.FriendNumber);
                        if (avatarPath == null)
                        {
                            Debugging.Write("Could not find public key for friend");
                            break;
                        }

                        if (!_transfers.ContainsKey(transfer))
                        {
                            //catch any scary exceptions (no access, file already in use and whatnot)
                            try { _transfers.Add(transfer, new FileStream(avatarPath, FileMode.Create)); }
                            catch (Exception ex)
                            {
                                Debugging.Write("Can't open filestream: " + ex.ToString());
                            }
                        }
                        else
                            Debugging.Write("We already have a transfer with that number!");

                        break;
                    }
                case ToxFileKind.Data:
                    {
                        if (!_transfers.ContainsKey(transfer))
                            _transfers.Add(transfer, null);
                        else
                        {
                            Debugging.Write("We already have a transfer with that number!");
                            return;
                        }

                        MainWindow.Instance.UInvoke(() =>
                        {
                            var friend = MainWindow.Instance.ViewModel.CurrentFriendListView.FindFriend(e.FriendNumber);
                            if (friend == null)
                                return;

                            (friend.ConversationView as ConversationViewModel).AddTransfer(transfer);
                        });

                        break;
                    }
            }

            Debugging.Write("New file send request: " + e.FileKind.ToString());
        }
    }
}
