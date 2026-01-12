using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using NewCore.Device;
using NewCore.Function.GPT.Helper;

namespace NewCore.Function.GPT.Managment
{
  /// <summary>
  /// Управляет установкой и чтением напряжения для различных режимов GPT-79904.
  /// </summary>
  public class VoltageManagment : IVoltageConfigurable
  {
    private readonly GPT79904 _gptModel;
    private readonly BreakdownTypeMode _mode;
    private readonly int _delay;
    private readonly Func<double> _getConfigVoltage;
    private readonly Action<double> _setConfigVoltage;

    /// <summary>
    /// Создает новый экземпляр класса <see cref="VoltageManagment"/>.
    /// </summary>
    /// <param name="gptModel">Модель устройства GPT-79904.</param>
    /// <param name="mode">Режим работы (ACW, DCW, IR и т.д.).</param>
    /// <param name="delay">Задержка перед вызовом команды, мс.</param>
    /// <param name="getConfigVoltage">Функция получения текущего напряжения из конфигурации режима.</param>
    /// <param name="setConfigVoltage">Действие обновления напряжения в конфигурации режима.</param>
    public VoltageManagment(
      GPT79904 gptModel,
      BreakdownTypeMode mode,
      int delay,
      Func<double> getConfigVoltage,
      Action<double> setConfigVoltage)
    {
      _gptModel = gptModel;
      _mode = mode;
      _delay = delay;
      _getConfigVoltage = getConfigVoltage;
      _setConfigVoltage = setConfigVoltage;
    }

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> SetVoltageAsync(double value, IUserInteractionService? userMessageService = null)
    {
      double kvValue = value / 1000;

      return await CongifHelper.SetParameterAsync(
        getter: async () => _getConfigVoltage(),
        setter: async () => await VoltageHelper.SetVoltageAsync(_gptModel, _mode, value, kvValue, _delay),
        updateConfig: v => _setConfigVoltage(v),
        newValue: kvValue);
    }

    /// <inheritdoc />
    public async Task<double> GetVoltageAsync()
    {
      return await VoltageHelper.GetVoltageAsync(_gptModel, _mode, _delay);
    }
  }
}
