using System;
using System.Diagnostics;
using System.Windows.Input;

namespace WpfApp1.Core
{
    internal class InternalRelayCommand : ICommand
    {
        #region Fields

        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;
        private readonly Func<object> _getParameters;
        private EventHandler _canExecuteChanged;

        #endregion // Fields

        #region Constructor(s)

        /// <summary>
        /// Creates a new command that can always execute.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        [DebuggerStepThrough]
        public InternalRelayCommand(Action<object> execute)
            : this(execute, null)
        {
        }

        /// <summary>
        /// Creates a new command.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        /// <param name="canExecute">The execution status logic.</param>
        [DebuggerStepThrough]
        public InternalRelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            _execute = execute;
            _canExecute = canExecute;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InternalRelayCommand"/> class.
        /// </summary>
        /// <param name="execute">The execute.</param>
        /// <param name="canExecute">The can execute.</param>
        /// <param name="getParameters">The get parameters.</param>
        [DebuggerStepThrough]
        public InternalRelayCommand(Action<object> execute, Predicate<object> canExecute, Func<object> getParameters)
            : this(execute, canExecute)
        {
            _getParameters = getParameters;
        }

        #endregion // Constructors

        #region ICommand Members

        [DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                // default to true
                return true;
            }
            if (_getParameters != null)
            {
                _canExecute(_getParameters());
            }

            return _canExecute(parameter);
        }


        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        [DebuggerStepThrough]
        public void Execute(object parameter)
        {
            if (_getParameters != null)
            {
                _execute(_getParameters());
            }

            _execute(parameter);
        }

        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
                _canExecuteChanged += value;
            }
            remove
            {
                // ReSharper disable once DelegateSubtraction
                _canExecuteChanged -= value;
                CommandManager.RequerySuggested -= value;
            }
        }

        #endregion // ICommand Members

        #region Public methods

        /// <summary>
        /// Raises the CanExecuteChanged event, so that the CanExecute predicate will be reevaluated.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            var evt = _canExecuteChanged;
            if (evt != null)
            {
                evt(this, EventArgs.Empty);
            }
        }

        #endregion
    }
}