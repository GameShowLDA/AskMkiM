using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using static Ask.LogLib.LoggerUtility;

namespace NewCore.Function.ModuleVoltageCurrentSource.SelfCheck
{
  static internal class VoltageCheckService
  {
    /// <summary>
    /// Проверка формирования дискрет напряжения.
    /// </summary>
    /// <param name="token">Токен для отмены операции.</param>
    static internal async Task GenerateDiscreteVoltageCheck(CancellationToken cancellationToken, IUserInteractionService messageService, IFastMeter fastMeter, IPowerSourceModule powerSource)
    {
      await messageService.ShowMessageAsync(new ShowMessageModel("Начало проверки формирования дискрет напряжения"));

      await CheckVoltageLevelsAsync(cancellationToken, messageService, 0.1, 0.9, 0.1, 20, fastMeter, powerSource);
      await CheckVoltageLevelsAsync(cancellationToken, messageService, 1, 9, 1, 20, fastMeter, powerSource);
      await CheckVoltageLevelsAsync(cancellationToken, messageService, 10, 40, 10, 20, fastMeter, powerSource);
    }


    /// <summary>
    /// Проверяет уровни напряжения по заданному диапазону и шагу.
    /// </summary>
    /// <param name="startVoltage">Начальное значение напряжения.</param>
    /// <param name="endVoltage">Конечное значение напряжения.</param>
    /// <param name="step">Шаг напряжения.</param>
    /// <param name="delay">Задержка между измерениями.</param>
    /// <param name="token">Токен для отмены операции.</param>
    static private async Task CheckVoltageLevelsAsync(CancellationToken cancellationToken, IUserInteractionService messageService, double startVoltage, double endVoltage, double step, int delay, IFastMeter fastMeter, IPowerSourceModule powerSource)
    {
      await messageService.ShowMessageAsync(new ShowMessageModel($"Проверка уровней напряжения от {startVoltage} до {endVoltage} с шагом {step}"));
      for (double voltage = startVoltage; voltage <= endVoltage; voltage += step)
      {
        await messageService.ShowMessageAsync(new ShowMessageModel($"Проверка напряжения {Math.Round(voltage, 1)}В"));
        cancellationToken.ThrowIfCancellationRequested();
        double roundedVoltage = Math.Round(voltage, 1);
        await SetVoltageAndShowMessage(messageService, roundedVoltage, powerSource);
        await MeasureAndCompareVoltage(messageService, roundedVoltage, delay, fastMeter);
      }
    }

    /// <summary>
    /// Устанавливает напряжение и отображает сообщение.
    /// </summary>
    /// <param name="voltage">Устанавливаемое напряжение.</param>
    static private async Task SetVoltageAndShowMessage(IUserInteractionService messageService, double voltage, IPowerSourceModule powerSource)
    {
      int a = (int)voltage;
      int b = (int)((voltage - a) * 10);
      await powerSource.VoltageManager.SetVoltageLevelAsync(a, b, messageService);
    }

    /// <summary>
    /// Измеряет и сравнивает напряжение.
    /// </summary>
    /// <param name="voltage">Ожидаемое напряжение.</param>
    /// <param name="delay">Задержка перед измерением.</param>
    /// <param name="token">Токен отмены.</param>
    static private async Task MeasureAndCompareVoltage(IUserInteractionService messageService, double voltage, int delay, IFastMeter fastMeter)
    {
      double firstNorm = Math.Round(voltage - ((0.01 * voltage) + 0.1), 3);
      double lastNorm = Math.Round(voltage + ((0.01 * voltage) + 0.1), 3);

      await Task.Delay(10);

      double result = await GetMeasurementResult(messageService, voltage, delay, fastMeter);
      bool error = !(result >= firstNorm && result <= lastNorm);

      var status = error ? ShowMessageModel.MessageType.Error : ShowMessageModel.MessageType.Success;
      await messageService.ShowMessageAsync(new ShowMessageModel($"Результат измерения", message: $"{result}В", type: status) { IndentLevel = 2 });
      await messageService.ShowMessageAsync(new ShowMessageModel($"Диапазон значений", message: $"от {firstNorm} до {lastNorm}В") { IndentLevel = 3 });
      await messageService.ShowMessageAsync(new ShowMessageModel($"Погрешность измерения", message: $"{Math.Abs(result - voltage)}В", type: status) { IndentLevel = 3 });

      await Task.Delay(1);
    }

    /// <summary>
    /// Получает результат измерения.
    /// </summary>
    /// <param name="voltage">Ожидаемое напряжение.</param>
    /// <param name="delay">Задержка перед измерением.</param>
    /// <param name="token">Токен отмены.</param>
    /// <returns>Результат измерения.</returns>
    static private async Task<double> GetMeasurementResult(IUserInteractionService messageService, double voltage, int delay, IFastMeter meter)
    {
      await Task.Delay(delay);
      double result = await meter.DcVoltageManager.MeasureDCVoltageAsync(voltage, messageService);
      LogInformation($"Измеренное напряжение: {result} В", isDeviceLog: true);
      return result;
    }
  }
}
