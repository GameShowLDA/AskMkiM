using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using Message;
using UI.Windows.WpfDocking.Windows.Docking;
using UI.Components.Invoke;
using UI.Controls.TextEditor;
using static Utilities.LoggerUtility;
using Application = System.Windows.Application;
using Path = System.IO.Path;
using Microsoft.Win32.SafeHandles;
using UI.Controls;

namespace UI.Components.MultiEditorMethods
{
  /// <summary>
  /// Класс для работы с сохранением файлов.
  /// </summary>
  public class SaveFileManager
  {
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
      var needToSave = fileManager.CompareFiles(control);
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
      }
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
        if (fileManager.FilePaths.ContainsKey(fileName))
        {
          if (fileManager.FilePaths[fileName] == string.Empty)
          {
            // TODO: сохранение файла
            return SaveFileAs();
          }
          else
          {
            var filePath = fileManager.FilePaths[fileName];
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
            return SaveDataFromTextEditor(textEditor, textEditor.TextEditorModel.FilePath);
          }
        }
      }

      return false;
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
        int index = fileManager.OpenPages.IndexOf(activeTab);
        if (fileManager.UserControls[index] is TextEditorContainer control)
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
        Filter = "Файлы программ контроля (*.pk, *.PK, *.Pk)|*.pk;*.PK;*.Pk|Текстовые файлы (*.txt)|*.txt",
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
      return fileManager.OpenPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
    }

    /// <summary>
    /// Получает имя активной вкладки.
    /// </summary>
    /// <returns>Имя активной вкладки.</returns>
    private string GetActiveTabName()
    {
      var activeTab = GetActiveTab();

      int index = fileManager.OpenPages.IndexOf(activeTab);
      if (fileManager.UserControls[index] is TextEditorContainer activeTextEditorContainer)
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

      if (!fileManager.FilePaths.ContainsKey(fileName))
      {
        fileManager.FilePaths.Add(fileName, filePath);
      }
      else
      {
        fileManager.FilePaths[fileName] = filePath;
      }
    }

    /// <summary>
    /// Сохраняет данные из текстового редактора.
    /// </summary>
    /// <param name="filePath">Путь к открытому файлу.</param>
    /// <returns><c>true</c>, если файл был успешно сохранен, иначе <c>false</c>.</returns>
    private bool SaveDataFromTextEditor(TextEditorUI textEditor, string filePath)
    {
      var fileData = textEditor.Text;
      if (filePath.ToLower().Contains(".pk") && !filePath.ToLower().Contains(".pkw"))
      {
        var encoding = textEditor.TextEditorModel.Encoding;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        File.WriteAllText(filePath, fileData, encoding == null ? Encoding.GetEncoding(866) : encoding);
      }
      else
      {
        File.WriteAllText(filePath, fileData);
      }
      LogInformation($"Файл {filePath} сохранен");
      MessageBoxCustom.Show($"Файл {filePath} сохранен", image: MessageBoxImage.Information);
      return true;
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
