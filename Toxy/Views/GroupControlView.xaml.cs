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
using Toxy.Windows;

namespace Toxy.Views
{
    /// <summary>
    /// Interaction logic for GroupControlView.xaml
    /// </summary>
    public partial class GroupControlView : UserControl
    {
        public GroupControlViewModel Context { get { return DataContext as GroupControlViewModel; } }

        public GroupControlView()
        {
            InitializeComponent();
        }

        private void OpenInWindow_Click(object sender, RoutedEventArgs e)
        {
            if (Context.ConversationView.Window != null)
            {
                if (Context.ConversationView.Window.WindowState == WindowState.Minimized)
                    Context.ConversationView.Window.WindowState = WindowState.Normal;
                else
                    Context.ConversationView.Window.Activate();

                return;
            }

            var window = new ConversationWindow(Context.ConversationView);

            Context.ConversationView.Window = window;
            MainWindow.Instance.AddChildWindow(window);

            window.Show();
        }

        private void LeaveGroup_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(string.Format("Are you sure you want to leave {0}?", Context.Name), "Leave group", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            if (!(MainWindow.Instance.ViewModel.CurrentFriendListView.RemoveObject(Context) && ProfileManager.Instance.Tox.DeleteGroupChat(Context.ChatNumber)))
            {
                Debugging.Write("Could not remove group");
                return;
            }
        }

        private void ClearScrollback_Click(object sender, RoutedEventArgs e)
        {
            Context.ConversationView.Messages.Clear();
        }
    }
}
