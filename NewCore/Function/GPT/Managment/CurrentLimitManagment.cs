using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using NewCore.Device;
using NewCore.Function.GPT.Helper;

namespace NewCore.Function.GPT.Managment
{
  /// <summary>
  /// Универсальный класс управления верхним и нижним пределами тока
  /// для различных режимов GPT-79904 (ACW, DCW, IR и т.д.).
  /// </summary>
  public class CurrentLimitManagment : ICurrentLimitsConfigurable
  {
    private readonly GPT79904 _gptModel;
    private readonly BreakdownTypeMode _mode;
    private readonly int _delay;
    private readonly Func<double> _getHighLimit;
    private readonly Action<double> _setHighLimit;
    private readonly Func<double> _getLowLimit;
    private readonly Action<double> _setLowLimit;
    private double _highLimit;
    private double _lowLimit;

    /// <summary>
    /// Создает новый экземпляр класса <see cref="CurrentLimitManagment"/>.
    /// </summary>
    /// <param name="gptModel">Модель устройства GPT-79904.</param>
    /// <param name="mode">Режим работы (ACW, DCW, IR и т.д.).</param>
    /// <param name="delay">Задержка перед вызовом команды (мс).</param>
    /// <param name="getHighLimit">Функция для получения верхнего предела тока из конфигурации.</param>
    /// <param name="setHighLimit">Действие для обновления верхнего предела тока в конфигурации.</param>
    /// <param name="getLowLimit">Функция для получения нижнего предела тока из конфигурации.</param>
    /// <param name="setLowLimit">Действие для обновления нижнего предела тока в конфигурации.</param>
    public CurrentLimitManagment(
      GPT79904 gptModel,
      BreakdownTypeMode mode,
      int delay,
      Func<double> getHighLimit,
      Action<double> setHighLimit,
      Func<double> getLowLimit,
      Action<double> setLowLimit)
    {
      _gptModel = gptModel;
      _mode = mode;
      _delay = delay;
      _getHighLimit = getHighLimit;
      _setHighLimit = setHighLimit;
      _getLowLimit = getLowLimit;
      _setLowLimit = setLowLimit;
    }

    /// <inheritdoc />
    /// <summary>
    /// Устанавливает верхний предел тока.
    /// </summary>
    public async Task<(bool Success, string Message)> SetHighCurrentLimitAsync(double value, IUserInteractionService? userMessageService = null)
    {
      var result = await CongifHelper.SetParameterAsync(
          getter: async () => _getHighLimit(),
          setter: async () => await CurrentLimitHelper.SetHighCurrentLimitAsync(_gptModel, _mode, value, _delay),
          updateConfig: v => _setHighLimit(v),
          newValue: value);

      if (result.Success)
      {
        _highLimit = value;
      }

      return result;
    }

    /// <inheritdoc />
    /// <summary>
    /// Считывает верхний предел тока.
    /// </summary>
    public async Task<double> GetHighCurrentLimitAsync()
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return _highLimit;
      }

      return await CurrentLimitHelper.GetHighCurrentLimitAsync(_gptModel, _mode, _delay);
    }

    /// <inheritdoc />
    /// <summary>
    /// Устанавливает нижний предел тока.
    /// </summary>
    public async Task<(bool Success, string Message)> SetLowCurrentLimitAsync(double value, IUserInteractionService? userMessageService = null)
    {
      var result = await CongifHelper.SetParameterAsync(
          getter: async () => _getLowLimit(),
          setter: async () => await CurrentLimitHelper.SetLowCurrentLimitAsync(_gptModel, _mode, value, _delay),
          updateConfig: v => _setLowLimit(v),
          newValue: value);

      if (result.Success)
      {
        _lowLimit = value;
      }

      return result;
    }

    /// <inheritdoc />
    /// <summary>
    /// Считывает нижний предел тока.
    /// </summary>
    public async Task<double> GetLowCurrentLimitAsync()
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return _lowLimit;
      }

      return await CurrentLimitHelper.GetLowCurrentLimitAsync(_gptModel, _mode, _delay);
    }
  }
}
