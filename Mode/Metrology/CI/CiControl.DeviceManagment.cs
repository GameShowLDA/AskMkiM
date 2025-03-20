using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mode.Metrology.Base;
using NewCore.Base.Device;
using Utilities.Models;
using static AppConfig.Config.ExecutionConfig;
using static NewCore.Enum.DeviceEnum;

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
        new List<IDevice>()
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
      await NewCore.Communication.DeviceCommandSender.ResetAllSystem();

      cancellationToken.ThrowIfCancellationRequested();
      await ShowMessageAsync(new ShowMessageModel("Подключение шин УКШ", goodText.Item2));
      if (!await GetIsIdleModeEnabled())
      {
        await deviceBusCommutation.ConnectorManager.ConnectBreakdownTester();
      }

      cancellationToken.ThrowIfCancellationRequested();
      await ShowMessageAsync(new ShowMessageModel("Подключение шин МКР", goodText.Item2));
      if (!await GetIsIdleModeEnabled())
      {
        await MetrologyDeviceCommunication.ModuleRelayControl_ConnectBusesAsync(
          measurementDataModel.FirstModuleRelayControl,
          SwitchingBusNew.AB1,
          ShowMessageAsync);
      }

      if (measurementDataModel.LastModuleRelayControl.IPAddress != measurementDataModel.FirstModuleRelayControl.IPAddress)
      {
        cancellationToken.ThrowIfCancellationRequested();
        if (!await GetIsIdleModeEnabled())
        {
          await MetrologyDeviceCommunication.ModuleRelayControl_ConnectBusesAsync(
            measurementDataModel.LastModuleRelayControl,
            SwitchingBusNew.AB1,
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
        await gptLibrary.IrManger.SetModeAsync();
      }

      if (!await GetIsIdleModeEnabled())
      {
        int time;
        if (!int.TryParse(TimeData.Text, out time))
        {
          time = 2;
        }

        await gptLibrary.IrManger.SetTimeAsync(time);
      }
    }
  }
}
