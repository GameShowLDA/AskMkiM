using Message;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using OpenFolderDialog = Microsoft.Win32.OpenFolderDialog;

namespace MainWindowProgram.Windows
{
  /// <summary>
  /// Представляет окно выбора OPK-файлов и папки для сохранения результатов конвертации.
  /// </summary>
  public partial class OpkToPkConversionWindow : Window
  {
    /// <summary>
    /// Хранит список выбранных пользователем OPK-файлов.
    /// </summary>
    private readonly ObservableCollection<string> _selectedFiles = [];

    /// <summary>
    /// Инициализирует новый экземпляр окна конвертации OPK в PK.
    /// </summary>
    public OpkToPkConversionWindow()
    {
      InitializeComponent();
      SelectedFilesListBox.ItemsSource = _selectedFiles;
      UpdateState();
    }

    /// <summary>
    /// Получает список выбранных OPK-файлов.
    /// </summary>
    public IReadOnlyList<string> SelectedFiles => _selectedFiles;

    /// <summary>
    /// Получает путь к папке, в которую будут сохранены результаты конвертации.
    /// </summary>
    public string OutputDirectory => OutputDirectoryTextBox.Text.Trim();

    /// <summary>
    /// Обрабатывает нажатие кнопки выбора OPK-файлов.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void SelectFilesButton_Click(object sender, RoutedEventArgs e)
    {
      var dialog = new OpenFileDialog
      {
        Title = "Выберите OPK-файлы",
        Filter = "Файлы OPK (*.opk)|*.opk",
        Multiselect = true,
        CheckFileExists = true,
      };

      if (!ShowDialog(dialog))
      {
        return;
      }

      _selectedFiles.Clear();
      foreach (var filePath in dialog.FileNames
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
      {
        _selectedFiles.Add(filePath);
      }

      if (string.IsNullOrWhiteSpace(OutputDirectory))
      {
        OutputDirectoryTextBox.Text = Path.GetDirectoryName(_selectedFiles[0]) ?? string.Empty;
      }

      UpdateState();
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки очистки списка выбранных файлов.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void ClearFilesButton_Click(object sender, RoutedEventArgs e)
    {
      _selectedFiles.Clear();
      UpdateState();
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки выбора папки для сохранения.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void SelectOutputDirectoryButton_Click(object sender, RoutedEventArgs e)
    {
      var dialog = new OpenFolderDialog
      {
        Title = "Выберите папку для сохранения PK-файлов",
        Multiselect = false,
        InitialDirectory = string.IsNullOrWhiteSpace(OutputDirectory)
          ? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
          : OutputDirectory,
      };

      if (!ShowDialog(dialog))
      {
        return;
      }

      OutputDirectoryTextBox.Text = dialog.FolderName;
      UpdateState();
    }

    /// <summary>
    /// Обрабатывает изменение текста в поле папки сохранения.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void OutputDirectoryTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
      UpdateState();
    }

    /// <summary>
    /// Обрабатывает подтверждение параметров конвертации.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void ConvertButton_Click(object sender, RoutedEventArgs e)
    {
      if (_selectedFiles.Count == 0)
      {
        MessageBoxCustom.Show("Выберите хотя бы один файл OPK.", "Конвертация OPK в PK", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      if (string.IsNullOrWhiteSpace(OutputDirectory))
      {
        MessageBoxCustom.Show("Укажите папку для сохранения результата.", "Конвертация OPK в PK", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      DialogResult = true;
      Close();
    }

    /// <summary>
    /// Обновляет состояние элементов интерфейса в зависимости от выбранных параметров.
    /// </summary>
    private void UpdateState()
    {
      SelectedFilesSummaryTextBlock.Text = _selectedFiles.Count switch
      {
        0 => "Файлы не выбраны.",
        1 => "Выбран 1 файл.",
        _ => $"Выбрано файлов: {_selectedFiles.Count}.",
      };

      ConvertButton.IsEnabled = _selectedFiles.Count > 0 && !string.IsNullOrWhiteSpace(OutputDirectory);
    }

    /// <summary>
    /// Открывает системовый диалог с учётом владельца текущего окна.
    /// </summary>
    /// <param name="dialog">Диалог, который требуется отобразить.</param>
    /// <returns><see langword="true"/>, если пользователь подтвердил выбор; иначе <see langword="false"/>.</returns>
    private bool ShowDialog(CommonDialog dialog)
    {
      return Owner != null
        ? dialog.ShowDialog(Owner) == true
        : dialog.ShowDialog() == true;
    }
  }
}
