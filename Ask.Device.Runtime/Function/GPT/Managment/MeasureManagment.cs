using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Runtime.Device;
using Ask.Device.Runtime.Function.GPT.Command;
using Ask.Device.Runtime.Function.GPT.Helper;
using static Ask.LogLib.LoggerUtility;
using static Ask.Device.Runtime.Function.GPT.Command.FunctionCommandManager;

namespace Ask.Device.Runtime.Function.GPT.Managment
{
  /// <summary>
  /// Универсальный класс управления измерениями (Measure) и применением напряжения.
  /// Используется в режимах GPT-79904 (ACW, DCW, IR и т.д.).
  /// </summary>
  public class MeasureManagment : IMeasurable
  {
    private readonly GPT79904 _gptModel;
    private readonly int _delayBeforeCall;
    private readonly Func<Task<double>> _getTestTime;
    private readonly Func<Task<double>> _getRampTime;
    private readonly bool _getIsIdleMode;

    /// <summary>
    /// Создаёт новый экземпляр класса <see cref="MeasureManagment"/>.
    /// </summary>
    /// <param name="gptModel">Модель устройства GPT-79904.</param>
    /// <param name="delayBeforeCall">Задержка перед вызовом команды (мс).</param>
    /// <param name="getTestTime">Функция для получения времени теста.</param>
    /// <param name="getRampTime">Функция для получения времени нарастания.</param>
    /// <param name="getIsIdleMode">Функция для проверки Idle Mode устройства.</param>
    public MeasureManagment(
      GPT79904 gptModel,
      int delayBeforeCall,
      Func<Task<double>> getTestTime,
      Func<Task<double>> getRampTime,
      bool getIsIdleMode)
    {
      _gptModel = gptModel;
      _delayBeforeCall = delayBeforeCall;
      _getTestTime = getTestTime;
      _getRampTime = getRampTime;
      _getIsIdleMode = getIsIdleMode;
    }

    /// <inheritdoc />
    public async Task<(double value, string unit)> MeasureAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, bool waitFullTime = false, IUserInteractionService? userMessageService = null)
    {
      var time = await _getTestTime();
      var timeRamp = await _getRampTime();

      if (_gptModel.Mode != Ask.Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.IR)
      {
        waitFullTime = true;
      }

      return await MeasureHelper.MeasureAsync(
        _gptModel,
        time,
        timeRamp,
        _delayBeforeCall,
        param,
        rangeFrom,
        rangeTo,
        waitFullTime,
        userMessageService);
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
        if (_getIsIdleMode)
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
  }
}
