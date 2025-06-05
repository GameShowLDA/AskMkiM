using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfiguration.Services;
using NewCore.Base.Function.FastMeter;
using NewCore.Device;
using NewCore.Function.Helpers;
using NewCore.Function.Keysight3466new;

namespace NewCore.FunctionAdapters.Keysight3466new
{
  internal class ResistanceMeasurementAdapter : IResistanceMeasurement
  {
    private readonly KeysightDevice _device;
    private readonly ResistanceMeasurement _resistanceMeasurement;

    /// <summary>
    /// Создаёт экземпляр класса <see cref="ResistanceMeasurement"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства Keysight.</param>
    /// <exception cref="ArgumentNullException">Выбрасывается, если переданный прибор <c>null</c>.</exception>
    public ResistanceMeasurementAdapter(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _resistanceMeasurement = new ResistanceMeasurement(device);
    }
    /// <inheritdoc />
    public async Task<double> MeasureResistanceAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1)
    {
      var resistance = await _resistanceMeasurement.MeasureResistanceAsync(param, rangeFrom, rangeTo);

      if (UserMessageServiceProvider.Instance != null)
      {
        var showMessage = DeviceMessageBuilder.GetDefaultSettings(_device);
        var result = resistance >= rangeFrom && resistance <= rangeTo;
        showMessage.Header = $"\tРезультат измеренного сопротивления({rangeFrom}-{rangeTo})";
        DeviceMessageBuilder.BuildMessage(ref showMessage, $"{resistance:F2}", !result);
        await DeviceMessageBuilder.ShowDeviceMessage(showMessage, result);
      }

      return resistance;
    }

    /// <inheritdoc />
    public async Task SetResistanceModeAsync()
    {
      await _resistanceMeasurement.SetResistanceModeAsync();

      if (UserMessageServiceProvider.Instance != null)
      {

        await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Установка режима измерения сопротивления",
          true,
          1);
      }
    }
  }
}
