using Ask.Core.Services.App;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для TimeControl.xaml
  /// </summary>
  public partial class TimeControl : UserControl
  {
    public static readonly DependencyProperty FullTimeProperty =
      DependencyProperty.Register(
        nameof(FullTime),
        typeof(bool),
        typeof(TimeControl),
        new PropertyMetadata(false, OnFullTimeChanged));

    private DateTime _lastDisplayedDate = DateTime.Today;
    private string _lastRenderedText = string.Empty;
    private bool _hasRendered;
    private bool _isSubscribed;

    public TimeControl()
    {
      InitializeComponent();
      Loaded += TimeControl_Loaded;
      Unloaded += TimeControl_Unloaded;
    }

    public event Action? ChangeDate;

    public bool FullTime
    {
      get => (bool)GetValue(FullTimeProperty);
      set => SetValue(FullTimeProperty, value);
    }

    private static void OnFullTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is TimeControl control)
      {
        control.UpdateClock(ApplicationClockService.CurrentDateTime);
      }
    }

    private void TimeControl_Loaded(object sender, RoutedEventArgs e)
    {
      UpdateClock(ApplicationClockService.CurrentDateTime);
      SubscribeToClock();
    }

    private void TimeControl_Unloaded(object sender, RoutedEventArgs e)
    {
      UnsubscribeFromClock();
    }

    private void OnClockTimeChanged(DateTime currentDateTime)
    {
      if (!Dispatcher.CheckAccess())
      {
        Dispatcher.BeginInvoke(() => UpdateClock(currentDateTime));
        return;
      }

      UpdateClock(currentDateTime);
    }

    private void UpdateClock(DateTime now)
    {
      var renderedText = FullTime ? now.ToString("HH:mm:ss") : now.ToString("HH:mm");

      if (_lastRenderedText != renderedText)
      {
        _lastRenderedText = renderedText;
        var previousText = Clock.Text;
        Clock.Text = renderedText;

        if (_hasRendered)
        {
          AnimateClockText(previousText);
        }

        _hasRendered = true;
      }

      if (_lastDisplayedDate != now.Date)
      {
        _lastDisplayedDate = now.Date;
        ChangeDate?.Invoke();
      }
    }

    private void AnimateClockText(string previousText)
    {
      if (string.IsNullOrEmpty(previousText))
      {
        return;
      }

      var easing = new CubicEase
      {
        EasingMode = EasingMode.EaseOut,
      };

      ClockGhost.Text = previousText;
      ClockGhost.BeginAnimation(OpacityProperty, new DoubleAnimation
      {
        From = 0.24,
        To = 0.0,
        Duration = TimeSpan.FromMilliseconds(150),
        EasingFunction = easing,
      });

      Clock.BeginAnimation(OpacityProperty, new DoubleAnimation
      {
        From = 0.88,
        To = 1.0,
        Duration = TimeSpan.FromMilliseconds(150),
        EasingFunction = easing,
      });
    }

    private void SubscribeToClock()
    {
      if (_isSubscribed)
      {
        return;
      }

      ApplicationClockService.TimeChanged += OnClockTimeChanged;
      _isSubscribed = true;
    }

    private void UnsubscribeFromClock()
    {
      if (!_isSubscribed)
      {
        return;
      }

      ApplicationClockService.TimeChanged -= OnClockTimeChanged;
      _isSubscribed = false;
    }
  }
}
