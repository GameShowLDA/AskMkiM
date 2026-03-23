using Ask.Core.Services.App;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace UI.Controls.EmptyWorkspace
{
  public partial class EmptyWorkspaceView : UserControl, INotifyPropertyChanged
  {
    private DateTime _currentDateTime;
    private string _buildDate = string.Empty;
    private string _appVersion = string.Empty;
    private bool _hasAnimatedClock;
    private bool _isSubscribed;

    private static Assembly AppAssembly => Assembly.GetEntryAssembly() ?? throw new InvalidOperationException("EntryAssembly not found");

    public DateTime CurrentDateTime
    {
      get => _currentDateTime;
      set
      {
        if (_currentDateTime != value)
        {
          var previousTimeText = GetTimeText(_currentDateTime);
          _currentDateTime = value;
          OnPropertyChanged(nameof(CurrentDateTime));

          if (_hasAnimatedClock)
          {
            AnimateMainClock(previousTimeText);
          }

          _hasAnimatedClock = true;
        }
      }
    }

    public string BuildDate
    {
      get => _buildDate;
      set
      {
        if (_buildDate != value)
        {
          _buildDate = value;
          OnPropertyChanged(nameof(BuildDate));
        }
      }
    }

    public string AppVersion
    {
      get => _appVersion;
      set
      {
        if (_appVersion != value)
        {
          _appVersion = value;
          OnPropertyChanged(nameof(AppVersion));
        }
      }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public EmptyWorkspaceView()
    {
      InitializeComponent();

      UpdateCurrentDateTime(ApplicationClockService.CurrentDateTime);
      BuildDate = GetBuildDate();
      AppVersion = GetAppVersion();

      Loaded += EmptyWorkspaceView_Loaded;
      Unloaded += EmptyWorkspaceView_Unloaded;
    }

    private string GetBuildDate()
    {
      try
      {
        var asm = AppAssembly;

        var attr = asm.GetCustomAttributes<AssemblyMetadataAttribute>()
                      .FirstOrDefault(a => a.Key == "BuildDate");

        return !string.IsNullOrWhiteSpace(attr?.Value)
          ? attr.Value
          : "Неизвестно";
      }
      catch
      {
        return "Неизвестно";
      }
    }

    private string GetAppVersion()
    {
      var version = AppAssembly.GetName().Version;

      var versionValue = version is null
        ? "Неизвестно"
        : $"{version.Major}.{version.Minor}.{version.Build}";

      return $"Версия {versionValue} • Сборка {BuildDate}";
    }

    private void OnPropertyChanged(string propertyName)
      => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void EmptyWorkspaceView_Loaded(object sender, RoutedEventArgs e)
    {
      UpdateCurrentDateTime(ApplicationClockService.CurrentDateTime);
      SubscribeToClock();
    }

    private void EmptyWorkspaceView_Unloaded(object sender, RoutedEventArgs e)
    {
      UnsubscribeFromClock();
    }

    private void UpdateCurrentDateTime(DateTime now)
    {
      var truncatedNow = new DateTime(
        now.Year,
        now.Month,
        now.Day,
        now.Hour,
        now.Minute,
        now.Second);

      CurrentDateTime = truncatedNow;
    }

    private void OnClockTimeChanged(DateTime currentDateTime)
    {
      if (!Dispatcher.CheckAccess())
      {
        Dispatcher.BeginInvoke(() => UpdateCurrentDateTime(currentDateTime));
        return;
      }

      UpdateCurrentDateTime(currentDateTime);
    }

    private void AnimateMainClock(string previousTimeText)
    {
      if (!IsLoaded || string.IsNullOrEmpty(previousTimeText))
      {
        return;
      }

      var easing = new CubicEase
      {
        EasingMode = EasingMode.EaseOut,
      };

      TimeGhostText.Text = previousTimeText;
      TimeGhostText.BeginAnimation(OpacityProperty, new DoubleAnimation
      {
        From = 0.22,
        To = 0.0,
        Duration = TimeSpan.FromMilliseconds(170),
        EasingFunction = easing,
      });

      TimeText.BeginAnimation(OpacityProperty, new DoubleAnimation
      {
        From = 0.9,
        To = 1.0,
        Duration = TimeSpan.FromMilliseconds(170),
        EasingFunction = easing,
      });
    }

    private static string GetTimeText(DateTime dateTime)
    {
      if (dateTime == default)
      {
        return string.Empty;
      }

      return dateTime.ToString("HH:mm:ss");
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
