using Ask.Core.Services.Errors.Device.ModuleVoltageCurrent;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Runtime.Function.Helpers;
using Ask.Device.Runtime.Function.ModuleVoltageCurrentSource;

namespace Ask.Device.Application.FunctionAdapters.ModuleVoltageCurrent
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

    public async Task SetCurrentLevelAsync(int integerPart, int decimalPart, IUserInteractionService? messageService = null)
    {
      string value = $"{integerPart}.{decimalPart:D3}";
      try
      {
        await _currentManager.SetCurrentLevelAsync(integerPart, decimalPart);
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_module, "Установка тока", $"{value} мА", true, 1, messageService);
      }
      catch (Exception ex)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_module, "Ошибка установки тока", ex.Message, false, 1, messageService);
        throw CurrentExceptionFactory.SetLevelFailed(value, ex.Message);
      }
    }

    public async Task<bool> LimitationOfTheOutputCurrent(int current, IUserInteractionService? messageService = null)
    {
      bool result = await _currentManager.LimitationOfTheOutputCurrent(current);

      await DeviceMessageBuilder.ShowConnectionMessageAsync(_module, "Ограничение тока", $"{current} мА", result, 1, messageService);

      if (!result)
        throw CurrentExceptionFactory.LimitFailed(current);

      return result;
    }
  }
}
