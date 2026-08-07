using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.DataBase.Engine.Contracts;
using Ask.DataBase.Engine.Services;

namespace Ask.DataBase.Engine.Static;

/// <summary>
/// Универсальный статический вход в движок устройств.
/// Предоставляет доступ к операциям <see cref="IDeviceEngine"/> без необходимости
/// явного создания и управления экземплярами сервисов.
///
/// Используется как фасад верхнего уровня для получения, создания,
/// обновления и удаления runtime-объектов устройств.
/// </summary>
/// <remarks>
/// Предназначен для упрощённого доступа к инфраструктуре работы с устройствами
/// (например, в UI или прикладном коде).
///
/// Внутри использует единый экземпляр <see cref="IDeviceEngine"/>,
/// что подразумевает наличие общего состояния (например, кэширования).
/// </remarks>
public static class DeviceRuntime
{
  private static readonly IDeviceEngine Engine = new DeviceEngine();

  /// <summary>
  /// Получает устройство по его идентификатору.
  /// </summary>
  public static Task<TDevice?> GetByIdAsync<TDevice>(int id, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice =>
    Engine.GetByIdAsync<TDevice>(id, cancellationToken);

  /// <summary>
  /// Получает все устройства указанного типа.
  /// </summary>
  public static Task<List<TDevice>> GetAllAsync<TDevice>(CancellationToken cancellationToken = default)
    where TDevice : class, IDevice =>
    Engine.GetAllAsync<TDevice>(cancellationToken);

  /// <summary>
  /// Получает устройство по его номеру.
  /// </summary>
  public static Task<TDevice?> GetByNumberAsync<TDevice>(int number, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice =>
    Engine.GetByNumberAsync<TDevice>(number, cancellationToken);

  /// <summary>
  /// Получает список устройств, относящихся к указанному шасси.
  /// </summary>
  public static Task<List<TDevice>> GetDevicesByNumberChassisAsync<TDevice>(
    int numberChassis,
    CancellationToken cancellationToken = default)
    where TDevice : class, IDevice =>
    Engine.GetDevicesByNumberChassisAsync<TDevice>(numberChassis, cancellationToken);

  /// <summary>
  /// Получает устройство по номеру шасси и номеру устройства.
  /// </summary>
  public static Task<TDevice?> GetDeviceByNumberChassisAsync<TDevice>(
    int numberChassis,
    int number,
    CancellationToken cancellationToken = default)
    where TDevice : class, IDevice =>
    Engine.GetDeviceByNumberChassisAsync<TDevice>(numberChassis, number, cancellationToken);

  /// <summary>
  /// Принудительно перезагружает устройство по идентификатору,
  /// игнорируя кэш и создавая новый runtime-объект.
  /// </summary>
  public static Task<TDevice?> ReloadByIdAsync<TDevice>(int id, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice =>
    Engine.ReloadByIdAsync<TDevice>(id, cancellationToken);

  /// <summary>
  /// Создаёт новое устройство.
  /// </summary>
  public static async Task<TDevice> CreateAsync<TDevice>(TDevice device, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice
  {
    var created = await Engine.CreateAsync(device, cancellationToken);
    PublishConfigurationChanged<TDevice>(DeviceConfigurationEvents.ChangeKind.Created, created.Id);
    return created;
  }

  /// <summary>
  /// Создаёт набор устройств.
  /// </summary>
  /// <typeparam name="TDevice">Тип устройств.</typeparam>
  /// <param name="devices">Коллекция устройств для создания.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Список созданных runtime-объектов устройств с актуальными данными.
  /// </returns>
  public static async Task<List<TDevice>> CreateRangeAsync<TDevice>(IEnumerable<TDevice> devices, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice
  {
    var created = await Engine.CreateRangeAsync(devices, cancellationToken);
    if (created.Count > 0)
    {
      PublishConfigurationChanged<TDevice>(DeviceConfigurationEvents.ChangeKind.Created);
    }

    return created;
  }

  /// <summary>
  /// Обновляет существующее устройство.
  /// </summary>
  public static async Task<TDevice> UpdateAsync<TDevice>(TDevice device, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice
  {
    var updated = await Engine.UpdateAsync(device, cancellationToken);
    PublishConfigurationChanged<TDevice>(DeviceConfigurationEvents.ChangeKind.Updated, updated.Id);
    return updated;
  }

  /// <summary>
  /// Удаляет устройство.
  /// </summary>
  public static Task<bool> DeleteAsync<TDevice>(TDevice device, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice
  {
    ArgumentNullException.ThrowIfNull(device);
    return DeleteByIdAsync<TDevice>(device.Id, cancellationToken);
  }

  /// <summary>
  /// Удаляет устройство по идентификатору.
  /// </summary>
  public static async Task<bool> DeleteByIdAsync<TDevice>(int id, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice
  {
    bool deleted = await Engine.DeleteByIdAsync<TDevice>(id, cancellationToken);
    if (deleted)
    {
      PublishConfigurationChanged<TDevice>(DeviceConfigurationEvents.ChangeKind.Deleted, id);
    }

    return deleted;
  }

  /// <summary>
  /// Удаляет все устройства из таблицы данных.
  /// </summary>
  public static async Task<bool> DeleteAllAsync<TDevice>(CancellationToken cancellationToken = default)
    where TDevice : class, IDevice
  {
    bool deleted = await Engine.DeleteAllAsync<TDevice>(cancellationToken);
    if (deleted)
    {
      PublishConfigurationChanged<TDevice>(DeviceConfigurationEvents.ChangeKind.Deleted);
    }

    return deleted;
  }

  /// <summary>
  /// Создаёт runtime-объект устройства на основе DTO.
  /// </summary>
  /// <param name="dto">DTO, содержащий данные устройства.</param>
  /// <typeparam name="TDevice">Тип устройства.</typeparam>
  /// <returns>
  /// Готовый runtime-объект устройства.
  /// </returns>
  public static TDevice Build<TDevice>(DeviceDto dto)
    where TDevice : class, IDevice =>
    Engine.Build<TDevice>(dto);

  /// <summary>
  /// Очищает внутренний кэш устройств.
  /// </summary>
  public static void ClearCache() => Engine.ClearCache();

  private static void PublishConfigurationChanged<TDevice>(
    DeviceConfigurationEvents.ChangeKind kind,
    int? deviceId = null)
    where TDevice : class, IDevice
  {
    EventAggregator.Publish(new DeviceConfigurationEvents.Changed(typeof(TDevice), kind, deviceId));
  }
}
