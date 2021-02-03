namespace PxCT
{
    using System;
    using System.Windows.Input;

    public class DelegateCommand : ICommand
    {
        #region Fields

        private readonly Predicate<object> _canExecute;

        private readonly Action<object> _execute;

        #endregion

        public DelegateCommand(Action<object> execute)
            : this(execute, null)
        {
        }

        public DelegateCommand(Action<object> execute,
            Predicate<object> canExecute)
        {
            this._execute = execute;
            this._canExecute = canExecute;
        }

        #region Delegates & Events

        public event EventHandler CanExecuteChanged;

        #endregion

        #region Methods

        public bool CanExecute(object parameter)
        {
            if (this._canExecute == null)
            {
                return true;
            }

            return this._canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            this._execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            if (this.CanExecuteChanged != null)
            {
                this.CanExecuteChanged(this, EventArgs.Empty);
            }
        }

        #endregion
    }
}