using Ask.Core.Shared.Metadata.Enums.UnitEnums;
using System.Reflection;

namespace Ask.Core.Shared.Metadata.Atributes
{
  /// <summary>
  /// Задаёт человеко-читаемое представление значения перечисления
  /// для использования в протоколах, отчётах и журналах.
  /// </summary>
  [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
  public sealed class UnitDisplayAttribute : Attribute
  {
    /// <summary>
    /// Человеко-читаемое представление единицы измерения.
    /// </summary>
    public string Display { get; }

    /// <summary>
    /// Обозначение физической величины
    /// (например: R, U, I, C).
    /// </summary>
    public QuantitySymbol Symbol { get; }

    /// <summary>
    /// Инициализирует атрибут отображения единицы измерения.
    /// </summary>
    /// <param name="value">Строковое представление единицы измерения.</param>
    /// <param name="quantitySymbol">Обозначение физической величины.</param>
    public UnitDisplayAttribute(string value, QuantitySymbol quantitySymbol)
    {
      Display = value;
      Symbol = quantitySymbol;
    }
  }
}