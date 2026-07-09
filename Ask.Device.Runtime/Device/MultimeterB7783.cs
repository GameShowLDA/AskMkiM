using Ask.Core.Shared.DTO.Devices.FastMeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums.MultimeterCommands;
using Ask.Device.Communication.Usb.Protocols;
using Ask.Device.Runtime.Base.Device;
using Ask.Device.Runtime.Function.Connected;
using Ask.Device.Runtime.Function.Multimeter.Measurements;
using System.Windows.Controls;

namespace Ask.Device.Runtime.Device
{
  public class MultimeterB7783 : DeviceWithUSB, IMultimeter
  {
    public MultimeterB7783()
    {
      Name = "АКИП B7-78/3";
      Description = "Мультиметр В7-78/3, подключение по USB.";
      DeviceClass = GetType().FullName ?? string.Empty;
      DeviceType = DeviceType.FastMeter;
      ConnectionDetails = "VID_164E&PID_0DB7";


      ConnectableManager = new Transport(this);
      ResistanceManager = new ResistanceMeasurementBase(this);
      ContinuityManager = new ContinuityMeasurementBase(this);
      CapacitanceManager = new CapacitanceMeasurementBase(this);
      AcVoltageManager = new ACVMeasurementBase(this);
      DcVoltageManager = new DCVMeasurementBase(this);
      DiodeManager = new DiodeMeasurementBase(this);
      SelfTestManager = new Function.Multimeter.SelfCheck.SelfTestManager();
      DeviceProtocol = new UsbProtocol(this, new Function.B7783.B7783UsbCommandHandler());
      ResistanceCommands = new ResistanceMeasurementProfile()
      {
        Timeout = 2500
      };

      ACVCommands = new ACVMeasurementProfile()
      {
        Measure = "READ?",
        Timeout = 2000,
      };


      DCVCommands = new DCVMeasurementProfile()
      {
        Measure = "READ?",
      };

      CapacitanceCommands = new CapacitanceMeasurementProfile()
      {
        Measure = "READ?",
      };

      ContinuityCommands = new ContinuityMeasurementProfile()
      {
        Measure = "READ?",
      };

      DiodeCommands = new DiodeMeasurementProfile()
      {
        Measure = "READ?",
      };


      MaxContinuityResistance = 100000;
      AcwPpuDividerCoefficientPercent = 48d;
      DcwPpuDividerCoefficientPercent = 8d;
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
    public string LastResolvedDevicePath { get; set; } = string.Empty;
    public ResistanceMeasurementProfile ResistanceCommands { get; set; }
    public ACVMeasurementProfile ACVCommands { get; set; }
    public DCVMeasurementProfile DCVCommands { get; set; }
    public CapacitanceMeasurementProfile CapacitanceCommands { get; set; }

    public ContinuityMeasurementProfile ContinuityCommands { get; set; }

    public DiodeMeasurementProfile DiodeCommands { get; set; }


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
