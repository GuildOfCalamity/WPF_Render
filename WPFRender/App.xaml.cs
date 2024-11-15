using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace WPFRender;

public partial class App : Application
{
    public static bool DebugMode { get; set; } = false;

    /// <summary>
    /// Any outside calling threads must use the <see cref="App.SyncContext"/>
    /// or the <see cref="App.UiContext"/> when updating notifiable properties 
    /// from inside the view models.
    /// </summary>
    public static TaskScheduler? SyncContext { get; private set; }
    public static SynchronizationContext? UiContext { get; private set; }
    public static Dispatcher? MainDispatcher { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        AppDomain.CurrentDomain.FirstChanceException += CurrentDomainFirstChanceException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        var args = e.Args.ToList();
        DebugMode = args.Any(a => a.Equals("-debug", StringComparison.InvariantCultureIgnoreCase) || a.Equals("debug", StringComparison.InvariantCultureIgnoreCase));

        base.OnStartup(e);

        UiContext = SynchronizationContext.Current;
        SyncContext = TaskScheduler.FromCurrentSynchronizationContext();
        MainDispatcher = Dispatcher.CurrentDispatcher;

        foreach (var assembly in Extensions.ListAssemblies())
        {
            Debug.WriteLine($"[INFO] {assembly}");
        }
    }

    /// <summary>
    /// Asynchronously marshals a delegate to the UI thread using the captured
    /// <see cref="SynchronizationContext"/> from <see cref="App.UiContext"/>.
    /// </summary>
    public static void PostOnUiThread(Action action, Action? onException = null)
    {
        //Task.Run(() => {
        App.UiContext?.Post(_ =>
        {
            try { action(); }
            catch (Exception) { onException?.Invoke(); }

        }, null);
        //});
    }

    /// <summary>
    /// Synchronously marshals a delegate to the UI thread using the captured 
    /// <see cref="SynchronizationContext"/> from <see cref="App.UiContext"/>.
    /// </summary>
    public static void SendOnUiThread(Action action, Action? onException = null)
    {
        //Task.Run(() => {
        App.UiContext?.Send(_ =>
        {
            try { action(); }
            catch (Exception) { onException?.Invoke(); }

        }, null);
        //});
    }

    public static void DebugLog(string message, LogLevel level = LogLevel.Info, [CallerMemberName] string origin = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        message = $"[{DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt")}] ⇒ {level} ⇒ {System.IO.Path.GetFileName(filePath)} ⇒ {origin}(line{lineNumber}) ⇒ {message}";
        if (level <= LogLevel.Debug)
        {
            Debug.WriteLine($"{message}");
        }
        else
        {
            using (var sw = File.AppendText(System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Debug.log")))
            {
                sw.WriteLine(message);
            }
        }
    }

    #region [Domain Events]
    void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            DebugLog($"Unhandled exception StackTrace: {Environment.StackTrace}", LogLevel.Error);
            //Logger.Write(GetCurrentAssemblyName(), $"Unhandled exception thrown from Dispatcher {e.Dispatcher}: {e.Exception}");
            //System.Diagnostics.EventLog.WriteEntry(SystemTitle, $"Unhandled exception thrown from Dispatcher {e.Dispatcher.ToString()}: {e.Exception.ToString()}");
            if (DebugMode)
                Extensions.ShowDialogThreadSafe(e.Exception.ToLogString(), "Unhandled Exception", true);
            e.Handled = true;
        }
        catch { }
    }

    void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            DebugLog($"Unhandled exception thrown: {((Exception)e.ExceptionObject).ToLogString()}", LogLevel.Error);
            //Logger.Write(GetCurrentAssemblyName(), $"Unhandled exception thrown: {((Exception)e.ExceptionObject).ToLogString()}");
            System.Diagnostics.EventLog.WriteEntry(GetCurrentAssemblyName(), $"Unhandled exception thrown:\r\n{((Exception)e.ExceptionObject).ToString()}");
            if (DebugMode)
                Extensions.ShowDialogThreadSafe($"{((Exception)e.ExceptionObject).ToLogString()}", "Unhandled Exception", true);
        }
        catch { }
    }

    void CurrentDomainFirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
    {
        if (e?.Exception != null && (e?.Exception.GetType() == typeof(SocketException) || (bool)e?.Exception?.Message.Contains("System.Net.Sockets.Socket")))
        {
            // During shutdown of IPC server the AcceptTcpClient will always throw a socket exception
            // because the I/O operation has been aborted due to either a thread or application exit.
            Debug.WriteLine($"[SocketException] {e?.Exception?.Message}");
        }
        else
        {
            if ((bool)e?.Exception?.Message?.Contains($"{GetCurrentNamespace()}.XmlSerializers"))
            {
                // Ignore the fake System.Xml.Serialization warning.
                Debug.WriteLine($"[INFO] AppDomain is looking for \"{GetCurrentNamespace()}.XmlSerializers\".");
            }
            else
            {
                DebugLog($"First chance exception from {sender?.GetType()}: {e?.Exception?.Message}", LogLevel.Error);
                if (e?.Exception?.InnerException != null)
                    DebugLog($"InnerException: {e.Exception.InnerException.Message}", LogLevel.Error);

                //Debug.WriteLine($"[WARNING] {Environment.StackTrace}");
            }
        }
    }

    void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        if (e.Exception is AggregateException aex)
        {
            aex?.Flatten().Handle(ex =>
            {
                DebugLog($"Unobserved task exception: {ex?.Message}", LogLevel.Error);
                Debug.WriteLine($"[WARNING] Unobserved task exception: {ex?.ToLogString()}");
                //Logger.Write(App.Title, $"Unobserved task exception: {ex?.Message}");
                if (DebugMode)
                    Extensions.ShowDialogThreadSafe($"{ex?.ToLogString()}", "Task Exception", true);
                return true;
            });
        }
        e.SetObserved(); // suppress and handle manually
    }
    #endregion

    #region [Reflection Helpers]
    /// <summary>Returns the declaring type's namespace.</summary>
    public static string GetCurrentNamespace() => System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType?.Namespace ?? "UsingTask";

    /// <summary>Returns the declaring type's full name.</summary>
    public static string GetCurrentFullName() => System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType?.Assembly?.FullName ?? "UsingTask";

    /// <summary>Returns the declaring type's assembly name.</summary>
    public static string GetCurrentAssemblyName() => System.Reflection.Assembly.GetExecutingAssembly()?.GetName()?.Name ?? "UsingTask";

    /// <summary>Returns the AssemblyVersion, not the FileVersion.</summary>
    public static Version GetCurrentAssemblyVersion() => System.Reflection.Assembly.GetExecutingAssembly()?.GetName()?.Version ?? new Version();
    #endregion

    #region [Home-brew service getter]
    static List<Object>? ServicesHost { get; set; }
    /// <summary>
    /// <para>Home-brew Dependency Injection.</para>
    /// <para>
    /// Returns an instance of the desired service type. Use this if you can't/won't pass
    /// the service through the target's constructor. Can be used from any window/page/model.
    /// </para>
    /// <example><code>
    /// var service1 = App.GetService&lt;SomeViewModel&gt;();
    /// var service2 = App.GetService&lt;FileLogger&gt;();
    /// </code></example>
    /// </summary>
    /// <remarks>
    /// The 1st call to this method will add and instantiate the pre-defined services.
    /// </remarks>
    public static T? GetService<T>() where T : class
    {
        try
        {
            // New-up the services container if needed.
            if (ServicesHost == null) { ServicesHost = new List<Object>(); }

            // If 1st time then add relevant services to the container.
            // This could be done elsewhere, e.g. in the main constructor.
            if (ServicesHost.Count == 0)
            {
                //ServicesHost?.Add(new MainViewModel());
                //ServicesHost?.Add(new SettingsManager());
                ServicesHost?.Add(new FileLogger(System.IO.Path.Combine(FileLogger.GetRoot(), "Logs"), LogLevel.Debug));
            }

            // Try and locate the desired service. We're not using FirstOrDefault
            // here so that a null will be returned when an exception is thrown.
            var vm = ServicesHost?.Where(o => o.GetType() == typeof(T)).First();

            if (vm != null)
                return (T)vm;
            else
                throw new ArgumentException($"{typeof(T)} must be registered first within {MethodBase.GetCurrentMethod()?.Name}.");
        }
        catch (Exception ex)
        {
            DebugLog($"{MethodBase.GetCurrentMethod()?.Name}: {ex.Message}", LogLevel.Error);
            return null;
        }
    }
    #endregion
}
