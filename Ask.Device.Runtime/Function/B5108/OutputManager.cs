using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Communication.Common.Threading;
using Ask.Device.Emulator;
using Ask.Device.ResponseProcessor.PowerSupply;
using System.Globalization;

namespace Ask.Device.Runtime.Function.B5108;

/// <summary>
/// Управляет выходом источника Б5-108.
/// </summary>
public sealed class OutputManager : IOutputManager
{
  private readonly Device.B5108 _device;

  /// <summary>
  /// Создаёт менеджер выхода источника.
  /// </summary>
  /// <param name="device">Источник питания Б5-108.</param>
  public OutputManager(Device.B5108 device) => _device = device ?? throw new ArgumentNullException(nameof(device));

  /// <inheritdoc />
  public async Task SetOutputStateAsync(bool enabled, CancellationToken cancellationToken = default, IUserInteractionService? userMessageService = null)
  {
    using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
      cancellationToken, userMessageService?.GetCancellationToken() ?? CancellationToken.None,
      EquipmentExecutionContext.CancellationToken);
    cancellationToken = operationCancellation.Token;
    var protocol = DeviceProtocolEmulator.CreatePowerSupply(_device);
    using (await protocol.OperationLock.LockAsync(cancellationToken))
    {
      await protocol.QueryAsync(enabled ? "OUTP ON" : "OUTP OFF", port: _device.ConnectedProfile.Port, cancellationToken: cancellationToken);
      PowerSupplyResponseProcessor.EnsureNoError(await protocol.QueryAsync("SYST:ERR?",
        timeout: _device.ConnectedProfile.Timeout, port: _device.ConnectedProfile.Port, cancellationToken: cancellationToken));
      PowerSupplyResponseProcessor.EnsureSetpoint(await protocol.QueryAsync("OUTP?",
        timeout: _device.ConnectedProfile.Timeout, port: _device.ConnectedProfile.Port, cancellationToken: cancellationToken), enabled ? 1 : 0);
    }
  }

  /// <inheritdoc />
  public async Task<bool> GetOutputStateAsync(CancellationToken cancellationToken = default, IUserInteractionService? userMessageService = null)
  {
    using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
      cancellationToken, userMessageService?.GetCancellationToken() ?? CancellationToken.None,
      EquipmentExecutionContext.CancellationToken);
    cancellationToken = operationCancellation.Token;
    var protocol = DeviceProtocolEmulator.CreatePowerSupply(_device);
    using (await protocol.OperationLock.LockAsync(cancellationToken))
      return PowerSupplyResponseProcessor.ParseOutputState(await protocol.QueryAsync("OUTP?",
        timeout: _device.ConnectedProfile.Timeout, port: _device.ConnectedProfile.Port, cancellationToken: cancellationToken));
  }
}
