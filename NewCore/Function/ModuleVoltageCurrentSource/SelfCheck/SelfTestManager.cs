using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AppConfiguration.Interface;
using NewCore.Base.Function.ModuleVoltageCurrentSource;
using NewCore.Base.Interface.Additionally;
using NewCore.Base.Interface.Main;
using NewCore.Communication;
using Utilities.Models;
using static NewCore.Enum.DeviceEnum;

namespace NewCore.Function.ModuleVoltageCurrentSource.SelfCheck
{
  public class SelfTestManager : ISelfTestCheckerModuleVoltageCurrentSource
  {

    /// <inheritdoc />
    public async Task StartSelfCheck(CancellationToken cancellationToken, IUserMessageService messageService, System.Enum selectedType, ISwitchingDevice dbc = null, IPowerSourceModule powerDevice = null, IFastMeter meter = null)
    {
      if (selectedType is not TypeConnector type)
      {
        await messageService.ShowMessageAsync(new ShowMessageModel(
          "Ошибка",
          message: "Неверный тип проверки: требуется TypeConnector",
          type: ShowMessageModel.MessageType.Error));

        return;
      }

      if (!await CheckConnectionsAsync(dbc, meter, powerDevice))
      {
        return;
      }


      switch (type)
      {

        case TypeConnector.FullCheck:
          await DeviceCommandSender.ResetAllSystem();
          await SettingsMeter(meter);
          await powerDevice.BusManager.ConnectBusToPositiveAsync(SwitchingBus.A2);
          await powerDevice.BusManager.ConnectBusToNegativeAsync(SwitchingBus.B2);
          await dbc.DeviceProtocol.QueryAsync(new DeviceCommand(5, 2, 2, 1).ToString());
          await VoltageCheckService.GenerateDiscreteVoltageCheck(cancellationToken, messageService, meter, powerDevice);

          await DeviceCommandSender.ResetAllSystem();
          await SwitchingSelfControl.CheckSwitching(cancellationToken, messageService, meter, powerDevice, dbc);

          await DeviceCommandSender.ResetAllSystem();
          await ResistanceMeasurementCheckService.PerformResistanceCheckAsync(cancellationToken, messageService, meter, powerDevice, dbc);
          break;


        case TypeConnector.OutputVoltageCheck:
          await SettingsMeter(meter);
          await powerDevice.BusManager.ConnectBusToPositiveAsync(SwitchingBus.A2);
          await powerDevice.BusManager.ConnectBusToNegativeAsync(SwitchingBus.B2);
          await dbc.DeviceProtocol.QueryAsync(new DeviceCommand(5, 2, 2, 1).ToString());
          await VoltageCheckService.GenerateDiscreteVoltageCheck(cancellationToken, messageService, meter, powerDevice);
          break;

        case TypeConnector.CommutationCheck:
          await DeviceCommandSender.ResetAllSystem();
          await SwitchingSelfControl.CheckSwitching(cancellationToken, messageService, meter, powerDevice, dbc);
          break;

        case TypeConnector.OutputCurrentCheck:
          await DeviceCommandSender.ResetAllSystem();
          await ResistanceMeasurementCheckService.PerformResistanceCheckAsync(cancellationToken, messageService, meter, powerDevice, dbc);
          break;
      }

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

    public Type GetTestTypeEnum()
    {
      return typeof(TypeConnector);
    }
  }
}
