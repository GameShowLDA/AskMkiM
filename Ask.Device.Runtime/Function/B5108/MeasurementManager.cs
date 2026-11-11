using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Communication.Common.Threading;
using Ask.Device.Emulator;
using Ask.Device.ResponseProcessor.PowerSupply;
using System.Globalization;

namespace Ask.Device.Runtime.Function.B5108;

/// <summary>
/// Измеряет выходные параметры источника Б5-108.
/// </summary>
public sealed class MeasurementManager : IMeasurementManager
{
  private readonly Device.B5108 _device;

  /// <summary>
  /// Создаёт менеджер измерений источника.
  /// </summary>
  /// <param name="device">Источник питания Б5-108.</param>
  public MeasurementManager(Device.B5108 device) => _device = device ?? throw new ArgumentNullException(nameof(device));

  /// <inheritdoc />
  public Task<decimal> MeasureVoltageAsync(CancellationToken cancellationToken = default, IUserInteractionService? userMessageService = null)
    => MeasureAsync("MEAS:VOLT?", cancellationToken, userMessageService);

  /// <inheritdoc />
  public Task<decimal> MeasureCurrentAsync(CancellationToken cancellationToken = default, IUserInteractionService? userMessageService = null)
    => MeasureAsync("MEAS:CURR?", cancellationToken, userMessageService);

  private async Task<decimal> MeasureAsync(string command, CancellationToken token, IUserInteractionService? userMessageService)
  {
    using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
      token, userMessageService?.GetCancellationToken() ?? CancellationToken.None,
      EquipmentExecutionContext.CancellationToken);
    token = operationCancellation.Token;
    var protocol = DeviceProtocolEmulator.CreatePowerSupply(_device);
    using (await protocol.OperationLock.LockAsync(token))
      return PowerSupplyResponseProcessor.ParseNumber(await protocol.QueryAsync(command,
        timeout: _device.ConnectedProfile.Timeout, port: _device.ConnectedProfile.Port, cancellationToken: token));
  }
}
