using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device;
using Ask.Device.Runtime.Function.GPT.Helper;

namespace Ask.Device.Runtime.Function.GPT.Managment
{
  /// <summary>
  /// Универсальный класс управления параметром смещения (Offset)
  /// для различных режимов GPT-79904.
  /// </summary>
  public class OffsetManagment : IOffsetConfigurable
  {
    private readonly GPT79904 _gptModel;
    private readonly BreakdownTypeMode _mode;
    private readonly int _delay;
    private readonly Func<double> _getOffset;
    private readonly Action<double> _setOffset;
    private double _offset;

    /// <summary>
    /// Создаёт новый экземпляр класса <see cref="OffsetManagment"/>.
    /// </summary>
    /// <param name="gptModel">Модель устройства GPT-79904.</param>
    /// <param name="mode">Режим работы (ACW, DCW, IR и т.д.).</param>
    /// <param name="delay">Задержка перед вызовом команды, мс.</param>
    /// <param name="getOffset">Функция получения текущего значения Offset из конфигурации.</param>
    /// <param name="setOffset">Действие обновления значения Offset в конфигурации.</param>
    public OffsetManagment(
      GPT79904 gptModel,
      BreakdownTypeMode mode,
      int delay,
      Func<double> getOffset,
      Action<double> setOffset)
    {
      _gptModel = gptModel;
      _mode = mode;
      _delay = delay;
      _getOffset = getOffset;
      _setOffset = setOffset;
    }

    /// <inheritdoc />
    /// <summary>
    /// Устанавливает значение Offset.
    /// </summary>
    public async Task<(bool Success, string Message)> SetOffsetAsync(double value, IUserInteractionService? userMessageService = null)
    {
      var result = await CongifHelper.SetParameterAsync(
          getter: async () => _getOffset(),
          setter: async () => await OffsetHelper.SetOffsetAsync(_gptModel, _mode, value, _delay),
          updateConfig: v => _setOffset(v),
          newValue: value);

      if (result.Success)
      {
        _offset = value;
      }

      return result;
    }

    /// <inheritdoc />
    /// <summary>
    /// Считывает текущее значение Offset.
    /// </summary>
    public async Task<double> GetOffsetAsync()
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return _offset;
      }
      return await OffsetHelper.GetOffsetAsync(_gptModel, _mode, _delay);
    }
  }
}
