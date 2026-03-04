using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Mode
{
  /// <summary>
  /// Интерфейс для режима измерения сопротивления изоляции (IR).
  /// </summary>
  public interface IIrModeBreakdown : IBreakdownMode<IrConfiguration>
  {
    IResistanceLimitsConfigurable ResistanceLimits { get; set; }
  }
}
