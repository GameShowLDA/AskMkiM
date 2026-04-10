using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device;
using Ask.Device.Runtime.Function.GPT.Helper;

namespace Ask.Device.Runtime.Function.GPT.Managment
{
  /// <summary>
  /// Универсальный класс управления параметром тока дуги (Arc Current)
  /// для различных режимов GPT-79904.
  /// </summary>
  public class ArcCurrentManagment : IArcCurrentConfigurable
  {
    private readonly GPT79904 _gptModel;
    private readonly BreakdownTypeMode _mode;
    private readonly int _delay;
    private readonly Func<double> _getArcCurrent;
    private readonly Action<double> _setArcCurrent;
    private double _arcCurrent;

    /// <summary>
    /// Создаёт новый экземпляр класса <see cref="ArcCurrentManagment"/>.
    /// </summary>
    /// <param name="gptModel">Модель устройства GPT-79904.</param>
    /// <param name="mode">Режим работы (ACW, DCW, IR и т.д.).</param>
    /// <param name="delay">Задержка перед вызовом команды, мс.</param>
    /// <param name="getArcCurrent">Функция для получения текущего значения тока дуги из конфигурации.</param>
    /// <param name="setArcCurrent">Действие для обновления значения тока дуги в конфигурации.</param>
    public ArcCurrentManagment(
      GPT79904 gptModel,
      BreakdownTypeMode mode,
      int delay,
      Func<double> getArcCurrent,
      Action<double> setArcCurrent)
    {
      _gptModel = gptModel;
      _mode = mode;
      _delay = delay;
      _getArcCurrent = getArcCurrent;
      _setArcCurrent = setArcCurrent;
    }

    /// <inheritdoc />
    /// <summary>
    /// Устанавливает значение тока дуги (Arc Current).
    /// </summary>
    public async Task<(bool Success, string Message)> SetArcCurrentAsync(double value, IUserInteractionService? userMessageService = null)
    {
      var result = await CongifHelper.SetParameterAsync(
          getter: async () => _getArcCurrent(),
          setter: async () => await ArcCurrentHelper.SetArcCurrentAsync(_gptModel, _mode, value, _delay),
          updateConfig: v => _setArcCurrent(v),
          newValue: value);

      if (result.Success)
      { 
        _arcCurrent = value;
      }

      return result;
    }

    /// <inheritdoc />
    /// <summary>
    /// Считывает текущее значение тока дуги (Arc Current).
    /// </summary>
    public async Task<double> GetArcCurrentAsync()
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return _arcCurrent;
      }

      return await ArcCurrentHelper.GetArcCurrentAsync(_gptModel, _mode, _delay);
    }
  }
}
