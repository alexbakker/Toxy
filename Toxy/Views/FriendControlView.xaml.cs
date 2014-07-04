using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using SharpTox.Core;

namespace Toxy.Views
{

    /// <summary>
    /// Interaction logic for FriendControlView.xaml
    /// </summary>
    public partial class FriendControlView : UserControl
    {
        public event RoutedEventHandler Click;
        public event RoutedEventHandler FocusTextBox;

        public FlowDocument RequestFlowDocument;
        public bool Selected = false;
        public readonly int FriendNumber;

        public FriendControlView()
        {
            InitializeComponent();
        }

        public FriendControlView(int friendnumber = 0)
            : this()
        {
            FriendNumber = friendnumber;
        }

        public void SetStatus(ToxUserStatus newStatus)
        {
            switch (newStatus)
            {
                case ToxUserStatus.NONE:
                    StatusRectangle.Fill = new SolidColorBrush(Color.FromRgb(6, 225, 1));
                    break;

                case ToxUserStatus.BUSY:
                    StatusRectangle.Fill = new SolidColorBrush(Color.FromRgb(214, 43, 79));
                    break;

                case ToxUserStatus.AWAY:
                    StatusRectangle.Fill = new SolidColorBrush(Color.FromRgb(229, 222, 31));
                    break;

                case ToxUserStatus.INVALID:
                    StatusRectangle.Fill = new SolidColorBrush(Colors.Red);
                    break;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            NewMessageIndicator.Fill = null;
            if (this.Selected)
                FocusTextBox(this, e);
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
