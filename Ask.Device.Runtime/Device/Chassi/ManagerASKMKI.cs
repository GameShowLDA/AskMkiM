using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Services.Config.LegacyMki;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Device.Runtime.AskMkiM.Function.ManagerChassis;
using Ask.Device.Runtime.Base.Connected;
using Ask.Device.Runtime.Base.DeviceProtocol;
using Ask.Device.Runtime.Device.ASKMKI;
using Ask.Engine.Tests.SelfControl.LegacyAskProtocol;
using System.IO.Ports;
using Ask.Core.Shared.DTO.Devices.Base;

namespace Ask.Device.Runtime.Device.Chassi;

/// <summary>
/// Контроллер СКУ старого тестера АСК.
/// </summary>
public sealed class ManagerASKMKI : DeviceWithCOM, IChassisManager, IAskMkiController
{
  public ManagerASKMKI()
  {
    ConnectableManager = new Transport(this);
    PowerManager = new PowerManager(this);
    DeviceType = DeviceType.ChassisManager;

    Name = "Тестер АСК";
    Description = "Стойка тестера АСК";
    DeviceClass = GetType().FullName ?? string.Empty;
    BusType = BusStructureEnum.Type.Bus2;
    NetworkAddress = LegacyAskDeviceAddress.Controller;
  }

  public IPower PowerManager { get; set; }

  public BusStructureEnum.Type BusType { get; set; }

  public bool IsIdleMode { get; set; }

  public bool UseNetworkProtocol { get; set; }

  public byte NetworkAddress { get; set; }

  public LegacyMkiHardwareProfile? LegacyProfile { get; set; }

  public override ComPortSettings DefaultComPortSettings => throw new NotImplementedException();

  public Task<ushort> ReadRegisterAsync(LegacyAskRegister register, CancellationToken cancellationToken = default)
  {
    return ExecuteAsync(controller => controller.ReadRegisterAsync(register, cancellationToken));
  }

  public Task WriteRegisterAsync(LegacyAskRegister register, ushort value, CancellationToken cancellationToken = default)
  {
    return ExecuteAsync(controller => controller.WriteRegisterAsync(register, value, cancellationToken));
  }

  public Task WriteSubRegisterAsync(LegacyAskRegister register, byte subRegister, ushort value, CancellationToken cancellationToken = default)
  {
    return ExecuteAsync(controller => controller.WriteSubRegisterAsync(register, subRegister, value, cancellationToken));
  }

  public Task<ushort> ReadAdcAsync(CancellationToken cancellationToken = default)
  {
    return ExecuteAsync(controller => controller.ReadAdcAsync(cancellationToken));
  }

  public Task WriteCommandRegisterAsync(ushort value, CancellationToken cancellationToken = default)
  {
    return ExecuteAsync(controller => controller.WriteCommandRegisterAsync(value, cancellationToken));
  }

  public Task WriteBusCommandAsync(ushort value, CancellationToken cancellationToken = default)
  {
    return ExecuteAsync(controller => controller.WriteBusCommandAsync(value, cancellationToken));
  }

  public Task CheckElectronicConnectionAsync(ushort pointAddress, CancellationToken cancellationToken = default)
  {
    return ExecuteAsync(controller => controller.CheckElectronicConnectionAsync(pointAddress, cancellationToken));
  }

  public Task CheckElectronicDisconnectionAsync(ushort pointAddress, CancellationToken cancellationToken = default)
  {
    return ExecuteAsync(controller => controller.CheckElectronicDisconnectionAsync(pointAddress, cancellationToken));
  }

  public Task CheckNoElectronicConnectionAsync(ushort pointAddress, CancellationToken cancellationToken = default)
  {
    return ExecuteAsync(controller => controller.CheckNoElectronicConnectionAsync(pointAddress, cancellationToken));
  }

  public Task SetTimerStopAsync(ushort value, CancellationToken cancellationToken = default)
  {
    return ExecuteAsync(controller => controller.SetTimerStopAsync(value, cancellationToken));
  }

  public Task StartTimerAsync(ushort value, CancellationToken cancellationToken = default)
  {
    return ExecuteAsync(controller => controller.StartTimerAsync(value, cancellationToken));
  }

  public Task<ushort> ReadTimerReadyAsync(ushort stopFlag, CancellationToken cancellationToken = default)
  {
    return ExecuteAsync(controller => controller.ReadTimerReadyAsync(stopFlag, cancellationToken));
  }

  public Task<ushort> ReadTimerWordAsync(ushort offset, CancellationToken cancellationToken = default)
  {
    return ExecuteAsync(controller => controller.ReadTimerWordAsync(offset, cancellationToken));
  }

  public Task WriteStrobeCountAsync(ushort count, CancellationToken cancellationToken = default)
  {
    return ExecuteAsync(controller => controller.WriteStrobeCountAsync(count, cancellationToken));
  }

  public Task SetStrobeAsync(ushort pointAddress, byte parameter, CancellationToken cancellationToken = default)
  {
    return ExecuteAsync(controller => controller.SetStrobeAsync(pointAddress, parameter, cancellationToken));
  }

  public ChassisManagerDto Convert()
  {
    return new ChassisManagerDto
    {
      Id = Id,
      Name = Name ?? string.Empty,
      Description = Description ?? string.Empty,
      Number = Number,
      ConnectionDetails = ConnectionDetails ?? string.Empty,
      DeviceType = DeviceType,
      DeviceClass = DeviceClass ?? string.Empty,
      BusType = BusType
    };
  }

  private async Task ExecuteAsync(Func<LegacyAskControllerProtocol, Task> action)
  {
    using var controller = CreateController();
    await action(controller).ConfigureAwait(false);
  }

  private async Task<T> ExecuteAsync<T>(Func<LegacyAskControllerProtocol, Task<T>> action)
  {
    using var controller = CreateController();
    return await action(controller).ConfigureAwait(false);
  }

  private LegacyAskControllerProtocol CreateController()
  {
    if (LegacyProfile != null)
    {
      var legacyOptions = LegacyAskControllerProtocol.CreateOptions(
        LegacyProfile,
        IsIdleMode,
        NetworkAddress == 0 ? LegacyAskDeviceAddress.Controller : NetworkAddress);

      return new LegacyAskControllerProtocol(legacyOptions);
    }

    var port = COMPort;
    string portName = string.IsNullOrWhiteSpace(port?.PortName) ? "COM1" : port.PortName;

    if (!IsIdleMode && string.IsNullOrWhiteSpace(port?.PortName))
    {
      throw new InvalidOperationException("Для контроллера СКУ АСК не настроен COM-порт.");
    }

    var options = new LegacyAskControllerProtocolOptions(
      PortName: portName,
      BaudRate: port?.BaudRate > 0 ? port.BaudRate : 9600,
      Parity: port?.Parity ?? Parity.None,
      DataBits: port?.DataBits > 0 ? port.DataBits : 8,
      StopBits: port?.StopBits is StopBits.None ? StopBits.One : port?.StopBits ?? StopBits.One,
      TimeoutMs: port?.ReadTimeout > 0 ? port.ReadTimeout : 1000,
      UseNetworkProtocol: UseNetworkProtocol,
      NetworkAddress: NetworkAddress == 0 ? LegacyAskDeviceAddress.Controller : NetworkAddress,
      RtsEnable: port?.RtsEnable ?? true,
      DtrEnable: port?.DtrEnable ?? true,
      IsIdleMode: IsIdleMode);

    return new LegacyAskControllerProtocol(options);
  }
}

