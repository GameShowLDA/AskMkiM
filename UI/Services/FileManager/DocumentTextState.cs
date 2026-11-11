using Ask.Core.Shared.DTO.TextEditor;

namespace UI.Services.FileManager
{
  /// <summary>
  /// Сравнивает текущее содержимое документа с последним сохранённым состоянием.
  /// </summary>
  internal static class DocumentTextState
  {
    /// <summary>
    /// Проверяет наличие содержательных изменений в тексте документа.
    /// </summary>
    /// <param name="savedText">Последнее успешно сохранённое содержимое.</param>
    /// <param name="currentText">Текущее содержимое редактора.</param>
    /// <returns>
    /// <see langword="true"/>, если содержимое изменилось; иначе <see langword="false"/>.
    /// </returns>
    internal static bool IsModified(string? savedText, string? currentText) =>
      !string.Equals(Normalize(savedText), Normalize(currentText), StringComparison.Ordinal);

    /// <summary>
    /// Фиксирует текущее содержимое как последнее успешно сохранённое состояние.
    /// </summary>
    /// <param name="model">Состояние документа редактора.</param>
    /// <param name="text">Успешно сохранённое содержимое.</param>
    internal static void MarkSaved(TextEditorModel model, string? text)
    {
      ArgumentNullException.ThrowIfNull(model);
      model.SavedTextSnapshot = text ?? string.Empty;
    }

    private static string Normalize(string? text) =>
      (text ?? string.Empty)
        .TrimStart('\uFEFF')
        .Replace("\r\n", "\n")
        .Replace('\r', '\n');
  }
}
