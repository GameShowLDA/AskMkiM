using System;
using System.Threading.Tasks;
using NewCore.Base.Function.ModuleVoltageCurrentSource;
using NewCore.Base.Interface.Main;
using NewCore.Device;
using NewCore.Function.Helpers;
using NewCore.Function.ModuleVoltageCurrentSource;
using static NewCore.Enum.DeviceEnum;

namespace NewCore.FunctionAdapters.ModuleVoltageCurrentSource
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

    public async Task SetSourceVoltageAsync(VoltageSources voltageSources)
    {
      await _voltageManager.SetSourceVoltageAsync(voltageSources);
      string label = voltageSources == VoltageSources.Supply12V ? "12 В" : "5 В";
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Выбор источника напряжения", $"Источник: {label}", true, 1);
    }

    public async Task SetVoltageLevelAsync(int integerPart, int decimalPart)
    {
      await _voltageManager.SetVoltageLevelAsync(integerPart, decimalPart);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(
        _device,
        "Установка уровня напряжения",
        $"Напряжение: {integerPart}.{decimalPart:D3} В",
        true,
        1
      );
    }
  }
}
