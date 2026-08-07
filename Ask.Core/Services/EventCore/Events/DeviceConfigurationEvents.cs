using Ask.Core.Shared.Interfaces.EventInterfaces;

namespace Ask.Core.Services.EventCore.Events;

/// <summary>
/// События изменения сохранённой конфигурации оборудования.
/// </summary>
public static class DeviceConfigurationEvents
{
  /// <summary>
  /// Вид изменения конфигурации оборудования.
  /// </summary>
  public enum ChangeKind
  {
    Created,
    Updated,
    Deleted,
    Replaced,
  }

  /// <summary>
  /// Уведомляет подписчиков об успешном изменении сохранённого устройства
  /// или полной замене конфигурации.
  /// </summary>
  public sealed class Changed : IEvent
  {
    /// <summary>
    /// Тип интерфейса изменённого устройства. Равен <see langword="null"/>
    /// при полной замене конфигурации.
    /// </summary>
    public Type? DeviceType { get; }

    /// <summary>
    /// Идентификатор изменённого устройства, если изменение относится к одной записи.
    /// </summary>
    public int? DeviceId { get; }

    /// <summary>
    /// Вид изменения.
    /// </summary>
    public ChangeKind Kind { get; }

    public Changed(Type? deviceType, ChangeKind kind, int? deviceId = null)
    {
      DeviceType = deviceType;
      DeviceId = deviceId;
      Kind = kind;
    }
  }
}
