using Ask.Core.Services.Devices;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Device.Runtime.Commands;

namespace Ask.Device.Runtime.Function.ModuleVoltageCurrentSource.SelfCheck
{
  public class SelfTestManager : ISelfTestCheckerModuleVoltageCurrentSource
  {

    /// <inheritdoc />
    public async Task StartSelfCheck(CancellationToken cancellationToken, IUserInteractionService messageService, ActionSettings settings, System.Enum selectedType, ISwitchingDevice dbc = null, IPowerSourceModule powerDevice = null, IMultimeter meter = null)
    {
      if (selectedType is not PowerSourceModuleTypeConnector type)
      {
        await messageService.ShowMessageAsync(new ShowMessageModel(
          "Ошибка",
          message: "Неверный тип проверки: требуется TypeConnector",
          type: ShowMessageModel.MessageType.Error));

        return;
      }

      if (!await CheckConnectionsAsync(messageService, dbc, meter, powerDevice))
      {
        return;
      }
      var deviceTitle = ExecutorMessageBuilder.BuildDeviceHealthCheckTitle(powerDevice);
      settings.DeviceResults.Add(new DeviceExecutionResult
      {
        DeviceName = $"{deviceTitle.Header} {deviceTitle.Message}"
      });
      await messageService.ShowMessageAsync(deviceTitle);

      switch (type)
      {

        case PowerSourceModuleTypeConnector.FullCheck:
          await ResetUsedDevicesAsync(dbc, powerDevice, meter, messageService, cancellationToken);
          await SettingsMeter(meter, messageService);
          await powerDevice.BusManager.ConnectBusToPositiveAsync(SwitchingBus.A2, messageService);
          await powerDevice.BusManager.ConnectBusToNegativeAsync(SwitchingBus.B2, messageService);
          await dbc.DeviceProtocol.QueryAsync(new DeviceCommand(5, 2, 2, 1).ToString());
          await VoltageCheckService.GenerateDiscreteVoltageCheck(cancellationToken, messageService, meter, powerDevice);

          await ResetUsedDevicesAsync(dbc, powerDevice, meter, messageService, cancellationToken);
          await SwitchingSelfControl.CheckSwitching(cancellationToken, messageService, meter, powerDevice, dbc);

          await ResetUsedDevicesAsync(dbc, powerDevice, meter, messageService, cancellationToken);
          await ResistanceMeasurementCheckService.PerformResistanceCheckAsync(cancellationToken, messageService, meter, powerDevice, dbc);
          break;

        case PowerSourceModuleTypeConnector.OutputVoltageCheck:
          await SettingsMeter(meter, messageService);
          await powerDevice.BusManager.ConnectBusToPositiveAsync(SwitchingBus.A2, messageService);
          await powerDevice.BusManager.ConnectBusToNegativeAsync(SwitchingBus.B2, messageService);
          await dbc.DeviceProtocol.QueryAsync(new DeviceCommand(5, 2, 2, 1).ToString());
          await VoltageCheckService.GenerateDiscreteVoltageCheck(cancellationToken, messageService, meter, powerDevice);
          break;

        case PowerSourceModuleTypeConnector.CommutationCheck:
          await ResetUsedDevicesAsync(dbc, powerDevice, meter, messageService, cancellationToken);
          await SwitchingSelfControl.CheckSwitching(cancellationToken, messageService, meter, powerDevice, dbc);
          break;

        case PowerSourceModuleTypeConnector.OutputCurrentCheck:
          await ResetUsedDevicesAsync(dbc, powerDevice, meter, messageService, cancellationToken);
          await ResistanceMeasurementCheckService.PerformResistanceCheckAsync(cancellationToken, messageService, meter, powerDevice, dbc);
          break;
      }

    }

    private static Task ResetUsedDevicesAsync(
      ISwitchingDevice dbc,
      IPowerSourceModule powerDevice,
      IMultimeter meter,
      IUserInteractionService messageService,
      CancellationToken cancellationToken)
    {
      return DeviceResetService.ResetDevicesAsync(
        [dbc, powerDevice, meter],
        messageService,
        cancellationToken);
    }

    private static async Task<bool> CheckConnectionsAsync(IUserInteractionService messageService, ISwitchingDevice device, IMultimeter meter, IPowerSourceModule powerSource)
    {
      Console.ForegroundColor = ConsoleColor.Green;
      Console.WriteLine("Проверка подключения устройств");
      var result1 = await device.ConnectableManager.InitializeAsync(messageService);
      var result2 = await meter.ConnectableManager.InitializeAsync(messageService);
      var result3 = await powerSource.ConnectableManager.InitializeAsync(messageService);
      Console.ForegroundColor = ConsoleColor.White;

      if (result1.Connect && result2.Connect && result3.Connect)
      {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Оба устройства подключены");
        return true;
      }
      if (!result1.Connect)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("УКШ не подключено");
        Console.ForegroundColor = ConsoleColor.White;
      }
      if (!result2.Connect)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Мультиметр не подключен");
        Console.ForegroundColor = ConsoleColor.White;
      }
      if (!result3.Connect)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("МИНТ не подключен");
        Console.ForegroundColor = ConsoleColor.White;
      }
      Console.ForegroundColor = ConsoleColor.White;
      return false;
    }

    private static async Task SettingsMeter(IMultimeter meter, IUserInteractionService messageService)
    {
      await meter.ConnectableManager.ConnectAsync(messageService);
      await meter.DcVoltageManager.SetDCVoltageModeAsync(messageService);
    }

    public Type GetTestTypeEnum()
    {
      return typeof(PowerSourceModuleTypeConnector);
    }
  }
}
