using System;
using System.Threading.Tasks;
using NewCore.Base.Function.ModuleVoltageCurrentSource;
using NewCore.Base.Interface.Main;
using NewCore.Function.Helpers;
using NewCore.Function.ModuleVoltageCurrentSource;
using Utilities.Error.Device.ModuleVoltageCurrent;
using static Utilities.LoggerUtility;

namespace NewCore.FunctionAdapters.ModuleVoltageCurrentSource
{
  /// <summary>
  /// Адаптер для управления током МИНТ с отображением сообщений.
  /// </summary>
  internal class CurrentManagerAdapter : ICurrentManager
  {
    private readonly IPowerSourceModule _module;
    private readonly CurrentManager _currentManager;

    public CurrentManagerAdapter(IPowerSourceModule module)
    {
      _module = module ?? throw new ArgumentNullException(nameof(module));
      _currentManager = new CurrentManager(module);
    }

    public async Task SetCurrentLevelAsync(int integerPart, int decimalPart)
    {
      string value = $"{integerPart}.{decimalPart:D3}";
      try
      {
        await _currentManager.SetCurrentLevelAsync(integerPart, decimalPart);
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_module, "Установка тока", $"{value} мА", true, 1);
      }
      catch (Exception ex)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_module, "Ошибка установки тока", ex.Message, false, 1);
        throw CurrentExceptionFactory.SetLevelFailed(value, ex.Message);
      }
    }

    public async Task<bool> LimitationOfTheOutputCurrent(int current)
    {
      bool result = await _currentManager.LimitationOfTheOutputCurrent(current);

      await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _module,
          "Ограничение тока",
          $"{current} мА",
          result,
          1);

      if (!result)
        throw CurrentExceptionFactory.LimitFailed(current);

      return result;
    }
  }
}
