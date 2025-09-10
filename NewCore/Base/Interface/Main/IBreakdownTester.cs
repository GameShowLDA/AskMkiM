using NewCore.Base.Function.Breakdown;
using NewCore.Base.Interface.Additionally;

namespace NewCore.Base.Interface.Main
{
  /// <summary>
  /// Интерфейс для пробойной установки.
  /// </summary>
  public interface IBreakdownTester : IAttachableDevice
  {
    /// <summary>
    /// Макссимально выдаваемое напряжение.
    /// </summary>
    public int MaxVoltage { get; set; }

    /// <summary>
    /// Управление режимом переменного тока (ACW) в пробойной установке.
    /// </summary>
    public IAcwModeBreakdown AcwManger { get; set; }

    /// <summary>
    /// Управление режимом постоянного тока (DCW) в пробойной установке.
    /// </summary>
    public IDcwModeBreakdown DcwManger { get; set; }

    /// <summary>
    /// Управление режимом измерения сопротивления изоляции (IR) в пробойной установке.
    /// </summary>
    public IIrModeBreakdown IrManger { get; set; }

    /// <summary>
    /// Управление системными настройками пробойной установки.
    /// </summary>
    public ISystemSettingsBreakdown SystemManger { get; set; }
  }
}
