using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.DataBase.Engine.Static.Devices;

namespace Ask.Engine.Tests.SelfControl
{
  public class ModuleSelfExecutor
  {
    private IDeviceSelectorProvider _deviceSelectorProvider;
    public ModuleSelfExecutor(IDeviceSelectorProvider deviceSelectorProvider)
    {
      _deviceSelectorProvider = deviceSelectorProvider;
    }

    /// <summary>
    /// Инициализирует все необходимые настройки для компонента.
    /// Очищает предыдущий контент и добавляет новые элементы управления.
    /// </summary>
    public void InitializeSettings(IExecutionController executionController)
    {
      ActionSettings settings = new ActionSettings()
      {
        StartDelegate = ExecuteMeasurementProcess,
        CheckType = CheckType.SelfTest,
        CheckPower = false,
        AccumulateErrorMessages = true,
        NameProvider = GetExecutionName,
      };

      executionController.SetSettings(settings);
    }

    /// <summary>
    /// Формирует имя самоконтроля для выбранного устройства.
    /// </summary>
    /// <returns>Имя самоконтроля или <see langword="null"/>, если устройство не выбрано.</returns>
    private string? GetExecutionName()
    {
      var device = _deviceSelectorProvider.GetDeviceSelector().GetSelectedRelayDeviceByTypeSafe();
      return device is IAttachableDevice attachableDevice
        ? $"Самоконтроль модуля {attachableDevice.Name} {attachableDevice.NumberChassis}.{attachableDevice.Number}"
        : null;
    }

    /// <summary>
    /// Выполнение контроля.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    private async Task ExecuteMeasurementProcess(IUserInteractionService _messageService, IInputFieldProvider inputFieldProvider, IInputHighlightService inputHighlightService, CancellationToken cancellationToken)
    {
      IDeviceSelector deviceSelector = _deviceSelectorProvider.GetDeviceSelector();

      var device = deviceSelector.GetSelectedRelayDeviceByTypeSafe();
      var type = deviceSelector.GetSelectedRelayDeviceType();
      var part = deviceSelector.GetSelectedSelfControlEnumUntypedSafe();

      if (device != null)
      {
        var meter = deviceSelector.GetFastMeterSafe();
        if (meter == null)
        {
          await _messageService.ShowMessageAsync(new ShowMessageModel("Ошибка", message: "Не удалось преобразовать объект в измеритель!", type: ShowMessageModel.MessageType.Error));
          return;
        }

        switch (type)
        {
          case DeviceType.RelaySwitchModule when device is IRelaySwitchModule relay:
            var dbcChassinumbers = relay.NumberChassis;
            var dbc = (await SwitchingDevices.GetDevicesByNumberChassisAsync(dbcChassinumbers)).FirstOrDefault();
            await relay.SelfTestManager.StartSelfCheck(_messageService.GetCancellationToken(), part, _messageService, dbc);
            if (dbc != null)
            {
              await dbc.ConnectableManager.ResetAsync();
            }
            break;

          case DeviceType.SwitchingDevice when device is ISwitchingDevice switcher:
            await switcher.SelfTestManager.StartSelfCheck(_messageService.GetCancellationToken(), part, _messageService, switcher, meter);
            break;

          case DeviceType.PowerSourceModule when device is IPowerSourceModule mint:
            var numberChassis = mint.NumberChassis;
            var switcher1 = (await SwitchingDevices.GetDevicesByNumberChassisAsync(numberChassis)).FirstOrDefault();
            await mint.SelfTestManager.StartSelfCheck(_messageService.GetCancellationToken(), _messageService, part, switcher1, mint, meter);
            break;

          case DeviceType.BreakdownTester when device is IBreakdownTester breakdown:
            var numberBreakdown = breakdown.NumberChassis;
            var switcher2 = (await SwitchingDevices.GetDevicesByNumberChassisAsync(numberBreakdown)).FirstOrDefault();
            await breakdown.SelfTestManager.StartSelfCheck(_messageService.GetCancellationToken(), part, _messageService, breakdown, switcher2, meter);
            break;

          case DeviceType.FastMeter when device is IMultimeter multimeter:
            var numberMultimeter = multimeter.NumberChassis;
            var switcher3 = (await SwitchingDevices.GetDevicesByNumberChassisAsync(numberMultimeter)).FirstOrDefault();
            await multimeter.SelfTestManager.StartSelfCheck(_messageService.GetCancellationToken(), part, _messageService, switcher3, multimeter);
            break;
        }
      }
      else
      {
        await _messageService.ShowMessageAsync(new ShowMessageModel("Ошибка", message: "Не удалось получить устройство.", type: ShowMessageModel.MessageType.Error));
        return;
      }
    }
  }
}
