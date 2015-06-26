using System;
using System.Windows.Controls;
using System.Windows.Input;
using Toxy.ViewModels;

namespace Toxy.Views
{
    /// <summary>
    /// Interaction logic for LoginExistingView.xaml
    /// </summary>
    public partial class LoginExistingView : UserControl
    {
        public LoginExistingViewModel Context { get { return DataContext as LoginExistingViewModel; } }

        public LoginExistingView()
        {
            InitializeComponent();
        }

        private void Login_Clicked(object sender, System.Windows.RoutedEventArgs e)
        {
            Context.RaiseButtonClicked(sender, e);
        }
    }
}
