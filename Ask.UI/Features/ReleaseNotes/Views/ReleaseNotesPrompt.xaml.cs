using Ask.Core.Services.App;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Support;
using Ask.Support.ReleaseNotes;
using Ask.LogLib;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace Ask.UI.Features.ReleaseNotes.Views;

/// <summary>Предлагает открыть изменения установленной версии до первого просмотра текущей ролью.</summary>
public partial class ReleaseNotesPrompt : UserControl
{
  private int _refreshId;

  /// <summary>Создаёт кнопку истории выпусков с подписками на время загрузки контрола.</summary>
  public ReleaseNotesPrompt()
  {
    InitializeComponent();
    Loaded += OnLoaded;
    Unloaded += OnUnloaded;
    IsVisibleChanged += (_, _) =>
    {
      if (IsLoaded && IsVisible) Refresh();
    };
  }

  private void OnLoaded(object sender, RoutedEventArgs e)
  {
    ReleaseNotesReadStateStore.Default.Changed += OnReadStateChanged;
    EventAggregator.Subscribe<SystemStateEvents.ConsoleAccessChanged>(OnRoleApplied);
    Refresh();
  }

  private void OnUnloaded(object sender, RoutedEventArgs e)
  {
    ReleaseNotesReadStateStore.Default.Changed -= OnReadStateChanged;
    EventAggregator.Unsubscribe<SystemStateEvents.ConsoleAccessChanged>(OnRoleApplied);
    _refreshId++;
  }

  private void OnRoleApplied(SystemStateEvents.ConsoleAccessChanged _) => OnReadStateChanged();

  private void OnReadStateChanged() => Dispatcher.BeginInvoke(() =>
  {
    if (IsLoaded) Refresh();
  });

  private async void Refresh()
  {
    int refreshId = ++_refreshId;
    string? role = RoleAuthorizationConfig.CurrentRole?.ToString();
    string version = ApplicationBuildInfo.Current.Version;
    if (role is null)
    {
      Visibility = Visibility.Collapsed;
      return;
    }
    try
    {
      bool show = await Task.Run(async () =>
      {
        if (ReleaseNotesReadStateStore.Default.HasViewed(role, version)) return false;
        var releases = await new ReleaseNotesReader().ReadDirectoryAsync(
          Path.Combine(AppContext.BaseDirectory, "AppHelp", "releases"));
        return releases.Any(release => release.Version == version);
      });
      if (!IsLoaded || refreshId != _refreshId || RoleAuthorizationConfig.CurrentRole?.ToString() != role) return;
      Caption.Text = $"Узнайте, что нового в версии {version}";
      OpenButton.SetValue(System.Windows.Automation.AutomationProperties.NameProperty, Caption.Text);
      Visibility = show ? Visibility.Visible : Visibility.Collapsed;
    }
    catch (Exception exception) when (exception is JsonException or InvalidDataException or IOException or UnauthorizedAccessException)
    {
      if (refreshId == _refreshId) Visibility = Visibility.Collapsed;
      LoggerUtility.LogException(exception, customMessage: "Не удалось проверить просмотр истории выпусков.");
    }
  }

  private void OpenButton_Click(object sender, RoutedEventArgs e) =>
    HelpProvider.ShowHelp(ReleaseNotesHtmlRenderer.HelpPageKey);
}
