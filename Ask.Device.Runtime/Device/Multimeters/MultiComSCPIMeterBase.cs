using Ask.Core.Shared.DTO.Devices.FastMeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Metadata.Commands.MultimeterCommands;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.AskMkiM.Function.Base;
using Ask.Device.Runtime.Base.Connected;
using Ask.Device.Runtime.Base.DeviceProtocol;
using Ask.Device.Runtime.Base.Multimeter.Measurements;
using Ask.Device.Runtime.Base.Multimeter.SelfCheck;

namespace Ask.Device.Runtime.Device.Multimeters;

/// <summary>
/// Базовая реализация COM-мультиметра, работающего по SCPI-командам.
/// </summary>
public abstract class MultiComSCPIMeterBase : DeviceWithCOM, IMultimeter
{
  /// <summary>
  /// Инициализирует общие менеджеры COM-мультиметра.
  /// </summary>
  protected MultiComSCPIMeterBase()
  {
    DeviceType = DeviceType.FastMeter;
    DeviceClass = GetType().FullName ?? string.Empty;

    ConnectableManager = new Transport(this);
    ResistanceManager = new ResistanceMeasurementBase(this);
    ContinuityManager = new ContinuityMeasurementBase(this);
    CapacitanceManager = new CapacitanceMeasurementBase(this);
    AcVoltageManager = new ACVMeasurementBase(this);
    DcVoltageManager = new DCVMeasurementBase(this);
    DiodeManager = new DiodeMeasurementBase(this);
    TextMessage = new EmptyTextMessage();
    SelfTestManager = new SelfTestManager();

    MaxContinuityResistance = 100000;
    AcwPpuDividerCoefficientPercent = 100d;
    DcwPpuDividerCoefficientPercent = 100d;
  }

  /// <inheritdoc />
  public MultimeterTypeMode TypeMode { get; set; }

  /// <inheritdoc />
  public IAcVoltageMeasurement AcVoltageManager { get; set; }

  /// <inheritdoc />
  public ICapacitanceMeasurement CapacitanceManager { get; set; }

  /// <inheritdoc />
  public IContinuityMeasurement ContinuityManager { get; set; }

  /// <inheritdoc />
  public IDcVoltageMeasurement DcVoltageManager { get; set; }

  /// <inheritdoc />
  public IDiodeMeasurement DiodeManager { get; set; }

  /// <inheritdoc />
  public IResistanceMeasurement ResistanceManager { get; set; }

  /// <inheritdoc />
  public ITextMessage TextMessage { get; set; }

  /// <inheritdoc />
  public int MaxContinuityResistance { get; set; }

  /// <inheritdoc />
  public double AcwPpuDividerCoefficientPercent { get; set; }

  /// <inheritdoc />
  public double DcwPpuDividerCoefficientPercent { get; set; }

  /// <inheritdoc />
  public ISelfTestCheckerMultimeter SelfTestManager { get; set; }

  /// <inheritdoc />
  public int NumberChassis { get; set; }

  /// <inheritdoc />
  public ResistanceMeasurementProfile ResistanceCommands { get; set; } = new();

  /// <inheritdoc />
  public ACVMeasurementProfile ACVCommands { get; set; } = new()
  {
    GetRangeError = "SYST:ERR?",
    SupportedRanges = [0.1d, 1d, 10d, 100d, 750d],
    Timeout = 2500
  };

  /// <inheritdoc />
  public DCVMeasurementProfile DCVCommands { get; set; } = new()
  {
    GetRangeError = "SYST:ERR?",
    SupportedRanges = [0.1d, 1d, 10d, 100d, 1000d],
    Timeout = 2500
  };

  /// <inheritdoc />
  public CapacitanceMeasurementProfile CapacitanceCommands { get; set; } = new()
  {
    Timeout = 7000
  };

  /// <inheritdoc />
  public ContinuityMeasurementProfile ContinuityCommands { get; set; } = new();

  /// <inheritdoc />
  public DiodeMeasurementProfile DiodeCommands { get; set; } = new();

  /// <summary>
  /// Заполняет имя и описание COM-мультиметра.
  /// </summary>
  /// <param name="name">Отображаемое имя устройства.</param>
  /// <param name="description">Описание устройства.</param>
  protected void ConfigureComMeter(string name, string description)
  {
    Name = name;
    Description = description;
  }

  /// <summary>
  /// Настраивает команды SCPI, которые старая система использовала для Agilent-совместимых COM-мультиметров.
  /// </summary>
  /// <param name="supportsCapacitance">Признак поддержки измерения емкости.</param>
  protected void ConfigureAgilentComCommands(bool supportsCapacitance)
  {
    ConnectedProfile.Initialize = "*IDN?";
    ConnectedProfile.CheckMode = string.Empty;
    ConnectedProfile.Reset = "*RST";
    ConnectedProfile.Clear = "*CLS";
    ConnectedProfile.Timeout = 2500;

    ResistanceCommands = new ResistanceMeasurementProfile
    {
      Measure = "READ?",
      Timeout = 2500
    };

    ACVCommands = new ACVMeasurementProfile
    {
      Measure = "READ?",
      SetRange = "CONF:VOLT:AC {0},{1}",
      GetRangeError = "SYSTEM:ERROR?",
      SupportedRanges = [0.1d, 1d, 10d, 100d, 750d],
      Timeout = 2500
    };

    DCVCommands = new DCVMeasurementProfile
    {
      Measure = "READ?",
      SetRange = "CONF:VOLT:DC {0},{1}",
      GetRangeError = "SYSTEM:ERROR?",
      SupportedRanges = [0.1d, 1d, 10d, 100d, 1000d],
      Timeout = 2500
    };

    CapacitanceCommands = new CapacitanceMeasurementProfile
    {
      Measure = "READ?",
      Timeout = supportsCapacitance ? 7000 : 1000
    };

    ContinuityCommands = new ContinuityMeasurementProfile
    {
      Measure = "READ?",
      Timeout = 2500
    };

    DiodeCommands = new DiodeMeasurementProfile
    {
      Measure = "READ?",
      Timeout = 2500
    };
  }

  /// <inheritdoc />
  public FastMeterDto Convert()
  {
    return new FastMeterDto
    {
      Id = Id,
      NumberChassis = NumberChassis,
      Name = Name ?? string.Empty,
      Description = Description ?? string.Empty,
      Number = Number,
      ConnectionDetails = ConnectionDetails ?? string.Empty,
      DeviceType = DeviceType,
      DeviceClass = DeviceClass ?? string.Empty,
      TypeMode = TypeMode,
      MaxContinuityResistance = MaxContinuityResistance,
      AcwPpuDividerCoefficientPercent = AcwPpuDividerCoefficientPercent,
      DcwPpuDividerCoefficientPercent = DcwPpuDividerCoefficientPercent
    };
  }

  /// <summary>
  /// Заглушка текстовых сообщений для мультиметров без управления дисплеем.
  /// </summary>
  private sealed class EmptyTextMessage : ITextMessage
  {
    /// <inheritdoc />
    public Task Message(string text, Core.Shared.Interfaces.UiInterfaces.IUserInteractionService? userMessageService = null)
    {
      return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ClearMessage(Core.Shared.Interfaces.UiInterfaces.IUserInteractionService? userMessageService = null)
    {
      return Task.CompletedTask;
    }
  }
}
