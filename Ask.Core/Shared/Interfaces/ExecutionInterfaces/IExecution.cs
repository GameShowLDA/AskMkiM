namespace Ask.Core.Shared.Interfaces.ExecutionInterfaces
{
  /// <summary>
  /// Определяет интерфейс выполнения, предоставляющий доступ к управляющему адаптеру текста.
  /// </summary>
  public interface IExecution
  {
    /// <summary>
    /// Возвращает адаптер управления текстом, используемый для отображения или обработки данных в ходе выполнения.
    /// </summary>
    /// <returns>Экземпляр <see cref="ITextAdapter"/>.</returns>
    ITextAdapter GetControl();
  }
}
