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

            Context.CurrentView = new LoginExistingViewModel();
            (Context.CurrentView as LoginExistingViewModel).OnLoginButtonClicked += LoginWindow_OnLoginButtonClicked;
        }

        private void LoginWindow_OnLoginButtonClicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var model = Context.CurrentView as LoginExistingViewModel;
            if (model == null)
                return;

            if (model.SelectedProfile != null)
            {
                ProfileManager.Instance.SwitchTo(model.SelectedProfile);

                if (model.RememberChoice)
                {
                    Config.Instance.ProfilePath = model.SelectedProfile.Path;
                    Config.Instance.Save();
                }

                Close();
            }
            else
            {
                MessageBox.Show("Could not load existing profile. Unknown error.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoginWindow_OnNewProfileButtonClicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var model = Context.CurrentView as LoginNewViewModel;
            if (model == null)
                return;

            if (!string.IsNullOrEmpty(model.ProfileName))
            {
                var profile = ProfileManager.Instance.CreateNew(model.ProfileName);
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

        private void NewUser_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var model = new LoginNewViewModel();
            model.OnNewProfileButtonClicked += LoginWindow_OnNewProfileButtonClicked;
            Context.CurrentView = model;
        }

        private void ExistingUser_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var model = new LoginExistingViewModel();
            model.OnLoginButtonClicked += LoginWindow_OnLoginButtonClicked;
            Context.CurrentView = model;
        }
    }
}
