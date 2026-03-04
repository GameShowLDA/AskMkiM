using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace UI.Controls.EmptyWorkspace
{
  public partial class EmptyWorkspaceView : UserControl, INotifyPropertyChanged
  {
    private readonly DispatcherTimer _timer;
    private DateTime _currentDateTime;
    private string _buildDate = string.Empty;
    private string _appVersion = string.Empty;

    private static Assembly AppAssembly => Assembly.GetEntryAssembly() ?? throw new InvalidOperationException("EntryAssembly not found");

    public DateTime CurrentDateTime
    {
      get => _currentDateTime;
      set
      {
        if (_currentDateTime != value)
        {
          _currentDateTime = value;
          OnPropertyChanged(nameof(CurrentDateTime));
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

      CurrentDateTime = DateTime.Now;
      BuildDate = GetBuildDate();
      AppVersion = GetAppVersion();

      _timer = new DispatcherTimer(DispatcherPriority.Background)
      {
        Interval = TimeSpan.FromSeconds(1)
      };

      _timer.Tick += (_, __) => CurrentDateTime = DateTime.Now;
      _timer.Start();

      Unloaded += (_, __) => _timer.Stop();
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
      var data = AppAssembly.GetName().Version;

      var versionValue = version is null
        ? "Неизвестно"
        : $"{version.Major}.{version.Minor}.{version.Build}";

      return $"Версия {versionValue} • Сборка {BuildDate}";
    }


    private void OnPropertyChanged(string propertyName)
      => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
}
