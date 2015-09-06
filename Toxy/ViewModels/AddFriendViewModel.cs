using System;
using Toxy.MVVM;

namespace Toxy.ViewModels
{
    public class AddFriendViewModel : ViewModelBase, IView
    {
        public string Title
        {
            get { return string.Format(BuildInfo.TitleFormat, System.Reflection.Assembly.GetExecutingAssembly().GetName().Version, "Add a friend"); }
        }
    }
}
