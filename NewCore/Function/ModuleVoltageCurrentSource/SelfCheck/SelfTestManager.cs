using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfiguration.Interface;
using NewCore.Base.Interface.Additionally;
using NewCore.Base.Interface.Main;
using NewCore.Communication;
using static NewCore.Enum.DeviceEnum;

namespace NewCore.Function.ModuleVoltageCurrentSource.SelfCheck
{
  public class SelfTestManager : ISelfTestCheckerModuleVoltageCurrentSource
  {
    /// <inheritdoc />
    public async Task StartSelfCheck(IUserMessageService messageService, ISwitchingDevice dbc = null, IPowerSourceModule powerDevice = null, IFastMeter meter = null)
    {
      if (!await CheckConnectionsAsync(dbc, meter, powerDevice))
      {
        return;
      }

      await dbc.ConnectableManager.ResetAsync();
      await powerDevice.ConnectableManager.ResetAsync();

      await SettingsMeter(meter);
      await powerDevice.BusManager.ConnectBusToPositiveAsync(SwitchingBus.A2);
      await powerDevice.BusManager.ConnectBusToNegativeAsync(SwitchingBus.B2);
      await dbc.DeviceProtocol.QueryAsync(new DeviceCommand(5, 2, 2, 1).ToString());
      await VoltageCheckService.GenerateDiscreteVoltageCheck(messageService, meter, powerDevice);
      
      //await CheckMintSwitching(meter, powerDevice, dbc);

      await dbc.ConnectableManager.ResetAsync();
      await powerDevice.ConnectableManager.ResetAsync();
    }

    private static async Task<bool> CheckConnectionsAsync(ISwitchingDevice device, IFastMeter meter, IPowerSourceModule powerSource)
    {
      Console.ForegroundColor = ConsoleColor.Green;
      Console.WriteLine("Проверка подключения устройств");
      var result1 = await device.ConnectableManager.InitializeAsync();
      var result2 = await meter.ConnectableManager.InitializeAsync();
      var result3 = await powerSource.ConnectableManager.InitializeAsync();
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

    private static async Task SettingsMeter(IFastMeter meter)
    {
      await meter.ConnectableManager.ConnectAsync();
      await meter.DcVoltageManager.SetDCVoltageModeAsync();
    }
  }
}
