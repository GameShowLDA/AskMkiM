using Ask.Core.Services.App;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using UI.Controls.Calendar;

namespace UI.Controls.EmptyWorkspace
{
  public partial class EmptyWorkspaceView : UserControl, INotifyPropertyChanged
  {
    private DateTime _currentDateTime;
    private string _buildDate = string.Empty;
    private string _appVersion = string.Empty;
    private string _todayNoteText = string.Empty;
    private bool _hasAnimatedClock;
    private bool _hasTodayNote;
    private bool _isTodayNoteExpanded;
    private bool _isSubscribed;
    private readonly CalendarNoteStore _calendarNoteStore = new();

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

    public string TodayNoteText
    {
      get => _todayNoteText;
      set
      {
        if (_todayNoteText != value)
        {
          _todayNoteText = value;
          OnPropertyChanged(nameof(TodayNoteText));
          OnPropertyChanged(nameof(TodayNoteBarText));
        }
      }
    }

    public bool HasTodayNote
    {
      get => _hasTodayNote;
      set
      {
        if (_hasTodayNote != value)
        {
          _hasTodayNote = value;
          OnPropertyChanged(nameof(HasTodayNote));
        }
      }
    }

    public bool IsTodayNoteExpanded
    {
      get => _isTodayNoteExpanded;
      set
      {
        if (_isTodayNoteExpanded != value)
        {
          _isTodayNoteExpanded = value;
          OnPropertyChanged(nameof(IsTodayNoteExpanded));
          OnPropertyChanged(nameof(TodayNoteBarText));
        }
      }
    }

    public string TodayNoteBarText => IsTodayNoteExpanded ? "Свернуть" : GetTodayNotePreviewText();

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
      CalendarNoteStore.NotesChanged += CalendarNoteStore_NotesChanged;
    }

    private void EmptyWorkspaceView_Unloaded(object sender, RoutedEventArgs e)
    {
      UnsubscribeFromClock();
      CalendarNoteStore.NotesChanged -= CalendarNoteStore_NotesChanged;
    }

    private void UpdateCurrentDateTime(DateTime now)
    {
      var previousDate = CurrentDateTime.Date;
      var truncatedNow = new DateTime(
        now.Year,
        now.Month,
        now.Day,
        now.Hour,
        now.Minute,
        now.Second);

      CurrentDateTime = truncatedNow;

      if (previousDate != truncatedNow.Date)
      {
        SetTodayNoteExpanded(false, animate: false);
        RefreshTodayNote();
      }
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

    private void RefreshTodayNote()
    {
      var notes = _calendarNoteStore.Load();
      var today = CurrentDateTime.Date;
      var hasNote = notes.TryGetValue(today, out var noteText) && !string.IsNullOrWhiteSpace(noteText);

      HasTodayNote = hasNote;
      TodayNoteText = hasNote ? noteText! : string.Empty;

      if (!hasNote && IsTodayNoteExpanded)
      {
        SetTodayNoteExpanded(false, animate: false);
      }
    }

    private void CalendarNoteStore_NotesChanged(object? sender, EventArgs e)
    {
      if (!Dispatcher.CheckAccess())
      {
        Dispatcher.BeginInvoke(new Action(RefreshTodayNote));
        return;
      }

      RefreshTodayNote();
    }

    private void TodayNoteBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (!HasTodayNote)
      {
        return;
      }

      SetTodayNoteExpanded(!IsTodayNoteExpanded, animate: true);
      e.Handled = true;
    }

    private void SetTodayNoteExpanded(bool isExpanded, bool animate)
    {
      IsTodayNoteExpanded = isExpanded;

      if (!animate)
      {
        TodayNotePreviewPanel.Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed;
        TodayNotePreviewPanel.Opacity = isExpanded ? 1 : 0;
        TodayNotePreviewTransform.Y = isExpanded ? 0 : 18;
        return;
      }

      AnimateTodayNotePreview(isExpanded);
    }

    private void AnimateTodayNotePreview(bool isExpanded)
    {
      var duration = TimeSpan.FromMilliseconds(180);
      var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

      TodayNotePreviewPanel.BeginAnimation(OpacityProperty, null);
      TodayNotePreviewTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, null);

      if (isExpanded)
      {
        TodayNotePreviewPanel.Visibility = Visibility.Visible;
        TodayNotePreviewPanel.BeginAnimation(OpacityProperty, new DoubleAnimation
        {
          From = 0,
          To = 1,
          Duration = duration,
          EasingFunction = easing,
        });
        TodayNotePreviewTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, new DoubleAnimation
        {
          From = 18,
          To = 0,
          Duration = duration,
          EasingFunction = easing,
        });
        return;
      }

      var opacityAnimation = new DoubleAnimation
      {
        From = TodayNotePreviewPanel.Opacity,
        To = 0,
        Duration = duration,
        EasingFunction = easing,
      };
      opacityAnimation.Completed += (_, _) =>
      {
        if (!IsTodayNoteExpanded)
        {
          TodayNotePreviewPanel.Visibility = Visibility.Collapsed;
        }
      };

      TodayNotePreviewPanel.BeginAnimation(OpacityProperty, opacityAnimation);
      TodayNotePreviewTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, new DoubleAnimation
      {
        From = TodayNotePreviewTransform.Y,
        To = 18,
        Duration = duration,
        EasingFunction = easing,
      });
    }

    private string GetTodayNotePreviewText()
    {
      if (string.IsNullOrWhiteSpace(TodayNoteText))
      {
        return string.Empty;
      }

      var lines = TodayNoteText
        .Replace("\r\n", "\n")
        .Split('\n');

      var firstLine = lines.FirstOrDefault() ?? string.Empty;
      var hasMoreLines = lines.Skip(1).Any(line => !string.IsNullOrWhiteSpace(line));

      if (hasMoreLines && !firstLine.EndsWith("...", StringComparison.Ordinal))
      {
        return $"{firstLine}...";
      }

      return firstLine;
    }
  }
}
