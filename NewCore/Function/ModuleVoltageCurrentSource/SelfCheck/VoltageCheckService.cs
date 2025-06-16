using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfiguration.Interface;
using NewCore.Base.Interface.Main;
using static Utilities.LoggerUtility;

namespace NewCore.Function.ModuleVoltageCurrentSource.SelfCheck
{
  static internal class VoltageCheckService
  {
    /// <summary>
    /// Проверка формирования дискрет напряжения.
    /// </summary>
    /// <param name="token">Токен для отмены операции.</param>
    static internal async Task GenerateDiscreteVoltageCheck(CancellationToken cancellationToken, IUserMessageService messageService, IFastMeter fastMeter, IPowerSourceModule powerSource)
    {
      await messageService.ShowMessageAsync(new Utilities.Models.ShowMessageModel("Начало проверки формирования дискрет напряжения"));

      await CheckVoltageLevelsAsync(cancellationToken, messageService, 0.1, 0.9, 0.1, 20, fastMeter, powerSource);
      await CheckVoltageLevelsAsync(cancellationToken, messageService, 1, 9, 1, 20, fastMeter, powerSource);
      await CheckVoltageLevelsAsync(cancellationToken, messageService, 10, 40, 10, 20, fastMeter, powerSource);

      await messageService.ShowMessageAsync(new Utilities.Models.ShowMessageModel("Завершение проверки формирования дискрет напряжения"));
    }


    /// <summary>
    /// Проверяет уровни напряжения по заданному диапазону и шагу.
    /// </summary>
    /// <param name="startVoltage">Начальное значение напряжения.</param>
    /// <param name="endVoltage">Конечное значение напряжения.</param>
    /// <param name="step">Шаг напряжения.</param>
    /// <param name="delay">Задержка между измерениями.</param>
    /// <param name="token">Токен для отмены операции.</param>
    static private async Task CheckVoltageLevelsAsync(CancellationToken cancellationToken, IUserMessageService messageService, double startVoltage, double endVoltage, double step, int delay, IFastMeter fastMeter, IPowerSourceModule powerSource)
    {
      await messageService.ShowMessageAsync(new Utilities.Models.ShowMessageModel($"Проверка уровней напряжения от {startVoltage} до {endVoltage} с шагом {step}"));
      for (double voltage = startVoltage; voltage <= endVoltage; voltage += step)
      {
        cancellationToken.ThrowIfCancellationRequested();
        double roundedVoltage = Math.Round(voltage, 1);
        await SetVoltageAndShowMessage(messageService, roundedVoltage, powerSource);
        await Task.Delay(1000);
        await MeasureAndCompareVoltage(messageService, roundedVoltage, delay, fastMeter);
      }
    }

    /// <summary>
    /// Устанавливает напряжение и отображает сообщение.
    /// </summary>
    /// <param name="voltage">Устанавливаемое напряжение.</param>
    static private async Task SetVoltageAndShowMessage(IUserMessageService messageService, double voltage, IPowerSourceModule powerSource)
    {
      int a = (int)voltage;
      int b = (int)((voltage - a) * 10);
      await powerSource.VoltageManager.SetVoltageLevelAsync(a, b);
    }

    /// <summary>
    /// Измеряет и сравнивает напряжение.
    /// </summary>
    /// <param name="voltage">Ожидаемое напряжение.</param>
    /// <param name="delay">Задержка перед измерением.</param>
    /// <param name="token">Токен отмены.</param>
    static private async Task MeasureAndCompareVoltage(IUserMessageService messageService, double voltage, int delay, IFastMeter fastMeter)
    {
      double tolerance = 0.0001;
      double firstNorm = voltage - (0.01 * voltage + 0.1);
      double lastNorm = voltage + (0.01 * voltage + 0.1);

      await Task.Delay(40).ConfigureAwait(true);
      double result = await GetMeasurementResult(messageService, voltage, delay, fastMeter);

      bool error = !(result >= firstNorm - tolerance && result <= lastNorm + tolerance);
      var statusText = !error ? "В норме" : "Вне нормы";
      if (!error)
      {
        await messageService.ShowMessageAsync(new Utilities.Models.ShowMessageModel($"Результат измерения: {result} В ({firstNorm} - {lastNorm}). Статус: {statusText}"));
      }
      else
      {
        await messageService.ShowMessageAsync(new Utilities.Models.ShowMessageModel($"Результат измерения: {result} В ({firstNorm} - {lastNorm}). Статус: {statusText}"));
      }

      await Task.Delay(1);
    }

    /// <summary>
    /// Получает результат измерения.
    /// </summary>
    /// <param name="voltage">Ожидаемое напряжение.</param>
    /// <param name="delay">Задержка перед измерением.</param>
    /// <param name="token">Токен отмены.</param>
    /// <returns>Результат измерения.</returns>
    static private async Task<double> GetMeasurementResult(IUserMessageService messageService, double voltage, int delay, IFastMeter meter)
    {
      await Task.Delay(delay);
      double result = await meter.DcVoltageManager.MeasureDCVoltageAsync();
      LogInformation($"Измеренное напряжение: {result} В");
      return result;
    }
  }
}
