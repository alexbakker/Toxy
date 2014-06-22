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

namespace Toxy
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
