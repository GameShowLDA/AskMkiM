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
    if (device.VoltageManager is Runtime.Function.B5108.VoltageManager manager)
    {
      _manager = manager;
    }
    else
    {
      var current = device.VoltageManager;
      _manager = new Runtime.Function.B5108.VoltageManager(device)
      {
        MinVoltage = current.MinVoltage,
        MaxVoltage = current.MaxVoltage,
        VoltageStep = current.VoltageStep,
      };
    }
  }
  /// <inheritdoc />
  public decimal MinVoltage
  {
    get => _manager.MinVoltage;
    set => _manager.MinVoltage = value;
  }
  /// <inheritdoc />
  public decimal MaxVoltage
  {
    get => _manager.MaxVoltage;
    set => _manager.MaxVoltage = value;
  }
  /// <inheritdoc />
  public decimal VoltageStep
  {
    get => _manager.VoltageStep;
    set => _manager.VoltageStep = value;
  }

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
