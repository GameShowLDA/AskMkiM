using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Device.Runtime.Function.ModuleVoltageCurrentSource.SelfCheck
{
  internal static class SwitchingSelfControl
  {
    static internal async Task CheckSwitching(CancellationToken cancellationToken, IUserInteractionService messageService, IFastMeter fastMeter, IPowerSourceModule powerSourceModule, ISwitchingDevice switchingDevice)
    {

      await messageService.ShowMessageAsync(new ShowMessageModel("Начало проверки коммутации"));
      await messageService.ShowMessageAsync(new ShowMessageModel("Настройка оборудования"));
      await powerSourceModule.VoltageManager.SetSourceVoltageAsync(VoltageSources.Supply12V, messageService);
      await powerSourceModule.VoltageManager.SetVoltageLevelAsync(5, 0, messageService);
      await Task.Delay(1000);

      var busesA = System.Enum.GetValues(typeof(SwitchingBus))
                       .Cast<SwitchingBus>()
                       .Where(bus => bus.ToString().StartsWith("A") && !bus.ToString().StartsWith("AB") && !bus.ToString().StartsWith("A1"))
                       .ToList();

      var busesB = System.Enum.GetValues(typeof(SwitchingBus))
                       .Cast<SwitchingBus>()
                       .Where(bus => bus.ToString().StartsWith("B") && !bus.ToString().StartsWith("B1"))
                       .ToList();

      await fastMeter.DcVoltageManager.SetDCVoltageModeAsync(messageService);

      foreach (var item in busesB)
      {
        cancellationToken.ThrowIfCancellationRequested();
        await powerSourceModule.BusManager.ConnectBusToNegativeAsync(item, messageService);
      }
      await Task.Delay(1000);

      foreach (var bus in busesA)
      {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(10);
        await CheckBus(messageService, bus, switchingDevice, powerSourceModule, fastMeter);
      }

      await messageService.ShowMessageAsync(new ShowMessageModel("Настройка оборудования"));
      foreach (var item in busesB)
      {
        cancellationToken.ThrowIfCancellationRequested();
        await powerSourceModule.BusManager.DisconnectBusToNegativeAsync(item, messageService);
      }

      foreach (var item in busesA)
      {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(10);
        await powerSourceModule.BusManager.ConnectBusToPositiveAsync(item, messageService);
      }
      await Task.Delay(1000);

      foreach (var bus in busesB)
      {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(10);
        await CheckBus(messageService, bus, switchingDevice, powerSourceModule, fastMeter);
      }

      await powerSourceModule.ConnectableManager.ResetAsync(messageService);
    }

    static private async Task CheckBus(IUserInteractionService messageService, SwitchingBus switchingBus, ISwitchingDevice switchingDevice, IPowerSourceModule powerSource, IFastMeter fastMeter)
    {
      var busSwitch = GetAbPair(switchingBus);
      if (busSwitch == null)
      {
        await messageService.ShowMessageAsync(new ShowMessageModel($"Не удалось определить шину AB для {switchingBus}"));
        return;
      }

      await messageService.ShowMessageAsync(new ShowMessageModel($"Проверка шины {switchingBus}"));

      var connectBus = await switchingDevice.ConnectorManager.ConnectMultimeter(busSwitch, messageService);


      if (switchingBus.ToString().StartsWith("A"))
      {
        await powerSource.BusManager.ConnectBusToPositiveAsync(switchingBus, messageService);
        await Task.Delay(100);
        var result = await fastMeter.DcVoltageManager.MeasureDCVoltageAsync(5, userMessageService: messageService);

        if (Math.Abs(result - 5.0) < 0.15)
        {
          await messageService.ShowMessageAsync(new ShowMessageModel($"Напряжение {result}", type: ShowMessageModel.MessageType.Success) { IndentLevel = 2 });
        }
        else
        {
          await messageService.ShowMessageAsync(new ShowMessageModel($"Напряжение {result}", type: ShowMessageModel.MessageType.Error) { IndentLevel = 2 });
        }
        await powerSource.BusManager.DisconnectBusToPositiveAsync(switchingBus, messageService);
      }
      else if (switchingBus.ToString().StartsWith("B"))
      {
        await powerSource.BusManager.ConnectBusToNegativeAsync(switchingBus, messageService);
        var result = await fastMeter.DcVoltageManager.MeasureDCVoltageAsync(5, userMessageService: messageService);

        if (Math.Abs(result - 5.0) < 0.15)
        {
          await messageService.ShowMessageAsync(new ShowMessageModel($"Напряжение {result}", type: ShowMessageModel.MessageType.Success) { IndentLevel = 2 });
        }
        else
        {
          await messageService.ShowMessageAsync(new ShowMessageModel($"Напряжение {result}", type: ShowMessageModel.MessageType.Error) { IndentLevel = 2 });
        }
        await powerSource.BusManager.DisconnectBusToNegativeAsync(switchingBus, messageService);
      }

      await switchingDevice.ConnectorManager.DisconnectMultimeter(busSwitch, messageService);
    }


    private static SwitchingBusNew GetAbPair(SwitchingBus bus)
    {
      if (bus.ToString().StartsWith("A") || bus.ToString().StartsWith("B"))
      {
        var index = bus.ToString().Substring(1); // Например: "1", "2", "3", "4"

        if (System.Enum.TryParse($"AB{index}", out SwitchingBusNew abBus))
          return abBus;
      }

      throw new Exception("Не удалось разобрать шины"); // Если не удалось сопоставить
    }
  }
}
