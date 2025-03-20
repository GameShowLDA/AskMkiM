using Core.Communication;
using Core.Model;
using Mode.Metrology.Base;
using Utilities.Models;
using static AppConfig.Config.ExecutionConfig;

namespace Mode.Metrology.CI
{
  public partial class CiControl
  {
    /// <summary>
    /// Пытается подключиться к устройствам.
    /// Выполняет проверку соединения с основными компонентами системы,
    /// включая менеджер шасси, модули реле, устройство коммутации шин и GPT.
    /// </summary>
    /// <returns>
    /// <c>true</c>, если соединение установлено успешно, иначе <c>false</c>.
    /// </returns>
    public async Task<bool> AttemptDeviceConnection() =>
      await ProtocolSelfCheckControl.AttemptDeviceConnection(
        new List<DeviceModel>()
        {
          measurementDataModel.ManagerShassy,
          measurementDataModel.FirstModuleRelayControl,
          measurementDataModel.LastModuleRelayControl,
          deviceBusCommutation,
          gptLibrary,
        },
        ShowMessageAsync
      );

    /// <summary>
    /// Настраивает устройства для измерения.
    /// Производит сброс системы, подключает шины и точки измерения,
    /// а также настраивает режим и время работы GPT.
    /// </summary>
    /// <param name="cancellationToken">
    /// Токен отмены для прерывания операции конфигурации, если это необходимо.
    /// </param>
    /// <returns>
    /// Задача асинхронного выполнения операции.
    /// </returns>
    public async Task ConfigureDevices(CancellationToken cancellationToken)
    {
      await CommunicationManager.ResetAllSystem();

      cancellationToken.ThrowIfCancellationRequested();
      await ShowMessageAsync(new ShowMessageModel("Подключение шин УКШ", goodText.Item2));
      if (!await GetIsIdleModeEnabled())
      {
        await Core.DeviceBusCommutation.Functions.ConnectToBreakdownTester(deviceBusCommutation.IPAddress);
      }

      cancellationToken.ThrowIfCancellationRequested();
      await ShowMessageAsync(new ShowMessageModel("Подключение шин МКР", goodText.Item2));
      if (!await GetIsIdleModeEnabled())
      {
        await MetrologyDeviceCommunication.ModuleRelayControl_ConnectBusesAsync(
          measurementDataModel.FirstModuleRelayControl,
          Core.ModuleRelayControl.Enums.BusModuleRelayControl.AB1,
          ShowMessageAsync);
      }

      if (measurementDataModel.LastModuleRelayControl.IPAddress != measurementDataModel.FirstModuleRelayControl.IPAddress)
      {
        cancellationToken.ThrowIfCancellationRequested();
        if (!await GetIsIdleModeEnabled())
        {
          await MetrologyDeviceCommunication.ModuleRelayControl_ConnectBusesAsync(
            measurementDataModel.LastModuleRelayControl,
            Core.ModuleRelayControl.Enums.BusModuleRelayControl.AB1,
            ShowMessageAsync);
        }
      }

      cancellationToken.ThrowIfCancellationRequested();
      if (!await GetIsIdleModeEnabled())
      {
        await MetrologyDeviceCommunication.ModuleRelayControl_ConnectRelayAsync(
          measurementDataModel.FirstPointModel,
          measurementDataModel.LastPointModel,
          measurementDataModel.FirstModuleRelayControl,
          measurementDataModel.LastModuleRelayControl,
          ShowMessageAsync);
      }

      cancellationToken.ThrowIfCancellationRequested();
      if (!await GetIsIdleModeEnabled())
      {
        await Core.GptLibrary.IrMode.SetModeAsync(gptLibrary as Core.GptLibrary.Model);
      }

      if (!await GetIsIdleModeEnabled())
      {
        int time;
        if (!int.TryParse(TimeData.Text, out time))
        {
          time = 2;
        }

        await Core.GptLibrary.IrMode.SetTimeAsync(gptLibrary as Core.GptLibrary.Model, time);
      }
    }
  }
}
