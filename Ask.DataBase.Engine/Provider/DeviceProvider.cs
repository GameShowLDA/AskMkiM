using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.DataBase.Engine.Builder;

namespace Ask.DataBase.Engine.Provider;

/// <summary>
/// Провайдер устройств.
/// Отвечает за получение устройств из кэша или базы данных,
/// их создание и инициализацию.
/// </summary>
public class DeviceProvider
{
  private readonly IDeviceRepository _repository;
  private readonly DeviceCache _cache;

  /// <summary>
  /// Инициализирует новый экземпляр провайдера устройств.
  /// </summary>
  public DeviceProvider(IDeviceRepository repository, DeviceCache cache)
  {
    _repository = repository;
    _cache = cache;
  }

  /// <summary>
  /// Получает устройство по идентификатору.
  /// Сначала проверяет кэш, затем обращается к БД при необходимости.
  /// </summary>
  /// <typeparam name="T">Тип устройства.</typeparam>
  /// <param name="id">Идентификатор устройства.</param>
  /// <returns>Готовый экземпляр устройства.</returns>
  public async Task<T> GetAsync<T>(int id) where T : class, IDevice
  {
    if (_cache.TryGet(typeof(T), id, out var cached))
    {
      if (cached is T typed)
        return typed;

      throw new InvalidOperationException($"Device {id} has different type");
    }

    var dto = await _repository.GetByIdAsync(id)
      ?? throw new InvalidOperationException($"Device {id} not found");

    var device = DeviceBuilder.Build(dto);

    _cache.Set(typeof(T), id, device);

    if (device is not T result)
      throw new InvalidOperationException($"Device {id} is not {typeof(T).Name}");

    return result;
  }

  /// <summary>
  /// Принудительно обновляет устройство (игнорируя кэш).
  /// </summary>
  public async Task<T> ReloadAsync<T>(int id) where T : class, IDevice
  {
    _cache.Remove(typeof(T), id);
    return await GetAsync<T>(id);
  }
}
