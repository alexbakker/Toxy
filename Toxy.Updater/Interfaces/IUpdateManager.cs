using System;

namespace Toxy.Updater
{
    internal interface IUpdateManager
    {
        event EventHandler Finish;
        void Update();
    }
}