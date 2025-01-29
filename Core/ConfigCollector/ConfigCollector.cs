using Core.Abstract;

namespace Core.ConfigCollector
{
  /// <summary>
  /// Конфигурация оборудования.
  /// </summary>
  static public partial class ConfigCollector
  {
    /// <summary>
    /// Gets or sets активный менеджер шасси.
    /// </summary>
    static private ManagerShassy.Model ManagerShassy { get; set; }

    /// <summary>
    /// Gets or sets активное устройство коммутации шин.
    /// </summary>
    static private DeviceBusCommutation.Model DeviceBusCommunication { get; set; }

    /// <summary>
    /// Gets or sets активное устройство модуля источника напряжения и тока.
    /// </summary>
    static private ModuleVoltageCurrentSource.Model ModuleVoltageCurrentSource { get; set; }

    /// <summary>
    /// Gets or sets активное устройство измерения (быстрый).
    /// </summary>
    static private MeterBase FastMeter { get; set; }

    /// <summary>
    /// Gets or sets активное устройство измерения (точный).
    /// </summary>
    static private MeterBase AccurateMeter { get; set; }

    /// <summary>
    /// Gets or sets активное пробойное устройство.
    /// </summary>
    static private BreakdownBase Breakdown { get; set; }

    /// <summary>
    /// Gets or sets список активных модулей коммутации реле.
    /// </summary>
    static private List<ModuleRelayControl.Model> ModuleRelayControlsList { get; set; }

    /// <summary>
    /// Сбрасывает модели МКР.
    /// </summary>
    static public void ClearMkrModel()
    {
      ModuleRelayControlsList = null;
    }
  }
}
