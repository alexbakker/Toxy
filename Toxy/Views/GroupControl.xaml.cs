using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Toxy.Views
{

    /// <summary>
    /// Interaction logic for FriendControl.xaml
    /// </summary>
    public partial class GroupControl : UserControl
    {
        public event RoutedEventHandler Click;

        public bool Selected = false;
        public readonly int GroupNumber;

        public GroupControl(int groupnumber)
        {
            GroupNumber = groupnumber;

            InitializeComponent();
        }

        public void SetName(string name)
        {
            GroupNameLabel.Content = name;
        }

        public void SetStatusMessage(string newStatusMessage)
        {
            GroupStatusLabel.Content = newStatusMessage;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            NewMessageIndicator.Fill = null;
            if (Click != null)
                Click(this, e);
        }

        private void HackButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!Selected)
                MainGrid.SetResourceReference(Grid.BackgroundProperty, "AccentColorBrush4");
        }

        private void HackButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!Selected)
                MainGrid.Background = null;
        }
    }
}
