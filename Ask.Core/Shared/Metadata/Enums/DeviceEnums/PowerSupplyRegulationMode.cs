namespace Ask.Core.Shared.Metadata.Enums.DeviceEnums;

/// <summary>
/// Задаёт текущий режим регулирования выхода источника питания.
/// </summary>
public enum PowerSupplyRegulationMode
{
  /// <summary>
  /// Выход источника выключен.
  /// </summary>
  OutputDisabled,

  /// <summary>
  /// Источник стабилизирует напряжение.
  /// </summary>
  ConstantVoltage,

  /// <summary>
  /// Источник ограничивает ток.
  /// </summary>
  ConstantCurrent
}
