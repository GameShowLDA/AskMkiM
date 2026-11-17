using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AskMkiM.Updater.Core;
using AskMkiM.Updater.Core.Models;
using Microsoft.Win32;

namespace AskMkiM.Updater.Stub;

/// <summary>
/// Предоставляет интерфейс установки автономного обновления.
/// </summary>
public partial class MainWindow : Window
{
  private readonly UpdateInstallationService _installer = new();
  private OpenedUpdatePackage? _package;
  private ApplicationVersionInfo? _currentVersion;

  /// <summary>Создаёт окно установки обновления.</summary>
  public MainWindow() => InitializeComponent();

  private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
  {
    if (e.ClickCount == 2)
    {
      ToggleWindowState();
      return;
    }

    if (e.LeftButton == MouseButtonState.Pressed)
    {
      DragMove();
    }
  }

  private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

  private void Maximize_Click(object sender, RoutedEventArgs e) => ToggleWindowState();

  private void Close_Click(object sender, RoutedEventArgs e) => Close();

  private void Window_StateChanged(object? sender, EventArgs e)
  {
    OuterBorder.CornerRadius = WindowState == WindowState.Maximized
      ? new CornerRadius(0)
      : new CornerRadius(18);
  }

  private void ToggleWindowState()
  {
    WindowState = WindowState == WindowState.Maximized
      ? WindowState.Normal
      : WindowState.Maximized;
  }

  private async void Window_Loaded(object sender, RoutedEventArgs e)
  {
    SetBusy(true);
    try
    {
      string executablePath = Environment.ProcessPath
        ?? throw new InvalidOperationException("Не удалось определить путь к файлу обновления.");
      _package = await UpdatePackageReader.OpenAsync(executablePath);
      NewVersionTextBlock.Text = _package.Manifest.Version;
      PackageVersionTextBlock.Text = $"Пакет версии {_package.Manifest.Version}";
      TargetPathTextBox.Text = InstallationPathStore.FindExistingInstallation() ?? @"C:\AskMkiM\Bin\AskMkiM.exe";
      RefreshTargetVersion();
    }
    catch (Exception ex)
    {
      StatusTextBlock.Text = ex.Message;
      MessageBox.Show(this, ex.Message, "Повреждённое обновление", MessageBoxButton.OK, MessageBoxImage.Error);
    }
    finally
    {
      SetBusy(false);
      UpdateButtonState();
    }
  }

  protected override void OnClosed(EventArgs e)
  {
    _package?.Dispose();
    base.OnClosed(e);
  }

  private void BrowseTarget_Click(object sender, RoutedEventArgs e)
  {
    var dialog = new OpenFileDialog
    {
      Title = "Выберите AskMkiM.exe",
      Filter = "AskMkiM (AskMkiM.exe)|AskMkiM.exe|Исполняемый файл (*.exe)|*.exe",
      FileName = TargetPathTextBox.Text,
    };
    if (dialog.ShowDialog(this) == true) TargetPathTextBox.Text = dialog.FileName;
  }

  private async void Update_Click(object sender, RoutedEventArgs e)
  {
    if (_package is null || _currentVersion is null) return;
    if (UpdateInstallationService.IsApplicationRunning(_currentVersion.ExecutablePath))
    {
      MessageBox.Show(
        this,
        "Закройте AskMkiM и повторите установку.",
        "Приложение запущено",
        MessageBoxButton.OK,
        MessageBoxImage.Warning);
      return;
    }

    SetBusy(true);
    StatusTextBlock.Text = string.Empty;
    try
    {
      var progress = new Progress<string>(AppendStatus);
      UpdateInstallationResult result = await _installer.InstallAsync(
        _package,
        _currentVersion.ExecutablePath,
        progress);
      InstallationPathStore.Save(_currentVersion.ExecutablePath);
      MessageBoxResult start = MessageBox.Show(
        this,
        $"AskMkiM обновлён с версии {result.PreviousVersion.Version} до {result.InstalledManifest.Version}.\n\nЗапустить приложение?",
        "Обновление установлено",
        MessageBoxButton.YesNo,
        MessageBoxImage.Information);
      if (start == MessageBoxResult.Yes)
      {
        Process.Start(new ProcessStartInfo(_currentVersion.ExecutablePath) { UseShellExecute = true });
      }

      Close();
    }
    catch (Exception ex)
    {
      AppendStatus($"Ошибка: {ex.Message}");
      MessageBox.Show(
        this,
        $"Обновление не установлено.\n\n{ex.Message}",
        "Ошибка обновления",
        MessageBoxButton.OK,
        MessageBoxImage.Error);
    }
    finally
    {
      SetBusy(false);
      UpdateButtonState();
    }
  }

  private void TargetPath_Changed(object sender, TextChangedEventArgs e)
  {
    if (!IsLoaded) return;
    RefreshTargetVersion();
  }

  private void RefreshTargetVersion()
  {
    try
    {
      _currentVersion = ApplicationVersionReader.Read(TargetPathTextBox.Text);
      CurrentVersionTextBlock.Text = _currentVersion.Version;
      string blockingReason = _package is null
        ? "Пакет обновления ещё не загружен."
        : ApplicationVersionComparer.GetBlockingReason(_currentVersion, _package.Manifest);
      StatusTextBlock.Text = string.IsNullOrEmpty(blockingReason)
        ? "Установка найдена. Можно выполнить обновление."
        : blockingReason;
    }
    catch (Exception ex)
    {
      _currentVersion = null;
      CurrentVersionTextBlock.Text = "не найдена";
      StatusTextBlock.Text = ex.Message;
    }

    UpdateButtonState();
  }

  private void AppendStatus(string message)
  {
    StatusTextBlock.Text += $"{DateTime.Now:HH:mm:ss}  {message}{Environment.NewLine}";
  }

  private void SetBusy(bool isBusy)
  {
    ProgressBar.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
    TargetPathTextBox.IsEnabled = !isBusy;
    UpdateButton.IsEnabled = !isBusy && _package is not null && _currentVersion is not null;
  }

  private void UpdateButtonState()
  {
    if (!IsLoaded) return;
    UpdateButton.IsEnabled = ProgressBar.Visibility != Visibility.Visible
      && _package is not null
      && _currentVersion is not null
      && string.IsNullOrEmpty(ApplicationVersionComparer.GetBlockingReason(_currentVersion, _package.Manifest));
  }
}
