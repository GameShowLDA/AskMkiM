using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Services.UI;
using System.Diagnostics;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.ResponseProcessor.BreakdownTester.ResponseProcessing;
using Ask.Device.Runtime.Device;
using Ask.Device.Runtime.Function.GPT.Command;
using Ask.Device.Runtime.Function.GPT.Helper;
using Ask.Device.Runtime.Function.Helpers;
using static Ask.Device.Runtime.Function.GPT.Command.FunctionCommandManager;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.Function.GPT.Managment
{
  /// <summary>
  /// Класс управления измерениями для режима IR (сопротивление изоляции).
  /// Последовательно опрашивает прибор с поддержкой отмены выполнения.
  /// </summary>
  public class IrMeasureManagment : IMeasurable
  {
    private readonly GPT79904 _gptModel;
    private readonly int _delayBeforeCall;
    private readonly Func<Task<double>> _getTestTime;
    private readonly Func<Task<double>> _getRampTime;
    private readonly Func<Task<bool>> _getIsIdleMode;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="IrMeasureManagment"/>.
    /// </summary>
    /// <param name="gptModel">Модель устройства GPT-79904.</param>
    /// <param name="delayBeforeCall">Задержка перед вызовом команды (мс).</param>
    /// <param name="getTestTime">Функция получения времени теста.</param>
    /// <param name="getRampTime">Функция получения времени нарастания.</param>
    /// <param name="getIsIdleMode">Функция для проверки Idle Mode устройства.</param>
    public IrMeasureManagment(
        GPT79904 gptModel,
        int delayBeforeCall,
        Func<Task<double>> getTestTime,
        Func<Task<double>> getRampTime,
        Func<Task<bool>> getIsIdleMode)
    {
      _gptModel = gptModel;
      _delayBeforeCall = delayBeforeCall;
      _getIsIdleMode = getIsIdleMode;
      _getTestTime = getTestTime;
      _getRampTime = getRampTime;
    }

    /// <inheritdoc />
    public async Task<BreakdownMeasurementResponse> MeasureAsync(
      ElectricalTestFunction electricalTestFunction,
      MeasurementRange measurementRange,
      bool waitFullTime = false,
      IUserInteractionService? userMessageService = null)
    {
      using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
        EquipmentExecutionContext.CancellationToken, userMessageService?.GetCancellationToken() ?? CancellationToken.None);
      using var executionScope = EquipmentExecutionContext.EnterExecution(cancellation.Token);
      var token = cancellation.Token;
      token.ThrowIfCancellationRequested();
      if (await _getIsIdleMode())
        return new BreakdownMeasurementResponse(BreakdownMeasurementStatus.Pass, MeasurementAdapterHelper.Round(measurementRange.TargetValue), string.Empty);

      await StopMeasure();
      await Task.Delay(_delayBeforeCall, token);

      var time = await _getTestTime();
      var timeRamp = await _getRampTime();

      string response = string.Empty;
      var testCommand = $"{GetCommandSyntax(FunctionCommand.FUNCTION_TEST)} ON";
      await _gptModel.DeviceProtocol.QueryAsync(testCommand, cancellationToken: token);
      var elapsed = Stopwatch.StartNew();
      var duration = TimeSpan.FromSeconds(Math.Max(0, time + timeRamp));
      while (elapsed.Elapsed < duration)
      {
        await Task.Delay(200, token);
        response = await _gptModel.DeviceProtocol.QueryAsync(
          $"{GetCommandSyntax(FunctionCommand.MEASURE)} ?",
          timeout: 500,
          delayBeforeCall: _delayBeforeCall, cancellationToken: token);

          var model = ParseMeasurement(response);
          if (model is null) continue;
          if (model.Status == BreakdownMeasurementStatus.Fail)
          {
            await _gptModel.DeviceProtocol.QueryAsync(testCommand, cancellationToken: token);
          }
          else if (model.Status == BreakdownMeasurementStatus.Test && model.Value > 0 && model.Value > measurementRange.TargetValue)
          {
            await StopMeasure();
            break;
          }
      }

      var completionWait = Stopwatch.StartNew();
      BreakdownMeasurementResponse measurement;
      while (true)
      {
        token.ThrowIfCancellationRequested();
        if (completionWait.Elapsed > MeasureHelper.CompletionTimeout)
          throw new TimeoutException("Прибор не вернул итоговый результат измерения сопротивления изоляции.");
        response = await _gptModel.DeviceProtocol.QueryAsync(
          $"{GetCommandSyntax(FunctionCommand.MEASURE)} ?",
          timeout: 500,
          delayBeforeCall: _delayBeforeCall, cancellationToken: token);

        if (!BreakdownTesterResponseProcessor.IsTestInProgress(response)
          && BreakdownTesterResponseProcessor.TryParseMeasurement(response, out measurement))
          break;

        await Task.Delay(50, token);
      }

      double multiplier = measurement.Unit.ToLowerInvariant() switch
      {
        "gohm" => 1000d,
        "mohm" => 1d,
        "kohm" => 0.001d,
        _ => throw new FormatException("Неизвестный формат результата измерения."),
      };

      return new BreakdownMeasurementResponse(BreakdownMeasurementStatus.Pass, MeasurementAdapterHelper.Round(measurement.Value * multiplier), string.Empty);
    }

    /// <inheritdoc />
    public async Task StopMeasure()
    {
      await MeasureHelper.StopMeasure(_gptModel);
    }

    /// <inheritdoc />
    public async Task ApplyVoltageAsync(IUserInteractionService? userMessageService = null)
    {
      LogInformation($"Начало {nameof(ApplyVoltageAsync)}", isDeviceLog: true);
      try
      {
        if (await _getIsIdleMode())
        {
          LogInformation($"{nameof(ApplyVoltageAsync)}: Устройство в Idle Mode. Пропускаем применение напряжения.", isDeviceLog: true);
          return;
        }

        var command = $"{FunctionCommandManager.GetCommandSyntax(FunctionCommand.FUNCTION_TEST)} ON";
        await _gptModel.DeviceProtocol.QueryAsync(command, delayBeforeCall: _delayBeforeCall);
        LogInformation($"{nameof(ApplyVoltageAsync)}: Напряжение применено.", isDeviceLog: true);
      }
      catch (Exception ex)
      {
        LogException($"Ошибка в {nameof(ApplyVoltageAsync)}", ex, isDeviceLog: true);
        throw;
      }
    }

    /// <summary>
    /// Разбирает строку ответа прибора в модель <see cref="MeasurementData"/>.
    /// </summary>
    /// <summary>
    /// Разбирает строку ответа прибора в модель MeasurementData.
    /// Ищет статус PASS / FAIL / TEST в любой части ответа.
    /// </summary>
    private BreakdownMeasurementResponse? ParseMeasurement(string response)
    {
      if (!BreakdownTesterResponseProcessor.TryParseMeasurement(response, out var parsed))
        return null;

      double resistance = parsed.Unit.ToUpperInvariant() switch
      {
        "GOHM" => parsed.Value * 1_000_000,
        "MOHM" => parsed.Value * 1_000,
        _ => parsed.Value,
      };

      return new BreakdownMeasurementResponse
      {
        Status = parsed.Status,
        Value = resistance,
      };
    }
  }
}
