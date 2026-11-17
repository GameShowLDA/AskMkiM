using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using AskMkiM.Updater.Core;

namespace AskMkiM.Updater.Builder;

/// <summary>
/// Предоставляет интерфейс формирования автономного обновления.
/// </summary>
public partial class MainWindow : Window
{
  private readonly UpdatePackageBuilder _builder = new();

  /// <summary>Создаёт окно конструктора.</summary>
  public MainWindow()
  {
    InitializeComponent();
    SourceDirectoryTextBox.Text = @"D:\AskMkiM\Bin";
    StubPathTextBox.Text = Path.Combine(AppContext.BaseDirectory, "AskMkiM.Updater.Stub.exe");
    OutputPathTextBox.Text = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
      "AskMkiM_Update.exe");
    UpdateButtonState();
  }

  private void BrowseSource_Click(object sender, RoutedEventArgs e)
  {
    var dialog = new OpenFolderDialog { Title = "Выберите папку опубликованной сборки AskMkiM" };
    if (Directory.Exists(SourceDirectoryTextBox.Text)) dialog.InitialDirectory = SourceDirectoryTextBox.Text;
    if (dialog.ShowDialog(this) == true)
    {
      SourceDirectoryTextBox.Text = dialog.FolderName;
      TryUpdateOutputName();
    }
  }

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

  private void BrowseStub_Click(object sender, RoutedEventArgs e)
  {
    var dialog = new OpenFileDialog
    {
      Title = "Выберите автономный установщик-заготовку",
      Filter = "Исполняемый файл (*.exe)|*.exe",
      FileName = StubPathTextBox.Text,
    };
    if (dialog.ShowDialog(this) == true) StubPathTextBox.Text = dialog.FileName;
  }

  private void BrowseOutput_Click(object sender, RoutedEventArgs e)
  {
    var dialog = new SaveFileDialog
    {
      Title = "Сохранить автономное обновление",
      Filter = "Исполняемый файл (*.exe)|*.exe",
      FileName = Path.GetFileName(OutputPathTextBox.Text),
      InitialDirectory = Path.GetDirectoryName(OutputPathTextBox.Text),
    };
    if (dialog.ShowDialog(this) == true) OutputPathTextBox.Text = dialog.FileName;
  }

  private async void Create_Click(object sender, RoutedEventArgs e)
  {
    SetBusy(true);
    LogTextBlock.Text = string.Empty;
    try
    {
      var progress = new Progress<string>(AppendLog);
      var manifest = await _builder.BuildAsync(
        SourceDirectoryTextBox.Text,
        StubPathTextBox.Text,
        OutputPathTextBox.Text,
        progress);
      MessageBox.Show(
        this,
        $"Обновление AskMkiM {manifest.Version} создано:\n{OutputPathTextBox.Text}",
        "Готово",
        MessageBoxButton.OK,
        MessageBoxImage.Information);
    }
    catch (Exception ex)
    {
      AppendLog($"Ошибка: {ex.Message}");
      MessageBox.Show(this, ex.Message, "Не удалось создать обновление", MessageBoxButton.OK, MessageBoxImage.Error);
    }
    finally
    {
      SetBusy(false);
    }
  }

  private void Input_Changed(object sender, TextChangedEventArgs e) => UpdateButtonState();

  private void TryUpdateOutputName()
  {
    try
    {
      string executable = Path.Combine(SourceDirectoryTextBox.Text, ApplicationVersionReader.ExecutableName);
      var version = ApplicationVersionReader.Read(executable);
      string directory = Path.GetDirectoryName(OutputPathTextBox.Text)
        ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
      OutputPathTextBox.Text = Path.Combine(directory, $"AskMkiM_Update_{SafeName(version.Version)}.exe");
    }
    catch
    {
      // Имя останется общим, а подробная ошибка будет показана при формировании.
    }
  }

  private static string SafeName(string value)
  {
    foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
    return value;
  }

  private void AppendLog(string message)
  {
    LogTextBlock.Text += $"{DateTime.Now:HH:mm:ss}  {message}{Environment.NewLine}";
  }

  private void SetBusy(bool isBusy)
  {
    ProgressBar.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
    SourceDirectoryTextBox.IsEnabled = !isBusy;
    StubPathTextBox.IsEnabled = !isBusy;
    OutputPathTextBox.IsEnabled = !isBusy;
    CreateButton.IsEnabled = !isBusy;
  }

  private void UpdateButtonState()
  {
    if (!IsLoaded) return;
    CreateButton.IsEnabled = !string.IsNullOrWhiteSpace(SourceDirectoryTextBox.Text)
      && !string.IsNullOrWhiteSpace(StubPathTextBox.Text)
      && !string.IsNullOrWhiteSpace(OutputPathTextBox.Text);
  }
}
