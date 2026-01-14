using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Mode;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using NewCore.Base.Device;
using NewCore.FunctionAdapters.GPT;
using System.IO.Ports;
using static Ask.LogLib.LoggerUtility;

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

      DeviceType = DeviceType.BreakdownTester;
      PiMaxVoltage = 700;
      SiMaxVoltage = 1000;
      IRMinVoltage = 50;

      AcwManger = new AcwModeAdapter(this);
      DcwManger = new DcwModeAdapter(this);
      IrManger = new IrModeAdapter(this);
      SystemManger = new SystemSettingsAdapter(this);
      ConnectableManager = new ConnectableManagerAdapter(this);
      SelfTestManager = new Function.GPT.SelfCheck.SelfTestManager();
      LogWarning($"[{GetType().Name}] ctor вызван. Hash={GetHashCode()}", isDeviceLog: true);

      Mode = BreakdownTypeMode.None;
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

    /// <inheritdoc />
    public int PiMaxVoltage { get; set; }

    /// <inheritdoc />
    public int SiMaxVoltage { get; set; }

    /// <inheritdoc />
    public int IRMinVoltage { get; set; }

    /// <inheritdoc />
    public ISelfTestCheckerBreakdownTester SelfTestManager { get; set; }

    /// <summary>
    /// Активный режим устройства.
    /// </summary>
    public BreakdownTypeMode Mode
    {
      get => _mode;
      set
      {
        if (_mode == value)
          return;

        LogInformation($"[{GetType().Name}] Переключение режима: {_mode} → {value}", isDeviceLog: true);

        if (value != BreakdownTypeMode.ACW)
          AcwManger.Config.ResetConfiguration();
        if (value != BreakdownTypeMode.DCW)
          DcwManger.Config.ResetConfiguration();
        if (value != BreakdownTypeMode.IR)
          IrManger.Config.ResetConfiguration();

        _mode = value;
      }
    }

    private BreakdownTypeMode _mode { get; set; }
  }
}
