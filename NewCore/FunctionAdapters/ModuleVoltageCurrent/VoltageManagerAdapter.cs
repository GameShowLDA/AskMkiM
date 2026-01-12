using Ask.Core.Services.Errors.Device.ModuleVoltageCurrent;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using NewCore.Function.Helpers;
using NewCore.Function.ModuleVoltageCurrentSource;

namespace NewCore.FunctionAdapters.ModuleVoltageCurrent
{
  /// <summary>
  /// Адаптер для управления напряжением на МИНТ с отображением сообщений.
  /// </summary>
  internal class VoltageManagerAdapter : IVoltageManager
  {
    private readonly IPowerSourceModule _device;
    private readonly VoltageManager _voltageManager;

    public VoltageManagerAdapter(IPowerSourceModule device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _voltageManager = new VoltageManager(device);
    }

    public async Task SetSourceVoltageAsync(VoltageSources voltageSources, IUserInteractionService? messageService = null)
    {
      string label = voltageSources == VoltageSources.Supply12V ? "12 В" : "5 В";

      try
      {
        await _voltageManager.SetSourceVoltageAsync(voltageSources);
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Выбор источника напряжения", $"Источник: {label}", true, 1, messageService);
      }
      catch (Exception ex)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Ошибка выбора источника напряжения", ex.Message, false, 1, messageService);

        throw VoltageExceptionFactory.SetSourceFailed(label, ex.Message);
      }
    }

    public async Task SetVoltageLevelAsync(int integerPart, int decimalPart, IUserInteractionService? messageService = null)
    {
      string value = $"{integerPart}.{decimalPart}";

      try
      {
        await _voltageManager.SetVoltageLevelAsync(integerPart, decimalPart);
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка уровня напряжения", $"Напряжение: {value} В", true, 1, messageService);
      }
      catch (Exception ex)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Ошибка установки напряжения", ex.Message, false, 1, messageService);

        throw VoltageExceptionFactory.SetLevelFailed(value, ex.Message);
      }
    }
  }
}
