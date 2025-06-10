using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppConfiguration.Interface;
using NewCore.Base.Interface.Main;
using static NewCore.Enum.DeviceEnum;
using static Utilities.LoggerUtility;
using NewCore.Base.DeviceResponses;
using System.Text.Json;
using Utilities.Models;
using AppConfiguration.Enums;
using AppConfiguration.MeasurementError;

namespace NewCore.Function.ModuleVoltageCurrentSource.SelfCheck
{
  /// <summary>
  /// Сервис проверки резисторов по номерам и токам.
  /// </summary>
  static internal class ResistanceMeasurementCheckService
  {
    /// <summary>
    /// Таблица проверки: номер резистора и ток (А).
    /// </summary>
    private static readonly List<(int ResistorNumber, int resistance, int integerPart, int decimalPart)> TestPoints = new()
        {
            (1,1, 20, 0),
            (2,100, 20, 0),
            (2,100, 9, 0),
            (3,1000, 9 , 0),
            (3,1000, 1 , 0),
            (4,10000, 1 , 0),
            (4,10000, 0, 90),
            (5,100000, 0, 90)
        };

    /// <summary>
    /// Основной метод проверки.
    /// </summary>
    static internal async Task PerformResistanceCheckAsync(
        CancellationToken cancellationToken,
        IUserMessageService messageService,
        IFastMeter fastMeter,
        IPowerSourceModule powerSource,
        ISwitchingDevice relayModule)
    {
      await messageService.ShowMessageAsync(new ShowMessageModel("Начало проверки резисторов по таблице"));

      await powerSource.VoltageManager.SetSourceVoltageAsync(VoltageSources.Supply5V);
      // Подключить шины A1, B1 и питание
      await ConnectShuntAndPowerAsync(powerSource);
      await ConnectBlockingRelaysAsync(relayModule, messageService);
      await relayModule.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB1);
      bool hightVoltage = false;

      foreach (var (resistorNumber, resistance, integerPart, decimalPart) in TestPoints)
      {
        await messageService.AppendEmptyLineAsync();
        cancellationToken.ThrowIfCancellationRequested();

        await messageService.ShowMessageAsync(new ShowMessageModel(
            $"Проверка сопротивления {resistance}Ом при токе {integerPart},{decimalPart}мА"));

        double currentAmps = ConvertToAmperes(integerPart, decimalPart);

        if (!hightVoltage && integerPart < 10)
        {
          await powerSource.VoltageManager.SetSourceVoltageAsync(VoltageSources.Supply12V);
          hightVoltage = true;
        }

        await SetCurrentAsync(powerSource, integerPart, decimalPart);
        await ConnectResistorByNumberAsync(relayModule, resistorNumber);


        var error = ErrorProviderLocator.Provider.GetErrorParameters(TypeCommand.PR);

        double firstNorm = resistance - ((resistance / 100.0 * error.Percent) + error.Numeric);
        double lastNorm = resistance + ((resistance / 100.0 * error.Percent) + error.Numeric);

        var voltage = await fastMeter.DcVoltageManager.MeasureDCVoltageAsync(currentAmps);
        var result = voltage / currentAmps;

        await DisconnectResistorByNumberAsync(relayModule, resistorNumber);

        ShowMessageModel showMessageModel = new ShowMessageModel($"\tРезультат измерения сопротивления ({firstNorm:F2}-{lastNorm:F2})", 
          message: $"{result:F2}",
          type: (result >= firstNorm && result <= lastNorm) ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error
          );

        showMessageModel.ExecutionError = (result >= firstNorm && result <= lastNorm) ? false : true;
        showMessageModel.CanBeDeleted = showMessageModel.ExecutionError;
        await messageService.ShowMessageAsync(showMessageModel);
      }
      await messageService.AppendEmptyLineAsync();

    }

    static private async Task ConnectShuntAndPowerAsync(IPowerSourceModule powerSourceModule)
    {
      await powerSourceModule.BusManager.ConnectBusToPositiveAsync(SwitchingBus.A1);
      await powerSourceModule.BusManager.ConnectBusToNegativeAsync(SwitchingBus.B1);
    }

    static private async Task ConnectBlockingRelaysAsync(ISwitchingDevice relayModule, IUserMessageService messageService)
    {
      await messageService.ShowMessageAsync(new Utilities.Models.ShowMessageModel("Подключение блокировочных реле на УКШ."));
      var relays = relayModule.SelfTestManager.GetValidBusContacts(Base.Function.DBC.TypeConnector.BlockingRelay);
      foreach (var item in relays)
      {
        var result = await relayModule.RelayManager.ConnectRelay(item);
      }
    }

    static private async Task SetCurrentAsync(IPowerSourceModule powerSource, int integerPart, int decimalPart)
    {
      await powerSource.CurrentManager.SetCurrentLevelAsync(integerPart, decimalPart);
    }

    static private async Task ConnectResistorByNumberAsync(ISwitchingDevice relayModule, int resistorNumber)
    {
      await relayModule.ResistorManager.ConnectResistor(resistorNumber.ToString());
    }

    static private async Task DisconnectResistorByNumberAsync(ISwitchingDevice relayModule, int resistorNumber)
    {
      await relayModule.ResistorManager.DisconnectResistor(resistorNumber.ToString());
    }

    /// <summary>
    /// Переводит целую и дробную части в амперы.
    /// </summary>
    static private double ConvertToAmperes(int integerPart, int decimalPart)
    {
      double milliamps = integerPart + decimalPart / 100.0;
      return milliamps / 1000.0;
    }
  }
}
