using Message;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using OpenFolderDialog = Microsoft.Win32.OpenFolderDialog;

namespace MainWindowProgram.Windows
{
  public partial class OpkToPkConversionWindow : Window
  {
    private readonly ObservableCollection<string> _selectedFiles = [];

    public OpkToPkConversionWindow()
    {
      InitializeComponent();
      SelectedFilesListBox.ItemsSource = _selectedFiles;
      UpdateState();
    }

    public IReadOnlyList<string> SelectedFiles => _selectedFiles;

    public string OutputDirectory => OutputDirectoryTextBox.Text.Trim();

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

    private void ClearFilesButton_Click(object sender, RoutedEventArgs e)
    {
      _selectedFiles.Clear();
      UpdateState();
    }

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

    private void OutputDirectoryTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
      UpdateState();
    }

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

    private bool ShowDialog(CommonDialog dialog)
    {
      return Owner != null
        ? dialog.ShowDialog(Owner) == true
        : dialog.ShowDialog() == true;
    }
  }
}
