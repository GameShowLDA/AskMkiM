using Ask.Core.Services.Devices;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Device.Application.Composition;

/// <summary>
/// Регистрирует устройство при обращении к операциям подключения.
/// </summary>
internal sealed class EquipmentTrackingConnectable : IConnectable
{
  /// <summary>
  /// Устройство, обращение к которому регистрируется.
  /// </summary>
  private readonly IDevice _device;

  /// <summary>
  /// Компонент управления подключением устройства.
  /// </summary>
  private readonly IConnectable _inner;

  /// <summary>
  /// Создаёт компонент учёта обращений к устройству.
  /// </summary>
  /// <param name="device">Отслеживаемое устройство.</param>
  /// <param name="inner">Компонент управления подключением устройства.</param>
  public EquipmentTrackingConnectable(IDevice device, IConnectable inner)
  {
    _device = device ?? throw new ArgumentNullException(nameof(device));
    _inner = inner ?? throw new ArgumentNullException(nameof(inner));
  }

  /// <inheritdoc />
  public event Action IsReset
  {
    add => _inner.IsReset += value;
    remove => _inner.IsReset -= value;
  }

  /// <inheritdoc />
  public Task<(bool Connect, string Answer)> InitializeAsync(
    IUserInteractionService? userMessageService = null)
  {
    EquipmentUsageTracker.Register(_device);
    return _inner.InitializeAsync(userMessageService);
  }

  /// <inheritdoc />
  public Task<(bool Connect, string Answer)> ConnectAsync(
    IUserInteractionService? userMessageService = null)
  {
    EquipmentUsageTracker.Register(_device);
    return _inner.ConnectAsync(userMessageService);
  }

  /// <inheritdoc />
  public Task<bool> DisconnectAsync(IUserInteractionService? userMessageService = null)
  {
    EquipmentUsageTracker.Register(_device);
    return _inner.DisconnectAsync(userMessageService);
  }

  /// <inheritdoc />
  public Task<bool> ResetAsync(IUserInteractionService? userMessageService = null)
  {
    EquipmentUsageTracker.Register(_device);
    return _inner.ResetAsync(userMessageService);
  }
}
