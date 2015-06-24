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
            if (Context.Friend.IsCalling)
            {
                //answer the call
                if (CallManager.Get().Answer(Context.Friend.ChatNumber, false))
                {
                    Context.Friend.IsCalling = false;
                    Context.Friend.IsRinging = false;
                    Context.Friend.IsInVideoCall = false;
                    Context.Friend.IsCallInProgress = true;
                }
            }
            else if (Context.Friend.IsCallInProgress || Context.Friend.IsRinging)
            {
                //hang up
                if (CallManager.Get().Hangup(Context.Friend.ChatNumber))
                {
                    Context.Friend.IsCalling = false;
                    Context.Friend.IsRinging = false;
                    Context.Friend.IsInVideoCall = false;
                    Context.Friend.IsCallInProgress = false; //or should we set this once we receive the first callstate change? hmmm
                }
            }
            else
            {
                //send call request
                if (CallManager.Get().SendRequest(Context.Friend.ChatNumber, false))
                {
                    Context.Friend.IsRinging = true;
                    Context.Friend.IsCalling = false;
                    Context.Friend.IsInVideoCall = false;
                    Context.Friend.IsCallInProgress = false;
                }
            }
        }

        private void ButtonVideo_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Context.Friend.IsCalling)
            {
                //answer the call
                if (CallManager.Get().Answer(Context.Friend.ChatNumber, true))
                {
                    Context.Friend.IsCalling = false;
                    Context.Friend.IsRinging = false;
                    Context.Friend.IsInVideoCall = true;
                    Context.Friend.IsCallInProgress = true;
                }
            }
            else if (Context.Friend.IsCallInProgress || Context.Friend.IsRinging)
            {
                //toggle video
                CallManager.Get().ToggleVideo(!Context.Friend.IsInVideoCall);
                Context.Friend.IsInVideoCall = !Context.Friend.IsInVideoCall;
            }
            else
            {
                //send call request
                if (CallManager.Get().SendRequest(Context.Friend.ChatNumber, true))
                {
                    Context.Friend.IsRinging = true;
                    Context.Friend.IsCalling = false;
                    Context.Friend.IsInVideoCall = true;
                    Context.Friend.IsCallInProgress = false;
                }
            }
        }
    }
}
