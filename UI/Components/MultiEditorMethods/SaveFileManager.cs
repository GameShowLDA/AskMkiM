using Ask.Core.Shared.Metadata.Static;
using Ask.UI.Features.Notifications.Models;
using Ask.UI.Infrastructure.UI.Overlay.Notifications.Runtime;
using Message;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using UI.Components.Invoke;
using UI.Controls;
using UI.Controls.TextEditor;
using UI.Windows.WpfDocking.Windows.Docking;
using static Ask.LogLib.LoggerUtility;
using Application = System.Windows.Application;
using Path = System.IO.Path;

namespace UI.Components.MultiEditorMethods
{
  /// <summary>
  /// Класс для работы с сохранением файлов.
  /// </summary>
  public class SaveFileManager
  {
    private static readonly TimeSpan SaveSuccessNotificationWindow = TimeSpan.FromSeconds(4);
    private static readonly object SaveNotificationSync = new();
    private static readonly Dictionary<string, DateTime> LastSaveNotificationByFile = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Экзмепляр класса FileManager.
    /// </summary>
    internal FileManager fileManager { get; set; }

    /// <summary>
    /// Открывает диалоговое окно для подтверждения сохранения файла перед его закрытием.
    /// </summary>
    /// <param name="result">Результат выбора пользователя в диалоговом окне (Yes или No).</param>
    /// <param name="saveFileResult">Результат сохранения файла. <c>true</c> если файл был успешно сохранен, иначе <c>false</c>.</param>
    /// <param name="index">Индекс открытой страницы, для которой проверяется необходимость сохранения.</param>
    public void SaveFileDialog(ref MessageBoxResult result, ref bool saveFileResult, DockItem control)
    {
      var needToSave = fileManager.FileService.Comparison.HasFileChanged(control);
      if (needToSave)
      {
        result = MessageBoxCustom.Show(
            $"Сохранить файл {control.Title} перед закрытием?",
            "Подтверждение",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
          saveFileResult = SaveFile(control);
        }
        else
        {
          SaveBackup(control);
        }
      }
    }

    /// <summary>
    /// Обрабатывает сохранение файла.
    /// </summary>
    /// <returns>Результат сохранения файла. <c>true</c>, если файл был успешно сохранен, иначе <c>false</c>.</returns>
    public bool SaveFile()
    {
      var activeTab = fileManager.EditorWorkspaceModel.OpenPages.FirstOrDefault(page =>
        page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      int index = fileManager.EditorWorkspaceModel.OpenPages.IndexOf(activeTab);
      if (fileManager.EditorWorkspaceModel.UserControls[index] is TextEditorContainer)
      {
        var activeTextEditorContainer = fileManager.EditorWorkspaceModel.UserControls[index] as TextEditorContainer;
        if (activeTextEditorContainer != null)
        {
          var activeDockItem = activeTextEditorContainer.DockManager.DockItems.FirstOrDefault(tab =>
            tab.IsActiveItem == true);
          return SaveFile(activeDockItem);
        }
      }
      return false;
    }

    /// <summary>
    /// Сохраняет файл. Если файл еще не сохранен, вызывает диалоговое окно для сохранения как нового файла.
    /// </summary>
    /// <param name="activeTab">Активная вкладка, для которой будет сохранен файл.</param>
    /// <returns><c>true</c>, если файл успешно сохранен, иначе <c>false</c>.</returns>
    public bool SaveFile(DockItem control)
    {
      if (control != null)
      {
        var fileName = control.Title;
        if (fileManager.EditorWorkspaceModel.FilePaths.ContainsKey(fileName))
        {
          if (fileManager.EditorWorkspaceModel.FilePaths[fileName] == string.Empty
            || fileManager.EditorWorkspaceModel.FilePaths[fileName].Contains(FileLocations.BackupDirectory))
          {
            return SaveFileAs();
          }
          else
          {
            if (!fileManager.FileService.Comparison.HasFileChanged(control))
            {
              return true;
            }

            var filePath = fileManager.EditorWorkspaceModel.FilePaths[fileName];
            if (control.Content is TextEditorUI)
            {
              var textEditor = control.Content as TextEditorUI;
              return SaveDataFromTextEditor(textEditor, filePath);
            }
          }
        }
        else
        {
          if (control.Content is TranslatorItem translator)
          {
            var textEditor = translator.GetLeftEditor();
            if (textEditor.TextEditorModel.FilePath.Contains(FileLocations.BackupDirectory))
            {
              return SaveFileAs();
            }
            else
            {
              return SaveDataFromTextEditor(textEditor, textEditor.TextEditorModel.FilePath);
            }
          }
        }
      }

      return false;
    }

    public bool SaveBackup(DockItem control)
    {
      if (control != null)
      {
        var fileName = control.Title;
        if (fileManager.EditorWorkspaceModel.FilePaths.ContainsKey(fileName))
        {
          if (control.Content is TextEditorUI)
          {
            var textEditor = control.Content as TextEditorUI;
            SetBackup(textEditor.TextEditorModel.FileName, textEditor.Text);
            return true;
          }
        }
        else
        {
          if (control.Content is TranslatorItem translator)
          {
            var leftTextEditor = translator.GetLeftEditor();
            var rightTextEditor = translator.GetRightEditor();
            var fullPath = SetBackup(leftTextEditor.TextEditorModel.FileName, leftTextEditor.Text);
            rightTextEditor.TextEditorModel.FilePath = fullPath;
            return true;
          }
        }
      }
      return false;
    }

    private static string SetBackup(string fileName, string text)
    {
      string fullPath = Path.Combine($"{FileLocations.BackupDirectory}", fileName);
      if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
      {
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
      }
      using var fs = new FileStream(
        fullPath,
        FileMode.Create,
        FileAccess.Write,
        FileShare.None);

      using var sw = new StreamWriter(fs);
      sw.Write(text);
      return fullPath;
    }

    /// <summary>
    /// Сохраняет файл с новым именем через диалоговое окно "Сохранить как".
    /// </summary>
    /// <returns><c>true</c>, если файл был успешно сохранен, иначе <c>false</c>.</returns>
    public bool SaveFileAs()
    {
      var saveFileDialog = CreateSaveFileDialog();

      if (saveFileDialog.ShowDialog() == DialogResult.OK)
      {
        string filePath = saveFileDialog.FileName;
        var activeTab = GetActiveTab();
        int index = fileManager.EditorWorkspaceModel.OpenPages.IndexOf(activeTab);
        if (fileManager.EditorWorkspaceModel.UserControls[index] is TextEditorContainer control)
        {
          if (control != null)
          {
            var activeDockItem = control.DockManager.DockItems.FirstOrDefault(tab => tab.IsActiveItem == true);
            if (activeDockItem != null)
            {
              var textEditor = new TextEditorUI();
              if (activeDockItem.Content is TranslatorItem translatorItem)
              {
                textEditor = translatorItem.GetLeftEditor();
              }
              if (activeDockItem.Content is TextEditorUI activeTextEditor)
              {
                textEditor = activeTextEditor;
              }
              if (textEditor != null)
              {
                SaveDataFromTextEditor(textEditor, filePath);
                RenamePage(activeDockItem, filePath);

                textEditor.TextEditorModel.FilePath = filePath;
                textEditor.TextEditorModel.FileName = Path.GetFileName(filePath);
              }
            }
          }
          UpdateFilePaths(filePath);

          return true;
        }
      }

      return false;
    }

    /// <summary>
    /// Создает диалоговое окно для сохранения файла.
    /// </summary>
    /// <returns>Объект диалогового окна SaveFileDialog.</returns>
    private SaveFileDialog CreateSaveFileDialog()
    {
      var saveFileDialog = new SaveFileDialog
      {
        Filter = "Файлы программ контроля (*.pkw, *.PKW, *.Pkw)|*.pkw;*.PKW;*.Pkw|Текстовые файлы (*.txt)|*.txt",
        Title = "Сохранить файл как",
        FileName = GetActiveTabName(),
      };

      return saveFileDialog;
    }

    /// <summary>
    /// Получает активную вкладку, которая будет использоваться для сохранения.
    /// </summary>
    /// <returns>Активная вкладка типа <see cref="OpenFileButton"/>.</returns>
    private OpenFileButton GetActiveTab()
    {
      return fileManager.EditorWorkspaceModel.OpenPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
    }

    /// <summary>
    /// Получает имя активной вкладки.
    /// </summary>
    /// <returns>Имя активной вкладки.</returns>
    private string GetActiveTabName()
    {
      var activeTab = GetActiveTab();

      int index = fileManager.EditorWorkspaceModel.OpenPages.IndexOf(activeTab);
      if (fileManager.EditorWorkspaceModel.UserControls[index] is TextEditorContainer activeTextEditorContainer)
      {
        var activeTextEditorTab = activeTextEditorContainer.DockManager.DockItems.FirstOrDefault(tab => tab.IsActiveItem == true);
        if (activeTextEditorTab.Content is TextEditorUI textEditor)
        {
          return activeTextEditorTab != null ? Path.GetFileNameWithoutExtension(textEditor.TextEditorModel.FileName) : string.Empty;
        }
        if (activeTextEditorTab.Content is TranslatorItem translatorTab)
        {
          var translatorName = translatorTab.GetLeftEditorName();
          return activeTextEditorTab != null ? Path.GetFileNameWithoutExtension(translatorName) : string.Empty;
        }
      }

      return string.Empty;
    }

    /// <summary>
    /// Обновляет путь к файлу в словаре файлов.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    private void UpdateFilePaths(string filePath)
    {
      var fileName = Path.GetFileName(filePath);

      if (!fileManager.EditorWorkspaceModel.FilePaths.ContainsKey(fileName))
      {
        fileManager.EditorWorkspaceModel.FilePaths.Add(fileName, filePath);
      }
      else
      {
        fileManager.EditorWorkspaceModel.FilePaths[fileName] = filePath;
      }
    }

    /// <summary>
    /// Сохраняет данные из текстового редактора.
    /// </summary>
    /// <param name="filePath">Путь к открытому файлу.</param>
    /// <returns><c>true</c>, если файл был успешно сохранен, иначе <c>false</c>.</returns>
    private bool SaveDataFromTextEditor(TextEditorUI textEditor, string filePath)
    {
      try
      {
        var fileData = textEditor.Text;
        if (filePath.ToLower().EndsWith(".pkw") || filePath.ToLower().EndsWith(".txt"))
        {
          Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
          File.WriteAllText(filePath, fileData, Encoding.UTF8);
        }
        else
        {
          var encoding = textEditor.TextEditorModel.Encoding;
          Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
          File.WriteAllText(filePath, fileData, encoding == null ? Encoding.GetEncoding(866) : encoding);
        }

        LogInformation($"Файл {filePath} сохранен");
        if (ShouldShowSaveSuccessNotification(filePath))
        {
          NotificationHostService.Instance.Show(
            "Сохранение файла",
            $"Файл {Path.GetFileName(filePath)} сохранён",
            NotificationType.Success);
        }
        return true;
      }
      catch (Exception ex)
      {
        LogException(ex, $"Ошибка при сохранении файла: {filePath}");
        NotificationHostService.Instance.Show(
          "Ошибка сохранения файла",
          ex.Message,
          NotificationType.Error);
        return false;
      }
    }

    private static bool ShouldShowSaveSuccessNotification(string filePath)
    {
      var now = DateTime.UtcNow;
      var fullPath = Path.GetFullPath(filePath);

      lock (SaveNotificationSync)
      {
        if (LastSaveNotificationByFile.Count > 256)
        {
          var staleKeys = LastSaveNotificationByFile
            .Where(pair => now - pair.Value > SaveSuccessNotificationWindow)
            .Select(pair => pair.Key)
            .ToList();

          foreach (var staleKey in staleKeys)
          {
            LastSaveNotificationByFile.Remove(staleKey);
          }
        }

        if (LastSaveNotificationByFile.TryGetValue(fullPath, out var lastShownAt) &&
            now - lastShownAt < SaveSuccessNotificationWindow)
        {
          return false;
        }

        LastSaveNotificationByFile[fullPath] = now;
        return true;
      }
    }

    /// <summary>
    /// Переименовывает документ.
    /// </summary>
    /// <param name="activeTab">Активная вкладка с документом.</param>
    /// <param name="filePath">Путь к файлу.</param>
    private void RenamePage(DockItem activeTab, string filePath)
    {
      activeTab.TabText = Path.GetFileName(filePath);
      activeTab.Title = activeTab.TabText;
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="SaveFileManager"/>.
    /// </summary>
    /// <param name="fileManager">Экземпляр <see cref="FileManager"/>, который будет использован для управления файлами.</param>
    public SaveFileManager(FileManager fileManager)
    {
      this.fileManager = fileManager;
    }
  }
}
