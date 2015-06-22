using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Toxy.Managers;
using Toxy.ViewModels;
using Toxy.Extensions;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;

namespace Toxy.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl
    {
        public SettingsViewModel Context { get { return DataContext as SettingsViewModel; } }

        private AudioEngine _audioEngine;
        private VideoEngine _videoEngine;

        public SettingsView()
        {
            InitializeComponent();
        }

        private void CopyIDButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(ProfileManager.Instance.Tox.Id.ToString());
        }

        private void LoadProfile_Click(object sender, RoutedEventArgs e)
        {
            if (Context.SelectedProfile == null)
                return;

            try
            {
                ProfileManager.Instance.SwitchTo(Context.SelectedProfile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error while trying to load profile", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportProfile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.InitialDirectory = ProfileManager.ProfileDataPath;
            dialog.Filter = "Tox Profiles|*.tox"; //TODO: support 'all files' in case the profile doesn't have a .tox extension
            dialog.Multiselect = false;

            if (dialog.ShowDialog() != true)
                return;

            ProfileInfo profile;

            //is this file already in the profile directory?
            if (!Directory.GetFiles(ProfileManager.ProfileDataPath).Contains(dialog.FileName))
            {
                //check whether or not we already have a profile with that name
                var tempProfile = new ProfileInfo(dialog.FileName);
                var path = Path.Combine(ProfileManager.ProfileDataPath, tempProfile.FileName);

                if (File.Exists(path))
                {
                    //TODO: auto rename the file?
                    MessageBox.Show("Could not copy the profile to the profile directory. A file with the same name already exists", "Error while importing profile", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                //copy the profile to the profile directory (or should we move the file? hmm)
                try { File.Move(dialog.FileName, path); }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not copy the profile to the profile directory: " + ex.Message, "Error while importing profile", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                profile = new ProfileInfo(path);

                //add the profile to the list
                Context.AddProfile(profile);
            }
            else
            {
                profile = new ProfileInfo(dialog.FileName);
            }

            var result = MessageBox.Show(profile.FileName + " has successfully been imported, do you want to switch to this profile?", "Profile imported", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                ProfileManager.Instance.SwitchTo(profile);
            }
            catch (Exception ex) 
            {
                MessageBox.Show(ex.Message, "Error while trying to load profile", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SettingTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabItemAudioVideo.IsSelected)
            {
                if (_audioEngine != null)
                    _audioEngine.Dispose();

                if (_videoEngine != null)
                    _videoEngine.Dispose();

                _audioEngine = new AudioEngine();
                _audioEngine.OnMicVolumeChanged += AudioEngine_OnMicVolumeChanged;
                _audioEngine.StartRecording();

                _videoEngine = new VideoEngine();
                _videoEngine.OnFrameAvailable += VideoEngine_OnFrameAvailable;
                _videoEngine.StartRecording();
            }
            else
            {
                if (_audioEngine != null)
                {
                    _audioEngine.Dispose();
                    _audioEngine = null;
                }

                if (_videoEngine != null)
                {
                    _videoEngine.Dispose();
                    _videoEngine = null;
                }
            }
        }

        private void VideoEngine_OnFrameAvailable(Bitmap frame)
        {
            //TODO: move this whole process to an extension method
            //TODO: edit aforge source code to create a bitmapsource direcrtly instead of converting (?)
            using (var stream = new MemoryStream())
            {
                frame.Save(stream, ImageFormat.Bmp);
                stream.Position = 0;
                frame.Dispose();

                var bitmapImg = new BitmapImage();
                bitmapImg.BeginInit();
                bitmapImg.StreamSource = stream;
                bitmapImg.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImg.EndInit();
                bitmapImg.Freeze();

                this.UInvoke(() => CurrentFrame.Source = bitmapImg);
            }
        }

        private void AudioEngine_OnMicVolumeChanged(float volume)
        {
            this.UInvoke(() => ProgressBarRecord.Value = volume * 100);
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            /* TODO */
        }

        private void VideoProperties_Click(object sender, RoutedEventArgs e)
        {
            var handle = new System.Windows.Interop.WindowInteropHelper(MainWindow.Instance).Handle;
            if (handle == IntPtr.Zero || !_videoEngine.DisplayPropertyWindow(handle))
                MessageBox.Show("There is no property window available for this webcam", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
