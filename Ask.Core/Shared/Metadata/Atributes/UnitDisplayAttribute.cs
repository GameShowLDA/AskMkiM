using Ask.Core.Shared.Metadata.Enums.UnitEnums;
using System.Reflection;

namespace Ask.Core.Shared.Metadata.Atributes
{
  /// <summary>
  /// Человеко-читаемое представление значения enum
  /// (для протоколов, отчётов и логов).
  /// </summary>
  [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
  public sealed class UnitDisplayAttribute : Attribute
  {
    public string Display { get; }

    /// <summary>
    /// Обозначение величины (например: R, U, I, C).
    /// </summary>
    public QuantitySymbol Symbol { get; }

    public UnitDisplayAttribute(string value, QuantitySymbol quantitySymbol)
    {
      Display = value;
      Symbol = quantitySymbol;
    }
  }
}
