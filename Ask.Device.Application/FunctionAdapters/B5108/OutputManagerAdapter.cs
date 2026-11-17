using Ask.Core.Services.UI;
using Ask.Core.Services.Errors.Device;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Protocol.Messages.EntryPoints;

namespace Ask.Device.Application.FunctionAdapters.B5108;

/// <summary>
/// Добавляет прикладную обработку операций источника Б5-108.
/// </summary>
internal sealed class OutputManagerAdapter : IOutputManager
{
  private readonly Runtime.Device.B5108 _device;
  private readonly Runtime.Function.B5108.OutputManager _manager;

  public OutputManagerAdapter(Runtime.Device.B5108 device)
  {
    _device = device ?? throw new ArgumentNullException(nameof(device));
    _manager = new Runtime.Function.B5108.OutputManager(device);
  }

  /// <inheritdoc />
  public async Task SetOutputStateAsync(bool enabled, CancellationToken cancellationToken = default, IUserInteractionService? userMessageService = null)
  {
    await UserActionHelper.RunWithUserRepeatAsync(async () =>
    {
      try
      {
        await _manager.SetOutputStateAsync(enabled, cancellationToken, userMessageService);
        await EquipmentMessages.PublishPowerSupplyOperationResultAsync(_device, "Состояние выхода", true,
          enabled.ToString(), userMessageService);
        return true;
      }
      catch (DeviceException error)
      {
        await EquipmentMessages.PublishPowerSupplyOperationResultAsync(_device, "Состояние выхода", false,
          error.Message, userMessageService);
        throw;
      }
    }, userMessageService, deviceTask: true);
  }

  /// <inheritdoc />
  public async Task<bool> GetOutputStateAsync(CancellationToken cancellationToken = default, IUserInteractionService? userMessageService = null)
  {
    return await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
    {
      try
      {
        bool result = await _manager.GetOutputStateAsync(cancellationToken, userMessageService);
        await EquipmentMessages.PublishPowerSupplyOperationResultAsync(_device, "Чтение состояния выхода", true,
          result.ToString(), userMessageService);
        return result;
      }
      catch (DeviceException error)
      {
        await EquipmentMessages.PublishPowerSupplyOperationResultAsync(_device, "Чтение состояния выхода", false,
          error.Message, userMessageService);
        throw;
      }
    }, static _ => true, userMessageService, deviceTask: true);
  }

}
