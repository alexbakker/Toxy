using System;
using System.Windows.Controls;
using System.Windows.Input;
using Toxy.ViewModels;

namespace Toxy.Views
{
    /// <summary>
    /// Interaction logic for LoginNewView.xaml
    /// </summary>
    public partial class LoginNewView : UserControl
    {
        public LoginNewViewModel Context { get { return DataContext as LoginNewViewModel; } }

        public LoginNewView()
        {
            InitializeComponent();
        }

        private void CreateProfile_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //no, apparently a binding doesn't work here..
            Context.ProfileName = TextBoxProfileName.Text;
            Context.RaiseButtonClicked(sender, e);
        }
    }
}
