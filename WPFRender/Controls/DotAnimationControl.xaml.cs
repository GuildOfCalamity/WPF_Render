using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WPFRender.Controls
{
    public partial class DotAnimationControl : UserControl
    {
        const double minimumScale = 0.5;
        Storyboard? dotAnimationStoryboard;

        public DotAnimationControl()
        {
            InitializeComponent();
            CreateDotAnimationStoryboard();
            Loaded += (s, e) =>
            {   // Initialize animation state based on IsRunning property.
                UpdateAnimationState();
            };
        }

        /// <summary>
        /// DotSpacing Dependency Property
        /// </summary>
        public double DotSpacing
        {
            get { return (double)GetValue(DotSpacingProperty); }
            set { SetValue(DotSpacingProperty, value); }
        }
        public static readonly DependencyProperty DotSpacingProperty = DependencyProperty.Register(
            nameof(DotSpacing),
            typeof(double),
            typeof(DotAnimationControl),
            new PropertyMetadata(12d, OnDotSpacingChanged));

        /// <summary>
        /// DotSize Dependency Property
        /// </summary>
        public double DotSize
        {
            get { return (double)GetValue(DotSizeProperty); }
            set { SetValue(DotSizeProperty, value); }
        }
        public static readonly DependencyProperty DotSizeProperty = DependencyProperty.Register(
            nameof(DotSize),
            typeof(double),
            typeof(DotAnimationControl),
            new PropertyMetadata(18d, OnDotSizeChanged));

        /// <summary>
        /// IsRunning Dependency Property
        /// </summary>
        public bool IsRunning
        {
            get { return (bool)GetValue(IsRunningProperty); }
            set { SetValue(IsRunningProperty, value); }
        }
        public static readonly DependencyProperty IsRunningProperty = DependencyProperty.Register(
            nameof(IsRunning),
            typeof(bool),
            typeof(DotAnimationControl),
            new PropertyMetadata(false, OnIsRunningChanged));

        static void OnIsRunningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (DotAnimationControl)d;
            control.UpdateAnimationState();
        }

        /// <summary>
        /// FillColor Dependency Property
        /// </summary>
        public Brush FillColor
        {
            get { return (Brush)GetValue(FillColorProperty); }
            set { SetValue(FillColorProperty, value); }
        }
        public static readonly DependencyProperty FillColorProperty = DependencyProperty.Register(
            nameof(FillColor),
            typeof(Brush),
            typeof(DotAnimationControl),
            new PropertyMetadata(Brushes.White));

        /// <summary>
        /// Updates the animation based on the IsRunning dependency property.
        /// </summary>
        void UpdateAnimationState()
        {
            // If you were fetching the storyboard from the XAML:
            //var sb = (Storyboard)Resources["DotAnimationStoryboard"];

            if (IsRunning)
            {
                Visibility = Visibility.Visible;
                dotAnimationStoryboard?.Begin(this, true);
            }
            else
            {
               dotAnimationStoryboard?.Stop(this);
               Visibility = Visibility.Collapsed;
            }
        }

        static void OnDotSpacingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (DotAnimationControl)d;
            if (e.NewValue != null && e.NewValue is double cs)
            {
                control.UpdateDotSpacing(cs);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[WARNING] e.NewValue is null or is not the correct type.");
            }
        }

        void UpdateDotSpacing(double space)
        {
            if (space != double.NaN && space > 0)
            {
                //cc1.Width = cc2.Width = cc3.Width = cc4.Width = new GridLength(space * 1.111d);
                hostGrid.Width = space * 5.5d;
            }
        }

        static void OnDotSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (DotAnimationControl)d;
            if (e.NewValue != null && e.NewValue is double cs)
            {
                control.UpdateDotSize(cs);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[WARNING] e.NewValue is null or is not the correct type.");
            }
        }

        void UpdateDotSize(double size)
        {
            if (size != double.NaN && size > 0)
            {
                var corner = Math.Ceiling(size / 3d); // Math.Ceiling(Math.Sqrt(size / 3d));
                Dot1.Width = Dot1.Height = size;
                Dot1.RadiusX = Dot1.RadiusY = corner;
                Dot2.Width = Dot2.Height = size;
                Dot2.RadiusX = Dot2.RadiusY = corner;
                Dot3.Width = Dot3.Height = size;
                Dot3.RadiusX = Dot3.RadiusY = corner;
                Dot4.Width = Dot4.Height = size;
                Dot4.RadiusX = Dot4.RadiusY = corner;
            }
        }

        /// <summary>
        /// Create the DotAnimationStoryboard programmatically
        /// </summary>
        void CreateDotAnimationStoryboard()
        {
            dotAnimationStoryboard = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };
            Duration duration = new Duration(TimeSpan.FromSeconds(0.4));
            AddDotAnimations(Dot1, TimeSpan.FromSeconds(0), duration);
            AddDotAnimations(Dot2, TimeSpan.FromSeconds(0.25), duration);
            AddDotAnimations(Dot3, TimeSpan.FromSeconds(0.5), duration);
            AddDotAnimations(Dot4, TimeSpan.FromSeconds(0.75), duration);
        }

        /// <summary>
        /// Method to add animations to a dot with a specific delay and duration
        /// </summary>
        /// <param name="dot"></param>
        /// <param name="beginTime"></param>
        /// <param name="duration"></param>
        void AddDotAnimations(UIElement dot, TimeSpan beginTime, Duration duration)
        {
            // Ensure each dot has a ScaleTransform applied
            var scaleTransform = new ScaleTransform(minimumScale, minimumScale);
            dot.RenderTransform = scaleTransform;
            dot.RenderTransformOrigin = new Point(0.5, 0.5);

            #region [ScaleX Animation]
            var scaleXAnimation = new DoubleAnimation
            {
                From = minimumScale,
                To = 1,
                Duration = duration,
                AutoReverse = true,
                BeginTime = beginTime, 
                EasingFunction = new QuadraticEase()
            };
            Storyboard.SetTarget(scaleXAnimation, dot);
            Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath("RenderTransform.ScaleX"));
            dotAnimationStoryboard?.Children?.Add(scaleXAnimation);
            #endregion

            #region [ScaleY Animation]
            var scaleYAnimation = new DoubleAnimation
            {
                From = minimumScale,
                To = 1,
                Duration = duration,
                AutoReverse = true,
                BeginTime = beginTime,
                EasingFunction = new QuadraticEase()
            };
            Storyboard.SetTarget(scaleYAnimation, dot);
            Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath("RenderTransform.ScaleY"));
            dotAnimationStoryboard?.Children?.Add(scaleYAnimation);
            #endregion
        }
    }

}
