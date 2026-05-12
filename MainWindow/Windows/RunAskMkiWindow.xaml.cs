using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Ask.Core.Services.Config.AppSettings;

namespace MainWindowProgram.Windows
{
  /// <summary>
  /// Окно выбора параметров для выполнения программы контроля
  /// через старую программу АСК-МКИ mkiw.exe.
  /// </summary>
  public partial class RunAskMkiWindow : Window
  {
    /// <summary>
    /// Полный путь к выбранному исполняемому файлу mkiw.exe.
    /// </summary>
    public string MkiPath { get; private set; } = string.Empty;

    /// <summary>
    /// Полный путь к выбранной программе контроля (*.acs или *.pk).
    /// </summary>
    public string ProgramPath { get; private set; } = string.Empty;

    /// <summary>
    /// Инициализирует новый экземпляр окна выбора параметров выполнения АСК-МКИ.
    /// </summary>
    public RunAskMkiWindow()
    {
      InitializeComponent();
      LoadSavedMkiPath();
    }

    /// <summary>
    /// Загружает сохранённый путь к mkiw.exe из настроек.
    /// </summary>
    private void LoadSavedMkiPath()
    {
      MkiPath = LegacyMkiConfig.GetMkiPath();
      MkiPathTextBox.Text = MkiPath;

      UpdateRunButtonState();
    }

    /// <summary>
    /// Позволяет перемещать окно за пользовательскую верхнюю панель.
    /// </summary>
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ButtonState == MouseButtonState.Pressed)
      {
        DragMove();
      }
    }

    /// <summary>
    /// Открывает диалог выбора исполняемого файла mkiw.exe.
    /// </summary>
    private void SelectMkiButton_Click(object sender, RoutedEventArgs e)
    {
      var dialog = new OpenFileDialog
      {
        Title = "Выберите mkiw.exe",
        Filter = "mkiw.exe|mkiw.exe|EXE-файлы (*.exe)|*.exe|Все файлы (*.*)|*.*",
        CheckFileExists = true,
        Multiselect = false
      };

      if (dialog.ShowDialog(this) != true)
      {
        return;
      }

      MkiPath = dialog.FileName;
      MkiPathTextBox.Text = MkiPath;

      LegacyMkiConfig.SetMkiPath(MkiPath);

      UpdateRunButtonState();

      UpdateRunButtonState();
    }

    /// <summary>
    /// Открывает диалог выбора программы контроля для выполнения.
    /// </summary>
    private void SelectProgramButton_Click(object sender, RoutedEventArgs e)
    {
      var dialog = new OpenFileDialog
      {
        Title = "Выберите программу контроля",
        Filter = "Программы контроля (*.acs;*.pk)|*.acs;*.pk|ACS-файлы (*.acs)|*.acs|PK-файлы (*.pk)|*.pk|Все файлы (*.*)|*.*",
        CheckFileExists = true,
        Multiselect = false
      };

      if (dialog.ShowDialog(this) != true)
      {
        return;
      }

      ProgramPath = dialog.FileName;
      ProgramPathTextBox.Text = ProgramPath;

      UpdateRunButtonState();
    }

    /// <summary>
    /// Закрывает окно без запуска выполнения.
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
      Close();
    }

    /// <summary>
    /// Подтверждает выбор файлов и закрывает окно с положительным результатом.
    /// </summary>
    private void RunButton_Click(object sender, RoutedEventArgs e)
    {
      if (!File.Exists(MkiPath))
      {
        MessageBox.Show(
          "Выбранный файл mkiw.exe не найден.",
          "Выполнение АСК-МКИ",
          MessageBoxButton.OK,
          MessageBoxImage.Error);

        return;
      }

      if (!File.Exists(ProgramPath))
      {
        MessageBox.Show(
          "Выбранная программа контроля не найдена.",
          "Выполнение АСК-МКИ",
          MessageBoxButton.OK,
          MessageBoxImage.Error);

        return;
      }

      DialogResult = true;
      Close();
    }

    /// <summary>
    /// Обновляет доступность кнопки запуска.
    /// Кнопка активна только после выбора существующих mkiw.exe и программы контроля.
    /// </summary>
    private void UpdateRunButtonState()
    {
      RunButton.IsEnabled =
        File.Exists(MkiPath)
        && File.Exists(ProgramPath);
    }
  }
}