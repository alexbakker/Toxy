using System;
using System.Windows;
using System.Windows.Controls;
using Toxy.ViewModels;

namespace Toxy.Views
{
    /// <summary>
    /// Interaction logic for MessageView.xaml
    /// </summary>
    public partial class MessageView : UserControl
    {
        public MessageViewModel Context { get { return DataContext as MessageViewModel; } }

        public MessageView()
        {
            InitializeComponent();
        }

        private void CopyMessage_Click(object sender, RoutedEventArgs e)
        {
            string message = string.Format("[{0}] {1}: {2}", Context.Time, Context.FriendName, Context.Message);
            Clipboard.SetText(message);
        }

        private void DeleteMessage_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
