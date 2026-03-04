using Ask.Core.Shared.Metadata.View.EditorHost.TextEditor;

namespace Ask.Core.Shared.Metadata.View.EditorHost
{
  /// <summary>
  /// Управляет вкладками трансляции: исходный файл + результат.
  /// </summary>
  public interface ITranslationService
  {
    /// <summary>
    /// Создаёт файл трансляции на основе исходного.
    /// </summary>
    ITextEditorView CreateTranslationFile(string parentFilePath);

    ///// <summary>
    ///// Добавляет элемент трансляции.
    ///// </summary>
    //Task<TranslatorItem> AddTranslatorItem(TextEditorUI source, TextEditorUI translated, EditorType type);

    ///// <summary>
    ///// Удаляет элемент трансляции.
    ///// </summary>
    //Task DeleteTranslatorItem(TranslatorItem item, EditorType type);
  }
}
