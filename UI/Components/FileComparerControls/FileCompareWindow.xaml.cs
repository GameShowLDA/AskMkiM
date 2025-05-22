using AppConfiguration.Base;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Utilities.LoggerUtility;

namespace UI.Components.FileComparerControls
{
  /// <summary>
  /// Логика взаимодействия для FileCompareWindow.xaml
  /// </summary>
  public partial class FileCompareWindow : Window
  {
    /// <summary>
    /// Событие, возникающее при закрытии диалога.
    /// </summary>
    public event EventHandler DialogClosed;


    /// <summary>
    /// Определяет, разрешено ли закрытие окна или диалога.
    /// </summary>
    private bool _allowClose;

    public FileCompareWindow()
    {
      InitializeComponent();
      ShowInTaskbar = false;
      WindowStyle = WindowStyle.None;
      ResizeMode = ResizeMode.NoResize;


      this.Closed += (s, e) =>
      {
        if (Owner != null)
        {
          Owner.IsEnabled = true;
          Owner.Focus();
          DialogClosed?.Invoke(this, EventArgs.Empty);
        }
      };

      this.Closing += (s, e) =>
      {
        if (!_allowClose)
        {
          e.Cancel = true;
        }
      };

      this.Deactivated += (s, e) =>
      {
        this.Activate();
        this.Focus();
      };

      this.Loaded += FileCompareWindow_Loaded; 
    }

    private void FileCompareWindow_Loaded(object sender, RoutedEventArgs e)
    {
    }

    public new bool? ShowDialog()
    {
      this.Activate();
      this.Focus();

      return base.ShowDialog();
    }

    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ChangedButton == MouseButton.Left)
      {
        this.DragMove();
      }
    }

    private void CloseButton_Click(object sender, MouseButtonEventArgs e)
    {
      CloseDialog();
    }

    public void CloseDialog()
    {
      _allowClose = true;
      this.Close();
    }

    private void FirstFileTextBlock_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      OpenFileDialog openFileDialog = new OpenFileDialog
      {
        Title = "Выберите файл",
        Filter = "Text files (*.txt)|*.txt|RTF files (*.rtf)|*.rtf|PK files (*.pk;*.Pk;*.PK)|*.pk;*.Pk;*.PK|All files (*.*)|*.*",
        Multiselect = false
      };

      if (openFileDialog.ShowDialog() == true)
      {
        string filePath = openFileDialog.FileName;

        // Пример: отобразим путь в текстблоке
        if (sender is TextBoxPlaceholder textBox)
        {
          var bothTextBoxEmpty = string.IsNullOrEmpty(FirstFileTextBlock.Text) && string.IsNullOrEmpty(SecondFileTextBlock.Text);
          var pathsNotEquals = (!string.Equals(FirstFileTextBlock.Text, filePath) && !string.IsNullOrEmpty(FirstFileTextBlock.Text))
            || (!string.Equals(SecondFileTextBlock.Text, filePath) && !string.IsNullOrEmpty(SecondFileTextBlock.Text));
          if (bothTextBoxEmpty || pathsNotEquals)
          {
            textBox.Text = filePath;
          }
          else
          {
            MessageBox.Show("Вы уже выбрали этот файл для сравнения", "Неверный путь к файлу", MessageBoxButton.OK, MessageBoxImage.Warning);
            LogWarning("Попытка сравнить один и тот же файл");
          }
        }
      }
    }

    private void CompareButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      var firstPath = FirstFileTextBlock.Text;
      var secondPath = SecondFileTextBlock.Text;

      if (string.IsNullOrEmpty(firstPath) || string.IsNullOrEmpty(secondPath))
      {
        MessageBox.Show("Укажите путь к файлу для сравнения", "Не указан путь к файлу", MessageBoxButton.OK, MessageBoxImage.Warning);
        LogWarning("Не указан путь к одному или нескольким файлам для сравнения");
      }
      else
      {
        if (CheckFileExists(firstPath, secondPath))
        {
          EventAggregator.RaiseCompareFiles(firstPath, secondPath);
          LogInformation("Вызвано сравнение файлов");
          CloseDialog();
        }
      }
    }

    private bool CheckFileExists(string firstPath, string secondPath)
    {
      if (!File.Exists(firstPath))
      {
        var message = "Неверно указан путь к первому файлу для сравнения";
        MessageBox.Show(message, "Файл не найден", MessageBoxButton.OK, MessageBoxImage.Error);
        LogError("Неверно указан путь к первому файлу для сравнения");
        return false;
      }
      if (!File.Exists(secondPath))
      {
        var message = "Неверно указан путь ко второму файлу для сравнения";
        MessageBox.Show(message, "Файл не найден", MessageBoxButton.OK, MessageBoxImage.Error);
        LogError(message);
        return false;
      }

      return true;
    }
  }
}
