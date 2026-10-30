using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Communication.Common.Threading;
using Ask.Device.Emulator;
using Ask.Device.ResponseProcessor.PowerSupply;
using System.Globalization;

namespace Ask.Device.Runtime.Function.B5108;

/// <summary>
/// Управляет напряжением источника Б5-108.
/// </summary>
public sealed class VoltageManager : IVoltageManager
{
  private readonly Device.B5108 _device;

  /// <summary>
  /// Создаёт менеджер источника питания.
  /// </summary>
  /// <param name="device">Источник питания Б5-108.</param>
  public VoltageManager(Device.B5108 device) => _device = device ?? throw new ArgumentNullException(nameof(device));

  /// <inheritdoc />
  public decimal MinVoltage => 0;
  /// <inheritdoc />
  public decimal MaxVoltage => 150;
  /// <inheritdoc />
  public decimal VoltageStep => 0.1m;

  /// <inheritdoc />
  public async Task SetVoltageAsync(decimal voltage, CancellationToken cancellationToken = default, IUserInteractionService? userMessageService = null)
  {
    using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
      cancellationToken, userMessageService?.GetCancellationToken() ?? CancellationToken.None,
      EquipmentExecutionContext.CancellationToken);
    cancellationToken = operationCancellation.Token;
    decimal value = voltage;
    if (value < MinVoltage || value > MaxVoltage || (value - MinVoltage) % VoltageStep != 0)
      throw new ArgumentOutOfRangeException(nameof(voltage), value, "Недопустимый диапазон или шаг уставки.");
    var protocol = DeviceProtocolEmulator.CreatePowerSupply(_device);
    using (await protocol.OperationLock.LockAsync(cancellationToken))
    {
      decimal limit = PowerSupplyResponseProcessor.ParseNumber(await protocol.QueryAsync("VOLT:LIM?",
        timeout: _device.ConnectedProfile.Timeout, port: _device.ConnectedProfile.Port, cancellationToken: cancellationToken));
      if (value > limit) throw new ArgumentOutOfRangeException(nameof(voltage), value, $"Превышен текущий LIM: {limit}.");
      await protocol.QueryAsync("VOLT " + value.ToString(CultureInfo.InvariantCulture),
        port: _device.ConnectedProfile.Port, cancellationToken: cancellationToken);
      PowerSupplyResponseProcessor.EnsureNoError(await protocol.QueryAsync("SYST:ERR?",
        timeout: _device.ConnectedProfile.Timeout, port: _device.ConnectedProfile.Port, cancellationToken: cancellationToken));
      PowerSupplyResponseProcessor.EnsureSetpoint(await protocol.QueryAsync("VOLT?",
        timeout: _device.ConnectedProfile.Timeout, port: _device.ConnectedProfile.Port, cancellationToken: cancellationToken), value);
    }
  }

  /// <inheritdoc />
  public async Task<decimal> GetVoltageAsync(CancellationToken cancellationToken = default, IUserInteractionService? userMessageService = null)
  {
    using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
      cancellationToken, userMessageService?.GetCancellationToken() ?? CancellationToken.None,
      EquipmentExecutionContext.CancellationToken);
    cancellationToken = operationCancellation.Token;
    var protocol = DeviceProtocolEmulator.CreatePowerSupply(_device);
    using (await protocol.OperationLock.LockAsync(cancellationToken))
      return PowerSupplyResponseProcessor.ParseNumber(await protocol.QueryAsync("VOLT?",
        timeout: _device.ConnectedProfile.Timeout, port: _device.ConnectedProfile.Port, cancellationToken: cancellationToken));
  }
}
