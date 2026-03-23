using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Runtime.Device;
using Ask.Device.Runtime.Function.GPT.Helper;

namespace Ask.Device.Runtime.Function.GPT.Managment
{
  /// <summary>
  /// Управляет включением и выключением земли для режимов GPT-79904.
  /// </summary>
  public class GroundModeManagment : IGroundModeConfigurable
  {
    private readonly GPT79904 _gptModel;
    private readonly int _delay;
    private readonly Func<bool> _getGroundMode;
    private readonly Action<bool> _setGroundMode;
    private bool _groundMode;

    /// <summary>
    /// Создаёт новый экземпляр класса <see cref="GroundModeManagment"/>.
    /// </summary>
    public GroundModeManagment(
      GPT79904 gptModel,
      int delay,
      Func<bool> getGroundMode,
      Action<bool> setGroundMode)
    {
      _gptModel = gptModel;
      _delay = delay;
      _getGroundMode = getGroundMode;
      _setGroundMode = setGroundMode;
    }

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> SetGroundModeAsync(bool state, IUserInteractionService? userMessageService = null)
    {
      var result = await CongifHelper.SetParameterAsync(
        getter: async () => _getGroundMode(),
        setter: async () => await GroundModeHelper.SetGroundModeAsync(_gptModel, state, _delay),
        updateConfig: value => _setGroundMode(value),
        newValue: state);

      if (result.Success)
      {
        _groundMode = state;
      }

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> GetGroundModeAsync()
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return _groundMode;
      }

      return await GroundModeHelper.GetGroundModeAsync(_gptModel, _delay);
    }
  }
}
