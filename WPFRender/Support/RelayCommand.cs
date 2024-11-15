using System;
using System.Diagnostics;
using System.Windows.Input;

namespace WPFRender;

/// <summary>
/// A basic <see cref="ICommand"/> that runs an <see cref="Action"/>.
/// </summary>
public class RelayCommand : ICommand
{
    #region [Members]

    /// <summary>
    /// The action to run
    /// </summary>
    private Action mAction;

    #endregion

    #region [Public Events]

    /// <summary>
    /// The event thats fired when the <see cref="CanExecute(object)"/> value has changed
    /// </summary>
    public event EventHandler CanExecuteChanged = (sender, e) => { };

    #endregion

    #region [Constructor]

    /// <summary>
    /// Default constructor
    /// </summary>
    public RelayCommand(Action action)
    {
        mAction = action;
    }

    #endregion

    #region [Command Methods]

    /// <summary>
    /// A relay command can always execute
    /// </summary>
    /// <param name="parameter"></param>
    /// <returns></returns>
    public bool CanExecute(object parameter)
    {
        return true;
    }

    /// <summary>
    /// Executes the commands Action
    /// </summary>
    /// <param name="parameter"></param>
    public void Execute(object parameter)
    {
        try
        {
            mAction();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WARNING] RelayCommand.Execute: {ex.Message}");
        }
    }

    #endregion
}

#region [Generic RelayCommand]
/// <summary>
/// Modified <see cref="RelayCommand"/> class using generic types.
/// </summary>
public class RelayCommand<T> : ICommand
{
    private Action<T> execute;
    private Func<T, bool> canExecute;

    public event EventHandler CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
    {
        this.execute = execute;
        this.canExecute = canExecute;
    }

    #region [Command Methods]
    public bool CanExecute(object parameter)
    {
        return this.canExecute == null || this.canExecute((T)parameter);
    }

    public void Execute(object parameter)
    {
        //if (parameter is System.Windows.Controls.TextBox obj1)
        //    System.Diagnostics.Debug.WriteLine($">> Object {obj1?.Name} is a TextBox");
        //else if (parameter is System.Windows.Controls.Button obj2)
        //    System.Diagnostics.Debug.WriteLine($">> Object {obj2?.Name} is a Button");
        try
        {
            this.execute((T)parameter);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WARNING] RelayCommand<T>.Execute: {ex.Message}");
        }
    }

    public override string ToString() => $"RelayCommand<{execute?.Target}> bound to event {execute?.Method.Name}";
    #endregion
}
#endregion

#region [Asynchronous RelayCommand]
public class AsyncRelayCommand : AsyncCommandBase
{
    readonly Func<Task> _callback;

    public AsyncRelayCommand(Func<Task> callback, Action<Exception> onException) : base(onException)
    {
        _callback = callback;
    }

    protected override async Task ExecuteAsync(object parameter)
    {
        await _callback();
    }
}

public abstract class AsyncCommandBase : ICommand
{
    readonly Action<Exception> _onException;

    bool _isExecuting;
    public bool IsExecuting
    {
        get => _isExecuting;
        set
        {
            _isExecuting = value;
            CanExecuteChanged?.Invoke(this, new EventArgs());
        }
    }

    public event EventHandler CanExecuteChanged;

    public AsyncCommandBase(Action<Exception> onException)
    {
        _onException = onException;
    }

    public bool CanExecute(object parameter)
    {
        return !IsExecuting;
    }

    public async void Execute(object parameter)
    {
        IsExecuting = true;

        try
        {
            await ExecuteAsync(parameter);
        }
        catch (Exception ex)
        {
            _onException?.Invoke(ex);
        }

        IsExecuting = false;
    }

    protected abstract Task ExecuteAsync(object parameter);
}
#endregion
