using System;
using System.Net;
using System.Windows.Forms;

namespace Toxy.Updater
{
    public class GUI : IUpdateGui
    {
        public event EventHandler ConfirmDownload;
        public event EventHandler AbortDownload;

        private Win32ProgressDialog _dialog;
        public void AskUserToDownload(object sender, EventArgs e)
        {
            var result =
                    MessageBox.Show("Could not find Toxy in this directory. Do you want to download the latest version?",
                        "Toxy not found", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                OnConfirmDownload();
            }
            else
            {
                OnAbortDownload();
            }
            
        }

        protected virtual void OnConfirmDownload()
        {
            var handler = ConfirmDownload;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        protected virtual void OnAbortDownload()
        {
            var handler = AbortDownload;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public void DownloadStarted(object sender, EventArgs e){
            UpdateStartEventArgs ev = (UpdateStartEventArgs) e;
            _dialog = new Win32ProgressDialog();
            _dialog.Title = "Toxy Updater";
            _dialog.Line1 = "Updating...";
            _dialog.Line2 = "Downloading update";
            _dialog.Line3 = ev.Uri;
            _dialog.ShowDialog(Win32ProgressDialog.PROGDLG.Normal);
        }

        public void ErrorOccurred(object sender, EventArgs eventArgs)
        {
            ErrorEventArgs e = (ErrorEventArgs) eventArgs;
            ShowError(e.ErrorMessage);
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public void DownloadstatusChanged(object sender, EventArgs e)
        {
            DownloadProgressChangedEventArgs ev = (DownloadProgressChangedEventArgs) e;
            if (_dialog != null)
            {
                _dialog.SetProgress(ev.ProgressPercentage);
            }
        }

        public void Extracting(object sender, EventArgs e)
        {
            if (_dialog != null)
            {
                _dialog.Line2 = "Extracting";    
            }
        }
    }
}
