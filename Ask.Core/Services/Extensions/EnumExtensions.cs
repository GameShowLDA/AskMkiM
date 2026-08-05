using Ask.Core.Shared.Metadata.Atributes;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Ask.Core.Services.Extensions
{
  /// <summary>
  /// Предоставляет набор методов-расширений для работы с перечислениями,
  /// использующими атрибуты метаданных.
  /// <para>
  /// Позволяет получать описания, отображаемые имена, сведения о командах,
  /// единицах измерения и выполнять сравнение строковых представлений
  /// с элементами перечислений.
  /// </para>
  /// </summary>
  public static class EnumExtensions
  {
    /// <summary>
    /// Возвращает атрибут указанного типа, применённый к значению перечисления.
    /// </summary>
    /// <typeparam name="TAttribute">Тип искомого атрибута.</typeparam>
    /// <param name="value">Значение перечисления.</param>
    /// <returns>
    /// Найденный атрибут либо <see langword="null"/>, если атрибут отсутствует.
    /// </returns>
    public static TAttribute GetAttribute<TAttribute>(this Enum value)
      where TAttribute : Attribute
    {
      var member = value.GetType()
                        .GetMember(value.ToString())
                        .FirstOrDefault();

      return member?.GetCustomAttribute<TAttribute>();
    }

    /// <summary>
    /// Возвращает атрибут указанного типа, применённый к значению перечисления.
    /// </summary>
    /// <typeparam name="TAttribute">Тип искомого атрибута.</typeparam>
    /// <param name="value">Значение перечисления.</param>
    /// <returns>Найденный атрибут.</returns>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если атрибут отсутствует.
    /// </exception>
    public static TAttribute GetRequiredAttribute<TAttribute>(this Enum value)
      where TAttribute : Attribute
    {
      return value.GetAttribute<TAttribute>()
          ?? throw new InvalidOperationException(
              $"Для значения '{value}' отсутствует атрибут {typeof(TAttribute).Name}.");
    }

    /// <summary>
    /// Возвращает нижнюю границу диапазона, указанную в атрибуте
    /// <see cref="CommandDisplayInfoAttribute"/>.
    /// </summary>
    /// <param name="value">Значение перечисления.</param>
    /// <returns>Нижняя граница диапазона.</returns>
    public static double GetLowerLimit(this Enum value)
    {
      return value.GetRequiredAttribute<CommandDisplayInfoAttribute>()
                  .LowerLimit;
    }

    /// <summary>
    /// Возвращает верхнюю границу диапазона, указанную в атрибуте
    /// <see cref="CommandDisplayInfoAttribute"/>.
    /// </summary>
    /// <param name="value">Значение перечисления.</param>
    /// <returns>Верхняя граница диапазона.</returns>
    public static double GetUpperLimit(this Enum value)
    {
      return value.GetRequiredAttribute<CommandDisplayInfoAttribute>()
                  .UpperLimit;
    }

    /// <summary>
    /// Возвращает атрибут отображения команды.
    /// </summary>
    public static CommandDisplayInfoAttribute GetCommandDisplayInfo(this Enum value)
      => value.GetAttribute<CommandDisplayInfoAttribute>();

    /// <summary>
    /// Возвращает атрибут отображения команды измерения.
    /// </summary>
    public static CommandDisplayInfoAttribute GetDisplayInfo(this MeasurementTypeCommand value)
      => value.GetCommandDisplayInfo();

    /// <summary>
    /// Возвращает организационный атрибут отображения команды.
    /// </summary>
    public static CommandOrganizationalAttribute GetCommandOrganizationalInfo(this Enum value)
      => value.GetAttribute<CommandOrganizationalAttribute>();

    /// <summary>
    /// Возвращает организационный атрибут отображения команды.
    /// </summary>
    public static CommandOrganizationalAttribute GetDisplayOrganizationalInfo(this OrganizationalComands value)
      => value.GetCommandOrganizationalInfo();

    /// <summary>
    /// Возвращает атрибут единицы измерения.
    /// </summary>
    public static UnitDisplayAttribute GetUnitDisplay(this Enum value)
      => value.GetAttribute<UnitDisplayAttribute>();

    /// <summary>
    /// Возвращает отображаемое обозначение единицы измерения
    /// (например: «В», «Ом», «мА»).
    /// </summary>
    public static string GetUnit(this Enum value)
      => value.GetRequiredAttribute<UnitDisplayAttribute>().Display;

    /// <summary>
    /// Возвращает обозначение физической величины
    /// (например: U, I, R, C).
    /// </summary>
    public static QuantitySymbol GetQuantitySymbol(this Enum value)
      => value.GetRequiredAttribute<UnitDisplayAttribute>().Symbol;

    /// <summary>
    /// Проверяет, соответствует ли указанная строка отображаемому имени
    /// элемента перечисления без учёта регистра.
    /// </summary>
    /// <param name="mnemonic">Строковое представление.</param>
    /// <param name="value">Значение перечисления.</param>
    /// <returns>
    /// <see langword="true"/>, если строки совпадают;
    /// иначе — <see langword="false"/>.
    /// </returns>
    public static bool MatchesEnum(this string mnemonic, Enum value)
    {
      var display = value.GetCommandDisplayInfo()
                    ?? (object?)value.GetCommandOrganizationalInfo();

      var displayMnemonic = display switch
      {
        CommandDisplayInfoAttribute info => info.DisplayName,
        CommandOrganizationalAttribute org => org.DisplayName,
        _ => value.ToString()
      };

      return string.Equals(
          mnemonic,
          displayMnemonic,
          StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Возвращает отображаемое имя из атрибута
    /// <see cref="DisplayAttribute"/>.
    /// Если атрибут отсутствует, возвращает имя элемента перечисления.
    /// </summary>
    public static string GetDisplayName(this Enum value)
      => value.GetAttribute<DisplayAttribute>()?.Name ?? value.ToString();

    public static string GetDisplayDescription(this Enum value)
    {
      return value.GetType()
          .GetMember(value.ToString())
          .FirstOrDefault()
          ?.GetCustomAttribute<DisplayAttribute>()
          ?.GetDescription() ?? string.Empty;
    }

    /// <summary>
    /// Возвращает описание из атрибута
    /// <see cref="DescriptionAttribute"/>.
    /// Если атрибут отсутствует, возвращает имя элемента перечисления.
    /// </summary>
    public static string GetDescription(this Enum value)
      => value.GetAttribute<DescriptionAttribute>()?.Description ?? value.ToString();
  }
}
