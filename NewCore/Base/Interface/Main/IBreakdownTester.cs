using NewCore.Base.Function.Breakdown;
using NewCore.Base.Interface.Additionally;

namespace NewCore.Base.Interface.Main
{
  /// <summary>
  /// Интерфейс для пробойной установки
  /// </summary>
  public interface IBreakdownTester : IAttachableDevice
  {
    public IAcwModeBreakdown AcwManger { get; set; }
    public IDcwModeBreakdown DcwManger { get; set; }
    public IIrModeBreakdown IrManger { get; set; }
    public ISystemSettingsBreakdown SystemManger { get; set; }
  }
}
