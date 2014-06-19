using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Text.RegularExpressions;

using MahApps.Metro;
using MahApps.Metro.Controls;

using SharpTox;
using Toxy.ViewModels;

using Path = System.IO.Path;
using Microsoft.Win32;

namespace Toxy
{
    public partial class MainWindow : MetroWindow
    {
        private Tox tox;
        private Dictionary<int, FlowDocument> convdic = new Dictionary<int, FlowDocument>();
        private Dictionary<int, FlowDocument> groupdic = new Dictionary<int, FlowDocument>();
        private List<FileTransfer> transfers = new List<FileTransfer>();

        private int current_number = 0;
        private Type current_type = typeof(FriendControl);
        private bool resizing = false;
        private bool focusTextbox = false;
        private bool typing = false;

        public MainWindow()
        {
            InitializeComponent();
            tox = new Tox(false);
            tox.Invoker = Dispatcher.BeginInvoke;
            tox.OnNameChange += tox_OnNameChange;
            tox.OnFriendMessage += tox_OnFriendMessage;
            tox.OnFriendAction += tox_OnFriendAction;
            tox.OnFriendRequest += tox_OnFriendRequest;
            tox.OnUserStatus += tox_OnUserStatus;
            tox.OnStatusMessage += tox_OnStatusMessage;
            tox.OnTypingChange += tox_OnTypingChange;
            tox.OnConnectionStatusChanged += tox_OnConnectionStatusChanged;
            tox.OnFileSendRequest += tox_OnFileSendRequest;
            tox.OnFileData += tox_OnFileData;
            tox.OnFileControl += tox_OnFileControl;

            tox.OnGroupInvite += tox_OnGroupInvite;
            tox.OnGroupMessage += tox_OnGroupMessage;
            tox.OnGroupAction += tox_OnGroupAction;
            tox.OnGroupNamelistChange += tox_OnGroupNamelistChange;

            bool bootstrap_success = false;
            foreach (ToxNode node in nodes)
            {
                if (tox.BootstrapFromNode(node))
                    bootstrap_success = true;
            }

            if (File.Exists("data"))
            {
                if (!tox.Load("data"))
                {
                    MessageBox.Show("Could not load tox data, this program will now exit.", "Error");
                    Close();
                }
            }

            tox.Start();

            if (string.IsNullOrEmpty(tox.GetSelfName()))
                tox.SetName("Toxy User");

            Username.Text = tox.GetSelfName();

            Userstatus.Text = tox.GetSelfStatusMessage();
            Status.SelectedIndex = 0;

            InitFriends();
            if (tox.GetFriendlistCount() > 0)
                SelectFriendControl(GetFriendControlByNumber(0));
        }

        private void tox_OnGroupNamelistChange(int groupnumber, int peernumber, ToxChatChange change)
        {
            GroupControl control = GetGroupControlByNumber(groupnumber);
            if (control == null)
                return;

            if (change == ToxChatChange.PEER_ADD || change == ToxChatChange.PEER_DEL)
            {
                string status = string.Format("Peers online: {0}", tox.GetGroupMemberCount(groupnumber));
                control.SetStatusMessage(status);

                if (current_number == groupnumber && current_type == typeof(GroupControl))
                    Friendstatus.Text = status;
            }
        }

        private void tox_OnGroupAction(int groupnumber, int friendgroupnumber, string action)
        {
            MessageData data = new MessageData() { Username = "*", Message = string.Format("{0} {1}", tox.GetGroupMemberName(groupnumber, friendgroupnumber), action) };

            if (groupdic.ContainsKey(groupnumber))
                AddNewRowToDocument(groupdic[groupnumber], data);
            else
            {
                FlowDocument document = GetNewFlowDocument();
                groupdic.Add(groupnumber, document);
                AddNewRowToDocument(groupdic[groupnumber], data);
            }

            if (!(current_number == groupnumber && current_type == typeof(GroupControl)))
                GetGroupControlByNumber(groupnumber).BorderBrush = (Brush)FindResource("AccentColorBrush");

            if (current_number == groupnumber && current_type == typeof(GroupControl))
                ScrollChatBox();

            this.Flash();
        }

        private void tox_OnGroupMessage(int groupnumber, int friendgroupnumber, string message)
        {
            MessageData data = new MessageData() { Username = tox.GetGroupMemberName(groupnumber, friendgroupnumber), Message = message };

            if (groupdic.ContainsKey(groupnumber))
            {
                Run run = GetLastMessageRun(groupdic[groupnumber]);

                if (run != null)
                {
                    if (run.Text == data.Username)
                        AppendToDocument(groupdic[groupnumber], data);
                    else
                        AddNewRowToDocument(groupdic[groupnumber], data);
                }
                else
                {
                    AddNewRowToDocument(groupdic[groupnumber], data);
                }
            }
            else
            {
                FlowDocument document = GetNewFlowDocument();
                groupdic.Add(groupnumber, document);
                AddNewRowToDocument(groupdic[groupnumber], data);
            }

            if (!(current_number == groupnumber && current_type == typeof(GroupControl)))
                GetGroupControlByNumber(groupnumber).BorderBrush = (Brush)FindResource("AccentColorBrush");

            if (current_number == groupnumber && current_type == typeof(GroupControl))
                ScrollChatBox();

            this.Flash();
        }

        private void tox_OnGroupInvite(int friendnumber, string group_public_key)
        {
            //auto join groupchats for now

            int groupnumber = tox.JoinGroup(friendnumber, group_public_key);

            if (groupnumber != -1)
                AddGroupToView(groupnumber);
        }

        private void tox_OnFriendRequest(string id, string message)
        {
            try
            {
                AddFriendRequestToView(id, message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void tox_OnFileControl(int friendnumber, int receive_send, int filenumber, int control_type, byte[] data)
        {
            switch ((ToxFileControl)control_type)
            {
                case ToxFileControl.FINISHED:
                    {
                        FileTransfer ft = GetFileTransfer(friendnumber, filenumber);

                        if (ft == null)
                            return;

                        ft.Stream.Close();
                        ft.Stream = null;

                        ft.Control.TransferFinished();
                        ft.Control.SetStatus("Finished");

                        transfers.Remove(ft);
                        break;
                    }
            }
        }

        private FileTransfer GetFileTransfer(int friendnumber, int filenumber)
        {
            foreach (FileTransfer ft in transfers)
                if (ft.FileNumber == filenumber && ft.FriendNumber == friendnumber)
                    return ft;

            return null;
        }

        private void tox_OnFileData(int friendnumber, int filenumber, byte[] data)
        {
            FileTransfer ft = GetFileTransfer(friendnumber, filenumber);

            if (ft == null)
                return;

            if (ft.Stream == null)
                throw new Exception("Unexpectedly received data");

            ulong remaining = tox.FileDataRemaining(friendnumber, filenumber, 1);
            double value = (double)remaining / (double)ft.FileSize;

            ft.Control.SetProgress(100 - (int)(value * 100));
            ft.Control.SetStatus(string.Format("{0}/{1}", ft.FileSize - remaining, ft.FileSize));

            ft.Stream.Write(data, 0, data.Length);
        }

        private void tox_OnFileSendRequest(int friendnumber, int filenumber, ulong filesiz, string filename)
        {
            if (!convdic.ContainsKey(friendnumber))
                convdic.Add(friendnumber, GetNewFlowDocument());

            FileTransfer transfer = AddNewFTRowToDocument(convdic[friendnumber], friendnumber, filenumber, filename, filesiz);

            transfer.Control.OnAccept += delegate(int friendnum, int filenum)
            {
                if (transfer.Stream != null)
                    return;

                SaveFileDialog dialog = new SaveFileDialog();
                dialog.FileName = filename;

                if (dialog.ShowDialog() == true) //guess what, this bool is nullable
                {
                    transfer.Stream = new FileStream(dialog.FileName, FileMode.Create);
                    tox.FileSendControl(friendnumber, 1, filenumber, ToxFileControl.ACCEPT, new byte[0]);
                }
            };

            transfer.Control.OnDecline += delegate(int friendnum, int filenum)
            {
                tox.FileSendControl(friendnumber, 1, filenumber, ToxFileControl.KILL, new byte[0]);
            };

            transfer.Control.OnFileOpen += delegate()
            {
                try { Process.Start(transfer.FileName); }
                catch { /*want to open a "choose program dialog" here*/ }
            };

            transfer.Control.OnFolderOpen += delegate()
            {
                string filePath = Path.Combine(Environment.CurrentDirectory, filename);
                Process.Start("explorer.exe", @"/select, " + filePath);
            };

            transfers.Add(transfer);

            this.Flash();
        }

        private void tox_OnConnectionStatusChanged(int friendnumber, int status)
        {
            if (status == 0)
            {
                DateTime lastOnline = tox.GetLastOnline(friendnumber);
                FriendControl control = GetFriendControlByNumber(friendnumber);

                if (control == null)
                    return;

                control.SetStatusMessage("Last seen: " + lastOnline.ToShortDateString() + " " + lastOnline.ToLongTimeString());
                control.SetStatus(ToxUserStatus.INVALID); //not the proper way to do it, I know...
            }
        }

        private void tox_OnTypingChange(int friendnumber, bool is_typing)
        {
            if (current_number == friendnumber)
            {
                if (is_typing)
                    TypingStatusLabel.Content = tox.GetName(friendnumber) + " is typing...";
                else
                    TypingStatusLabel.Content = "";
            }
        }

        private void tox_OnFriendAction(int friendnumber, string action)
        {
            MessageData data = new MessageData() { Username = "*", Message = string.Format("{0} {1}", tox.GetName(friendnumber), action) };

            if (convdic.ContainsKey(friendnumber))
                AddNewRowToDocument(convdic[friendnumber], data);
            else
            {
                FlowDocument document = GetNewFlowDocument();
                convdic.Add(friendnumber, document);
                AddNewRowToDocument(convdic[friendnumber], data);
            }

            if (!(current_number == friendnumber && current_type == typeof(FriendControl)))
                GetFriendControlByNumber(friendnumber).BorderBrush = (Brush)FindResource("AccentColorBrush");

            if (current_number == friendnumber && current_type == typeof(FriendControl))
                ScrollChatBox();

            this.Flash();
        }

        private FlowDocument GetNewFlowDocument()
        {
            Stream doc_stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Toxy.Message.xaml");
            FlowDocument doc = (FlowDocument)XamlReader.Load(doc_stream);
            doc.IsEnabled = true;

            return doc;
        }

        private void tox_OnFriendMessage(int friendnumber, string message)
        {
            MessageData data = new MessageData() { Username = tox.GetName(friendnumber), Message = message };

            if (convdic.ContainsKey(friendnumber))
            {
                Run run = GetLastMessageRun(convdic[friendnumber]);

                if (run != null)
                {
                    if (run.Text == tox.GetName(friendnumber))
                        AppendToDocument(convdic[friendnumber], data);
                    else
                        AddNewRowToDocument(convdic[friendnumber], data);
                }
                else
                {
                    AddNewRowToDocument(convdic[friendnumber], data);
                }
            }
            else
            {
                FlowDocument document = GetNewFlowDocument();
                convdic.Add(friendnumber, document);
                AddNewRowToDocument(convdic[friendnumber], data);
            }

            if(!(current_number == friendnumber && current_type == typeof(FriendControl)))
                GetFriendControlByNumber(friendnumber).BorderBrush = (Brush)FindResource("AccentColorBrush");

            if (current_number == friendnumber && current_type == typeof(FriendControl))
                ScrollChatBox();

            this.Flash();
        }

        public void ScrollChatBox()
        {
            ScrollViewer viewer = FindScrollViewer(ChatBox);

            if (viewer != null)
                viewer.ScrollToBottom();
        }

        public static ScrollViewer FindScrollViewer(FlowDocumentScrollViewer viewer)
        {
            if (VisualTreeHelper.GetChildrenCount(viewer) == 0)
                return null;

            DependencyObject first = VisualTreeHelper.GetChild(viewer, 0);
            if (first == null)
                return null;

            Decorator border = (Decorator)VisualTreeHelper.GetChild(first, 0);
            if (border == null)
                return null;

            return (ScrollViewer)border.Child;
        }

        private Run GetLastMessageRun(FlowDocument doc)
        {
            try
            {
                Paragraph para = (Paragraph)doc.FindChildren<TableRow>()
                    .Last()
                    .FindChildren<TableCell>()
                    .First()
                    .Blocks.FirstBlock;

                Run run = (Run)para.Inlines.FirstInline;
                return run;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private FileTransfer AddNewFTRowToDocument(FlowDocument doc, int friendnumber, int filenumber, string filename, ulong filesize)
        {
            FileTransferControl fileTransferControl = new FileTransferControl(tox.GetName(friendnumber), friendnumber, filenumber, filename, filesize);
            FileTransfer transfer = new FileTransfer() { FriendNumber = friendnumber, FileNumber = filenumber, FileName = filename, FileSize = filesize, Control = fileTransferControl };

            Section usernameParagraph = new Section();
            TableRow newTableRow = new TableRow();

            BlockUIContainer fileTransferContainer = new BlockUIContainer();
            fileTransferControl.HorizontalAlignment = HorizontalAlignment.Stretch;
            fileTransferControl.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            fileTransferContainer.Child = fileTransferControl;

            usernameParagraph.Blocks.Add(fileTransferContainer);
            usernameParagraph.Padding = new Thickness(0);

            TableCell fileTableCell = new TableCell();
            fileTableCell.ColumnSpan = 2;
            fileTableCell.Blocks.Add(usernameParagraph);
            newTableRow.Cells.Add(fileTableCell);
            fileTableCell.Padding = new Thickness(0, 10, 0, 10);

            TableRowGroup MessageRows = (TableRowGroup)doc.FindName("MessageRows");
            MessageRows.Rows.Add(newTableRow);

            return transfer;
        }

        private void AddNewRowToDocument(FlowDocument doc, MessageData data)
        {
            doc.IsEnabled = true;

            //Make a new row
            TableRow newTableRow = new TableRow();

            //Make a new cell and create a paragraph in it
            TableCell usernameTableCell = new TableCell();
            usernameTableCell.Name = "usernameTableCell";
            usernameTableCell.Padding = new Thickness(10, 0, 0, 0);

            Paragraph usernameParagraph = new Paragraph();
            usernameParagraph.Foreground = new SolidColorBrush(Color.FromRgb(164, 164, 164));
            if (data.Username != tox.GetSelfName())
            {
                usernameParagraph.SetResourceReference(Paragraph.ForegroundProperty, "AccentColorBrush"); 
            }
            usernameParagraph.Inlines.Add(data.Username);
            usernameTableCell.Blocks.Add(usernameParagraph);

            //Make a new cell and create a paragraph in it
            TableCell messageTableCell = new TableCell();
            Paragraph messageParagraph = new Paragraph();

            ProcessMessage(data, messageParagraph, false);

            //messageParagraph.Inlines.Add(fakeHyperlink);
            messageTableCell.Blocks.Add(messageParagraph);

            //Add the two cells to the row we made before
            newTableRow.Cells.Add(usernameTableCell);
            newTableRow.Cells.Add(messageTableCell);

            //Adds row to the Table > TableRowGroup
            TableRowGroup MessageRows = (TableRowGroup)doc.FindName("MessageRows");
            MessageRows.Rows.Add(newTableRow);
        }

        private void ProcessMessage(MessageData data, Paragraph messageParagraph, bool append)
        {
            List<string> urls = new List<string>();
            List<int> indices = new List<int>();
            string[] parts = data.Message.Split(' ');

            foreach (string part in parts)
            {
                if (Regex.IsMatch(part, @"([\d\w-.]+?\.(a[cdefgilmnoqrstuwz]|b[abdefghijmnorstvwyz]|c[acdfghiklmnoruvxyz]|d[ejkmnoz]|e[ceghrst]|f[ijkmnor]|g[abdefghilmnpqrstuwy]|h[kmnrtu]|i[delmnoqrst]|j[emop]|k[eghimnprwyz]|l[abcikrstuvy]|m[acdghklmnopqrstuvwxyz]|n[acefgilopruz]|om|p[aefghklmnrstwy]|qa|r[eouw]|s[abcdeghijklmnortuvyz]|t[cdfghjkmnoprtvwz]|u[augkmsyz]|v[aceginu]|w[fs]|y[etu]|z[amw]|aero|arpa|biz|com|coop|edu|info|int|gov|me|mil|museum|name|net|org|pro)(\b|\W(?<!&|=)(?!\.\s|\.{3}).*?))(\s|$)"))
                    urls.Add(part);
            }

            if (urls.Count > 0)
            {
                foreach (string url in urls)
                {
                    indices.Add(data.Message.IndexOf(url));
                    data.Message = data.Message.Replace(url, "");
                }

                if (!append)
                    messageParagraph.Inlines.Add(data.Message);
                else
                    messageParagraph.Inlines.Add("\n" + data.Message);
                
                Inline inline = messageParagraph.Inlines.LastInline;

                for (int i = indices.Count; i-- > 0; )
                {
                    string url = urls[i];
                    int index = indices[i];

                    Run run = new Run(url);
                    TextPointer pointer = inline.ContentStart;

                    Hyperlink link = new Hyperlink(run, pointer.GetPositionAtOffset(index));
                    link.IsEnabled = true;
                    link.Click += delegate(object sender, RoutedEventArgs args)
                    {
                        try { Process.Start(url); }
                        catch
                        {
                            try { Process.Start("http://" + url); }
                            catch { }
                        }
                    };
                }
            }
            else
            {
                if (!append)
                    messageParagraph.Inlines.Add(data.Message);
                else
                    messageParagraph.Inlines.Add("\n" + data.Message);
            }
        }

        private void AppendToDocument(FlowDocument doc, MessageData data)
        {
            TableRow tableRow = doc.FindChildren<TableRow>().Last();
            Paragraph para = (Paragraph)tableRow.FindChildren<TableCell>().Last().Blocks.LastBlock;

            ProcessMessage(data, para, true);
        }

        private FriendControl GetFriendControlByNumber(int friendnumber)
        {
            foreach (FriendControl control in FriendWrapper.FindChildren<FriendControl>())
                if (control.FriendNumber == friendnumber)
                    return control;

            return null;
        }

        private GroupControl GetGroupControlByNumber(int groupnumber)
        {
            foreach (GroupControl control in FriendWrapper.FindChildren<GroupControl>())
                if (control.GroupNumber == groupnumber)
                    return control;

            return null;
        }

        private void tox_OnUserStatus(int friendnumber, ToxUserStatus status)
        {
            FriendControl control = GetFriendControlByNumber(friendnumber);
            control.SetStatus(status);
        }

        private void tox_OnStatusMessage(int friendnumber, string newstatus)
        {
            FriendControl control = GetFriendControlByNumber(friendnumber);
            control.SetStatusMessage(newstatus);

            if (current_number == friendnumber && current_type == typeof(FriendControl))
                Friendstatus.Text = newstatus;
        }

        private void tox_OnNameChange(int friendnumber, string newname)
        {
            FriendControl control = GetFriendControlByNumber(friendnumber);
            control.SetUsername(newname);

            if (current_number == friendnumber && current_type == typeof(FriendControl))
                Friendname.Text = newname;
        }

        private ToxNode[] nodes = new ToxNode[] { 
            new ToxNode("192.254.75.98", 33445, "951C88B7E75C867418ACDB5D273821372BB5BD652740BCDF623A4FA293E75D2F", false),
            new ToxNode("37.187.46.132", 33445, "A9D98212B3F972BD11DA52BEB0658C326FCCC1BFD49F347F9C2D3D8B61E1B927", false),
            new ToxNode("54.199.139.199", 33445, "7F9C31FE850E97CEFD4C4591DF93FC757C7C12549DDD55F8EEAECC34FE76C029", false) 
        };

        private void InitFriends()
        {
            //Creates a new FriendControl for every friend
            foreach (int FriendNumber in tox.GetFriendlist())
                AddFriendToView(FriendNumber);
        }

        private void AddGroupToView(int groupnumber)
        {
            string groupname = string.Format("Groupchat #{0}", groupnumber);
            string groupstatus = string.Format("Peers online: {0}", tox.GetGroupMemberCount(groupnumber));

            GroupControl group = new GroupControl(groupnumber);
            group.GroupNameLabel.Content = groupname;
            group.GroupStatusLabel.Content = groupstatus;
            group.Click += group_Click;
            FriendWrapper.Children.Add(group);

            MenuItem item = new MenuItem();
            item.Header = "Delete";
            item.Click += delegate(object sender, RoutedEventArgs e)
            {
                if (group != null)
                {
                    FriendWrapper.Children.Remove(group);
                    group = null;

                    if (groupdic.ContainsKey(groupnumber))
                    {
                        groupdic.Remove(groupnumber);

                        if (current_number == groupnumber && current_type == typeof(GroupControl))
                            ChatBox.Document = null;
                    }

                    tox.DeleteGroupChat(groupnumber);
                }
            };

            group.ContextMenu = new ContextMenu();
            group.ContextMenu.Items.Add(item);
        }

        private void group_Click(object sender, RoutedEventArgs e)
        {
            GroupControl control = (GroupControl)sender;

            TypingStatusLabel.Content = "";

            if (!control.Selected)
            {
                SelectGroupControl(control);
                ScrollChatBox();
            }
        }

        private void AddFriendToView(int friendNumber)
        {
            string friendName = tox.GetName(friendNumber);
            string friendStatus;

            if (tox.GetFriendConnectionStatus(friendNumber) != 0)
            {
                friendStatus = tox.GetStatusMessage(friendNumber);
            }
            else
            {
                DateTime lastOnline = tox.GetLastOnline(friendNumber);
                friendStatus = "Last seen: " + lastOnline.ToShortDateString() + " " + lastOnline.ToLongTimeString();
            }

            if (string.IsNullOrEmpty(friendName))
                friendName = tox.GetClientID(friendNumber);

            FriendControl friend = new FriendControl(friendNumber);
            friend.FriendNameLabel.Content = friendName;
            friend.FriendStatusLabel.Content = friendStatus;
            friend.Click += friend_Click;
            FriendWrapper.Children.Add(friend);

            MenuItem item = new MenuItem();
            item.Header = "Delete";
            item.Click += delegate(object sender, RoutedEventArgs e)
            {
                if (friend != null)
                {
                    FriendWrapper.Children.Remove(friend);
                    friend = null;

                    if (convdic.ContainsKey(friendNumber))
                    {
                        convdic.Remove(friendNumber);

                        if (current_number == friendNumber && current_type == typeof(FriendControl))
                            ChatBox.Document = null;
                    }

                    tox.DeleteFriend(friendNumber);
                }
            };

            friend.ContextMenu = new ContextMenu();
            friend.ContextMenu.Items.Add(item);
        }

        private void AddFriendRequestToView(string id, string message)
        {
            string friendName = id;

            FriendControl friend = new FriendControl();
            friend.FriendNameLabel.Content = friendName;
            friend.ButtonsGrid.Visibility = Visibility.Visible;
            friend.AcceptButton.Click += (sender, e) => AcceptButton_Click(id, friend);
            friend.DeclineButton.Click += (sender, e) => DeclineButton_Click(friend);
            friend.FriendStatusLabel.Visibility = Visibility.Collapsed;
            MessageData messageData = new MessageData() { Message = message, Username = "Request Message" };
            friend.RequestFlowDocument = GetNewFlowDocument();
            friend.Click += (sender, e) => FriendRequest_Click(friend, messageData);

            NotificationWrapper.Children.Add(friend);
            if (ListViewTabControl.SelectedIndex != 1)
            {
                RequestsTabItem.Header = "Requests*";
            }
        }

        private void FriendRequest_Click(FriendControl friendControl, MessageData messageData)
        {
            AddNewRowToDocument(friendControl.RequestFlowDocument, messageData);
        }

        void AcceptButton_Click(string id, FriendControl friendControl)
        {
            int friendnumber = tox.AddFriendNoRequest(id);
            tox.SetSendsReceipts(friendnumber, true);
            AddFriendToView(friendnumber);
            NotificationWrapper.Children.Remove(friendControl);
        }

        void DeclineButton_Click(FriendControl friendControl)
        {
            NotificationWrapper.Children.Remove(friendControl);
        }

        private void SelectGroupControl(GroupControl group)
        {
            Grid grid = (Grid)group.FindName("MainGrid");
            group.Selected = true;
            grid.SetResourceReference(Grid.BackgroundProperty, "AccentColorBrush3"); 

            int friendNumber = group.GroupNumber;

            foreach (FriendControl control in FriendWrapper.FindChildren<FriendControl>())
            {
                Grid grid1 = (Grid)control.FindName("MainGrid");
                control.Selected = false;
                grid1.Background = new SolidColorBrush(Colors.White);
            }

            foreach (GroupControl control in FriendWrapper.FindChildren<GroupControl>())
            {
                if (group != control)
                {
                    Grid grid1 = (Grid)control.FindName("MainGrid");
                    control.Selected = false;
                    grid1.Background = new SolidColorBrush(Colors.White);
                }
            }

            Friendname.Text = string.Format("Groupchat #{0}", group.GroupNumber);
            Friendstatus.Text = string.Format("Peers online: {0}", tox.GetGroupMemberCount(group.GroupNumber));

            current_type = typeof(GroupControl);
            current_number = group.GroupNumber;

            if (groupdic.ContainsKey(current_number))
            {
                ChatBox.Document = groupdic[current_number];
            }
            else
            {
                FlowDocument document = GetNewFlowDocument();
                groupdic.Add(current_number, document);
                ChatBox.Document = groupdic[current_number];
            }
        }

        private void SelectFriendControl(FriendControl friend)
        {
            Grid grid = (Grid)friend.FindName("MainGrid");
            friend.Selected = true;
            //grid.Background = new SolidColorBrush(Color.FromRgb(236, 236, 236));
            grid.SetResourceReference(Grid.BackgroundProperty, "AccentColorBrush3"); 

            int friendNumber = friend.FriendNumber;

            foreach (FriendControl control in FriendWrapper.FindChildren<FriendControl>())
            {
                if (friend != control)
                {
                    Grid grid1 = (Grid)control.FindName("MainGrid");
                    control.Selected = false;
                    grid1.Background = new SolidColorBrush(Colors.White);
                }
            }

            foreach (GroupControl control in FriendWrapper.FindChildren<GroupControl>())
            {
                Grid grid1 = (Grid)control.FindName("MainGrid");
                control.Selected = false;
                grid1.Background = new SolidColorBrush(Colors.White);
            }

            Friendname.Text = tox.GetName(friendNumber);
            Friendstatus.Text = tox.GetStatusMessage(friendNumber);

            current_type = typeof(FriendControl);
            current_number = friend.FriendNumber;

            if (convdic.ContainsKey(current_number))
            {
                ChatBox.Document = convdic[current_number];
            }
            else
            {
                FlowDocument document = GetNewFlowDocument();
                convdic.Add(current_number, document);
                ChatBox.Document = convdic[current_number];
            }
        }

        private void friend_Click(object sender, RoutedEventArgs e)
        {
            FriendControl control = (FriendControl)sender;

            if (!tox.GetIsTyping(control.FriendNumber))
                TypingStatusLabel.Content = "";
            else
                TypingStatusLabel.Content = tox.GetName(control.FriendNumber) + " is typing...";

            if (!control.Selected)
            {
                SelectFriendControl(control);
                ScrollChatBox();
            }
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            tox.Save("data");
            tox.Kill();
        }

        private void OpenAddFriend_Click(object sender, RoutedEventArgs e)
        {
            FriendFlyout.IsOpen = !FriendFlyout.IsOpen;
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsUsername.Text = tox.GetSelfName();
            SettingsStatus.Text = tox.GetSelfStatusMessage();
            SettingsFlyout.IsOpen = !SettingsFlyout.IsOpen;
        }

        private void AddFriend_Click(object sender, RoutedEventArgs e)
        {
            TextRange message = new TextRange(AddFriendMessage.Document.ContentStart, AddFriendMessage.Document.ContentEnd);

            if (!(!string.IsNullOrEmpty(AddFriendID.Text) && !string.IsNullOrEmpty(message.Text)))
                return;

            if (AddFriendID.Text.Contains("@"))
            {
                try
                {
                    string id = DnsTools.DiscoverToxID(AddFriendID.Text);
                    AddFriendID.Text = id;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not find a tox id:\n" + ex.ToString());
                }

                return;
            }

            int friendnumber;
            try
            {
                friendnumber = tox.AddFriend(AddFriendID.Text, message.Text);
                FriendFlyout.IsOpen = false;
                AddFriendToView(friendnumber);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }

            AddFriendID.Text = string.Empty;
            AddFriendMessage.Document.Blocks.Clear();
            AddFriendMessage.Document.Blocks.Add(new Paragraph(new Run("Hello, I'd like to add you to my friends list.")));

            FriendFlyout.IsOpen = false;
        }

        private void Status_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tox.SetUserStatus((ToxUserStatus)Status.SelectedIndex);
        }

        private void SaveSettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            tox.SetName(SettingsUsername.Text);
            tox.SetStatusMessage(SettingsStatus.Text);

            Username.Text = SettingsUsername.Text;
            Userstatus.Text = SettingsStatus.Text;

            SettingsFlyout.IsOpen = false;

            var theme = ThemeManager.DetectAppStyle(System.Windows.Application.Current);
            var accent = ThemeManager.GetAccent(((AccentColorMenuData)AccentListBox.SelectedItem).Name);
            ThemeManager.ChangeAppStyle(System.Windows.Application.Current, accent, theme.Item1);

        }

        private void TextToSend_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (Keyboard.IsKeyDown(Key.LeftShift))
                {
                    TextToSend.Text += Environment.NewLine;
                    TextToSend.CaretIndex = TextToSend.Text.Length;
                    return;
                }

                if (e.IsRepeat)
                    return;

                string text = TextToSend.Text;

                if (string.IsNullOrEmpty(text))
                    return;

                if (tox.GetFriendConnectionStatus(current_number) == 0)
                    return;

                if (text.StartsWith("/me "))
                {
                    //action
                    string action = text.Substring(4);
                    int messageid = -1;

                    if (current_type == typeof(FriendControl))
                        messageid = tox.SendAction(current_number, action);
                    else if (current_type == typeof(GroupControl))
                        tox.SendGroupAction(current_number, action);

                    MessageData data = new MessageData() { Username = "*", Message = string.Format("{0} {1}", tox.GetSelfName(), action) };

                    if (convdic.ContainsKey(current_number))
                    {
                        AddNewRowToDocument(convdic[current_number], data);
                    }
                    else
                    {
                        FlowDocument document = GetNewFlowDocument();
                        convdic.Add(current_number, document);
                        AddNewRowToDocument(convdic[current_number], data);
                    }
                }
                else
                {
                    //regular message
                    string message = text;

                    int messageid = -1;

                    if (current_type == typeof(FriendControl))
                        messageid = tox.SendMessage(current_number, message);
                    else if (current_type == typeof(GroupControl))
                        tox.SendGroupMessage(current_number, message);

                    MessageData data = new MessageData() { Username = tox.GetSelfName(), Message = message };

                    if (convdic.ContainsKey(current_number))
                    {
                        Run run = GetLastMessageRun(convdic[current_number]);
                        if (run != null)
                        {
                            if (run.Text == data.Username)
                                AppendToDocument(convdic[current_number], data);
                            else
                                AddNewRowToDocument(convdic[current_number], data);
                        }
                        else
                        {
                            AddNewRowToDocument(convdic[current_number], data);
                        }
                    }
                    else
                    {
                        FlowDocument document = GetNewFlowDocument();
                        convdic.Add(current_number, document);
                        AddNewRowToDocument(convdic[current_number], data);
                    }
                }

                ScrollChatBox();

                TextToSend.Text = "";
                e.Handled = true;
            }
        }

        private void GithubButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Reverp/Toxy-WPF");
        }

        private void TextToSend_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = TextToSend.Text;

            if (string.IsNullOrEmpty(text))
            {
                if (typing)
                {
                    typing = false;
                    tox.SetUserIsTyping(current_number, typing);
                }
            }
            else
            {
                if (!typing)
                {
                    typing = true;
                    tox.SetUserIsTyping(current_number, typing);
                }
            }
        }

        private void CopyIDButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(tox.GetAddress());
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
            tox.SetUserStatus(ToxUserStatus.NONE);
        }

        private void AwayThumbButton_Click(object sender, EventArgs e)
        {
            tox.SetUserStatus(ToxUserStatus.AWAY);
        }

        private void BusyThumbButton_Click(object sender, EventArgs e)
        {
            tox.SetUserStatus(ToxUserStatus.BUSY);
        }

        private void ListViewTabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RequestsTabItem.IsSelected)
            {
                RequestsTabItem.Header = "Requests";
            }
        }
    }

    public class MessageData
    {
        public string Username { get; set; }
        public string Message { get; set; }
    }

    public class FileTransfer
    {
        public int FriendNumber { get; set; }
        public int FileNumber { get; set; }
        public ulong FileSize { get; set; }
        public string FileName { get; set; }
        public Stream Stream { get; set; }

        public FileTransferControl Control { get; set; }
    }
}
