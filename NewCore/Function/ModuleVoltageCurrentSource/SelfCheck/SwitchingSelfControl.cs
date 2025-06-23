using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfiguration.Interface;
using NewCore.Base.Interface.Main;
using Utilities.Models;
using static NewCore.Enum.DeviceEnum;
using static Utilities.LoggerUtility;

namespace NewCore.Function.ModuleVoltageCurrentSource.SelfCheck
{
  internal static class SwitchingSelfControl
  {
    static internal async Task CheckSwitching(CancellationToken cancellationToken, IUserMessageService messageService, IFastMeter fastMeter, IPowerSourceModule powerSourceModule, ISwitchingDevice switchingDevice)
    {

      await messageService.ShowMessageAsync(new Utilities.Models.ShowMessageModel("Начало проверки коммутации"));
      await messageService.ShowMessageAsync(new Utilities.Models.ShowMessageModel("Настройка оборудования"));
      await powerSourceModule.VoltageManager.SetSourceVoltageAsync(VoltageSources.Supply12V);
      await powerSourceModule.VoltageManager.SetVoltageLevelAsync(5, 0);
      await Task.Delay(1000);

      var busesA = System.Enum.GetValues(typeof(SwitchingBus))
                       .Cast<SwitchingBus>()
                       .Where(bus => bus.ToString().StartsWith("A") && !bus.ToString().StartsWith("AB") && !bus.ToString().StartsWith("A1"))
                       .ToList();

      var busesB = System.Enum.GetValues(typeof(SwitchingBus))
                       .Cast<SwitchingBus>()
                       .Where(bus => bus.ToString().StartsWith("B") && !bus.ToString().StartsWith("B1"))
                       .ToList();

      await fastMeter.DcVoltageManager.SetDCVoltageModeAsync();

      foreach (var item in busesB)
      {
        cancellationToken.ThrowIfCancellationRequested();
        await powerSourceModule.BusManager.ConnectBusToNegativeAsync(item);
      }
      await Task.Delay(1000);

      foreach (var bus in busesA)
      {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(10);
        await CheckBus(messageService, bus, switchingDevice, powerSourceModule, fastMeter);
      }

      await messageService.ShowMessageAsync(new Utilities.Models.ShowMessageModel("Настройка оборудования"));
      foreach (var item in busesB)
      {
        cancellationToken.ThrowIfCancellationRequested();
        await powerSourceModule.BusManager.DisconnectBusToNegativeAsync(item);
      }

      foreach (var item in busesA)
      {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(10);
        await powerSourceModule.BusManager.ConnectBusToPositiveAsync(item);
      }
      await Task.Delay(1000);

      foreach (var bus in busesB)
      {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(10);
        await CheckBus(messageService, bus, switchingDevice, powerSourceModule, fastMeter);
      }

      await powerSourceModule.ConnectableManager.ResetAsync();
    }

    static private async Task CheckBus(IUserMessageService messageService, SwitchingBus switchingBus, ISwitchingDevice switchingDevice, IPowerSourceModule powerSource, IFastMeter fastMeter)
    {
      var busSwitch = GetAbPair(switchingBus);
      if (busSwitch == null)
      {
        await messageService.ShowMessageAsync(new ShowMessageModel($"Не удалось определить шину AB для {switchingBus}"));
        return;
      }

      await messageService.ShowMessageAsync(new ShowMessageModel($"Проверка шины {switchingBus}"));

      var connectBus = await switchingDevice.ConnectorManager.ConnectMultimeter(busSwitch);


      if (switchingBus.ToString().StartsWith("A"))
      {
        await powerSource.BusManager.ConnectBusToPositiveAsync(switchingBus);
        await Task.Delay(100);
        var result = await fastMeter.DcVoltageManager.MeasureDCVoltageAsync(5);

        if (Math.Abs(result - 5.0) < 0.15)
        {
          await messageService.ShowMessageAsync(new ShowMessageModel($"Напряжение {result}", type: ShowMessageModel.MessageType.Success) { IndentLevel = 2 });
        }
        else
        {
          await messageService.ShowMessageAsync(new ShowMessageModel($"Напряжение {result}", type: ShowMessageModel.MessageType.Error) { IndentLevel = 2 });
        }
        await powerSource.BusManager.DisconnectBusToPositiveAsync(switchingBus);
      }
      else if (switchingBus.ToString().StartsWith("B"))
      {
        await powerSource.BusManager.ConnectBusToNegativeAsync(switchingBus);
        var result = await fastMeter.DcVoltageManager.MeasureDCVoltageAsync(5);

        if (Math.Abs(result - 5.0) < 0.15)
        {
          await messageService.ShowMessageAsync(new ShowMessageModel($"Напряжение {result}", type: ShowMessageModel.MessageType.Success) { IndentLevel = 2 });
        }
        else
        {
          await messageService.ShowMessageAsync(new ShowMessageModel($"Напряжение {result}", type: ShowMessageModel.MessageType.Error) { IndentLevel = 2 });
        }
        await powerSource.BusManager.DisconnectBusToNegativeAsync(switchingBus);
      }

      await switchingDevice.ConnectorManager.DisconnectMultimeter(busSwitch);
    }


    static SwitchingBusNew GetAbPair(SwitchingBus bus)
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
