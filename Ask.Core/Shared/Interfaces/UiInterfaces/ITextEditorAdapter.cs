namespace Ask.Core.Shared.Interfaces.UiInterfaces
{
  /// <summary>
  /// Универсальный фасад для взаимодействия с редактором текста.
  /// </summary>
  public interface ITextEditorAdapter
  {
    /// <summary>
    /// Установить маркер на указанную строку, очищая остальные.
    /// </summary>
    void SetActiveLine(int lineNumber);

    string Text { get; set; }
  }
}
