using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Mode
{
  /// <summary>
  /// Управление режимом ACW на пробойной установке.
  /// </summary>
  public interface IAcwModeBreakdown : IBreakdownMode<AcwConfiguration>
  {
    /// <summary>
    /// Управление пределами тока (верхний и нижний токовые лимиты).
    /// </summary>
    ICurrentLimitsConfigurable CurrentLimits { get; set; }

    /// <summary>
    /// Управление параметром тока дуги (Arc Current).
    /// </summary>
    IArcCurrentConfigurable ArcCurrent { get; set; }

    IFrequencyConfigurable FrequencyConfigurable { get; set; }
  }
}
