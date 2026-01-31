using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Device;
using static Ask.LogLib.LoggerUtility;
using static NewCore.Function.GPT.Command.ManualCommandManager;

namespace NewCore.Function.GPT.Managment
{
  /// <summary>
  /// Класс управления пределами сопротивления для режима IR.
  /// Реализует интерфейс <see cref="IResistanceLimitsConfigurable"/>.
  /// </summary>
  internal class ResistanceLimitsManagment : IResistanceLimitsConfigurable
  {
    private readonly GPT79904 _gptModel;
    private readonly int _delay;
    private readonly Func<bool> _getIsIdleMode;
    private readonly Func<double> _getHighLimitConfig;
    private readonly Action<double> _setHighLimitConfig;
    private readonly Func<double> _getLowLimitConfig;
    private readonly Action<double> _setLowLimitConfig;

    public ResistanceLimitsManagment(
      GPT79904 gptModel,
      int delay,
      Func<bool> getIsIdleMode,
      Func<double> getHighLimitConfig,
      Action<double> setHighLimitConfig,
      Func<double> getLowLimitConfig,
      Action<double> setLowLimitConfig)
    {
      _gptModel = gptModel;
      _delay = delay;
      _getIsIdleMode = getIsIdleMode;
      _getHighLimitConfig = getHighLimitConfig;
      _setHighLimitConfig = setHighLimitConfig;
      _getLowLimitConfig = getLowLimitConfig;
      _setLowLimitConfig = setLowLimitConfig;
    }

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> SetHighResistanceLimitAsync(double value, IUserInteractionService? userMessageService = null)
    {
      if (_getIsIdleMode())
        return (true, string.Empty);

      string command = $"{GetCommandSyntax(ManualCommand.MANU_IR_RHISET)} {value:F3}".Replace(',', '.');

      for (int attempt = 1; attempt <= 2; attempt++)
      {
        await _gptModel.DeviceProtocol.QueryAsync(command);
        double actual = await GetHighResistanceLimitAsync();

        if (Math.Abs(actual - value) < 0.1)
        {
          _setHighLimitConfig(value);
          return (true, string.Empty);
        }

        LogWarning($"Попытка {attempt} установки высокого предела сопротивления неудачна. Ответ: {actual} ГОм", isDeviceLog: true);
      }

      return (false, $"Не удалось установить высокий предел IR: {value} ГОм.");
    }

    /// <inheritdoc />
    public async Task<double> GetHighResistanceLimitAsync()
    {
      var response = await _gptModel.DeviceProtocol.QueryAsync($"{GetCommandSyntax(ManualCommand.MANU_IR_RHISET)} ?", timeout: 1000);
      return ParseDouble(response, "G");
    }

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> SetLowResistanceLimitAsync(double value, IUserInteractionService? userMessageService = null)
    {
      if (_getIsIdleMode())
        return (true, string.Empty);

      if (value == 1000) value = 999; // спец. случай

      string command = $"{GetCommandSyntax(ManualCommand.MANU_IR_RLOSET)} {value:F0}M";

      for (int attempt = 1; attempt <= 2; attempt++)
      {
        await _gptModel.DeviceProtocol.QueryAsync(command);
        await Task.Delay(_delay);

        double actual = await GetLowResistanceLimitAsync();
        if (Math.Abs(actual - value) < 0.5)
        {
          _setLowLimitConfig(value);
          return (true, string.Empty);
        }

        LogWarning($"Попытка {attempt} установки нижнего предела сопротивления неудачна. Ответ: {actual} МОм", isDeviceLog: true);
      }

      return (false, $"Не удалось установить нижний предел IR: {value} МОм.");
    }

    /// <inheritdoc />
    public async Task<double> GetLowResistanceLimitAsync()
    {
      var response = await _gptModel.DeviceProtocol.QueryAsync($"{GetCommandSyntax(ManualCommand.MANU_IR_RLOSET)} ?", timeout: 1000);
      return ParseDouble(response, "M");
    }

    /// <summary>
    /// Парсинг числового значения с единицей измерения (ГОм/МОм).
    /// </summary>
    private double ParseDouble(string response, string suffix)
    {
      if (response.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        response = response.Substring(0, response.Length - 1);

      return double.TryParse(response, out var result) ? result : 0;
    }
  }
}
