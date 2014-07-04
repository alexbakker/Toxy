using System.Windows;
using System.Windows.Controls;

namespace Toxy.Views
{
    /// <summary>
    /// Interaction logic for CallControl.xaml
    /// </summary>
    public partial class CallControl : UserControl
    {
        public event RoutedEventHandler Click;

        public CallControl()
        {
            InitializeComponent();
        }

        public void SetLabel(string username)
        {
            PartnerLabel.Content = username;
        }

        private void HangupButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
