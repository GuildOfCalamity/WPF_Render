using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WPFRender;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    static int _warmUp = 1; // allow cycles to pass until the window if fully rendered
    static double _maxWidth = 700;
    static double _maxHeight = 500;
    static double _marginX = 10;
    static double _marginY = 10;
    static bool _shutdown = false;
    static bool _useGeometry = false;
    static IntPtr winHnd = IntPtr.Zero;
    static ValueStopwatch _vsw = ValueStopwatch.StartNew();
    List<RectangleObject> _rects = new();
    List<ImageObject> _images = new();

    #region [Props]
    public ICommand CloseCommand { get; private set; }
    public ILogger? Logger { get; private set; }

    bool _isBusy = false;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy != value) // This will alleviate unnecessary UI draw calls.
            {
                _isBusy = value;
                OnPropertyChanged();

                if (_isBusy)
                    AppIsBusy();
                else
                    AppIsNotBusy();
            }
        }
    }

    int _objectCount = 100;
    public int ObjectCount
    {
        get => _objectCount;
        set
        {
            if (_objectCount != value)
            {
                _objectCount = value;
                OnPropertyChanged();
            }
        }
    }

    int _progressAmount = 0;
    public int ProgressAmount
    {
        get => _progressAmount;
        set
        {
            if (_progressAmount != value)
            {
                _progressAmount = value;
                OnPropertyChanged();
            }
        }
    }

    string _statusText = string.Empty;

    public string StatusText
    {
        get => _statusText;
        set
        {
            // This will alleviate unnecessary UI draw calls.
            if (_statusText != value)
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion

    /// <summary>
    /// Primary constructor
    /// </summary>
    public MainWindow()
    {
        Debug.WriteLine($"[INFO] {MethodBase.GetCurrentMethod()?.DeclaringType?.Name}__{MethodBase.GetCurrentMethod()?.Name}");

        InitializeComponent();

        this.Closing += MainWindowOnClosing;
        this.Loaded += MainWindowOnLoaded;
        canvas.SizeChanged += CanvasOnSizeChanged;

        if (Debugger.IsAttached)
        {
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.WindowState = WindowState.Normal;
        }
        else
        {
            this.WindowStyle = WindowStyle.None;
            this.WindowState = WindowState.Maximized;
        }

        // NOTE: We're using our own timer for the animation loop,
        // so we won't be needing the composition renderer, but it's a
        // good substitute if you don't want to scaffold your own solution.
        //CompositionTarget.Rendering += OnCompositionRender;

        // NOTE: INotifyPropertyChanged will not work unless DataContext binding is set.
        DataContext = this;
        
        CloseCommand = new RelayCommand(() => this.Close());

        Logger = App.GetService<FileLogger>();

        // Don't use a DispatcherTimer for this, since it can only achieve a maximum of 33.34 FPS.
        ThreadPool.QueueUserWorkItem(obj => AccumulatorStyleLoop(60));
    }

    /// <summary>
    /// <para>
    ///  This is the superior game loop w/r/t the alternative <see cref="Thread.Sleep(int)"/> technique.
    /// </para>
    /// <para>
    ///  We are not using a <see cref="DispatcherTimer"/> since its max resolution is 30 FPS; the
    ///  <see cref="System.Windows.Media.CompositionTarget.Rendering"/> can be used to achieve 60 FPS,
    ///  but you would be bound to that single frame rate without the ability to adjust it.
    /// </para>
    /// </summary>
    /// <param name="FPS">desired frame rate</param>
    void AccumulatorStyleLoop(double FPS = 60)
    {
        double ticksPerSecond = (double)System.Diagnostics.Stopwatch.Frequency;
        int drawCount = 0;
        long currentTime = 0;
        double drawInterval = ticksPerSecond / FPS; // 10,000,000 / FPS
        double delta = 0;
        double msAverage = 0;
        long lastTime = DateTime.Now.Ticks;

        while (!_shutdown)
        {
            _vsw = ValueStopwatch.StartNew();

            currentTime = DateTime.Now.Ticks;
            delta += (currentTime - lastTime) / drawInterval;
            lastTime = currentTime;

            if (delta >= 1)
            {
                if (_warmUp == 0)
                {
                    UpdateGameState();
                    RepaintWindow();
                }
                delta--;
                drawCount++;
            }

            if (drawCount >= FPS)
            {
                this.Dispatcher?.Invoke(() => StatusText = $"{FPS} frames took {msAverage / FPS:N1} ms to render ({ObjectCount} objects)");
                ProgressAmount = (int)msAverage < 1000 ? (int)msAverage : 1000;
                msAverage = 0;
                drawCount = 0;
                if (_warmUp > 0)
                {
                    this.Dispatcher?.Invoke(() => StatusText = $"{_warmUp} warm-up");
                    _warmUp--;
                }
            }

            msAverage += _vsw.GetElapsedTime().TotalMilliseconds;
        }

        Debug.WriteLine($"[INFO] Loop exit ");
    }

    ///<summary>
    /// Code to update game properties here.
    ///</summary>
    void UpdateGameState()
    {
        if (_useGeometry)
        {
            foreach (var rg in _rects)
            {
                // Update the object position.
                rg.PosX += rg.SpeedX;
                rg.PosY += rg.SpeedY;

                // Check object X boundary.
                if (rg.PosX < _marginX || (rg.PosX + rg.Size) > (_maxWidth + Math.Abs(_marginX)))
                    rg.SpeedX = -rg.SpeedX;

                // Check object Y boundary.
                if (rg.PosY < _marginY || (rg.PosY + rg.Size) > (_maxHeight + Math.Abs(_marginY)))
                    rg.SpeedY = -rg.SpeedY;
            }
        }
        else
        {
            foreach (var img in _images)
            {
                // Update the object position.
                img.PosX += img.SpeedX;
                img.PosY += img.SpeedY;

                // Check object X boundary.
                if (img.PosX < _marginX || (img.PosX + img.Size) > (_maxWidth + Math.Abs(_marginX)))
                    img.SpeedX = -img.SpeedX;

                // Check object Y boundary.
                if (img.PosY < _marginY || (img.PosY + img.Size) > (_maxHeight + Math.Abs(_marginY)))
                    img.SpeedY = -img.SpeedY;
            }
        }
    }

    ///<summary>
    /// Code to update screen objects here.
    ///</summary>
    void RepaintWindow()
    {
        this.Dispatcher?.Invoke(() =>
        {
            if (_useGeometry)
            {
                foreach (var rg in _rects)
                {
                    if (rg.Rectangle != null)
                        rg.Rectangle.Rect = new Rect(rg.PosX, rg.PosY, rg.Size, rg.Size);
                }
            }
            else
            {
                foreach (var img in _images)
                {
                    if (img.Image != null)
                    {
                        img.Image.Margin = new Thickness(img.PosX, img.PosY, 0, 0);
                    }
                }
            }

        }, DispatcherPriority.Render);
    }

    #region [Control Events] 
    void MainWindowOnLoaded(object? sender, RoutedEventArgs e)
    {
        // Get the IntPtr of the window
        winHnd = new WindowInteropHelper(this).Handle;

        if (_useGeometry)
        {
            // Create the RenderObjects (performance starts to suffer above 500 objects)
            for (int i = 1; i < ObjectCount; i++)
            {
                var X = Random.Shared.Next(20, (int)_maxWidth - 30 > 0 ? (int)_maxWidth - 30 : 400);
                var Y = Random.Shared.Next(20, (int)_maxHeight - 30 > 0 ? (int)_maxHeight - 30 : 400);
                var size = Random.Shared.Next(11, 42);
                _rects.Add(new RectangleObject
                {
                    Size = size,
                    PosX = X,
                    PosY = Y,
                    SpeedX = RandSpeed(),
                    SpeedY = RandSpeed(),
                    Rectangle = new RectangleGeometry(new Rect(X, Y, size, size), 6, 6),
                });
            }

            // Add each RenderObject to the canvas
            foreach (var rg in _rects)
            {
                canvas.Children.Add(new Path()
                {
                    Data = rg.Rectangle,
                    Opacity = 0.8,
                    StrokeThickness = 3,
                    Stroke = Extensions.GetAppResource<Brush>("geometryGradient"),
                    Fill = Extensions.GetAppResource<Brush>("animationGradient"),
                });
            }
        }
        else // sprites
        {
            for (int i = 1; i < ObjectCount; i++)
            {
                var X = Random.Shared.Next(20, (int)_maxWidth - 100 > 0 ? (int)_maxWidth - 100 : 400);
                var Y = Random.Shared.Next(20, (int)_maxHeight - 100 > 0 ? (int)_maxHeight - 100 : 400);
                var size = Random.Shared.Next(32, 91);
                var img = new Image { Margin = new Thickness(0), Opacity = 0.6 };
                var bi = new BitmapImage();
                /** You can use the begin/end init to change the image during run-time **/
                bi.BeginInit();
                if (i % 2 == 0)
                    bi.UriSource = new Uri("Assets/FireIcon2.png", UriKind.Relative);
                else
                    bi.UriSource = new Uri("Assets/FireIcon3.png", UriKind.Relative);
                bi.EndInit();
                img.Source = bi;
                img.VerticalAlignment = VerticalAlignment.Center;
                img.HorizontalAlignment = HorizontalAlignment.Center;
                img.Width = img.Height = size;
                _images.Add(new ImageObject
                {
                    Size = size,
                    PosX = X,
                    PosY = Y,
                    SpeedX = RandSpeed(),
                    SpeedY = RandSpeed(),
                    Image = img,
                });

                /** You can also apply the BitmapImage to an ImageBrush **/
                //var ib = new ImageBrush(new BitmapImage(new Uri(@"pack://application:,,,/Assets/AppLogo.png")));
                //button.Background = ib;

            }

            // Add each RenderObject to the canvas
            foreach (var img in _images)
            {
                canvas.Children.Add(img.Image);
            }
        }
    }

    void CanvasOnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        Extensions.Debounce(() =>
        {
            _maxWidth = e.NewSize.Width;
            _maxHeight = e.NewSize.Height;
            StatusText = $"Canvas size changed to {e.NewSize}";
        });
    }

    void MainWindowOnClosing(object? sender, CancelEventArgs e) => _shutdown = true;
    #endregion

    #region [Extras]
    /// <summary>
    /// <para>
    ///  This event is tightly coupled/synchronized with the display's refresh rate, 
    ///  providing a more accurate timer for animations (usually 60 FPS or better).
    /// </para>
    /// <para>
    ///  The <see cref="DispatcherTimer"/> in WPF has a resolution of approximately 33.34 milliseconds 
    ///  per tick, which corresponds to a frame rate of around 30 frames per second. This means that 
    ///  the timer ticks at a frequency of approximately 30 times per second, with each tick occurring 
    ///  roughly every 33 milliseconds.
    /// </para>
    /// <para>
    ///  The <see cref="DispatcherTimer"/> is based on the UI thread's message pump, which is why its 
    ///  accuracy is bound to the UI thread's message processing rate. In most scenarios, especially for 
    ///  UI-related tasks and animations, a frame rate of 60 FPS (16.67 milliseconds per frame) or slightly 
    ///  above is generally best for a smooth user experience.
    /// </para>
    /// <para>
    ///  If you need a higher update frequency or a more accurate timer, you could consider alternative 
    ///  timer mechanisms. One such alternative is the <see cref="System.Windows.Media.CompositionTarget.Rendering"/>
    ///  event, which is triggered each time a new frame is rendered. This event is tightly synchronized 
    ///  with the display's refresh rate, providing a more accurate timer for animations (usually 60 FPS).
    /// </para>
    /// </summary>
    void OnCompositionRender(object? sender, EventArgs e)
    {
        //Debug.WriteLine($"[INFO] CompositionRenderEvent at {DateTime.Now.ToString("hh:mm:ss.fff tt")}");
    }

    /// <summary>
    ///  Background thread to animate the <see cref="System.Windows.Controls.ProgressBar"/>.
    ///  Could be used for health bars, et. al.
    /// </summary>
    void RunProgressBar(int maxCount = 100, int incAmnt = 15)
    {
        if (!IsBusy)
            return;

        ThreadPool.QueueUserWorkItem(obj =>
        {
            try
            {
                while (IsBusy)
                {
                    for (int i = 0; i < maxCount + 1; i += incAmnt)
                    {
                        ProgressAmount = i;
                        Thread.Sleep(incAmnt * 70);
                    }
                }
            }
            catch (Exception) { /* possible object disposed */ }
        });
    }

    /// <summary>
    /// The higher the <paramref name="factor"/> the slower the speed.
    /// </summary>
    double RandSpeed(double factor = 100) => Random.Shared.Next(1, 100) / factor;

    /// <summary>
    /// Randomly returns true or false.
    /// </summary>
    bool RandBool() => Random.Shared.Next(1, 3) == 2 ? true : false;

    /// <summary>
    ///  Simple logger method for debugging.
    /// </summary>
    void Log(Exception ex, bool logToDisk = false)
    {
        if (logToDisk)
            Logger?.WriteLine($"{ex.GetType()}: {ex.Message}", LogLevel.Warning);

        Task.Run(() => { Extensions.ShowDialogThreadSafe($"{ex.ToLogString()}", "Task Result", true, true, true); });
    }

    /// <summary>
    ///  Changes the taskbar application state to <see cref="TaskbarProgress.TaskbarStates.Indeterminate"/>.
    /// </summary>
    void AppIsBusy() => TaskbarProgress.SetState(winHnd, TaskbarProgress.TaskbarStates.Indeterminate);

    /// <summary>
    ///  Changes the taskbar application state to <see cref="TaskbarProgress.TaskbarStates.NoProgress"/>.
    /// </summary>
    void AppIsNotBusy() => TaskbarProgress.SetState(winHnd, TaskbarProgress.TaskbarStates.NoProgress);

    /// <summary>
    ///  Changes the taskbar application state to <see cref="TaskbarProgress.TaskbarStates.Error"/>.
    /// </summary>
    void AppIsFaulted() => TaskbarProgress.SetState(winHnd, TaskbarProgress.TaskbarStates.Error);

    #endregion
}
