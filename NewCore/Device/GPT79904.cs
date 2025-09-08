using System.IO.Ports;
using NewCore.Base.Device;
using NewCore.Base.Function.Breakdown;
using NewCore.Base.Interface.Main;
using NewCore.Enum;
using NewCore.Function.GPT;
using NewCore.FunctionAdapters.GPT;

namespace NewCore.Device
{
  /// <summary>
  /// Класс, представляющий пробойную установку GPT79904, работающую через последовательный порт (COM).
  /// </summary>
  public class GPT79904 : DeviceWithCOM, IBreakdownTester
  {

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="GPT79904"/>.
    /// </summary>
    public GPT79904()
    {
      BaudRate = 115200;
      StopBits = StopBits.One;
      DataBits = 8;
      Parity = Parity.None;
      DeviceClass = GetType().FullName;

      DeviceType = DeviceEnum.DeviceType.BreakdownTester;

      AcwManger = new AcwModeAdapter(this);
      DcwManger = new DcwModeAdapter(this);
      IrManger = new IrModeAdapter(this);
      SystemManger = new SystemSettingsAdapter(this);
      ConnectableManager = new ConnectableManagerAdapter(this);
    }

    /// <inheritdoc />
    public new string Name { get => "GPT79904"; }

    /// <inheritdoc />
    public new string Description { get => "Реализовать описание в NewCore.Device.GPT79904"; }

    /// <inheritdoc />
    public int NumberChassis { get; set; }

    /// <inheritdoc />
    public IAcwModeBreakdown AcwManger { get; set; }

    /// <inheritdoc />
    public IDcwModeBreakdown DcwManger { get; set; }

    /// <inheritdoc />
    public IIrModeBreakdown IrManger { get; set; }

    /// <inheritdoc />
    public ISystemSettingsBreakdown SystemManger { get; set; }
  }
}
