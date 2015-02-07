using System;

namespace Toxy.Updater
{
    public interface IUpdateGui
    {
        event EventHandler ConfirmDownload;
        event EventHandler AbortDownload;
    }
}