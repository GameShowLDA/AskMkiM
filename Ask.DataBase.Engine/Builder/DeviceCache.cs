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
  /// <summary>
  /// Внутреннее хранилище устройств по идентификатору.
  /// </summary>
  private readonly ConcurrentDictionary<int, IDevice> _cache = new();

  /// <summary>
  /// Пытается получить устройство из кэша.
  /// </summary>
  /// <param name="id">Идентификатор устройства.</param>
  /// <param name="device">Найденное устройство.</param>
  /// <returns>True, если устройство найдено.</returns>
  public bool TryGet(int id, out IDevice? device)
  {
    return _cache.TryGetValue(id, out device);
  }

  /// <summary>
  /// Добавляет устройство в кэш.
  /// Если устройство уже существует — перезаписывает его.
  /// </summary>
  public void Set(int id, IDevice device)
  {
    _cache[id] = device;
  }

  /// <summary>
  /// Удаляет устройство из кэша.
  /// </summary>
  public void Remove(int id)
  {
    _cache.TryRemove(id, out _);
  }

  /// <summary>
  /// Очищает весь кэш устройств.
  /// </summary>
  public void Clear()
  {
    _cache.Clear();
  }
}