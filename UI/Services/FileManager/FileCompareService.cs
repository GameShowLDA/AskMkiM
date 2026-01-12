using Ask.Core.Shared.Interfaces.UiInterfaces;
using Message;
using System.IO;
using System.Windows;

namespace UI.Services.FileManager
{
  /// <summary>
  /// Сервис для сравнения содержимого открытого файла с содержимым редактора.
  /// Предназначен для определения, были ли внесены изменения в текст по сравнению с сохранённой версией на диске.
  /// </summary>
  public class FileCompareService
  {
    private readonly EditorWorkspaceModel _context;

    /// <summary>
    /// Инициализирует новый экземпляр сервиса сравнения файлов.
    /// </summary>
    /// <param name="editorWorkspaceModel">Контекст редактора, содержащий пути к открытым файлам.</param>
    public FileCompareService(EditorWorkspaceModel editorWorkspaceModel)
    {
      _context = editorWorkspaceModel;
    }

    /// <summary>
    /// Проверяет, изменилось ли содержимое открытого файла в редакторе по сравнению с сохранённой версией на диске.
    /// </summary>
    /// <param name="control">Объект <see cref="IDockItem"/>, представляющий открытую вкладку с редактируемым текстом.</param>
    /// <returns>
    /// Возвращает <c>true</c>, если файл был изменён (содержимое отличается, файл не найден или является новым);
    /// <c>false</c>, если содержимое файла не изменялось.
    /// </returns>
    public bool HasFileChanged(IDockItem control)
    {
      if (!IsValidDockItem(control)) return false;
      if (IsIgnoredFile(control.Title)) return false;

      if (!TryGetFilePath(control.Title, out var filePath))
        return true;

      return string.IsNullOrEmpty(filePath)
        ? CheckUnsavedFile(control)
        : CompareWithSavedFile(control, filePath);
    }

    #region 🔍 Подметоды проверки состояния файла

    /// <summary>
    /// Проверяет корректность объекта <see cref="IDockItem"/>.
    /// </summary>
    private static bool IsValidDockItem(IDockItem control)
    {
      return !string.IsNullOrEmpty(control?.Title);
    }

    /// <summary>
    /// Проверяет, относится ли файл к игнорируемым (например, служебные файлы OPK).
    /// </summary>
    private static bool IsIgnoredFile(string fileName)
    {
      return fileName.Contains(".opk", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Пытается получить путь к файлу из контекста.
    /// </summary>
    private bool TryGetFilePath(string fileName, out string filePath)
    {
      return _context.FilePaths.TryGetValue(fileName, out filePath);
    }

    /// <summary>
    /// Проверяет состояние несохранённого файла: если в редакторе есть содержимое — он считается изменённым.
    /// </summary>
    private static bool CheckUnsavedFile(IDockItem control)
    {
      if (control.Content is ITextEditorAdapter editor)
        return !string.IsNullOrWhiteSpace(editor.Text);

      return false;
    }

    /// <summary>
    /// Сравнивает содержимое редактора с сохранённым файлом на диске.
    /// </summary>
    private static bool CompareWithSavedFile(IDockItem control, string filePath)
    {
      if (!File.Exists(filePath))
        return HandleMissingFile();

      var diskContent = File.ReadAllText(filePath);

      if (control.Content is ITextEditorAdapter editor)
        return diskContent != editor.Text;

      return false;
    }

    /// <summary>
    /// Обрабатывает ситуацию, когда файл отсутствует на диске.
    /// </summary>
    private static bool HandleMissingFile()
    {
      MessageBoxCustom.Show("Файл был удалён или повреждён", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
      return true;
    }

    #endregion
  }
}
