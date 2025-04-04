namespace ConsoleUtilities.Core
{
  /// <summary>
  /// Интерфейс для форматирования и отображения табличных данных в консоли.
  /// </summary>
  public interface ITableFormatter
  {
    /// <summary>
    /// Отображает список записей в виде таблицы.
    /// </summary>
    /// <typeparam name="T">Тип записей в таблице.</typeparam>
    /// <param name="records">Список отображаемых объектов.</param>
    void DisplayTable<T>(List<T> records);
  }
}
