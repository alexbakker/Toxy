using System;
using System.Windows.Input;
using Toxy.MVVM;
using Toxy.Windows;

namespace Toxy.ViewModels
{
    public class LoginNewViewModel : ViewModelBase
    {
        public string ProfileName { get; set; }
        public string Password { get; set; }

        public event MouseButtonEventHandler OnNewProfileButtonClicked;

        public void RaiseButtonClicked(object sender, MouseButtonEventArgs e)
        {
            if (OnNewProfileButtonClicked != null)
                OnNewProfileButtonClicked(sender, e);
        }
    }
}
