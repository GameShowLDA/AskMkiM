using Ask.Core.Services.UI;
using Ask.Core.Services.Errors.Device;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Protocol.Messages.EntryPoints;

namespace Ask.Device.Application.FunctionAdapters.B5108;

/// <summary>
/// Добавляет прикладную обработку операций источника Б5-108.
/// </summary>
internal sealed class MeasurementManagerAdapter : IMeasurementManager
{
  private readonly Runtime.Device.B5108 _device;
  private readonly Runtime.Function.B5108.MeasurementManager _manager;

  public MeasurementManagerAdapter(Runtime.Device.B5108 device)
  {
    _device = device ?? throw new ArgumentNullException(nameof(device));
    _manager = new Runtime.Function.B5108.MeasurementManager(device);
  }

  /// <inheritdoc />
  public async Task<decimal> MeasureVoltageAsync(CancellationToken cancellationToken = default, IUserInteractionService? userMessageService = null)
  {
    return await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
    {
      try
      {
        decimal result = await _manager.MeasureVoltageAsync(cancellationToken, userMessageService);
        await EquipmentMessages.PublishPowerSupplyOperationResultAsync(_device, "Измерение напряжения", true,
          result.ToString(), userMessageService);
        return result;
      }
      catch (DeviceException error)
      {
        await EquipmentMessages.PublishPowerSupplyOperationResultAsync(_device, "Измерение напряжения", false,
          error.Message, userMessageService);
        throw;
      }
    }, static _ => true, userMessageService, deviceTask: true);
  }

  /// <inheritdoc />
  public async Task<decimal> MeasureCurrentAsync(CancellationToken cancellationToken = default, IUserInteractionService? userMessageService = null)
  {
    return await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
    {
      try
      {
        decimal result = await _manager.MeasureCurrentAsync(cancellationToken, userMessageService);
        await EquipmentMessages.PublishPowerSupplyOperationResultAsync(_device, "Измерение тока", true,
          result.ToString(), userMessageService);
        return result;
      }
      catch (DeviceException error)
      {
        await EquipmentMessages.PublishPowerSupplyOperationResultAsync(_device, "Измерение тока", false,
          error.Message, userMessageService);
        throw;
      }
    }, static _ => true, userMessageService, deviceTask: true);
  }

}
