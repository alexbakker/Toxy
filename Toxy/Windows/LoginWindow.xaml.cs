using System;
using System.Windows;
using Toxy.Managers;
using Toxy.ViewModels;

namespace Toxy.Windows
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindowViewModel Context { get { return DataContext as LoginWindowViewModel; } }

        public LoginWindow()
        {
            InitializeComponent();
            DataContext = new LoginWindowViewModel();
        }

        private void NewUser_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Context.IsLoginExistingSelected = false;
        }

        private void ExistingUser_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Context.IsLoginExistingSelected = true;
        }

        private void CreateProfile_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Context.ProfileName))
            {
                var profile = ProfileManager.Instance.CreateNew(Context.ProfileName);
                if (profile != null)
                {
                    ProfileManager.Instance.SwitchTo(profile);
                    Close();
                }
                else
                {
                    MessageBox.Show("Could not create a new profile. Unknown error.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Login_Clicked(object sender, RoutedEventArgs e)
        {
            if (Context.SelectedProfile != null)
            {
                ProfileManager.Instance.SwitchTo(Context.SelectedProfile);

                if (Context.RememberChoice)
                {
                    Config.Instance.ProfilePath = Context.SelectedProfile.Path;
                    Config.Instance.Save();
                }

                Close();
            }
            else
            {
                MessageBox.Show("Could not load existing profile. Unknown error.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
