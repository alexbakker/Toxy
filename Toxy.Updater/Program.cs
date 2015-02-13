using System;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Toxy.Updater
{
    internal class Program
    {
        private static bool _finish;

        private static void Main(string[] args)
        {
            var updateParamteterDescription = ProcessArguments(args);
            var updateManger = new Logic(updateParamteterDescription);
            var updateGui = new GUI();

            updateManger.DownloadAvaible += updateGui.AskUserToDownload;
            updateManger.StartDownloading += updateGui.DownloadStarted;
            updateManger.ErrorOccurred += updateGui.ErrorOccurred;
            updateManger.DownloadStatusChanged += updateGui.DownloadstatusChanged;
            updateManger.Extracting += updateGui.Extracting;
            updateManger.Finish += Finished;

            updateGui.ConfirmDownload += updateManger.StartDownload;
            updateGui.AbortDownload += Finished;

            updateManger.Update();

            // TODO: DIRTY
            while (!_finish)
            {
                Thread.Sleep(100);
            }
        }

        private static void Finished(object sender, EventArgs eventArgs)
        {
            _finish = true;
        }


        /// <summary>
        /// parse the commandlinearguments
        /// </summary>
        /// <param name="args">string to parse</param>
        /// <returns>UpdateDescription</returns>
        private static UpdateParameterDescription ProcessArguments(string[] args)
        {
            var description = new UpdateParameterDescription();

            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "/force":
                    case "/f":
                        description.ForceUpdate = true;
                        break;
                    case "/nightly":
                        description.ForceNightly = true;
                        break;
                }
            }

            return description;
        }
    }
}