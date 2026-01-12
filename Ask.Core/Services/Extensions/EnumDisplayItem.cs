namespace Ask.Core.Services.Extensions
{
  /// <summary>
  /// Элемент представления значения перечисления для отображения в UI.
  /// Содержит текстовое описание для пользователя и фактическое значение enum.
  /// </summary>
  public class EnumDisplayItem
  {
    /// <summary>
    /// Отображаемое описание значения перечисления.
    /// Используется в элементах интерфейса (списки, выпадающие меню и т.п.).
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Реальное значение перечисления, связанное с данным элементом.
    /// Используется в логике приложения.
    /// </summary>
    public Enum Value { get; set; } = null!;
  }

}
