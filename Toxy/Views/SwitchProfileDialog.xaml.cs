using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using Microsoft.Win32;

namespace Toxy.Views
{
    /// <summary>
    /// Interaction logic for SwitchProfileDialog.xaml
    /// </summary>
    public partial class SwitchProfileDialog : BaseMetroDialog
    {
        public SwitchProfileDialog(string[] profiles, MetroWindow parentWindow)
            : this(profiles, parentWindow, null)
        {
        }

        public SwitchProfileDialog(string[] profiles, MetroWindow parentWindow, MetroDialogSettings settings)
            : base(parentWindow, settings)
        {
            InitializeComponent();

            foreach (string profile in profiles)
                PART_ProfileComboBox.Items.Add(profile);
        }

        public Task<string> WaitForButtonPressAsync()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                this.Focus();
                PART_ProfileComboBox.Focus();
            }));

            var tcs = new TaskCompletionSource<string>();

            RoutedEventHandler negativeHandler = null;
            KeyEventHandler negativeKeyHandler = null;

            RoutedEventHandler affirmativeHandler = null;
            KeyEventHandler affirmativeKeyHandler = null;

            RoutedEventHandler importHandler = null; 

            KeyEventHandler escapeKeyHandler = null;

            Action cleanUpHandlers = () =>
            {
                this.KeyDown -= escapeKeyHandler;

                PART_NegativeButton.Click -= negativeHandler;
                PART_AffirmativeButton.Click -= affirmativeHandler;

                PART_NegativeButton.KeyDown -= negativeKeyHandler;
                PART_AffirmativeButton.KeyDown -= affirmativeKeyHandler;
            };

            escapeKeyHandler = (sender, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    cleanUpHandlers();

                    tcs.TrySetResult(null);
                }
            };

            negativeKeyHandler = (sender, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    cleanUpHandlers();

                    tcs.TrySetResult(null);
                }
            };

            affirmativeKeyHandler = (sender, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    cleanUpHandlers();

                    tcs.TrySetResult(Input);
                }
            };

            negativeHandler = (sender, e) =>
            {
                cleanUpHandlers();

                tcs.TrySetResult(null);

                e.Handled = true;
            };

            affirmativeHandler = (sender, e) =>
            {
                cleanUpHandlers();

                tcs.TrySetResult(Input);

                e.Handled = true;
            };

            importHandler = (sender, e) =>
            {
                /*OpenFileDialog dialog = new OpenFileDialog();
                dialog.InitialDirectory = Environment.CurrentDirectory;
                dialog.Multiselect = false;
                if (dialog.ShowDialog() != true)
                    return;

                string fileName = dialog.FileName;
                if (string.IsNullOrEmpty(fileName))
                    return;*/

                //show the 'give this profile a name' dialog

                cleanUpHandlers();

                tcs.TrySetResult(Input);

                e.Handled = true;
            };

            PART_NegativeButton.KeyDown += negativeKeyHandler;
            PART_AffirmativeButton.KeyDown += affirmativeKeyHandler;

            this.KeyDown += escapeKeyHandler;

            PART_NegativeButton.Click += negativeHandler;
            PART_AffirmativeButton.Click += affirmativeHandler;
            PART_ImportButton.Click += importHandler;

            return tcs.Task;
        }

        private void Dialog_Loaded(object sender, RoutedEventArgs e)
        {
            switch (this.DialogSettings.ColorScheme)
            {
                case MetroDialogColorScheme.Accented:
                    this.PART_NegativeButton.Style = this.FindResource("HighlightedSquareButtonStyle") as Style;
                    PART_ProfileComboBox.SetResourceReference(ForegroundProperty, "BlackColorBrush");
                    break;
            }
        }
        public static readonly DependencyProperty InputProperty = DependencyProperty.Register("Input", typeof(string), typeof(InputDialog), new PropertyMetadata(default(string)));

        public string Input
        {
            get { return (string)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }
    }
}
