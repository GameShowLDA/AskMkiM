using Mode.Metrology.Base;
using NewCore.Base.Device;
using Utilities.Models;
using static AppConfig.Config.ExecutionConfig;
using static NewCore.Enum.DeviceEnum;

namespace Mode.Metrology.IE
{
  /// <inheritdoc />
  public partial class IeControl
  {
    /// <summary>
    /// Пытается подключиться к устройствам.
    /// </summary>
    public async Task<bool> AttemptDeviceConnection() => await ProtocolSelfCheckControl.AttemptDeviceConnection
    (
        new List<IDevice>()
        {
          measurementDataModel.ManagerShassy,
          measurementDataModel.FirstModuleRelayControl,
          measurementDataModel.LastModuleRelayControl,
          deviceBusCommutation,
          meter,
        },
        ShowMessageAsync
      );

    /// <summary>
    /// Настраивает устройства для измерения.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены, позволяющий прервать операцию конфигурации.</param>
    public async Task ConfigureDevices(CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await ShowMessageAsync(new ShowMessageModel("Подключение шин УКШ", goodText.Item2));
      if (!await GetIsIdleModeEnabled())
      {
        await MetrologyDeviceCommunication.DeviceBusCommutationConnectBus(cancellationToken, ShowMessageAsync, deviceBusCommutation);
      }

      await ShowMessageAsync(new ShowMessageModel("\tЗамыкание шин AB2", null, $"[{goodText.Item1}]", goodText.Item2));

      cancellationToken.ThrowIfCancellationRequested();
      await ShowMessageAsync(new ShowMessageModel("Подключение шин МКР", goodText.Item2));
      if (!await GetIsIdleModeEnabled())
      {
        await MetrologyDeviceCommunication.ModuleRelayControl_ConnectBusesAsync(measurementDataModel.FirstModuleRelayControl, SwitchingBusNew.AB2, ShowMessageAsync);
      }

      await ShowMessageAsync(new ShowMessageModel($"\tМКР{measurementDataModel.FirstPointModel.ModuleNumber} Замыкание шин AB2", null, $"[{goodText.Item1}]", goodText.Item2));

      if (measurementDataModel.LastModuleRelayControl.IPAddress != measurementDataModel.FirstModuleRelayControl.IPAddress)
      {
        cancellationToken.ThrowIfCancellationRequested();
        if (!await GetIsIdleModeEnabled())
        {
          await MetrologyDeviceCommunication.ModuleRelayControl_ConnectBusesAsync(measurementDataModel.LastModuleRelayControl, SwitchingBusNew.AB2, ShowMessageAsync);
        }

        await ShowMessageAsync(new ShowMessageModel($"\tМКР{measurementDataModel.LastPointModel.ModuleNumber} Замыкание шин AB2", null, $"[{goodText.Item1}]", goodText.Item2));
      }

      cancellationToken.ThrowIfCancellationRequested();
      await ShowMessageAsync(new ShowMessageModel("Подключение точек МКР", goodText.Item2));
      if (!await GetIsIdleModeEnabled())
      {
        await MetrologyDeviceCommunication.ModuleRelayControl_ConnectRelayAsync(measurementDataModel.FirstPointModel, measurementDataModel.LastPointModel, measurementDataModel.FirstModuleRelayControl, measurementDataModel.LastModuleRelayControl, ShowMessageAsync);
      }

      await ShowMessageAsync(new ShowMessageModel($"\tТочка {measurementDataModel.FirstPointModel.PointNumber}", null, $"[{goodText.Item1}]", goodText.Item2));
      await ShowMessageAsync(new ShowMessageModel($"\tТочка {measurementDataModel.LastPointModel.PointNumber}", null, $"[{goodText.Item1}]", goodText.Item2));

      cancellationToken.ThrowIfCancellationRequested();
      if (!await GetIsIdleModeEnabled())
      {
        await meter.ResistanceManager.SetResistanceModeAsync();
      }
    }
  }
}
