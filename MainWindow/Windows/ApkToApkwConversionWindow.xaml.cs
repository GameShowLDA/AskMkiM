using Message;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace MainWindowProgram.Windows
{
  /// <summary>
  /// Dialog for selecting a legacy APK archive that will be converted into the application's APKW archive format.
  /// </summary>
  public partial class ApkToApkwConversionWindow : Window
  {
    public ApkToApkwConversionWindow()
    {
      InitializeComponent();
      UpdateState();
    }

    public string InputFilePath => InputFileTextBox.Text.Trim();

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
        Title = "Выберите APK-архив",
        Filter = "Архивы APK (*.apk)|*.apk",
        Multiselect = false,
        CheckFileExists = true,
      };

      if (!ShowDialog(dialog))
      {
        return;
      }

      InputFileTextBox.Text = dialog.FileName;
      UpdateState();
    }

    private void InputFileTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
      UpdateState();
    }

    private void ConvertButton_Click(object sender, RoutedEventArgs e)
    {
      if (string.IsNullOrWhiteSpace(InputFilePath))
      {
        MessageBoxCustom.Show("Выберите исходный APK-архив.", "Конвертация APK в APKW", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      if (!File.Exists(InputFilePath))
      {
        MessageBoxCustom.Show("Исходный APK-архив не найден.", "Конвертация APK в APKW", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      DialogResult = true;
      Close();
    }

    private void UpdateState()
    {
      var hasInputFile = !string.IsNullOrWhiteSpace(InputFilePath);
      ConvertButton.IsEnabled = hasInputFile;

      StateTextBlock.Text = hasInputFile
        ? "После конвертации архив появится в разделе Archives приложения и будет открыт автоматически."
        : "Выберите исходный APK-архив.";
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
