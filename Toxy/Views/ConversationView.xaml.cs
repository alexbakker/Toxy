using System;
using System.Windows.Controls;
using SharpTox.Core;
using SharpTox.Av;
using Toxy.ViewModels;
using Toxy.Extensions;
using System.Windows.Input;
using Toxy.Managers;

namespace Toxy.Views
{
    /// <summary>
    /// Interaction logic for ConversationView.xaml
    /// </summary>
    public partial class ConversationView : UserControl
    {
        private bool _autoScroll;

        public ConversationViewModel Context { get { return DataContext as ConversationViewModel; } }

        public ConversationView()
        {
            InitializeComponent();
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

            var chatNumber = Context.Friend.ChatNumber;
            if (!ProfileManager.Instance.Tox.IsFriendOnline(chatNumber))
                return;

            var model = new MessageViewModel(-1);
            model.FriendName = ProfileManager.Instance.Tox.Name;
            model.Time = DateTime.Now.ToShortTimeString();

            if (text.StartsWith("/me "))
            {
                //action
                string action = text.Substring(4);
                int messageid = ProfileManager.Instance.Tox.SendMessage(chatNumber, action, ToxMessageType.Action);

                model.Message = action;
                model.MessageId = messageid;
                Context.AddMessage(model);
            }
            else
            {
                //regular message
                //foreach (string message in text.WordWrap(ToxConstants.MaxMessageLength))
                //{
                int messageid = ProfileManager.Instance.Tox.SendMessage(chatNumber, text, ToxMessageType.Message);


                model.Message = text;
                model.MessageId = messageid;
                Context.AddMessage(model);
                //}
            }

            //ScrollChatBox();
            TextBoxEnteredText.Text = string.Empty;
        }

        private void Call_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if ((Context.Friend.CallState & CallState.Calling) != 0)
            {
                //answer the call
                if (CallManager.Get().Answer(Context.Friend.ChatNumber, false))
                {
                    Context.Friend.CallState = CallState.InProgress;
                }
            }
            else if ((Context.Friend.CallState & CallState.InProgress) != 0 || (Context.Friend.CallState & CallState.Ringing) != 0)
            {
                //hang up
                if (CallManager.Get().Hangup(Context.Friend.ChatNumber))
                {
                    Context.Friend.CallState = CallState.None;
                }
            }
            else
            {
                //send call request
                if (CallManager.Get().SendRequest(Context.Friend.ChatNumber, false))
                {
                    Context.Friend.CallState = CallState.Ringing;
                }
            }
        }

        private void ButtonVideo_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if ((Context.Friend.CallState & CallState.Calling) != 0)
            {
                //answer the call
                if (CallManager.Get().Answer(Context.Friend.ChatNumber, true))
                {
                    Context.Friend.CallState = CallState.ReceivingVideo | CallState.InProgress;
                }
            }
            else if ((Context.Friend.CallState & CallState.InProgress) != 0)
            {
                //toggle video
                CallManager.Get().ToggleVideo(!Context.Friend.CallState.HasFlag(CallState.ReceivingVideo));
                Context.Friend.CallState = Context.Friend.CallState ^ CallState.ReceivingVideo;
            }
            else if ((Context.Friend.CallState & CallState.Ringing) == 0)
            {
                //send call request
                if (CallManager.Get().SendRequest(Context.Friend.ChatNumber, true))
                {
                    Context.Friend.CallState = CallState.Ringing | CallState.ReceivingVideo;
                }
            }
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
    }
}
