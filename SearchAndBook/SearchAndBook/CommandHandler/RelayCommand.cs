namespace SearchAndBook.CommandHandler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Input;

    /// <summary>
    /// Implements the ICommand interface to create a reusable command that can be bound to UI elements in WPF applications. The RelayCommand class allows you to define the logic for executing a command and determining whether the command can execute, using delegates for flexibility. It also provides a method to raise the CanExecuteChanged event, enabling dynamic updates to the command's executable state in the UI.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> execute;
        private readonly Func<object?, bool>? canExecute;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand"/> class with the specified execute action and optional.
        /// can-execute predicate.
        /// </summary>
        /// <remarks>Use this constructor to create a command that delegates its execution and can-execute
        /// logic to the provided delegates. This is commonly used in MVVM patterns to bind UI actions to view model
        /// logic.</remarks>
        /// <param name="execute">The action to execute when the command is invoked. Cannot be null.</param>
        /// <param name="canExecute">An optional predicate that determines whether the command can execute. If null, the command is always
        /// enabled.</param>
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        /// <summary>
        /// Occurs when changes occur that affect whether the command can execute.
        /// </summary>
        /// <remarks>Subscribe to this event to be notified when the result of the command's CanExecute
        /// method may have changed. This event is typically raised by command sources to signal that the command's
        /// ability to execute should be re-evaluated.</remarks>
        public event EventHandler? CanExecuteChanged;

        /// <summary>
        /// Determines whether the command can execute in its current state.
        /// </summary>
        /// <remarks>If no predicate was provided when the command was created, this method always returns
        /// true. Otherwise, it evaluates the predicate with the specified parameter.</remarks>
        /// <param name="parameter">An optional parameter to be used by the command. The value may be null if the command does not require a
        /// parameter.</param>
        /// <returns>true if the command can execute; otherwise, false.</returns>
        public bool CanExecute(object? parameter)
        {
            return this.canExecute == null || this.canExecute(parameter);
        }

        /// <summary>
        /// Executes the associated command logic using the specified parameter.
        /// </summary>
        /// <remarks>This method is typically used to invoke the action associated with a command in
        /// response to user interaction or programmatic triggers. The behavior of the command may vary based on the
        /// value of the parameter provided.</remarks>
        /// <param name="parameter">An optional parameter to pass to the command. The meaning and expected type of this parameter depend on the
        /// command implementation.</param>
        public void Execute(object? parameter)
        {
            this.execute(parameter);
        }

        /// <summary>
        /// Notifies subscribers that the ability of the command to execute has changed.
        /// </summary>
        /// <remarks>Call this method when conditions affecting whether the command can execute may have
        /// changed. This will raise the CanExecuteChanged event, prompting any bound UI elements to re-query the
        /// command's CanExecute method and update their enabled state accordingly.</remarks>
        public void RaiseCanExecuteChanged()
        {
            this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
