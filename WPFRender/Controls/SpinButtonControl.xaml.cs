using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace WPFRender.Controls
{
    /// <summary>
    /// Interaction logic for SpinButtonControl.xaml
    /// </summary>
    public partial class SpinButtonControl : UserControl
    {
        #region [Properties]
        Storyboard _sb = new Storyboard();

        /// <summary>
        /// Mouse click event property.
        /// </summary>
        public RoutedEventHandler SpinClickEvent
        {
            get { return (RoutedEventHandler)this.GetValue(SpinClickEventProperty); }
            set { this.SetValue(SpinClickEventProperty, value); }
        }
        public static readonly DependencyProperty SpinClickEventProperty = DependencyProperty.Register(
            nameof(SpinClickEvent),
            typeof(RoutedEventHandler),
            typeof(SpinButtonControl),
            new PropertyMetadata(null));

        /// <summary>
        /// Control width property.
        /// </summary>
        public double SpinWidth
        {
            get { return (double)this.GetValue(SpinWidthProperty); }
            set { this.SetValue(SpinWidthProperty, value); }
        }
        public static readonly DependencyProperty SpinWidthProperty = DependencyProperty.Register(
            nameof(SpinWidth),
            typeof(double),
            typeof(SpinButtonControl),
            new PropertyMetadata(100d));

        /// <summary>
        /// Control height property.
        /// </summary>
        public double SpinHeight
        {
            get { return (double)this.GetValue(SpinHeightProperty); }
            set { this.SetValue(SpinHeightProperty, value); }
        }
        public static readonly DependencyProperty SpinHeightProperty = DependencyProperty.Register(
            nameof(SpinHeight),
            typeof(double),
            typeof(SpinButtonControl),
            new PropertyMetadata(100d));

        /// <summary>
        /// Fill color property.
        /// </summary>
        public Brush SpinBrush
        {
            get { return (Brush)this.GetValue(SpinBrushProperty); }
            set { this.SetValue(SpinBrushProperty, value); }
        }
        public static readonly DependencyProperty SpinBrushProperty = DependencyProperty.Register(
            nameof(SpinBrush),
            typeof(Brush),
            typeof(SpinButtonControl),
            new PropertyMetadata(new SolidColorBrush(Colors.DodgerBlue)));

        /// <summary>
        /// Spin speed property
        /// </summary>
        public Duration SpinSpeed
        {
            get { return (Duration)GetValue(SpinSpeedProperty); }
            set { SetValue(SpinSpeedProperty, value); }
        }
        public static readonly DependencyProperty SpinSpeedProperty = DependencyProperty.Register(
            nameof(SpinSpeed),
            typeof(Duration),
            typeof(SpinButtonControl),
            new PropertyMetadata(new Duration(TimeSpan.FromMilliseconds(1000)), OnSpeedValueChanged));

        /// <summary>
        /// We have to provide a go-between to get from the static callback to an instance-based method.
        /// </summary>
        static void OnSpeedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SpinButtonControl)d).OnSpeedChanged(e);
        }
        protected virtual void OnSpeedChanged(DependencyPropertyChangedEventArgs e)
        {
            Duration? d = e.NewValue as Duration?;
            if (d != null)
            {
                // Change the control animation.
                _sb.Stop();
                _sb.Children.Clear();
                var animation = new DoubleAnimation
                {
                    RepeatBehavior = RepeatBehavior.Forever,
                    Duration = d.Value,
                    From = SpinClockwise ? 0 : 360,
                    To = SpinClockwise ? 360 : 0
                };
                // Set the target of the animation
                Storyboard.SetTarget(animation, this.btnSpin);
                Storyboard.SetTargetProperty(animation, new PropertyPath("(UIElement.RenderTransform).(RotateTransform.Angle)"));
                // Kick the animation off
                _sb.Children.Add(animation);
                _sb.Begin();
            }
        }

        /// <summary>
        /// If false the animation will run counter-clockwise.
        /// </summary>
        public bool SpinClockwise
        {
            get { return (bool)GetValue(SpinClockwiseProperty); }
            set { SetValue(SpinClockwiseProperty, value); }
        }
        public static readonly DependencyProperty SpinClockwiseProperty = DependencyProperty.Register(
            nameof(SpinClockwise),
            typeof(bool),
            typeof(SpinButtonControl),
            new PropertyMetadata(true, OnSpinClockwiseChanged));

        /// <summary>
        /// We have to provide a go-between to get from the static callback to an instance-based method.
        /// </summary>
        static void OnSpinClockwiseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SpinButtonControl)d).OnClockwiseChanged(e);
        }
        protected virtual void OnClockwiseChanged(DependencyPropertyChangedEventArgs e)
        {
            bool? d = e.NewValue as bool?;
            if (d != null)
            {
                // Change the control animation.
                _sb.Stop();
                _sb.Children.Clear();
                var animation = new DoubleAnimation
                {
                    RepeatBehavior = RepeatBehavior.Forever,
                    Duration = SpinSpeed,
                    From = d.Value ? 0 : 360,
                    To = d.Value ? 360 : 0
                };
                // Set the target of the animation
                Storyboard.SetTarget(animation, this.btnSpin);
                Storyboard.SetTargetProperty(animation, new PropertyPath("(UIElement.RenderTransform).(RotateTransform.Angle)"));
                // Kick the animation off
                _sb.Children.Add(animation);
                _sb.Begin();
            }
        }

        /// <summary>
        /// If false the animation will not run and the control will be collapsed.
        /// </summary>
        public bool SpinVisible
        {
            get { return (bool)GetValue(SpinVisibleProperty); }
            set { SetValue(SpinVisibleProperty, value); }
        }
        public static readonly DependencyProperty SpinVisibleProperty = DependencyProperty.Register(
            nameof(SpinVisible),
            typeof(bool),
            typeof(SpinButtonControl),
            new PropertyMetadata(true, OnSpinVisibleChanged));

        /// <summary>
        /// We have to provide a go-between to get from the static callback to an instance-based method.
        /// </summary>
        static void OnSpinVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SpinButtonControl)d).OnVisibleChanged(e);
        }
        protected virtual void OnVisibleChanged(DependencyPropertyChangedEventArgs e)
        {
            bool? d = e.NewValue as bool?;
            if (d != null)
            {
                if (!d.Value)
                {
                    this.btnSpin.Visibility = Visibility.Collapsed;
                    _sb.Stop();
                }
                else
                {
                    this.btnSpin.Visibility = Visibility.Visible;
                    _sb.Begin();
                }
            }
        }

        /// <summary>
        /// This will be the path data.
        /// </summary>
        public Geometry SpinIcon
        {
            get { return (Geometry)GetValue(SpinIconProperty); }
            set { SetValue(SpinIconProperty, value); }
        }
        public static readonly DependencyProperty SpinIconProperty = DependencyProperty.Register(
            nameof(SpinIcon),
            typeof(Geometry),
            typeof(SpinButtonControl),
            new PropertyMetadata(Geometry.Parse("M11 2h2v5h-2zm0 15h2v5h-2zm11-6v2h-5v-2zM7 11v2H2v-2zm11.364-6.778l1.414 1.414l-3.536 3.536l-1.414-1.414zM7.758 14.828l1.414 1.414l-3.536 3.536l-1.414-1.414zm12.02 3.536l-1.414 1.414l-3.536-3.536l1.414-1.414zM9.172 7.758L7.758 9.172L4.222 5.636l1.414-1.414z")));
        #endregion

        public SpinButtonControl()
        {
            InitializeComponent();
            ConfigureAnimation();
        }

        void btnSpin_Click(object sender, RoutedEventArgs e) => SpinClickEvent?.Invoke(this, new RoutedEventArgs());

        void ConfigureAnimation()
        {
            var animation = new DoubleAnimation
            {
                RepeatBehavior = RepeatBehavior.Forever,
                Duration = new Duration(TimeSpan.FromMilliseconds(1000)),
                From = SpinClockwise ? 0 : 360,
                To = SpinClockwise ? 360 : 0
            };
            // Set the target of the animation
            Storyboard.SetTarget(animation, this.btnSpin);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(UIElement.RenderTransform).(RotateTransform.Angle)"));
            // Kick the animation off
            _sb.Children.Add(animation);
            _sb.Begin();
        }
    }

}
