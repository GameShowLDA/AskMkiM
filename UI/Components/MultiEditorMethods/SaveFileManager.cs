using System.IO;
using System.Windows.Forms;
using System.Windows.Media;
using UI.Components.Invoke;
using UI.Controls.TextEditor;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using static Utilities.LoggerUtility;
using Path = System.IO.Path;
using System.Windows;
using System.Text;

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
    public void SaveFileDialog(ref MessageBoxResult result, ref bool saveFileResult, int index)
    {
      var needToSave = fileManager.CompareFiles(fileManager.OpenPages[index]);
      if (needToSave)
      {
        result = MessageBox.Show(
            $"Сохранить файл {fileManager.OpenPages[index].Text} перед закрытием?",
            "Подтверждение",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
          saveFileResult = SaveFile(fileManager.OpenPages[index]);
        }
      }
    }

    /// <summary>
    /// Сохраняет файл. Если файл еще не сохранен, вызывает диалоговое окно для сохранения как нового файла.
    /// </summary>
    /// <param name="activeTab">Активная вкладка, для которой будет сохранен файл.</param>
    /// <returns><c>true</c>, если файл успешно сохранен, иначе <c>false</c>.</returns>
    public bool SaveFile(OpenFileButton activeTab)
    {
      if (activeTab != null)
      {
        var fileName = activeTab.Text;
        if (fileManager.FilePaths[fileName] == string.Empty)
        {
          return SaveFileAs();
        }
        else
        {
          var filePath = fileManager.FilePaths[fileName];
          return SaveDataFromTextEditor(activeTab, filePath);
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

        SaveDataFromTextEditor(activeTab, filePath);

        RenamePage(activeTab, filePath);

        UpdateFilePaths(filePath);

        return true;
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
        Filter = "Text Files (*.txt)|*.txt|RTF Files (*.rtf)|*.rtf",
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
      return activeTab != null ? Path.GetFileNameWithoutExtension(activeTab.Text) : string.Empty;
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
    /// <param name="activeTab">Активная вкладка с документом.</param>
    /// <param name="filePath">Путь к открытому файлу.</param>
    /// <returns><c>true</c>, если файл был успешно сохранен, иначе <c>false</c>.</returns>
    private bool SaveDataFromTextEditor(OpenFileButton activeTab, string filePath)
    {
      string fileData = string.Empty;

      int index = fileManager.OpenPages.IndexOf(activeTab);
      if (fileManager.UserControls[index] is TextEditorUI)
      {
        var textEditor = fileManager.UserControls[index] as TextEditorUI;
        fileData = textEditor.Text;
        if (filePath.ToLower().Contains(".pk") && !filePath.ToLower().Contains(".pkw"))
        {
          Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
          File.WriteAllText(filePath, fileData, Encoding.GetEncoding(866));
        }
        else
        {
          File.WriteAllText(filePath, fileData);
        }
        LogInformation($"Файл {filePath} сохранен");
        MessageBox.Show($"Файл {filePath} сохранен");
        return true;
      }

      return false;
    }

    /// <summary>
    /// Переименовывает документ.
    /// </summary>
    /// <param name="activeTab">Активная вкладка с документом.</param>
    /// <param name="filePath">Путь к файлу.</param>
    private void RenamePage(OpenFileButton activeTab, string filePath)
    {
      var acivePage = fileManager.OpenPages.FirstOrDefault(p => p == activeTab);
      if (acivePage != null)
      {
        activeTab.Header.Text = System.IO.Path.GetFileName(filePath);
      }
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
