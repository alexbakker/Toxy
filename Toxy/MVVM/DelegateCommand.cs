using System;
using System.Windows.Input;

namespace Toxy.MVVM
{
    public class DelegateCommand : ICommand
    {
        private readonly Func<bool> _canExecuteMethod;
        private readonly Action _executeMethod;

        public event EventHandler CanExecuteChanged;

        public DelegateCommand(Action executeMethod)
            : this(executeMethod, null)
        { }

        public DelegateCommand(Action executeMethod, Func<bool> canExecuteMethod)
        {
            _executeMethod = executeMethod;
            _canExecuteMethod = canExecuteMethod;
        }

        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute();
        }

        void ICommand.Execute(object parameter)
        {
            Execute();
        }

        public bool CanExecute()
        {
            if (_canExecuteMethod != null)
                return _canExecuteMethod();

            return true;
        }

        public void Execute()
        {
            if (_executeMethod != null)
                _executeMethod();
        }
    }

    public class DelegateCommand<T> : ICommand
    {
        private readonly Func<T, bool> _canExecuteMethod;
        private readonly Action<T> _executeMethod;

        public event EventHandler CanExecuteChanged;

        public DelegateCommand(Action<T> executeMethod)
            : this(executeMethod, null)
        { }

        public DelegateCommand(Action<T> executeMethod, Func<T, bool> canExecuteMethod)
        {
            _executeMethod = executeMethod;
            _canExecuteMethod = canExecuteMethod;
        }

        public bool CanExecute(T parameter)
        {
            if (_canExecuteMethod != null)
                return _canExecuteMethod(parameter);

            return true;
        }

        public void Execute(T parameter)
        {
            if (_executeMethod != null)
                _executeMethod(parameter);
        }

        bool ICommand.CanExecute(object parameter)
        {
            if (parameter == null && typeof(T).IsValueType)
                return _canExecuteMethod == null;

            if (parameter is T)
                return CanExecute((T)parameter);

            return CanExecute(default(T));
        }

        void ICommand.Execute(object parameter)
        {
            if (parameter is T)
                Execute((T)parameter);
            else
                Execute(default(T));
        }
    }
}
