using Ask.Core.Services.Config.AppSettings;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace UI.Controls.Settings.AskMkiConfig
{
  /// <summary>
  /// Контрол настроек конфигурации АСК-МКИ.
  /// </summary>
  public partial class AskMkiConfigControl : UserControl
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="AskMkiConfigControl"/>.
    /// </summary>
    public AskMkiConfigControl()
    {
      InitializeComponent();
      LoadMkiPath();
    }

    /// <summary>
    /// Загружает сохранённый путь к mkiw.exe в поле настроек.
    /// </summary>
    private void LoadMkiPath()
    {
      MkiPathTextBox.Text = LegacyMkiConfig.GetMkiPath();
      UpdateClearButtonState();
    }

    /// <summary>
    /// Открывает диалог выбора mkiw.exe и сохраняет выбранный путь.
    /// </summary>
    private void SelectMkiPathButton_Click(object sender, RoutedEventArgs e)
    {
      var dialog = new OpenFileDialog
      {
        Title = "Выберите mkiw.exe",
        Filter = "mkiw.exe|mkiw.exe|EXE-файлы (*.exe)|*.exe|Все файлы (*.*)|*.*",
        CheckFileExists = true,
        Multiselect = false
      };

      var currentPath = LegacyMkiConfig.GetMkiPath();
      var currentDirectory = string.IsNullOrWhiteSpace(currentPath)
        ? null
        : Path.GetDirectoryName(currentPath);

      if (!string.IsNullOrWhiteSpace(currentDirectory) && Directory.Exists(currentDirectory))
      {
        dialog.InitialDirectory = currentDirectory;
      }

      if (dialog.ShowDialog() != true)
      {
        return;
      }

      LegacyMkiConfig.SetMkiPath(dialog.FileName);
      MkiPathTextBox.Text = dialog.FileName;

      UpdateClearButtonState();
    }

    /// <summary>
    /// Очищает сохранённый путь к mkiw.exe.
    /// </summary>
    private void ClearMkiPathButton_Click(object sender, RoutedEventArgs e)
    {
      LegacyMkiConfig.ClearMkiPath();
      MkiPathTextBox.Text = string.Empty;

      UpdateClearButtonState();
    }

    /// <summary>
    /// Обновляет доступность кнопки очистки пути.
    /// </summary>
    private void UpdateClearButtonState()
    {
      ClearMkiPathButton.IsEnabled = !string.IsNullOrWhiteSpace(MkiPathTextBox.Text);
    }
  }
}