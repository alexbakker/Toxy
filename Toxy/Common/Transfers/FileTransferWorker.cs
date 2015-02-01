using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Toxy.Common.Transfers
{
    static class FileTransferWorker
    {
        public static ConcurrentDictionary<int, FileSender> Senders { get; private set; }
        private static CancellationTokenSource _cancelTokenSource;

        static FileTransferWorker()
        {
            Senders = new ConcurrentDictionary<int, FileSender>();
        }

        public static void StartTransfer(FileSender sender)
        {
            if (!Senders.ContainsKey(sender.FileNumber))
                Senders.TryAdd(sender.FileNumber, sender);

            loop();
        }

        public static void PauseTransfer(FileSender sender)
        {
            KillTransfer(sender);
        }

        public static void KillTransfer(FileSender sender)
        {
            if (Senders.ContainsKey(sender.FileNumber))
                Senders.TryRemove(sender.FileNumber, out sender);
        }

        public static void ResumeTransfer(FileSender sender)
        {
            if (!Senders.ContainsKey(sender.FileNumber))
                Senders.TryAdd(sender.FileNumber, sender);

            loop();
        }

        static void loop()
        {
            if (_cancelTokenSource == null)
            {
                _cancelTokenSource = new CancellationTokenSource();

                Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        if (Senders.Count == 0 || _cancelTokenSource.IsCancellationRequested)
                        {
                            _cancelTokenSource.Dispose();
                            _cancelTokenSource = null;
                            break;
                        }

                        bool fail = false;

                        foreach (FileSender sender in Senders.Values)
                            if (!sender.Broken && !sender.Paused)
                                if (!sender.SendNextChunk())
                                    fail = true;

                        if (fail)
                            Thread.Sleep(50);
                    }
                }, _cancelTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
        }
    }
}
