namespace Ask.Core.Shared.Metadata.Enums.DeviceEnums
{
  /// <summary>
  /// Перечисление типов устройств.
  /// </summary>
  public enum DeviceType
  {
    /// <summary>
    /// Тестер АСКМ
    /// </summary>
    ChassisManager,

    /// <summary>
    /// Стойка СКМ
    /// </summary>
    Rack,

    /// <summary>
    /// Модуль коммутации реле
    /// </summary>
    RelaySwitchModule,

    /// <summary>
    /// Модуль источника напряжения и тока
    /// </summary>
    PowerSourceModule,

    /// <summary>
    /// Устройство коммутации
    /// </summary>
    SwitchingDevice,

    /// <summary>
    /// Измеритель точный
    /// </summary>
    PrecisionMeter,

    /// <summary>
    /// Измеритель быстрый
    /// </summary>
    FastMeter,

    /// <summary>
    /// Пробойная установка
    /// </summary>
    BreakdownTester,

    /// <summary>
    /// Бесперебойник
    /// </summary>
    UninterruptiblePowerSupply,

    Unknown,
  }
}
