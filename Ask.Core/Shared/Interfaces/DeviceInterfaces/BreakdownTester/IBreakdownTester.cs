using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Mode;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester
{
  /// <summary>
  /// Интерфейс для пробойной установки.
  /// </summary>
  public interface IBreakdownTester : IAttachableDevice
  {
    /// <summary>
    /// Активный режим ППУ.
    /// </summary>
    BreakdownTypeMode Mode { get; set; }

    /// <summary>
    /// Макссимально выдаваемое напряжение.
    /// </summary>
    int MaxVoltage { get; set; }

    /// <summary>
    /// Минимально выдаваемое напряжение при измерении сопротивления.
    /// </summary>
    int IRMinVoltage { get; set; }

    /// <summary>
    /// Управление режимом переменного тока (ACW) в пробойной установке.
    /// </summary>
    IAcwModeBreakdown AcwManger { get; set; }

    /// <summary>
    /// Управление режимом постоянного тока (DCW) в пробойной установке.
    /// </summary>
    IDcwModeBreakdown DcwManger { get; set; }

    /// <summary>
    /// Управление режимом измерения сопротивления изоляции (IR) в пробойной установке.
    /// </summary>
    IIrModeBreakdown IrManger { get; set; }

    /// <summary>
    /// Управление системными настройками пробойной установки.
    /// </summary>
    ISystemSettingsBreakdown SystemManger { get; set; }

    ISelfTestCheckerBreakdownTester SelfTestManager { get; set; }
  }
}
