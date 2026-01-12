namespace Ask.Core.Shared.Interfaces.ExecutionInterfaces
{
  /// <summary>
  /// Представляет адаптер работы с текстовыми данными,
  /// используемый для получения текстового содержимого из источника.
  /// </summary>
  public interface ITextAdapter
  {
    /// <summary>
    /// Возвращает текущее текстовое содержимое,
    /// предоставляемое адаптером.
    /// </summary>
    /// <returns>Строка с текстом.</returns>
    string GetText();
  }
}
