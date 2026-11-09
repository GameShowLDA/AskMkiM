using Ask.Core.Services.UI;
using Ask.Core.Services.Errors.Device;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Protocol.Messages.EntryPoints;

namespace Ask.Device.Application.FunctionAdapters.B5108;

/// <summary>
/// Добавляет прикладную обработку операций источника Б5-108.
/// </summary>
internal sealed class VoltageManagerAdapter : IVoltageManager
{
  private readonly Runtime.Device.B5108 _device;
  private readonly Runtime.Function.B5108.VoltageManager _manager;

  public VoltageManagerAdapter(Runtime.Device.B5108 device)
  {
    _device = device ?? throw new ArgumentNullException(nameof(device));
    _manager = new Runtime.Function.B5108.VoltageManager(device);
  }
  /// <inheritdoc />
  public decimal MinVoltage => _manager.MinVoltage;
  /// <inheritdoc />
  public decimal MaxVoltage => _manager.MaxVoltage;
  /// <inheritdoc />
  public decimal VoltageStep => _manager.VoltageStep;

  /// <inheritdoc />
  public async Task SetVoltageAsync(decimal voltage, CancellationToken cancellationToken = default, IUserInteractionService? userMessageService = null)
  {
    await UserActionHelper.RunWithUserRepeatAsync(async () =>
    {
      try
      {
        await _manager.SetVoltageAsync(voltage, cancellationToken, userMessageService);
        await EquipmentMessages.PublishPowerSupplyOperationResultAsync(_device, "Установка напряжения", true,
          voltage.ToString(), userMessageService);
        return true;
      }
      catch (DeviceException error)
      {
        await EquipmentMessages.PublishPowerSupplyOperationResultAsync(_device, "Установка напряжения", false,
          error.Message, userMessageService);
        throw;
      }
    }, userMessageService, deviceTask: true);
  }

  /// <inheritdoc />
  public async Task<decimal> GetVoltageAsync(CancellationToken cancellationToken = default, IUserInteractionService? userMessageService = null)
  {
    return await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
    {
      try
      {
        decimal result = await _manager.GetVoltageAsync(cancellationToken, userMessageService);
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
