using Ask.Core.Services.UI;
using Ask.Core.Services.Errors.Device;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Protocol.Messages.EntryPoints;

namespace Ask.Device.Application.FunctionAdapters.B5108;

/// <summary>
/// Добавляет прикладную обработку операций источника Б5-108.
/// </summary>
internal sealed class CurrentManagerAdapter : ICurrentManager
{
  private readonly Runtime.Device.B5108 _device;
  private readonly Runtime.Function.B5108.CurrentManager _manager;

  public CurrentManagerAdapter(Runtime.Device.B5108 device)
  {
    _device = device ?? throw new ArgumentNullException(nameof(device));
    if (device.CurrentManager is Runtime.Function.B5108.CurrentManager manager)
    {
      _manager = manager;
    }
    else
    {
      var current = device.CurrentManager;
      _manager = new Runtime.Function.B5108.CurrentManager(device)
      {
        MinCurrent = current.MinCurrent,
        MaxCurrent = current.MaxCurrent,
        CurrentStep = current.CurrentStep,
      };
    }
  }
  /// <inheritdoc />
  public decimal MinCurrent
  {
    get => _manager.MinCurrent;
    set => _manager.MinCurrent = value;
  }
  /// <inheritdoc />
  public decimal MaxCurrent
  {
    get => _manager.MaxCurrent;
    set => _manager.MaxCurrent = value;
  }
  /// <inheritdoc />
  public decimal CurrentStep
  {
    get => _manager.CurrentStep;
    set => _manager.CurrentStep = value;
  }

  /// <inheritdoc />
  public async Task SetCurrentAsync(decimal current, CancellationToken cancellationToken = default, IUserInteractionService? userMessageService = null)
  {
    await UserActionHelper.RunWithUserRepeatAsync(async () =>
    {
      try
      {
        await _manager.SetCurrentAsync(current, cancellationToken, userMessageService);
        await EquipmentMessages.PublishPowerSupplyOperationResultAsync(_device, "Установка тока", true,
          current.ToString(), userMessageService);
        return true;
      }
      catch (DeviceException error)
      {
        await EquipmentMessages.PublishPowerSupplyOperationResultAsync(_device, "Установка тока", false,
          error.Message, userMessageService);
        throw;
      }
    }, userMessageService, deviceTask: true);
  }

  /// <inheritdoc />
  public async Task<decimal> GetCurrentAsync(CancellationToken cancellationToken = default, IUserInteractionService? userMessageService = null)
  {
    return await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
    {
      try
      {
        decimal result = await _manager.GetCurrentAsync(cancellationToken, userMessageService);
        await EquipmentMessages.PublishPowerSupplyOperationResultAsync(_device, "Чтение уставки", true,
          result.ToString(), userMessageService);
        return result;
      }
      catch (DeviceException error)
      {
        await EquipmentMessages.PublishPowerSupplyOperationResultAsync(_device, "Чтение уставки", false,
          error.Message, userMessageService);
        throw;
      }
    }, static _ => true, userMessageService, deviceTask: true);
  }

}
