using System;
using System.Threading.Tasks;
using NewCore.Base.Function.ModuleRelayControl;
using NewCore.Base.Interface.Main;
using NewCore.Function.Helpers;
using NewCore.Function.ModuleRelayControl;

namespace NewCore.FunctionAdapters.ModuleRelayControl
{
  /// <summary>
  /// Адаптер для управления измерителем модуля МКР с отображением сообщений.
  /// </summary>
  internal class MeterManagerAdapter : IMeterManager
  {
    private readonly IRelaySwitchModule _moduleRelayControl;
    private readonly MeterManager _meterManager;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MeterManagerAdapter"/>.
    /// </summary>
    /// <param name="moduleRelayControl">Экземпляр модуля реле.</param>
    public MeterManagerAdapter(IRelaySwitchModule moduleRelayControl)
    {
      _moduleRelayControl = moduleRelayControl ?? throw new ArgumentNullException(nameof(moduleRelayControl));
      _meterManager = new MeterManager(moduleRelayControl);
    }

    /// <inheritdoc />
    public async Task<bool> ConnectMeterAsync()
    {
      var result = await _meterManager.ConnectMeterAsync();

      await DeviceMessageBuilder.ShowConnectionMessageAsync(
        _moduleRelayControl,
        "Подключение измерителя модуля МКР",
        result,
        1);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectMeterAsync()
    {
      var result = await _meterManager.DisconnectMeterAsync();

      await DeviceMessageBuilder.ShowConnectionMessageAsync(
        _moduleRelayControl,
        "Отключение измерителя модуля МКР",
        result,
        1);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> GetMeterResponseAsync()
    {
      return await _meterManager.GetMeterResponseAsync();
    }
  }
}
