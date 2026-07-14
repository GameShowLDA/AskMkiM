using System.ComponentModel;
using System.Reflection;

namespace Ask.Engine.Tests.SelfControl;

/// <summary>
/// Предоставляет служебные сведения о модулях самоконтроля старого тестера АСК.
/// </summary>
public static class LegacyAskSelfControlModuleMetadata
{
  /// <summary>
  /// Возвращает человекочитаемое название модуля.
  /// </summary>
  public static string GetDisplayName(LegacyAskSelfControlModule module)
  {
    var field = typeof(LegacyAskSelfControlModule).GetField(module.ToString());
    var description = field?.GetCustomAttribute<DescriptionAttribute>()?.Description;
    return string.IsNullOrWhiteSpace(description) ? module.ToString() : description;
  }

  /// <summary>
  /// Возвращает ключ старой программы для запуска модуля через mkiw.exe /tests.
  /// </summary>
  public static string GetLegacyTestCode(LegacyAskSelfControlModule module) => ((int)module).ToString();

  /// <summary>
  /// Возвращает краткое описание условия доступности модуля.
  /// </summary>
  public static string GetUnavailableReason(LegacyAskSelfControlModule module) => module switch
  {
    LegacyAskSelfControlModule.DigitalVoltmeter => "В конфигурации отключён или отсутствует цифровой вольтметр.",
    LegacyAskSelfControlModule.Adc => "В конфигурации отключён АЦП.",
    LegacyAskSelfControlModule.DeviceSwitching => "В конфигурации не задан ПИНТ3 или ПИНТ4 для проверки коммутации устройств.",
    LegacyAskSelfControlModule.Pints => "В конфигурации не задан ПИНТ3 или ПИНТ4.",
    LegacyAskSelfControlModule.Commutator => "В конфигурации не заданы диапазоны коммутатора.",
    LegacyAskSelfControlModule.Ppu => "В конфигурации отключена ППУ.",
    LegacyAskSelfControlModule.Pki => "В конфигурации отключена ПКИ.",
    LegacyAskSelfControlModule.Timer => "Для проверки таймера нужен ПИНТ4 и неразделённые входы БК.",
    _ => "Модуль недоступен по текущей конфигурации."
  };
}
