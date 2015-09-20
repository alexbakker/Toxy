using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Toxy.Managers;
using Toxy.ViewModels;

namespace Toxy.Views
{
    /// <summary>
    /// Interaction logic for GroupConversationView.xaml
    /// </summary>
    public partial class GroupConversationView : UserControl
    {
        public GroupConversationViewModel Context { get { return DataContext as GroupConversationViewModel; } }

        private bool _autoScroll;

        public GroupConversationView()
        {
            InitializeComponent();
        }

        private void Call_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ScrollbackViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer == null)
                return;

            if (e.ExtentHeightChange == 0)
                _autoScroll = scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight;

            if (_autoScroll && e.ExtentHeightChange != 0)
                scrollViewer.ScrollToVerticalOffset(scrollViewer.ExtentHeight);
        }

        private void TextBoxEnteredText_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var text = TextBoxEnteredText.Text;

            if (e.Key == Key.Enter)
            {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    return;

                if (e.IsRepeat)
                    return;

                SendMessage(text);
                e.Handled = true;
            }
        }

        private void ButtonSendMessage_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SendMessage(TextBoxEnteredText.Text);
        }

        private void SendMessage(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            var chatNumber = Context.Group.ChatNumber;

            if (text.StartsWith("/me "))
            {
                //action
                string action = text.Substring(4);
                if (!ProfileManager.Instance.Tox.SendGroupAction(chatNumber, action))
                {
                    Debugging.Write("Could not send action to group");
                    return;
                }
            }
            else
            {
                if (!ProfileManager.Instance.Tox.SendGroupMessage(chatNumber, text))
                {
                    Debugging.Write("Could not send message to group");
                    return;
                }
            }

            TextBoxEnteredText.Text = string.Empty;
        }

        private void PeerCopyPublicKey_Click(object sender, RoutedEventArgs e)
        {
            var peer = (e.Source as MenuItem)?.DataContext as GroupPeer;
            if (peer == null)
                return;

            Clipboard.SetText(ProfileManager.Instance.Tox.GetGroupPeerPublicKey(Context.ChatObject.ChatNumber, peer.PeerNumber).ToString());
        }
    }
}
