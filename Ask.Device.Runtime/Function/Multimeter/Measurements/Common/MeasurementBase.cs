using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Runtime.Function.Helpers;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.Function.Multimeter.Measurements.Common
{
  static internal class MeasurementBase
  {
    static public async Task<double> MeasureAsync(IFastMeter device, IMeasurementProfile profile, double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null)
    {
      var header = EnumExtensions.GetDescription(profile.ElectricalTest);
      var unit = profile.Unit.GetUnit();

      if (device.TypeMode != profile.TypeMode)
      {
        await SetModeBase.SetModeAsync(device, profile, userMessageService);
      }

      if (rangeTo == -1)
      {
        rangeTo = double.MaxValue;
      }

      var random = Simulated.GetSimulatedValue(rangeFrom, rangeTo, profile.ElectricalTest);
      if (random != -1)
      {
        return random;
      }

      var execution = await AdapterMeasurementExecutor.ExecuteAsync(
        device,
        header,
        () => MeasureCoreAsync(device, profile, header, param, rangeFrom, rangeTo));

      if (!execution.Success)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(device, $"Ошибка при \"{header}\"", execution.ErrorMessage, false, 2, userMessageService);
        return -1;
      }

      double result = execution.Value;
      await DeviceMessageBuilder.ShowConnectionMessageAsync(device, $"Результат \"{header}\"", $"{result} {unit}", true, 2, userMessageService);
      return result;
    }

    /// <inheritdoc />
    static private async Task<double> MeasureCoreAsync(IFastMeter device, IMeasurementProfile profile, string header, double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return MeasurementAdapterHelper.Round(param);
      }

      if (!device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      string response = await device.DeviceProtocol.QueryAsync(device.CapacitanceCommands.Measure, responseDelay: 1500, timeout: device.CapacitanceCommands.Timeout);
      response = response.Trim().Replace("+", "");

      if (double.TryParse(response, System.Globalization.NumberStyles.Float,
                          System.Globalization.CultureInfo.InvariantCulture, out double capacitance))
      {
        return MeasurementAdapterHelper.Round(capacitance * 1e9);
      }

      throw new InvalidOperationException(LogError($"Не удалось обработать значение при \"{header}\": {response}", isDeviceLog: true));
    }
  }
}
