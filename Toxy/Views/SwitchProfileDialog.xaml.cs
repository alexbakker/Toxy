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

        public Task<SwitchProfileResult> WaitForButtonPressAsync()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                this.Focus();
                PART_ProfileComboBox.Focus();
            }));

            var tcs = new TaskCompletionSource<SwitchProfileResult>();

            RoutedEventHandler negativeHandler = null;
            KeyEventHandler negativeKeyHandler = null;

            RoutedEventHandler affirmativeHandler = null;
            KeyEventHandler affirmativeKeyHandler = null;

            RoutedEventHandler importHandler = null;
            RoutedEventHandler newProfileHandler = null; 

            KeyEventHandler escapeKeyHandler = null;

            Action cleanUpHandlers = () =>
            {
                this.KeyDown -= escapeKeyHandler;

                PART_NegativeButton.Click -= negativeHandler;
                PART_AffirmativeButton.Click -= affirmativeHandler;
                PART_NewProfileButton.Click -= newProfileHandler;

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

                    tcs.TrySetResult(new SwitchProfileResult() { Input = this.Input, Result = SwitchProfileDialogResult.OK });
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

                tcs.TrySetResult(new SwitchProfileResult() { Input = this.Input, Result = SwitchProfileDialogResult.OK });

                e.Handled = true;
            };

            importHandler = (sender, e) =>
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.InitialDirectory = Environment.CurrentDirectory;
                dialog.Multiselect = false;
                if (dialog.ShowDialog() != true)
                    return;

                string fileName = dialog.FileName;
                if (string.IsNullOrEmpty(fileName))
                    return;

                cleanUpHandlers();

                tcs.TrySetResult(new SwitchProfileResult() { Input = fileName, Result = SwitchProfileDialogResult.Import });

                e.Handled = true;
            };

            newProfileHandler = (sender, e) =>
            {
                cleanUpHandlers();

                tcs.TrySetResult(new SwitchProfileResult() { Input = string.Empty, Result = SwitchProfileDialogResult.New });

                e.Handled = true;
            };

            PART_NegativeButton.KeyDown += negativeKeyHandler;
            PART_AffirmativeButton.KeyDown += affirmativeKeyHandler;

            this.KeyDown += escapeKeyHandler;

            PART_NegativeButton.Click += negativeHandler;
            PART_AffirmativeButton.Click += affirmativeHandler;
            PART_ImportButton.Click += importHandler;
            PART_NewProfileButton.Click += newProfileHandler;

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

        public static readonly DependencyProperty InputProperty = DependencyProperty.Register("Input", typeof(string), typeof(SwitchProfileDialog), new PropertyMetadata(default(string)));

        public string Input
        {
            get { return (string)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }
    }

    public class SwitchProfileResult
    {
        public string Input { get; set; }
        public SwitchProfileDialogResult Result { get; set; }
    }

    public enum SwitchProfileDialogResult
    {
        OK,
        Import,
        New
    }
}
