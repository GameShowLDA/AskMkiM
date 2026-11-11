using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Communication.Common.Threading;
using Ask.Device.Emulator;
using Ask.Device.ResponseProcessor.PowerSupply;
using System.Globalization;

namespace Ask.Device.Runtime.Function.B5108;

/// <summary>
/// Управляет ограничением тока источника Б5-108.
/// </summary>
public sealed class CurrentManager : ICurrentManager
{
  private readonly Device.B5108 _device;

  /// <summary>
  /// Создаёт менеджер источника питания.
  /// </summary>
  /// <param name="device">Источник питания Б5-108.</param>
  public CurrentManager(Device.B5108 device) => _device = device ?? throw new ArgumentNullException(nameof(device));

  /// <inheritdoc />
  public decimal MinCurrent { get; set; } = 0m;
  /// <inheritdoc />
  public decimal MaxCurrent { get; set; } = 5m;
  /// <inheritdoc />
  public decimal CurrentStep { get; set; } = 0.001m;

  /// <inheritdoc />
  public async Task SetCurrentAsync(decimal current, CancellationToken cancellationToken = default, IUserInteractionService? userMessageService = null)
  {
    using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
      cancellationToken, userMessageService?.GetCancellationToken() ?? CancellationToken.None,
      EquipmentExecutionContext.CancellationToken);
    cancellationToken = operationCancellation.Token;
    decimal value = current;
    if (value < MinCurrent || value > MaxCurrent || (value - MinCurrent) % CurrentStep != 0)
      throw new ArgumentOutOfRangeException(nameof(current), value, "Недопустимый диапазон или шаг уставки.");
    var protocol = DeviceProtocolEmulator.CreatePowerSupply(_device);
    using (await protocol.OperationLock.LockAsync(cancellationToken))
    {
      decimal limit = PowerSupplyResponseProcessor.ParseNumber(await protocol.QueryAsync("CURR:LIM?",
        timeout: _device.ConnectedProfile.Timeout, port: _device.ConnectedProfile.Port, cancellationToken: cancellationToken));
      if (value > limit) throw new ArgumentOutOfRangeException(nameof(current), value, $"Превышен текущий LIM: {limit}.");
      await protocol.QueryAsync("CURR " + value.ToString(CultureInfo.InvariantCulture),
        port: _device.ConnectedProfile.Port, cancellationToken: cancellationToken);
      PowerSupplyResponseProcessor.EnsureNoError(await protocol.QueryAsync("SYST:ERR?",
        timeout: _device.ConnectedProfile.Timeout, port: _device.ConnectedProfile.Port, cancellationToken: cancellationToken));
      PowerSupplyResponseProcessor.EnsureSetpoint(await protocol.QueryAsync("CURR?",
        timeout: _device.ConnectedProfile.Timeout, port: _device.ConnectedProfile.Port, cancellationToken: cancellationToken), value);
    }
  }

  /// <inheritdoc />
  public async Task<decimal> GetCurrentAsync(CancellationToken cancellationToken = default, IUserInteractionService? userMessageService = null)
  {
    using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
      cancellationToken, userMessageService?.GetCancellationToken() ?? CancellationToken.None,
      EquipmentExecutionContext.CancellationToken);
    cancellationToken = operationCancellation.Token;
    var protocol = DeviceProtocolEmulator.CreatePowerSupply(_device);
    using (await protocol.OperationLock.LockAsync(cancellationToken))
      return PowerSupplyResponseProcessor.ParseNumber(await protocol.QueryAsync("CURR?",
        timeout: _device.ConnectedProfile.Timeout, port: _device.ConnectedProfile.Port, cancellationToken: cancellationToken));
  }
}
