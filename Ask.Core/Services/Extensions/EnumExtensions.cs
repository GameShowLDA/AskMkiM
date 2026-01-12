using Ask.Core.Shared.Metadata.Atributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Ask.Core.Services.Extensions
{
  /// <summary>
  /// Набор методов-расширений для работы с перечислениями,
  /// поддерживающими отображаемые мнемоники через атрибуты.
  /// Позволяет получать информацию об отображении и сравнивать
  /// строковые мнемоники с значениями enum.
  /// </summary>
  public static class EnumExtensions
  {
    /// <summary>
    /// Возвращает атрибут с отображаемой информацией команды
    /// <see cref="CommandDisplayInfoAttribute"/> для указанного значения перечисления.
    /// </summary>
    public static CommandDisplayInfoAttribute? GetDisplayInfo(this Enum value)
    {
      var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
      return member?.GetCustomAttribute<CommandDisplayInfoAttribute>();
    }

    /// <summary>
    /// Возвращает организационный атрибут отображения команды
    /// <see cref="CommandOrganizationalAttribute"/> для указанного значения перечисления.
    /// </summary>
    public static CommandOrganizationalAttribute? GetDisplayOrganizationalInfo(this Enum value)
    {
      var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
      return member?.GetCustomAttribute<CommandOrganizationalAttribute>();
    }

    /// <summary>
    /// Проверяет, соответствует ли указанная строковая мнемоника значению перечисления,
    /// учитывая отображаемое имя из атрибутов или название элемента enum.
    /// Сравнение выполняется без учёта регистра.
    /// </summary>
    public static bool MatchesEnum(this string mnemonic, Enum value)
    {
      var display = value.GetDisplayInfo() ?? (object?)value.GetDisplayOrganizationalInfo();

      var displayMnemonic =
          display switch
          {
            CommandDisplayInfoAttribute info => info.DisplayName,
            CommandOrganizationalAttribute org => org.DisplayName,
            _ => value.ToString()
          };

      return string.Equals(mnemonic, displayMnemonic, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Получает локализованное отображаемое имя для значения перечисления (Display.Name).
    /// </summary>
    public static string GetDisplayName(this Enum enumValue)
    {
      return enumValue.GetType()
          .GetMember(enumValue.ToString())
          .First()
          .GetCustomAttribute<DisplayAttribute>()?.Name ?? enumValue.ToString();
    }

    public static string GetDescription(this Enum value)
    {
      var member = value.GetType().GetMember(value.ToString())[0];
      var attr = member.GetCustomAttribute<DescriptionAttribute>();
      return attr?.Description ?? value.ToString();
    }
  }
}
