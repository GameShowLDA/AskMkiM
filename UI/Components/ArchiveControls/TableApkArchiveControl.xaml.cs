using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AppConfiguration.Base;
using Microsoft.Win32;
using UI.Components.ArchiveManager;
using UI.Components.ArchiveManager.ArchiveFiles;
using UI.Components.ArchiveManager.ArchiveFiles.ApkwArchive;
using UI.Components.ArchiveManager.ArchiveFiles.Index;
using UI.Components.ArchiveManager.Models;
using UI.Controls.TextEditor;
using static UI.Controls.ProtocolNew.ProtocolUI;
using static Utilities.LoggerUtility;
using Path = System.IO.Path;

namespace UI.Components.ArchiveControls
{
  /// <summary>
  /// Логика взаимодействия для TableApkArchiveControl.xaml
  /// </summary>
  public partial class TableApkArchiveControl : UserControl
  {
    static private bool visibleLeftColumn = true;
    private string _archiveName { get; set; }
    private bool isMain { get; set; }
    private OpkFile opkFile { get; set; }

    /// <summary>
    /// Событие возникает при нажатии на кнопку "Открыть".
    /// </summary>
    public event PreviewMouseDownEventHandler StartMeasureResistanceButtonPreviewMouseDown;

    public TableApkArchiveControl(string archiveName)
    {
      InitializeComponent();
      _archiveName = archiveName;
      this.Loaded += async (s, e) => await ShowOpkFiles();
      EventAggregator.AdminRightsChanged += ApplicationDataHandler_AdminRightsChanged;
    }


    private async Task<bool> IsArchiveMain(string archiveName)
    {
      var existingArchives = new List<ApkArchive>();
      existingArchives = await IndexEditor.GetApkArrayAsync(Path.Combine(ArchiveSettings.ArchivePath, ArchiveSettings.IndexName), existingArchives);
      var foundArchive = existingArchives.FirstOrDefault(fa => fa.ArchiveName == archiveName);
      return foundArchive.IsMain;
    }

    private async Task ShowOpkFiles()
    {
      var archiveDirectory = ArchiveSettings.ArchivePath;
      var indexEditor = new IndexEditor();

      this.isMain = await IsArchiveMain(_archiveName);
      var result = (EventAggregator.GetAdminRights() && isMain == true) || isMain == false;
      if (result == true)
      {
        buttonsGrid.Visibility = Visibility.Visible;
      }

      await indexEditor.PrintTable(Path.Combine(archiveDirectory, $"{_archiveName}.apkw"), opkFilesDataGrid);
    }

    private async void Border_PreviewMouseDownAsync(object sender, MouseButtonEventArgs e)
    {
      visibleLeftColumn = !visibleLeftColumn;

      if (!visibleLeftColumn)
      {
        int newWidth = 60;
        while (PanelManagment.Width.Value > newWidth)
        {
          PanelManagment.Width = new GridLength(PanelManagment.Width.Value - 25);
          if (ButtonPanel.Opacity > 0)
          {
            ButtonPanel.Opacity -= 0.1;
          }
          await Task.Delay(1);
        }
        ButtonPanel.Opacity = 0;
      }
      else
      {
        int newWidth = 160;
        while (PanelManagment.Width.Value < newWidth)
        {
          PanelManagment.Width = new GridLength(PanelManagment.Width.Value + 25);
          if (ButtonPanel.Opacity < 1)
          {
            ButtonPanel.Opacity += 0.1;
          }
          await Task.Delay(1);
        }

        PanelManagment.Width = new GridLength(150);
        ButtonPanel.Opacity = 1;
      }
    }

    private async void Add_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      OpenFileDialog openFileDialog = OpenPkFile();

      if (openFileDialog.ShowDialog() == true)
      {
        var archiveDirectory = $"{Path.Combine(ArchiveSettings.ArchivePath, _archiveName)}.apkw";
        var pkEditor = new PkEditor();
        string filePath = openFileDialog.FileName;
        await TryConvertPkToOpkAsync(archiveDirectory, pkEditor, filePath);
        await ShowOpkFiles();
      }
    }

    private async Task TryConvertPkToOpkAsync(string archiveDirectory, PkEditor pkEditor, string filePath)
    {
      if (await pkEditor.ConvertPkToOpk(filePath, archiveDirectory))
      {
        LogInformation("Файл добавлен");
        var indexEditor = new IndexEditor();
        await indexEditor.PrintTable(archiveDirectory, opkFilesDataGrid);
      }
      else
      {
        MessageBox.Show("Файл не найден.", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Warning);
        LogInformation("Файл не найден");
      }
    }

    private async void Update_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (opkFile != null)
      {
        OpenFileDialog openFileDialog = OpenPkFile();

        if (openFileDialog.ShowDialog() == true)
        {
          var fileEditor = new FileEditor();
          string newOpkFile = openFileDialog.FileName;
          var archiveDirectory = Path.Combine(ArchiveSettings.ArchivePath, $"{_archiveName}.apkw");
          ValidatePkFile(newOpkFile);

          try
          {
            await TryUpdateFileAsync(fileEditor, newOpkFile, archiveDirectory);
            await ShowOpkFiles();
          }
          catch (Exception ex)
          {
            MessageBox.Show($"Произошла ошибка: {ex.Message}");
            LogError($"Произошла ошибка: {ex.Message}");
          }
        }
        else
        {
          MessageBox.Show("Файл не найден.", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Warning);
          LogInformation("Файл не найден");
        }
      }
    }

    private static OpenFileDialog OpenPkFile()
    {
      return new OpenFileDialog
      {
        Filter = "Pk files (*.pk, *.PK, *.Pk)|*.pk;*.PK;*.Pk",
        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
      };
    }

    private async Task TryUpdateFileAsync(FileEditor fileEditor, string newOpkFile, string archiveDirectory)
    {
      bool result = await fileEditor.UpdateFile(archiveDirectory, opkFile.OpkFilename, newOpkFile);
      if (result)
      {
        MessageBox.Show("Файл успешно обновлен");
        LogInformation($"Файл {opkFile.OpkFilename} в архиве {archiveDirectory} успешно обновлен.");
      }
      else
      {
        MessageBox.Show("Не удалось обновить файл. Исходный файл сохранен.");
        LogError($"Файл {opkFile.OpkFilename} в архиве {archiveDirectory} не удалось обновить. Исходный файл сохранен.");
      }
    }

    private static void ValidatePkFile(string newOpkFile)
    {
      if (!File.Exists(newOpkFile))
      {
        throw new FileNotFoundException("Новый файл не найден", newOpkFile);
      }

      string extension = Path.GetExtension(newOpkFile).ToLower();
      if (extension != ".opk" && extension != ".pk")
      {
        throw new InvalidOperationException("Неверный формат файла. Ожидается .opk или .pk");
      }
    }


    private async void Delete_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (opkFile != null)
      {
        var archiveDirectory = Path.Combine(ArchiveSettings.ArchivePath, $"{_archiveName}.apkw");
        var fileEditor = new FileEditor();

        MessageBoxResult result = MessageBox.Show(
                "Вы уверены, что хотите удалить этот файл?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
          if (await fileEditor.DeleteFileFromArchive(archiveDirectory, opkFile.OpkFilename))
          {
            var message = $"Файл {opkFile.OpkFilename} удален из архива {_archiveName}.apkw";
            await ShowOpkFiles();
            LogInformation(message);
          }
          else
          {
            var message = $"Файл {opkFile.OpkFilename} не удалось корректно удалить из архива {_archiveName}.apkw.";
            MessageBox.Show(message, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Warning);
            LogError(message);
          }
        }
      }
    }

    private void opkFilesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if ((sender as DataGrid).SelectedItem is OpkFile)
      {
        saveAsButton.Visibility = Visibility.Visible;
        viewButton.Visibility = Visibility.Visible;
        translateButton.Visibility = Visibility.Visible;
        var row = (sender as DataGrid).SelectedItem as OpkFile;
        if (row != null)
        {
          opkFile = row;
        }
      }
    }

    private void ApplicationDataHandler_AdminRightsChanged(bool newValue)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        if (newValue || isMain == false)
        {
          this.buttonsGrid.Visibility = Visibility.Visible;
        }
        else
        {
          this.buttonsGrid.Visibility = Visibility.Collapsed;
        }
      });
    }

    private async void SaveAs_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      var foundOpk = await GetOpkPath();
      if (foundOpk != string.Empty)
      {
        var opkFileName = opkFile.OpkFilename;
        var fileEditor = new FileEditor();
        fileEditor.SaveOpkFile(foundOpk, opkFileName.Replace(".opk", string.Empty));
      }
      else
      {
        LogWarning($"Файл {opkFile.OpkFilename} не найден.");
        MessageBox.Show("Opk файл не найден в архиве!", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private async Task<string> GetOpkPath()
    {
      var opkFileName = opkFile.OpkFilename;
      var archiveName = $"{_archiveName}.apkw";
      var archivePath = Path.Combine(ArchiveSettings.ArchivePath, archiveName);
      var archiveEditor = new ArchiveEditor();
      var foundOpkPath = await archiveEditor.GetArchiveEntry(archivePath, opkFileName);
      return foundOpkPath;
    }

    private async void viewButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      LogInformation($"Сработан обработчик события для кнопки \"Открыть\"");
      string foundOpkPath = await GetOpkPath();
      var content = File.ReadAllText(foundOpkPath);
      var textEditor = new TextEditorUI();
      textEditor.Text = content;
      EventAggregator.RaiseOpenOpk(textEditor, $"{opkFile.OpkFilename}", content);
    }

    private void translateButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      MessageBox.Show("Тут будет функционал для трансляции opk файла:)", "Заглушка", MessageBoxButton.OK);
    }

    private async void viewOpkFilesDataGrid_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      LogInformation($"Сработан обработчик события для двойного клика по строке opk-архива");
      string foundOpkPath = await GetOpkPath();
      var content = File.ReadAllText(foundOpkPath);
      var textEditor = new TextEditorUI();
      textEditor.Text = content;
      EventAggregator.RaiseOpenOpk(textEditor, $"{opkFile.OpkFilename}", content);
    }
  }
}
