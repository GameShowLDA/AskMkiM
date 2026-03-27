using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using System.Collections.Concurrent;

namespace Ask.DataBase.Engine.Builder;

/// <summary>
/// Потокобезопасный кэш устройств.
/// Хранит созданные экземпляры устройств для повторного использования
/// и предотвращает повторное создание и загрузку из БД.
/// </summary>
public class DeviceCache
{
  private readonly record struct DeviceCacheKey(Type DeviceType, int Id);

  /// <summary>
  /// Внутреннее хранилище устройств по идентификатору.
  /// </summary>
  private readonly ConcurrentDictionary<DeviceCacheKey, IDevice> _cache = new();

  /// <summary>
  /// Пытается получить устройство из кэша.
  /// </summary>
  /// <param name="id">Идентификатор устройства.</param>
  /// <param name="device">Найденное устройство.</param>
  /// <returns>True, если устройство найдено.</returns>
  public bool TryGet(int id, out IDevice? device)
  {
    return TryGet(typeof(IDevice), id, out device);
  }

  /// <summary>
  /// Пытается получить устройство из кэша по типу и идентификатору.
  /// </summary>
  public bool TryGet(Type deviceType, int id, out IDevice? device)
  {
    ArgumentNullException.ThrowIfNull(deviceType);
    return _cache.TryGetValue(new DeviceCacheKey(deviceType, id), out device);
  }

  /// <summary>
  /// Добавляет устройство в кэш.
  /// Если устройство уже существует — перезаписывает его.
  /// </summary>
  public void Set(int id, IDevice device)
  {
    Set(typeof(IDevice), id, device);
  }

  /// <summary>
  /// Добавляет устройство в кэш по типу и идентификатору.
  /// Если устройство уже существует — перезаписывает его.
  /// </summary>
  public void Set(Type deviceType, int id, IDevice device)
  {
    ArgumentNullException.ThrowIfNull(deviceType);
    ArgumentNullException.ThrowIfNull(device);
    _cache[new DeviceCacheKey(deviceType, id)] = device;
  }

  /// <summary>
  /// Удаляет устройство из кэша.
  /// </summary>
  public void Remove(int id)
  {
    Remove(typeof(IDevice), id);
  }

  /// <summary>
  /// Удаляет устройство из кэша по типу и идентификатору.
  /// </summary>
  public void Remove(Type deviceType, int id)
  {
    ArgumentNullException.ThrowIfNull(deviceType);
    _cache.TryRemove(new DeviceCacheKey(deviceType, id), out _);
  }

  /// <summary>
  /// Очищает весь кэш устройств.
  /// </summary>
  public void Clear()
  {
    _cache.Clear();
  }
}
