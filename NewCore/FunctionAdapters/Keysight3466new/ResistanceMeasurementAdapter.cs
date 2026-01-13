using Ask.Core.Services.App;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.Multimeter;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Device;
using NewCore.Function.Helpers;
using NewCore.Function.Keysight3466new;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
    public async Task<double> MeasureResistanceAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null)
    {
      var resistance = await _resistanceMeasurement.MeasureResistanceAsync(param, rangeFrom, rangeTo);
      return resistance;
    }

    /// <inheritdoc />
    public async Task<bool> SetResistanceModeAsync(IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(() => _resistanceMeasurement.SetResistanceModeAsync(), userMessageService, deviceTask: true);

      if (!result || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка режима измерения сопротивления", result, 1);
      }

      if (!result)
      {
        throw ResistanceExceptionFactory.SetModeFailed(_device.Name, _device.NumberChassis, _device.Number);
      }

      return result;
    }
  }
}
