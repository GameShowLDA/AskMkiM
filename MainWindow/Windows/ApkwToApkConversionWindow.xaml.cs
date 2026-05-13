using Message;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Input;
using OpenFolderDialog = Microsoft.Win32.OpenFolderDialog;

namespace MainWindowProgram.Windows
{
  public partial class ApkwToApkConversionWindow : Window
  {
    public ApkwToApkConversionWindow()
    {
      InitializeComponent();
      UpdateState();
    }

    public string InputFilePath => InputFileTextBox.Text.Trim();

    public string OutputDirectory => OutputDirectoryTextBox.Text.Trim();

    private void SelectInputFileButton_Click(object sender, RoutedEventArgs e)
    {
      SelectInputFile();
    }

    private void InputFileTextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      SelectInputFile();
      e.Handled = true;
    }

    private void SelectInputFile()
    {
      var dialog = new OpenFileDialog
      {
        Title = "Выберите APKW-архив",
        Filter = "Архивы APKW (*.apkw)|*.apkw",
        Multiselect = false,
        CheckFileExists = true,
      };

      if (!ShowDialog(dialog))
      {
        return;
      }

      InputFileTextBox.Text = dialog.FileName;
      if (string.IsNullOrWhiteSpace(OutputDirectory))
      {
        OutputDirectoryTextBox.Text = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
      }

      UpdateState();
    }

    private void SelectOutputDirectoryButton_Click(object sender, RoutedEventArgs e)
    {
      SelectOutputDirectory();
    }

    private void OutputDirectoryTextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      SelectOutputDirectory();
      e.Handled = true;
    }

    private void SelectOutputDirectory()
    {
      var dialog = new OpenFolderDialog
      {
        Title = "Выберите папку для сохранения APK и OPK",
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

    private void InputFileTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
      UpdateState();
    }

    private void OutputDirectoryTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
      UpdateState();
    }

    private void ConvertButton_Click(object sender, RoutedEventArgs e)
    {
      if (string.IsNullOrWhiteSpace(InputFilePath))
      {
        MessageBoxCustom.Show("Выберите исходный APKW-архив.", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      if (!File.Exists(InputFilePath))
      {
        MessageBoxCustom.Show("Исходный APKW-архив не найден.", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      if (string.IsNullOrWhiteSpace(OutputDirectory))
      {
        MessageBoxCustom.Show("Укажите папку для сохранения legacy-архива.", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      DialogResult = true;
      Close();
    }

    private void UpdateState()
    {
      var hasInputFile = !string.IsNullOrWhiteSpace(InputFilePath);
      var hasOutputDirectory = !string.IsNullOrWhiteSpace(OutputDirectory);
      ConvertButton.IsEnabled = hasInputFile && hasOutputDirectory;

      StateTextBlock.Text = hasInputFile && hasOutputDirectory
        ? "Будут созданы APK-индекс и OPK-файлы рядом в выбранной папке."
        : "Выберите APKW-архив и папку для сохранения.";
    }

    private bool ShowDialog(CommonDialog dialog)
    {
      return Owner != null
        ? dialog.ShowDialog(Owner) == true
        : dialog.ShowDialog() == true;
    }

    private void HeaderBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (e.LeftButton == MouseButtonState.Pressed)
      {
        DragMove();
      }
    }

    private void CloseButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      Close();
    }
  }
}
