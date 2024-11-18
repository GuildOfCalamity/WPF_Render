using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Text.RegularExpressions;
using System.IO;

namespace WPFRender;

public static class Extensions
{
    /// <summary>
    /// Determine if current OS is Windows 7 or higher.
    /// </summary>
    public static bool IsWindows7OrLater
    {
        get => Environment.OSVersion.Version >= new Version(6, 1);
    }

    /// <summary>
    /// Determines if two <see cref="System.Windows.Rect"/>s intersect.
    /// </summary>
    /// <param name="rect1">The first <see cref="System.Windows.Rect"/>.</param>
    /// <param name="rect2">The second <see cref="System.Windows.Rect"/>.</param>
    /// <returns>True if the rectangles intersect; otherwise, false.</returns>
    public static bool Intersect(this System.Windows.Rect rect1, System.Windows.Rect rect2)
    {
        return rect1.Left < rect2.Right &&
               rect1.Right > rect2.Left &&
               rect1.Top < rect2.Bottom &&
               rect1.Bottom > rect2.Top;
    }

    /// <summary>
    /// Returns the Euclidian distance between two <see cref="System.Windows.Media.Color"/>s.
    /// </summary>
    /// <param name="color1">1st <see cref="System.Windows.Media.Color"/></param>
    /// <param name="color2">2nd <see cref="System.Windows.Media.Color"/></param>
    public static double ColorDistance(System.Windows.Media.Color color1, System.Windows.Media.Color color2)
    {
        return Math.Sqrt(Math.Pow(color1.R - color2.R, 2) + Math.Pow(color1.G - color2.G, 2) + Math.Pow(color1.B - color2.B, 2));
    }

    /// <summary>
    /// Finds the contrast ratio.
    /// This is helpful for determining if one control's foreground and another control's background will be hard to distinguish.
    /// https://www.w3.org/WAI/GL/wiki/Contrast_ratio
    /// (L1 + 0.05) / (L2 + 0.05), where
    /// L1 is the relative luminance of the lighter of the colors, and
    /// L2 is the relative luminance of the darker of the colors.
    /// </summary>
    /// <param name="first"><see cref="System.Windows.Media.Color"/></param>
    /// <param name="second"><see cref="System.Windows.Media.Color"/></param>
    /// <returns>ratio between relative luminance</returns>
    public static double CalculateContrastRatio(System.Windows.Media.Color first, System.Windows.Media.Color second)
    {
        double relLuminanceOne = GetRelativeLuminance(first);
        double relLuminanceTwo = GetRelativeLuminance(second);
        return (Math.Max(relLuminanceOne, relLuminanceTwo) + 0.05) / (Math.Min(relLuminanceOne, relLuminanceTwo) + 0.05);
    }

    /// <summary>
    /// Gets the relative luminance.
    /// https://www.w3.org/WAI/GL/wiki/Relative_luminance
    /// For the sRGB colorspace, the relative luminance of a color is defined as L = 0.2126 * R + 0.7152 * G + 0.0722 * B
    /// </summary>
    /// <param name="c"><see cref="System.Windows.Media.Color"/></param>
    /// <remarks>This is mainly used by <see cref="Extensions.CalculateContrastRatio(Color, Color)"/></remarks>
    public static double GetRelativeLuminance(System.Windows.Media.Color c)
    {
        double rSRGB = c.R / 255.0;
        double gSRGB = c.G / 255.0;
        double bSRGB = c.B / 255.0;

        // WebContentAccessibilityGuideline 2.x definition was 0.03928 (incorrect)
        // WebContentAccessibilityGuideline 3.x definition is 0.04045 (correct)
        double r = rSRGB <= 0.04045 ? rSRGB / 12.92 : Math.Pow(((rSRGB + 0.055) / 1.055), 2.4);
        double g = gSRGB <= 0.04045 ? gSRGB / 12.92 : Math.Pow(((gSRGB + 0.055) / 1.055), 2.4);
        double b = bSRGB <= 0.04045 ? bSRGB / 12.92 : Math.Pow(((bSRGB + 0.055) / 1.055), 2.4);
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }

    /// <summary>
    /// Calculates the linear interpolated Color based on the given Color values.
    /// </summary>
    /// <param name="colorFrom">Source Color.</param>
    /// <param name="colorTo">Target Color.</param>
    /// <param name="amount">Weight given to the target color.</param>
    /// <returns>Linear Interpolated Color.</returns>
    public static System.Windows.Media.Color Lerp(this System.Windows.Media.Color colorFrom, System.Windows.Media.Color colorTo, float amount)
    {
        // Convert colorFrom components to lerp-able floats
        float sa = colorFrom.A, sr = colorFrom.R, sg = colorFrom.G, sb = colorFrom.B;

        // Convert colorTo components to lerp-able floats
        float ea = colorTo.A, er = colorTo.R, eg = colorTo.G, eb = colorTo.B;

        // lerp the colors to get the difference
        byte a = (byte)Math.Max(0, Math.Min(255, sa.Lerp(ea, amount))),
             r = (byte)Math.Max(0, Math.Min(255, sr.Lerp(er, amount))),
             g = (byte)Math.Max(0, Math.Min(255, sg.Lerp(eg, amount))),
             b = (byte)Math.Max(0, Math.Min(255, sb.Lerp(eb, amount)));

        // return the new color
        return System.Windows.Media.Color.FromArgb(a, r, g, b);
    }

    /// <summary>
    /// Darkens the color by the given percentage using lerp.
    /// </summary>
    /// <param name="color">Source color.</param>
    /// <param name="amount">Percentage to darken. Value should be between 0 and 1.</param>
    /// <returns>Color</returns>
    public static System.Windows.Media.Color DarkerBy(this System.Windows.Media.Color color, float amount)
    {
        return color.Lerp(Colors.Black, amount);
    }

    /// <summary>
    /// Lightens the color by the given percentage using lerp.
    /// </summary>
    /// <param name="color">Source color.</param>
    /// <param name="amount">Percentage to lighten. Value should be between 0 and 1.</param>
    /// <returns>Color</returns>
    public static System.Windows.Media.Color LighterBy(this System.Windows.Media.Color color, float amount)
    {
        return color.Lerp(Colors.White, amount);
    }

    /// <summary>
    /// Clamping function for any value of type <see cref="IComparable{T}"/>.
    /// </summary>
    /// <param name="val">initial value</param>
    /// <param name="min">lowest range</param>
    /// <param name="max">highest range</param>
    /// <returns>clamped value</returns>
    public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
    {
        return val.CompareTo(min) < 0 ? min : (val.CompareTo(max) > 0 ? max : val);
    }

    /// <summary>
    /// Used to gradually reduce the effect of certain changes over time.
    /// </summary>
    /// <param name="value">Some initial value, e.g. 40</param>
    /// <param name="target">Where we want the value to end up, e.g. 100</param>
    /// <param name="rate">How quickly we want to reach the target, e.g. 0.25</param>
    /// <returns></returns>
    public static float Dampen(this float value, float target, float rate)
    {
        float dampenedValue = value;
        if (value != target)
        {
            float dampeningFactor = MathF.Pow(1 - MathF.Abs((value - target) / rate), 2);
            dampenedValue = target + ((value - target) * dampeningFactor);
        }
        return dampenedValue;
    }

    /// <summary>
    /// Linear interpolation for a range of floats.
    /// </summary>
    public static float Lerp(this float start, float end, float amount = 0.5F) => start + (end - start) * amount;
    
    /// <summary>
    /// Linear interpolation for a range of double.
    /// </summary>
    public static double Lerp(this double start, double end, double amount = 0.5F) => start + (end - start) * amount;

    /// <summary>
    /// Scales a range of floats. [baseMin to baseMax] will become [limitMin to limitMax]
    /// </summary>
    public static float Scale(this float valueIn, float baseMin, float baseMax, float limitMin, float limitMax) => ((limitMax - limitMin) * (valueIn - baseMin) / (baseMax - baseMin)) + limitMin;
    
    /// <summary>
    /// Scales a range of double. [baseMin to baseMax] will become [limitMin to limitMax]
    /// </summary>
    public static double Scale(this double valueIn, double baseMin, double baseMax, double limitMin, double limitMax) => ((limitMax - limitMin) * (valueIn - baseMin) / (baseMax - baseMin)) + limitMin;


    /// <summary>
    /// If not using a WPF or WinUI app, set <paramref name="consoleApp"/> to true.
    /// </summary>
    /// <param name="task"><see cref="Task"/></param>
    /// <param name="onSuccess"><see cref="Action"/> to perform if <see cref="TaskContinuationOptions.OnlyOnRanToCompletion"/></param>
    /// <param name="onCanceled"><see cref="Action"/> to perform if <see cref="TaskContinuationOptions.OnlyOnCanceled"/></param>
    /// <param name="onFaulted"><see cref="Action"/> to perform if <see cref="TaskContinuationOptions.OnlyOnFaulted"/></param>
    public static void ContinueTaskWithActions(this Task task, Action onSuccess, Action onCanceled, Action onFaulted, bool consoleApp = false)
    {
        #region [ContinueWith-Success]
        task.ContinueWith(task => { onSuccess?.Invoke(); },
        CancellationToken.None,
        TaskContinuationOptions.OnlyOnRanToCompletion,
        consoleApp ? TaskScheduler.Current : TaskScheduler.FromCurrentSynchronizationContext());
        #endregion

        #region [ContinueWith-Canceled]
        task.ContinueWith(task => { onCanceled?.Invoke(); },
        CancellationToken.None,
        TaskContinuationOptions.OnlyOnCanceled,
        consoleApp ? TaskScheduler.Current : TaskScheduler.FromCurrentSynchronizationContext());
        #endregion

        #region [ContinueWith-Faulted]
        task.ContinueWith(task => { onFaulted?.Invoke(); },
        CancellationToken.None,
        TaskContinuationOptions.OnlyOnFaulted,
        consoleApp ? TaskScheduler.Current : TaskScheduler.FromCurrentSynchronizationContext());
        #endregion
    }

    static System.Threading.Timer? dbTimer = null;
    /// <summary>
    /// A simple debounce method for caller actions. 
    /// e.g. Could be used for saving the main window position during movement event spamming.
    /// </summary>
    /// <param name="action"><see cref="Action"/></param>
    /// <param name="msDelay">debounce delay in milliseconds</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Debounce(Action action, int msDelay = 250)
    {
        if (dbTimer != null) { dbTimer.Dispose(); }
        dbTimer = new System.Threading.Timer(_ => action(), null, msDelay, Timeout.Infinite);
    }

    /// <summary>
    /// Determine internally referenced assemblies.
    /// </summary>
    /// <returns><see cref="List{T}"/></returns>
    public static List<string> ListAssemblies(bool mainOnly = false)
    {
        List<string> result = new List<string>();
        try
        {
            if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
                Thread.CurrentThread.Name = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

            // Write Main Assembly
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            System.Reflection.AssemblyName main = assembly.GetName();
            result.Add($"Main Assembly: {main.Name}, Version: {main.Version}");

            if (!mainOnly)
            {
                // Get referenced assemblies
                List<string> assemblyGeneric = new List<string>();
                GetAssemblies(assembly, assemblyGeneric);

                // Sort Array
                string[] assemList = assemblyGeneric.ToArray();
                if (assemList.Count() > 0)
                {
                    Array.Sort(assemList);
                    assemList.ToList().ForEach(assemInfo => { result.Add($"Referenced {assemInfo}"); });
                }
                else
                {
                    Debug.WriteLine($"[INFO] No other referenced assemblies detected.");
                }
            }
        }
        catch (Exception) { }
        return result;
    }
    static void GetAssemblies(System.Reflection.Assembly main, List<string> assemblyGeneric)
    {
        System.Reflection.AssemblyName[] names = main.GetReferencedAssemblies()
                // Ignore standard Microsoft libs
                .Where(p => !p.Name.ToLower().StartsWith("system") &&
                                      !p.Name.ToLower().StartsWith("microsoft") &&
                                      !p.Name.ToLower().StartsWith("mscorlib") &&
                                      !p.Name.ToLower().StartsWith("presentation") &&
                                      !p.Name.ToLower().StartsWith("windows"))
                .ToArray();

        foreach (var a in names)
        {
            assemblyGeneric.Add(String.Format("Assembly: {0}, Version: {1}", a.Name, a.Version));
            try
            {   // Recursive
                GetAssemblies(System.Reflection.Assembly.Load(a), assemblyGeneric);
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.WriteLine($"Could not find assembly: {a.Name}, Version: {a.Version}", ConsoleColor.DarkRed);
            }
        }
    }

    /// <summary>
    /// Can be called from any thread.
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="caption"></param>
    /// <param name="IsWarning"></param>
    /// <param name="shadows"></param>
    public static void ShowDialogThreadSafe(string msg, string caption = "Notice", bool IsWarning = false, bool shadows = true, bool addIcon = false)
    {
        try
        {
            System.Threading.Thread thread = new System.Threading.Thread(() =>
            {
                System.Threading.SynchronizationContext.SetSynchronizationContext(new System.Windows.Threading.DispatcherSynchronizationContext(System.Windows.Threading.Dispatcher.CurrentDispatcher));

                //The Border class in WPF represents a Border element. The code snippet listed in Listing 2 is C# code that creates a Border, sets its properties, and places it around a Canvas element.
                //Note: Border can have only one child element. If you need to place border around multiple elements, you must place a border around each element.
                Border border = new Border();
                border.Width = 700;
                border.Height = 340;
                if (IsWarning)
                    border.Background = GetAppResource<Brush>("warningGradient") ?? ChangeBackgroundColor(Color.FromRgb(120, 20, 10), Color.FromRgb(100, 0, 0));
                else
                    border.Background = GetAppResource<Brush>("informationGradient") ?? ChangeBackgroundColor(Color.FromRgb(32, 32, 32), Color.FromRgb(10, 10, 10));
                border.BorderThickness = new Thickness(2);
                border.BorderBrush = new SolidColorBrush(Colors.LightGray);
                border.CornerRadius = new CornerRadius(8);
                border.HorizontalAlignment = HorizontalAlignment.Stretch;
                border.VerticalAlignment = VerticalAlignment.Stretch;
                
                Canvas cnvs = new Canvas();
                cnvs.VerticalAlignment = VerticalAlignment.Stretch;
                cnvs.HorizontalAlignment = HorizontalAlignment.Stretch;

                // StackPanel setup
                var sp = new StackPanel
                {
                    Background = Brushes.Transparent,
                    Orientation = Orientation.Vertical,
                    Height = border.Height,
                    Width = border.Width
                };

                // TextBox setup
                var tbx = new TextBox()
                {
                    Background = sp.Background,
                    FontSize = 20,
                    AcceptsReturn = true,
                    BorderThickness = new Thickness(0),
                    MaxHeight = border.Height / (addIcon ? 1.8 : 1.7),
                    MinHeight = border.Height / (addIcon ? 1.8 : 1.7),
                    MaxWidth = border.Width / 1.111,
                    MinWidth = border.Width / 1.111,
                    Margin = new Thickness(10, addIcon ? 5 : 24, 10, 4),
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Top,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                    FontWeight = FontWeights.Regular,
                    Text = msg
                };

                var bbkgnd = GetAppResource<Brush>("backgroundGradient");

                // Button setup
                var btn = new Button()
                {
                    Width = 170,
                    Height = 38,
                    Content = "Close",
                    FontSize = 20,
                    Template = GetAppResource<ControlTemplate>("CloseButton"),
                    FontWeight = FontWeights.Regular,
                    Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                    Margin = new Thickness(10, addIcon ? 15 : 30, 10, 2),
                    VerticalAlignment = VerticalAlignment.Bottom,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Background = sp.Background
                };

                if (shadows)
                {
                    btn.Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = IsWarning ? new Color { A = 255, R = 255, G = 120, B = 120 } : new Color { A = 255, R = 120, G = 120, B = 255 },
                        Direction = 310,
                        ShadowDepth = 1,
                        Opacity = 1,
                        BlurRadius = 2
                    };
                }

                if (addIcon)
                {
                    // Image stuff here
                    var img = new Image()
                    {
                        Width = 50,
                        Margin = new Thickness(6, 6, 6, 0),
                        VerticalAlignment = VerticalAlignment.Top,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Source = IsWarning ? ReturnImageSource(@"pack://application:,,,/Assets/WarningIcon.png") : ReturnImageSource(@"pack://application:,,,/Assets/InfoIcon.png"),
                    };
                    img.Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = IsWarning ? new Color { A = 255, R = 250, G = 150, B = 40 } : new Color { A = 255, R = 150, G = 250, B = 250 },
                        Direction = 270,
                        ShadowDepth = 1,
                        Opacity = 1,
                        BlurRadius = 8
                    };
                    RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.Fant);
                    sp.Children.Add(img);
                }

                sp.Children.Add(tbx);
                sp.Children.Add(btn);
                cnvs.Children.Add(sp);
                border.Child = cnvs;
                if (shadows)
                {
                    border.Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = new Color { A = 255, R = 100, G = 100, B = 100 },
                        Direction = 310,
                        ShadowDepth = 6,
                        Opacity = 1,
                        BlurRadius = 8
                    };
                }

                // Create window to hold content
                var w = new Window();
                w.WindowStyle = WindowStyle.None;
                w.AllowsTransparency = true;
                w.Background = Brushes.Transparent;
                w.VerticalAlignment = VerticalAlignment.Center;
                w.HorizontalAlignment = HorizontalAlignment.Center;
                w.Height = border.Height + 20; // add padding for shadow effect
                w.Width = border.Width + 20; // add padding for shadow effect

                // Apply content to new window
                w.Content = border;

                if (string.IsNullOrEmpty(caption))
                    caption = System.Reflection.Assembly.GetExecutingAssembly()?.GetName()?.Name ?? "";

                w.Title = caption;

                w.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                // Setup a delegate for the window loaded event
                w.Loaded += (s, e) =>
                {
                    // We could add a timer here to self-close
                };

                // Setup a delegate for the window closed event
                w.Closed += (s, e) =>
                {
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown();
                };

                // Setup a delegate for the close button click event
                btn.Click += (s, e) =>
                {
                    w.Close();
                };

                // Setup a delegate for the window mouse-down event
                w.MouseDown += (s, e) =>
                {
                    if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                    {
                        w.DragMove();
                    }
                    else if (e.RightButton == System.Windows.Input.MouseButtonState.Pressed)
                    {
                        // There could be a formatting sitch where the close button
                        // is pushed off the window, so provide a backup close method.
                        w.Close();
                    }
                };


                // Show our constructed window. We're not on the
                // main UI thread, so we shouldn't use "w.ShowDialog()"
                w.Show();

                // Make sure the essential WPF duties are taken care of.
                System.Windows.Threading.Dispatcher.Run();
            });

            // You can only show a dialog in a STA thread.
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
            thread.Join();

        }
        catch (Exception ex) { Debug.WriteLine($"[WARNING] Couldn't show dialog: {ex.Message}"); }
    }

    public static LinearGradientBrush ChangeBackgroundColor(Color c1, Color c2, Color c3)
    {
        var gs1 = new GradientStop(c1, 0);
        var gs2 = new GradientStop(c2, 0.5);
        var gs3 = new GradientStop(c3, 1);
        var gsc = new GradientStopCollection { gs1, gs2, gs3 };
        var lgb = new LinearGradientBrush
        {
            ColorInterpolationMode = ColorInterpolationMode.ScRgbLinearInterpolation,
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
            GradientStops = gsc
        };

        return lgb;
    }

    public static RadialGradientBrush ChangeBackgroundColor(Color c1, Color c2)
    {
        var gs1 = new GradientStop(c1, 0);
        var gs2 = new GradientStop(c2, 1);
        var gsc = new GradientStopCollection { gs1, gs2 };
        var lgb = new RadialGradientBrush
        {
            ColorInterpolationMode = ColorInterpolationMode.ScRgbLinearInterpolation,
            GradientStops = gsc
        };

        return lgb;
    }

    public static SolidColorBrush GenerateRandomBrush()
    {
        // Generate a random color
        byte r = (byte)Random.Shared.Next(0, 256);
        byte g = (byte)Random.Shared.Next(0, 256);
        byte b = (byte)Random.Shared.Next(0, 256);
        return new SolidColorBrush(Color.FromRgb(r, g, b));
    }

    /// <summary>
    /// Returns a <see cref="System.Windows.Media.Imaging.BitmapImage"/> from the provided <paramref name="uriPath"/>.
    /// </summary>
    /// <param name="uriPath">the pack uri path to the image</param>
    /// <returns><see cref="System.Windows.Media.Imaging.BitmapImage"/></returns>
    /// <remarks>
    /// URI Packing can assume the following formats:
    /// 1) Content File
    ///    "pack://application:,,,/Assets/logo.png"
    ///    https://learn.microsoft.com/en-us/dotnet/desktop/wpf/app-development/pack-uris-in-wpf?view=netframeworkdesktop-4.8#content-file-pack-uris
    /// 2) Referenced Assembly Resource
    ///    "pack://application:,,,/AssemblyNameHere;component/Resources/logo.png"
    ///    https://learn.microsoft.com/en-us/dotnet/desktop/wpf/app-development/pack-uris-in-wpf?view=netframeworkdesktop-4.8#referenced-assembly-resource-file
    /// 3) Site Of Origin
    ///    "pack://siteoforigin:,,,/Assets/SiteOfOriginFile.xaml"
    ///    https://learn.microsoft.com/en-us/dotnet/desktop/wpf/app-development/pack-uris-in-wpf?view=netframeworkdesktop-4.8#site-of-origin-pack-uris
    /// </remarks>
    public static System.Windows.Media.Imaging.BitmapImage ReturnImageSource(this string uriPath)
    {
        try
        {
            System.Windows.Media.Imaging.BitmapImage holder = new System.Windows.Media.Imaging.BitmapImage();
            holder.BeginInit();
            holder.UriSource = new Uri(uriPath); //new Uri("pack://application:,,,/AssemblyName;component/Resources/logo.png");
            holder.EndInit();
            return holder;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] ReturnImageSource: {ex.Message}");
            return new System.Windows.Media.Imaging.BitmapImage();
        }
    }

    /// <summary>
    /// Retrieve an <see cref="ImageBrush"/> from an assembly bitmap resource.
    /// </summary>
    /// <param name="gdiBitmapHandle">A handle to the GDI bitmap object.</param>
    /// <returns><see cref="ImageBrush"/></returns>
    public static ImageBrush? GetBitmapSource(IntPtr gdiBitmapHandle)
    {
        var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
            gdiBitmapHandle,
            IntPtr.Zero,
            Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());

        //btnImageTest.Background = new ImageBrush(bitmapSource);
        //MyImageControl.Source = bitmapSource;

        if (bitmapSource != null)
            return new ImageBrush(bitmapSource);
        else
            return null;
    }

    /// <summary>
    /// A blur effect on the way in. (tried adding this to StoryboardHelpers but it did not seem to work properly)
    /// </summary>
    /// <param name="element"></param>
    /// <param name="seconds"></param>
    /// <returns>
    /// <see cref="ValueTask"/> uses lest heap memory than <see cref="Task"/>, 
    /// however you should not await a <see cref="ValueTask"/> more than once.
    /// </returns>
    public static async ValueTask BlurInAsync(FrameworkElement element, float seconds = 0.5f)
    {
        var blur = new BlurEffect();
        blur.Radius = 10;
        blur.KernelType = KernelType.Gaussian;
        element.Effect = blur;
        DoubleAnimation da = new DoubleAnimation();
        da.From = 0;
        da.To = blur.Radius;
        da.Duration = new Duration(TimeSpan.FromSeconds(seconds));
        blur.BeginAnimation(BlurEffect.RadiusProperty, da);

        await Task.Delay((int)seconds * 1000);
    }

    /// <summary>
    /// A blur effect on the way out. (tried adding this to StoryboardHelpers but it did not seem to work properly)
    /// </summary>
    /// <param name="element"></param>
    /// <param name="seconds"></param>
    /// <returns>
    /// <see cref="ValueTask"/> uses lest heap memory than <see cref="Task"/>, 
    /// however you should not await a <see cref="ValueTask"/> more than once.
    /// </returns>
    public static async ValueTask BlurOutAsync(FrameworkElement element, float seconds = 0.5f)
    {
        var blur = new BlurEffect();
        blur.Radius = 10;
        blur.KernelType = KernelType.Gaussian;
        element.Effect = blur;
        DoubleAnimation da = new DoubleAnimation();
        da.From = blur.Radius;
        da.To = 0;
        da.Duration = new Duration(TimeSpan.FromSeconds(seconds));
        blur.BeginAnimation(BlurEffect.RadiusProperty, da);

        await Task.Delay((int)seconds * 1000);

        // Make sure we remove any effects from the element (this will immeadiately remove the blur)
        //element.Effect = null;
    }

    /// <summary>
    /// Increments through an enum type.
    /// </summary>
    public static T Next<T>(this T value) where T : struct, Enum
    {
        var values = Enum.GetValues(typeof(T)).Cast<T>();
        var index = (values.ToList().IndexOf(value) + 1) % values.Count();
        return values.ElementAt(index);
    }

    /// <summary>
    /// <para>Creates a log-string from the Exception.</para>
    /// <para>The result includes the stacktrace, innerexception et cetera, separated by <see cref="Environment.NewLine"/>.</para>
    /// </summary>
    /// <param name="ex">The exception to create the string from.</param>
    /// <param name="additionalMessage">Additional message to place at the top of the string, maybe be empty or null.</param>
    /// <returns>formatted string</returns>
    public static string ToLogString(this Exception ex, string additionalMessage = "")
    {
        StringBuilder msg = new StringBuilder();

        msg.Append($"–––[{ex?.GetType()}]–––");
        msg.Append(Environment.NewLine); msg.Append(Environment.NewLine);

        if (ex != null)
        {
            try
            {
                Exception? orgEx = ex;

                msg.Append("[Exception]: ");
                //msg.Append(Environment.NewLine);
                while (orgEx != null)
                {
                    msg.Append(orgEx.Message);
                    msg.Append(Environment.NewLine);
                    orgEx = orgEx.InnerException;
                }

                if (ex.Source != null)
                {
                    msg.Append("[Source]: ");
                    msg.Append(ex.Source);
                    msg.Append(Environment.NewLine);
                }

                if (ex.Data != null)
                {
                    foreach (object i in ex.Data)
                    {
                        msg.Append("[Data]: ");
                        msg.Append(i.ToString());
                        msg.Append(Environment.NewLine);
                    }
                }

                if (ex.StackTrace != null)
                {
                    msg.Append("[StackTrace]: ");
                    msg.Append(ex.StackTrace.ToString());
                    msg.Append(Environment.NewLine);
                }

                if (ex.TargetSite != null)
                {
                    msg.Append("[TargetSite]: ");
                    msg.Append(ex.TargetSite.ToString());
                    msg.Append(Environment.NewLine);
                }

                Exception baseException = ex.GetBaseException();
                if (baseException != null)
                {
                    msg.Append("[BaseException]: ");
                    msg.Append(ex.GetBaseException());
                }
            }
            catch (Exception iex)
            {
                Debug.WriteLine($"[WARNING] ToLogString: {iex.Message}");
            }
        }
        return msg.ToString();
    }

    /// <summary>
    /// A application resource fetching helper. How you would use in code:
    ///    yourButton.Style = Extensions.Get<Style>("ButtonNormalStyle");
    /// And if you wanted to get a brush resource you'd use:
    ///    ItemTemplate = Extensions.Get<DataTemplate>("MyDataTemplate");
    /// </summary>
    public static T GetAppResource<T>(string resourceName) where T : class
    {
        return System.Windows.Application.Current.TryFindResource(resourceName) as T;
    }

    /// <summary>
    /// Find & return a <see cref="System.Windows.Controls.Control"/> based on its resource key name.
    /// </summary>
    public static T FindControl<T>(this System.Windows.Controls.Control control, string resourceKey) where T : System.Windows.Controls.Control
    {
        return (T)control.FindResource(resourceKey);
    }

    /// <summary>
    /// Find & return a <see cref="System.Windows.FrameworkElement"/> based on its resource key name.
    /// </summary>
    public static T FindControl<T>(this System.Windows.FrameworkElement control, string resourceKey) where T : System.Windows.FrameworkElement
    {
        return (T)control.FindResource(resourceKey);
    }

    /// <summary>
    /// Finds all controls on a Window object by their type.
    /// </summary>
    /// <typeparam name="T">type of control to find</typeparam>
    /// <param name="depObj">the <see cref="DependencyObject"/> to search</param>
    /// <returns><see cref="IEnumerable{T}"/> of controls matching the type</returns>
    /// <remarks>
    /// If you're trying to get this to work and finding that your Window has zero
    /// visual children, try running this method in the Loaded event handler. 
    /// If you run it in the constructor (even after InitializeComponent()), 
    /// the visual children aren't loaded yet, and won't work as expected. 
    /// </remarks>
    public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject depObj) where T : DependencyObject
    {
        /* [Example]
        foreach (TextBlock tb in FindVisualChildren<TextBlock>(window))
        {
            // do something with tb here
        }
        */
        if (depObj == null)
            yield return (T)Enumerable.Empty<T>();

        // NOTE: Switching VisualTreeHelper to LogicalTreeHelpers will cause invisible elements to be included too.
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
        {
            DependencyObject ithChild = VisualTreeHelper.GetChild(depObj, i);
            if (ithChild == null)
                continue;
            if (ithChild is T t)
                yield return t;
            foreach (T childOfChild in FindVisualChildren<T>(ithChild))
                yield return childOfChild;
        }
    }

    public static void HideAllVisualChildren<T>(this UIElementCollection coll) where T : UIElementCollection
    {
        // Casting the UIElementCollection into List
        List<FrameworkElement> lstElement = coll.Cast<FrameworkElement>().ToList();

        // Getting all Control from list
        var lstControl = lstElement.OfType<Control>();

        // Hide all Controls
        foreach (Control control in lstControl)
        {
            if (control == null)
                continue;

            control.Visibility = System.Windows.Visibility.Hidden;
        }
    }

    public static IEnumerable<System.Windows.Controls.Control> GetAllControls<T>(this UIElementCollection coll) where T : UIElementCollection
    {
        // Casting the UIElementCollection into List
        List<FrameworkElement> lstElement = coll.Cast<FrameworkElement>().ToList();

        // Geting all Control from list
        var lstControl = lstElement.OfType<Control>();

        // Iterate control objects
        foreach (Control control in lstControl)
        {
            if (control == null)
                continue;

            yield return control;
        }
    }

    /// <summary>
    /// EXAMPLE: IEnumerable<DependencyObject> ctrls = this.FindUIElements();
    /// NOTE: If you're trying to get this to work and finding that your 
    /// Window (for instance) has 0 visual children, try running this method 
    /// in the Loaded event handler. If you run it in the constructor 
    /// (even after InitializeComponent()), the visual children aren't 
    /// loaded yet, and it won't work. 
    /// </summary>
    /// <param name="parent">some parent control like <see cref="System.Windows.Window"/></param>
    /// <returns>list of <see cref="IEnumerable{DependencyObject}"/></returns>
    public static IEnumerable<DependencyObject> FindUIElements(this DependencyObject parent)
    {
        if (parent == null)
            yield break;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            DependencyObject o = VisualTreeHelper.GetChild(parent, i);

            foreach (DependencyObject obj in FindUIElements(o))
            {
                if (obj == null)
                    continue;

                if (obj is UIElement ret)
                    yield return ret;
            }
        }

        yield return parent;
    }

    /// <summary>
    /// Image helper method
    /// </summary>
    /// <param name="UriPath"></param>
    /// <returns><see cref="BitmapFrame"/></returns>
    public static BitmapFrame? GetBitmapFrame(this string UriPath)
    {
        try
        {
            IconBitmapDecoder ibd = new IconBitmapDecoder(
                new Uri(UriPath, UriKind.RelativeOrAbsolute),
                         BitmapCreateOptions.None,
                         BitmapCacheOption.Default);
            return ibd.Frames[0];
        }
        catch (System.IO.IOException ex)
        {
            Debug.WriteLine($"[WARNING] GetBitmapFrame(IOException): {ex.Message}");
        }
        catch (System.IO.FileFormatException ex)
        {
            Debug.WriteLine($"[WARNING] GetBitmapFrame(FileFormatException): {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// To populate parameters with a typical URI assigning format.
    /// This method assumes the format is like "mode=1,state=2,theme=dark"
    /// </summary>
    public static Dictionary<string, string> ParseAssignedValues(string inputString, string delimiter = ",")
    {
        Dictionary<string, string> parameters = new();

        try
        {
            var parts = inputString.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
            parameters = parts.Select(x => x.Split("=")).ToDictionary(x => x.First(), x => x.Last());
        }
        catch (Exception ex) { Debug.WriteLine($"[ERROR] ParseAssignedValues: {ex.Message}"); }

        return parameters;
    }


    /// <summary>
    /// IEnumerable file reader.
    /// </summary>
    public static IEnumerable<string> ReadFileLines(string path)
    {
        string? line = string.Empty;

        if (!File.Exists(path))
            yield return line;
        else
        {
            using (TextReader reader = File.OpenText(path))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }
    }

    /// <summary>
    /// IAsyncEnumerable file reader.
    /// </summary>
    public static async IAsyncEnumerable<string> ReadFileLinesAsync(string path)
    {
        string? line = string.Empty;

        if (!File.Exists(path))
            yield return line;
        else
        {
            using (TextReader reader = File.OpenText(path))
            {
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    yield return line;
                }
            }
        }
    }

    /// <summary>
    /// File writer for <see cref="IEnumerable{T}"/> parameters.
    /// </summary>
    public static bool WriteFileLines(string path, IEnumerable<string> lines)
    {
        using (TextWriter writer = File.CreateText(path))
        {
            foreach (var line in lines)
            {
                writer.WriteLine(line);
            }
        }

        return true;
    }

    /// <summary>
    /// De-dupe file reader using a <see cref="HashSet{T}"/>.
    /// </summary>
    public static HashSet<string> ReadLines(string path)
    {
        if (!File.Exists(path))
            return new();

        return new HashSet<string>(File.ReadAllLines(path), StringComparer.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// De-dupe file writer using a <see cref="HashSet{T}"/>.
    /// </summary>
    public static bool WriteLines(string path, IEnumerable<string> lines)
    {
        var output = new HashSet<string>(lines, StringComparer.InvariantCultureIgnoreCase);

        using (TextWriter writer = File.CreateText(path))
        {
            foreach (var line in output)
            {
                writer.WriteLine(line);
            }
        }
        return true;
    }

    public static T? DeserializeFromFile<T>(string filePath, ref string error)
    {
        try
        {
            string jsonString = System.IO.File.ReadAllText(filePath);
            T? result = System.Text.Json.JsonSerializer.Deserialize<T>(jsonString);
            error = string.Empty;
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] {nameof(DeserializeFromFile)}: {ex.Message}");
            error = ex.Message;
            return default;
        }
    }

    public static bool SerializeToFile<T>(T obj, string filePath, ref string error)
    {
        if (obj == null || string.IsNullOrEmpty(filePath))
            return false;

        try
        {
            string jsonString = System.Text.Json.JsonSerializer.Serialize(obj);
            System.IO.File.WriteAllText(filePath, jsonString);
            error = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] {nameof(SerializeToFile)}: {ex.Message}");
            error = ex.Message;
            return false;
        }
    }

    #region [Dispatcher Extensions]
    /// <summary>
    /// Invokes the specified <paramref name="action"/> on the given <paramref name="dispatcher"/>.
    /// </summary>
    /// <param name="dispatcher">The dispatcher on which the <paramref name="action"/> executes.</param>
    /// <param name="action">The <see cref="Action"/> to execute.</param>
    /// <param name="priority">The <see cref="DispatcherPriority"/>.  Defaults to <see cref="DispatcherPriority.ApplicationIdle"/></param>
    public static void InvokeAction(this System.Windows.Threading.Dispatcher dispatcher, Action action, System.Windows.Threading.DispatcherPriority priority)
    {
        /*
        [old way]
        dispatcher.Invoke((Action<string>)((x) => { Console.Write(x); }), "annoying");
        [this way]
        dispatcher.InvokeAction(x => Console.Write(X), "yay lol");
        */
        if (dispatcher == null)
            throw new ArgumentNullException("dispatcher");
        if (action == null)
            throw new ArgumentNullException("action");

        dispatcher.Invoke(action, priority);
    }
    /// <summary>
    /// Invokes the specified <paramref name="action"/> on the given <paramref name="dispatcher"/>.
    /// </summary>
    /// <typeparam name="T">The type of the argument of the <paramref name="action"/>.</typeparam>
    /// <param name="dispatcher">The dispatcher on which the <paramref name="action"/> executes.</param>
    /// <param name="action">The <see cref="Action{T}"/> to execute.</param>
    /// <param name="arg">The first argument of the action.</param>
    /// <param name="priority">The <see cref="DispatcherPriority"/>.  Defaults to <see cref="DispatcherPriority.ApplicationIdle"/></param>
    public static void InvokeAction<T>(this System.Windows.Threading.Dispatcher dispatcher, Action<T> action, T arg, System.Windows.Threading.DispatcherPriority priority = System.Windows.Threading.DispatcherPriority.ApplicationIdle)
    {
        if (dispatcher == null)
            throw new ArgumentNullException("dispatcher");
        if (action == null)
            throw new ArgumentNullException("action");

        dispatcher.Invoke(action, priority, arg);
    }
    /// <summary>
    /// Invokes the specified <paramref name="action"/> on the given <paramref name="dispatcher"/>.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument of the <paramref name="action"/>.</typeparam>
    /// <typeparam name="T2">The type of the second argument of the <paramref name="action"/>.</typeparam>
    /// <param name="dispatcher">The dispatcher on which the <paramref name="action"/> executes.</param>
    /// <param name="action">The <see cref="Action{T1,T2}"/> to execute.</param>
    /// <param name="arg1">The first argument of the action.</param>
    /// <param name="arg2">The second argument of the action.</param>
    /// <param name="priority">The <see cref="DispatcherPriority"/>.  Defaults to <see cref="DispatcherPriority.ApplicationIdle"/></param>
    public static void InvokeAction<T1, T2>(this System.Windows.Threading.Dispatcher dispatcher, Action<T1, T2> action, T1 arg1, T2 arg2, System.Windows.Threading.DispatcherPriority priority = System.Windows.Threading.DispatcherPriority.ApplicationIdle)
    {
        if (dispatcher == null)
            throw new ArgumentNullException("dispatcher");
        if (action == null)
            throw new ArgumentNullException("action");

        dispatcher.Invoke(action, priority, arg1, arg2);
    }
    /// <summary>
    /// Invokes the specified <paramref name="action"/> on the given <paramref name="dispatcher"/>.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument of the <paramref name="action"/>.</typeparam>
    /// <typeparam name="T2">The type of the second argument of the <paramref name="action"/>.</typeparam>
    /// <typeparam name="T3">The type of the third argument of the <paramref name="action"/>.</typeparam>
    /// <param name="dispatcher">The dispatcher on which the <paramref name="action"/> executes.</param>
    /// <param name="action">The <see cref="Action{T1,T2,T3}"/> to execute.</param>
    /// <param name="arg1">The first argument of the action.</param>
    /// <param name="arg2">The second argument of the action.</param>
    /// <param name="arg3">The third argument of the action.</param>
    /// <param name="priority">The <see cref="DispatcherPriority"/>.  Defaults to <see cref="DispatcherPriority.ApplicationIdle"/></param>
    public static void InvokeAction<T1, T2, T3>(this System.Windows.Threading.Dispatcher dispatcher, Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3, System.Windows.Threading.DispatcherPriority priority = System.Windows.Threading.DispatcherPriority.ApplicationIdle)
    {
        if (dispatcher == null)
            throw new ArgumentNullException("dispatcher");
        if (action == null)
            throw new ArgumentNullException("action");

        dispatcher.Invoke(action, priority, arg1, arg2, arg3);
    }
    #endregion [Dispatcher Extensions]

}
