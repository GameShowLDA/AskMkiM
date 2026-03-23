using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device;
using Ask.Device.Runtime.Function.GPT.Helper;

namespace Ask.Device.Runtime.Function.GPT.Managment
{
  /// <summary>
  /// Универсальный класс управления временными параметрами
  /// (временем теста и временем нарастания) для различных режимов GPT-79904.
  /// </summary>
  public class TimeManagment : ITimeConfigurable
  {
    private readonly GPT79904 _gptModel;
    private readonly BreakdownTypeMode _mode;
    private readonly int _delay;
    private readonly Func<double> _getTestTime;
    private readonly Action<double> _setTestTime;
    private readonly Func<double> _getRampTime;
    private readonly Action<double> _setRampTime;
    private double _testTime;
    private double _rampTime;

    /// <summary>
    /// Создаёт новый экземпляр класса <see cref="TimeManagment"/>.
    /// </summary>
    /// <param name="gptModel">Модель устройства GPT-79904.</param>
    /// <param name="mode">Режим работы (ACW, DCW, IR и т.д.).</param>
    /// <param name="delay">Задержка перед вызовом команды, мс.</param>
    /// <param name="getTestTime">Функция для получения текущего времени теста из конфигурации.</param>
    /// <param name="setTestTime">Действие для обновления времени теста в конфигурации.</param>
    /// <param name="getRampTime">Функция для получения текущего времени нарастания из конфигурации.</param>
    /// <param name="setRampTime">Действие для обновления времени нарастания в конфигурации.</param>
    public TimeManagment(
      GPT79904 gptModel,
      BreakdownTypeMode mode,
      int delay,
      Func<double> getTestTime,
      Action<double> setTestTime,
      Func<double> getRampTime,
      Action<double> setRampTime)
    {
      _gptModel = gptModel;
      _mode = mode;
      _delay = delay;
      _getTestTime = getTestTime;
      _setTestTime = setTestTime;
      _getRampTime = getRampTime;
      _setRampTime = setRampTime;
    }

    /// <inheritdoc />
    /// <summary>
    /// Устанавливает время теста.
    /// </summary>
    public async Task<(bool Success, string Message)> SetTestTimeAsync(double value, IUserInteractionService? userMessageService = null)
    {
      var result = await CongifHelper.SetParameterAsync(
          getter: async () => _getTestTime(),
          setter: async () => await TimeHelper.SetTestTimeAsync(_gptModel, _mode, value, _delay),
          updateConfig: v => _setTestTime(v),
          newValue: value);

      if (result.Success)
      {
        _testTime = value;
      }

      return result;
    }

    /// <inheritdoc />
    /// <summary>
    /// Считывает время теста.
    /// </summary>
    public async Task<double> GetTestTimeAsync()
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return _testTime;
      }
      else
      {
        return await TimeHelper.GetTestTimeAsync(_gptModel, _mode, _delay);
      }
    }

    /// <inheritdoc />
    /// <summary>
    /// Устанавливает время нарастания напряжения.
    /// </summary>
    public async Task<(bool Success, string Message)> SetRampTimeAsync(double value, IUserInteractionService? userMessageService = null)
    {
      var result = await CongifHelper.SetParameterAsync(
           getter: async () => _getRampTime(),
           setter: async () => await TimeHelper.SetRampTimeAsync(_gptModel, value, _delay),
           updateConfig: v => _setRampTime(v),
           newValue: value);

      if (result.Success)
      {
        _rampTime = value;
      }

      return result;
    }

    /// <inheritdoc />
    /// <summary>
    /// Считывает время нарастания напряжения.
    /// </summary>
    public async Task<double> GetRampTimeAsync()
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return _rampTime;
      }
      else
      {
        return await TimeHelper.GetRampTimeAsync(_gptModel, _delay);
      }
    }
  }
}
