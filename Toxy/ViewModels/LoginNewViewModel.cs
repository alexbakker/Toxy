using System;
using System.Windows;
using System.Windows.Input;
using Toxy.MVVM;
using Toxy.Windows;

namespace Toxy.ViewModels
{
    public class LoginNewViewModel : ViewModelBase
    {
        public string ProfileName { get; set; }
        public string Password { get; set; }

        public event RoutedEventHandler OnNewProfileButtonClicked;

        public void RaiseButtonClicked(object sender, RoutedEventArgs e)
        {
            if (OnNewProfileButtonClicked != null)
                OnNewProfileButtonClicked(sender, e);
        }
    }
}
