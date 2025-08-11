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
using Utilities.Interface;

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
    /// Основной метод проверки резисторов по таблице TestPoints.
    /// Настраивает оборудование, подключает необходимые шины и реле, выполняет измерения и сравнивает результат с нормой.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <param name="messageService">Сервис отображения сообщений пользователю.</param>
    /// <param name="fastMeter">Быстрый измеритель для снятия показаний.</param>
    /// <param name="powerSource">Модуль источника питания и тока.</param>
    /// <param name="relayModule">Модуль коммутации (реле, резисторы и т.д.).</param>
    static internal async Task PerformResistanceCheckAsync(
        CancellationToken cancellationToken,
        IUserMessageService messageService,
        IFastMeter fastMeter,
        IPowerSourceModule powerSource,
        ISwitchingDevice relayModule)
    {
      await messageService.ShowMessageAsync(new ShowMessageModel("Начало проверки резисторов по таблице"));
      await messageService.ShowMessageAsync(new ShowMessageModel("Настройка оборудования"));

      await powerSource.VoltageManager.SetSourceVoltageAsync(VoltageSources.Supply5V);
      // Подключить шины A1, B1 и питание
      await ConnectShuntAndPowerAsync(powerSource);
      await ConnectBlockingRelaysAsync(relayModule, messageService);
      await relayModule.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB1);
      bool hightVoltage = false;

      foreach (var (resistorNumber, resistance, integerPart, decimalPart) in TestPoints)
      {
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

        var voltage = await fastMeter.DcVoltageManager.MeasureDCVoltageAsync();
        double result = resistance;
        
        if (!await AppConfiguration.Execution.ExecutionConfig.GetIsIdleModeEnabled())
        {
          result = voltage / currentAmps;
        }

        ShowMessageModel showMessageModel = new ShowMessageModel($"\tРезультат измерения сопротивления ({firstNorm:F2}-{lastNorm:F2})", 
          message: $"{result:F2}",
          type: (result >= firstNorm && result <= lastNorm) ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error
          );

        showMessageModel.ExecutionError = (result >= firstNorm && result <= lastNorm) ? false : true;
        showMessageModel.CanBeDeleted = showMessageModel.ExecutionError;
        await messageService.ShowMessageAsync(showMessageModel);

        await DisconnectResistorByNumberAsync(relayModule, resistorNumber);
      }

    }

    /// <summary>
    /// Подключает шунтирующие шины A1 и B1 к положительной и отрицательной полярности соответственно.
    /// </summary>
    /// <param name="powerSourceModule">Модуль источника питания и тока.</param>
    static private async Task ConnectShuntAndPowerAsync(IPowerSourceModule powerSourceModule)
    {
      await powerSourceModule.BusManager.ConnectBusToPositiveAsync(SwitchingBus.A1);
      await powerSourceModule.BusManager.ConnectBusToNegativeAsync(SwitchingBus.B1);
    }

    /// <summary>
    /// Подключает блокировочные реле на УКШ.
    /// </summary>
    /// <param name="relayModule">Модуль коммутации (реле, резисторы и т.д.).</param>
    /// <param name="messageService">Сервис отображения сообщений пользователю.</param>
    static private async Task ConnectBlockingRelaysAsync(ISwitchingDevice relayModule, IUserMessageService messageService)
    {
      await messageService.ShowMessageAsync(new Utilities.Models.ShowMessageModel("Подключение блокировочных реле на УКШ."));
      var relays = relayModule.SelfTestManager.GetValidBusContacts(Base.Function.DBC.TypeConnector.BlockingRelay);
      foreach (var item in relays)
      {
        var result = await relayModule.RelayManager.ConnectRelay(item);
      }
    }

    /// <summary>
    /// Устанавливает дискретное значение тока на модуле источника питания и тока.
    /// </summary>
    /// <param name="powerSource">Модуль источника питания и тока.</param>
    /// <param name="integerPart">Целая часть значения тока (мА).</param>
    /// <param name="decimalPart">Дробная часть значения тока (мА).</param>
    static private async Task SetCurrentAsync(IPowerSourceModule powerSource, int integerPart, int decimalPart)
    {
      await powerSource.CurrentManager.SetCurrentLevelAsync(integerPart, decimalPart);
    }

    /// <summary>
    /// Подключает резистор по его номеру через модуль коммутации.
    /// </summary>
    /// <param name="relayModule">Модуль коммутации (реле, резисторы и т.д.).</param>
    /// <param name="resistorNumber">Номер резистора для подключения.</param>
    static private async Task ConnectResistorByNumberAsync(ISwitchingDevice relayModule, int resistorNumber)
    {
      await relayModule.ResistorManager.ConnectResistor(resistorNumber.ToString());
    }

    /// <summary>
    /// Отключает резистор по его номеру через модуль коммутации.
    /// </summary>
    /// <param name="relayModule">Модуль коммутации (реле, резисторы и т.д.).</param>
    /// <param name="resistorNumber">Номер резистора для отключения.</param>
    static private async Task DisconnectResistorByNumberAsync(ISwitchingDevice relayModule, int resistorNumber)
    {
      await relayModule.ResistorManager.DisconnectResistor(resistorNumber.ToString());
    }

    /// <summary>
    /// Переводит целую и дробную части значения тока в амперы.
    /// </summary>
    /// <param name="integerPart">Целая часть значения тока (мА).</param>
    /// <param name="decimalPart">Дробная часть значения тока (мА).</param>
    /// <returns>Значение тока в амперах.</returns>
    static private double ConvertToAmperes(int integerPart, int decimalPart)
    {
      double milliamps = integerPart + decimalPart / 100.0;
      return milliamps / 1000.0;
    }
  }
}
