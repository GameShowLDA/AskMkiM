using Ask.Core.Shared.DTO.Devices.FastMeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums.MultimeterCommands;
using Ask.Device.Communication.Usb.Protocols;
using Ask.Device.Runtime.Base.Device;
using Ask.Device.Runtime.Function.Multimeter.Measurements;
using System.Windows.Controls;

namespace Ask.Device.Runtime.Device
{
  public class MultimeterB7783 : DeviceWithUSB, IFastMeter
  {
    public MultimeterB7783()
    {
      Name = "Мультиметр B7-78/3";
      Description = "Мультиметр В7-78/3, подключение по USB.";
      DeviceClass = GetType().FullName ?? string.Empty;
      DeviceType = DeviceType.FastMeter;
      ConnectionDetails = "VID_164E&PID_0DB7";

      ConnectableManager = new Function.B7783.StateManager(this);
      ResistanceManager = new ResistanceMeasurementBase(this);
      ContinuityManager = new ContinuityMeasurementBase(this);
      CapacitanceManager = new CapacitanceMeasurementBase(this);
      AcVoltageManager = new ACVMeasurementBase(this);
      DcVoltageManager = new DCVMeasurementBase(this);
      SelfTestManager = new Function.Multimeter.SelfCheck.SelfTestManager();
      DeviceProtocol = new UsbProtocol(this, new Function.B7783.B7783UsbCommandHandler());
      ResistanceCommands = new ResistanceMeasurementParameters()
      {
        Timeout = 2500
      };

      ACVCommands = new ACVMeasurementParameters()
      {
        Measure = "READ?",
        Timeout = 2000,
      };


      DCVCommands = new DCVMeasurementParameters()
      {
        Measure = "READ?",
        CheckMode = "VOLT"
      };

      CapacitanceCommands = new CapacitanceMeasurementParameters()
      {
        Measure = "READ?",
      };

      ContinuityCommands = new ContinuityMeasurementParameters()
      {
        Measure = "READ?",
      };


      MaxContinuityResistance = 100000;
      AcwPpuDividerCoefficientPercent = 100d;
      DcwPpuDividerCoefficientPercent = 100d;
    }

    public MultimeterTypeMode TypeMode { get; set; }

    public IAcVoltageMeasurement AcVoltageManager { get; set; } = null!;

    public ICapacitanceMeasurement CapacitanceManager { get; set; } = null!;

    public IContinuityMeasurement ContinuityManager { get; set; } = null!;

    public IDcVoltageMeasurement DcVoltageManager { get; set; } = null!;

    public IDiodeMeasurement DiodeManager { get; set; } = null!;

    public IResistanceMeasurement ResistanceManager { get; set; }

    public ITextMessage TextMessage { get; set; } = null!;

    public int MaxContinuityResistance { get; set; }

    public double AcwPpuDividerCoefficientPercent { get; set; }

    public double DcwPpuDividerCoefficientPercent { get; set; }

    public ISelfTestCheckerMultimeter SelfTestManager { get; set; }

    public int NumberChassis { get; set; }

    public bool IsConnected { get; set; }

    public string LastResolvedDevicePath { get; set; } = string.Empty;
    public ResistanceMeasurementParameters ResistanceCommands { get; set; }
    public ACVMeasurementParameters ACVCommands { get; set; }
    public DCVMeasurementParameters DCVCommands { get; set; }
    public CapacitanceMeasurementParameters CapacitanceCommands { get; set; }

    public ContinuityMeasurementParameters ContinuityCommands { get; set; }

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
  }
}
