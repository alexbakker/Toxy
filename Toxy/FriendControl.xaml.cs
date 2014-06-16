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
using SharpTox;

namespace Toxy
{

    /// <summary>
    /// Interaction logic for FriendControl.xaml
    /// </summary>
    public partial class FriendControl : UserControl
    {
        public event RoutedEventHandler Click;

        public bool Selected = false;
        public readonly int FriendNumber;

        public FriendControl(int friendnumber = 0)
        {
            FriendNumber = friendnumber;

            InitializeComponent();
        }

        public void SetStatus(ToxUserStatus newStatus)
        {
            switch (newStatus)
            {
                case ToxUserStatus.NONE:
                    this.BorderBrush = new SolidColorBrush(Color.FromRgb(6, 225, 1));
                    break;

                case ToxUserStatus.BUSY:
                    this.BorderBrush = new SolidColorBrush(Color.FromRgb(214, 43, 79));
                    break;

                case ToxUserStatus.AWAY:
                    this.BorderBrush = new SolidColorBrush(Color.FromRgb(229, 222, 31));
                    break;

                case ToxUserStatus.INVALID:
                    this.BorderBrush = new SolidColorBrush(Colors.Red);
                    break;
            }
        }

        public void SetStatusMessage(string newStatusMessage)
        {
            FriendStatusLabel.Content = newStatusMessage;
        }

        public void SetUsername(string newUsername)
        {
            FriendNameLabel.Content = newUsername;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Click != null)
                Click(this, e);
        }
    }
}
