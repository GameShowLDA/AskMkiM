using Ask.Core.Shared.Interfaces.UiInterfaces;
using Message;
using System.IO;
using System.Windows;
using UI.Controls;
using UI.Controls.TextEditorControl;

namespace UI.Services.FileManager
{
  /// <summary>
  /// Сервис для определения локальных изменений в редакторе.
  /// Сравнивает текущий текст с последним загруженным или сохранённым снимком.
  /// </summary>
  public class FileCompareService
  {
    /// <summary>
    /// Инициализирует новый экземпляр сервиса сравнения файлов.
    /// </summary>
    /// <param name="editorWorkspaceModel">Контекст редактора.</param>
    public FileCompareService(EditorWorkspaceModel editorWorkspaceModel)
    {
      _ = editorWorkspaceModel;
    }

    /// <summary>
    /// Проверяет, изменилось ли содержимое редактора относительно последнего сохранённого состояния.
    /// </summary>
    /// <param name="control">Вкладка редактора или транслятора.</param>
    /// <returns>
    /// Возвращает <c>true</c>, если в редакторе есть несохранённые изменения;
    /// иначе <c>false</c>.
    /// </returns>
    public bool HasFileChanged(IDockItem control)
    {
      if (!IsValidDockItem(control))
      {
        return false;
      }

      var editor = ExtractSourceEditor(control);
      if (editor?.TextEditorModel == null)
      {
        return false;
      }

      if (IsIgnoredFile(editor.TextEditorModel.FileName ?? control.Title))
      {
        return false;
      }

      if (string.IsNullOrWhiteSpace(editor.TextEditorModel.FilePath))
      {
        return CheckUnsavedFile(editor);
      }

      if (editor.TextEditorModel.SavedTextSnapshot != null)
      {
        return !string.Equals(
          NormalizeText(editor.TextEditorModel.SavedTextSnapshot),
          NormalizeText(editor.Text),
          StringComparison.Ordinal);
      }

      return CompareWithSavedFile(editor, editor.TextEditorModel.FilePath);
    }

    /// <summary>
    /// Проверяет корректность объекта <see cref="IDockItem"/>.
    /// </summary>
    private static bool IsValidDockItem(IDockItem control)
    {
      return control != null;
    }

    /// <summary>
    /// Проверяет, относится ли файл к игнорируемым.
    /// </summary>
    private static bool IsIgnoredFile(string? fileName)
    {
      return !string.IsNullOrWhiteSpace(fileName)
        && fileName.Contains(".opk", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Возвращает исходный редактор текста для вкладки.
    /// Для транслятора используется левый редактор с исходным файлом.
    /// </summary>
    private static TextEditorUI? ExtractSourceEditor(IDockItem control)
    {
      return control.Content switch
      {
        TextEditorUI textEditor => textEditor,
        TranslatorItem translatorItem => translatorItem.GetLeftBox()?.GetTextEditor(),
        TranslatorEditor translatorEditor => translatorEditor.GetTextEditor(),
        _ => null,
      };
    }

    /// <summary>
    /// Проверяет состояние нового несохранённого файла.
    /// </summary>
    private static bool CheckUnsavedFile(TextEditorUI editor)
    {
      return !string.IsNullOrWhiteSpace(editor.Text);
    }

    /// <summary>
    /// Сравнивает текст редактора с содержимым файла на диске.
    /// Используется только как резервный путь, если снимок ещё не заполнен.
    /// </summary>
    private static bool CompareWithSavedFile(TextEditorUI editor, string filePath)
    {
      if (!File.Exists(filePath))
      {
        return HandleMissingFile();
      }

      var diskContent = File.ReadAllText(filePath);
      return !string.Equals(
        NormalizeText(diskContent),
        NormalizeText(editor.Text),
        StringComparison.Ordinal);
    }

    /// <summary>
    /// Обрабатывает ситуацию, когда файл отсутствует на диске.
    /// </summary>
    private static bool HandleMissingFile()
    {
      MessageBoxCustom.Show("Файл был удалён или повреждён", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
      return true;
    }

    /// <summary>
    /// Нормализует переводы строк, чтобы они не считались изменением содержимого.
    /// </summary>
    private static string NormalizeText(string? text)
    {
      return (text ?? string.Empty)
        .Replace("\r\n", "\n")
        .Replace('\r', '\n');
    }
  }
}
