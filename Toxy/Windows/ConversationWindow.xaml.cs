using System;
using System.Windows;
using Toxy.ViewModels;
using Toxy.Extensions;

namespace Toxy.Windows
{
    /// <summary>
    /// Interaction logic for ConversationWindow.xaml
    /// </summary>
    public partial class ConversationWindow : Window
    {
        public ConversationWindowViewModel Context { get { return DataContext as ConversationWindowViewModel; } }

        public ConversationWindow(IConversationView model)
        {
            InitializeComponent();
            DataContext = new ConversationWindowViewModel(model);

            this.FixBackground();
        }
    }
}
