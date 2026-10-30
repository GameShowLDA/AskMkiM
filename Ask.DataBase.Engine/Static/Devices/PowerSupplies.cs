using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply;

namespace Ask.DataBase.Engine.Static.Devices;

/// <summary>
/// Статический фасад для работы с внешними источниками питания.
/// Предоставляет типобезопасный доступ к операциям <see cref="DeviceRuntime"/>
/// для устройств типа <see cref="IPowerSupply"/>.
/// </summary>
/// <remarks>
/// Оборачивает универсальный движок устройств и не содержит
/// собственной бизнес-логики.
/// </remarks>
public static class PowerSupplies
{
  /// <summary>
  /// Получает внешний источник питания по его идентификатору.
  /// </summary>
  /// <param name="id">Уникальный идентификатор устройства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Найденное устройство или <c>null</c>, если устройство не существует.
  /// </returns>
  public static Task<IPowerSupply?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByIdAsync<IPowerSupply>(id, cancellationToken);

  /// <summary>
  /// Получает все внешние источники питания.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Список устройств. Если устройства отсутствуют — возвращается пустой список.
  /// </returns>
  public static Task<List<IPowerSupply>> GetAllAsync(CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetAllAsync<IPowerSupply>(cancellationToken);

  /// <summary>
  /// Получает внешний источник питания по его номеру.
  /// </summary>
  /// <param name="number">Номер устройства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Найденное устройство или <c>null</c>, если устройство не найдено.
  /// </returns>
  public static Task<IPowerSupply?> GetByNumberAsync(int number, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByNumberAsync<IPowerSupply>(number, cancellationToken);

  /// <summary>
  /// Возвращает источники питания выбранного тестера.
  /// </summary>
  /// <param name="numberChassis">Номер тестера.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>Источники выбранного тестера.</returns>
  public static Task<List<IPowerSupply>> GetDevicesByNumberChassisAsync(int numberChassis, CancellationToken cancellationToken = default)
    => DeviceRuntime.GetDevicesByNumberChassisAsync<IPowerSupply>(numberChassis, cancellationToken);

  /// <summary>
  /// Возвращает источник по номеру тестера и номеру устройства.
  /// </summary>
  /// <param name="numberChassis">Номер тестера.</param>
  /// <param name="number">Номер источника питания.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>Найденный источник или null.</returns>
  public static Task<IPowerSupply?> GetDeviceByNumberChassisAsync(int numberChassis, int number, CancellationToken cancellationToken = default)
    => DeviceRuntime.GetDeviceByNumberChassisAsync<IPowerSupply>(numberChassis, number, cancellationToken);

  /// <summary>
  /// Создаёт новый внешний источник питания.
  /// </summary>
  /// <param name="device">Экземпляр устройства для создания.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Созданный runtime-объект устройства с актуальными данными.
  /// </returns>
  public static Task<IPowerSupply> CreateAsync(IPowerSupply device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.CreateAsync(device, cancellationToken);

  /// <summary>
  /// Создаёт набор внешних источников питания.
  /// </summary>
  /// <param name="devices">Коллекция устройств для создания.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Список созданных runtime-объектов устройств с актуальными данными.
  /// </returns>
  public static Task<List<IPowerSupply>> CreateRangeAsync(IEnumerable<IPowerSupply> devices, CancellationToken cancellationToken = default) =>
    DeviceRuntime.CreateRangeAsync(devices, cancellationToken);

  /// <summary>
  /// Обновляет существующий внешний источник питания.
  /// </summary>
  /// <param name="device">Экземпляр устройства с обновлёнными данными.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Обновлённый runtime-объект устройства.
  /// </returns>
  public static Task<IPowerSupply> UpdateAsync(IPowerSupply device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.UpdateAsync(device, cancellationToken);

  /// <summary>
  /// Удаляет внешний источник питания.
  /// </summary>
  /// <param name="device">Экземпляр устройства для удаления.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// <c>true</c>, если устройство успешно удалено; иначе <c>false</c>.
  /// </returns>
  public static Task<bool> DeleteAsync(IPowerSupply device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteAsync(device, cancellationToken);

  /// <summary>
  /// Удаляет внешний источник питания по идентификатору.
  /// </summary>
  /// <param name="id">Уникальный идентификатор устройства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// <c>true</c>, если устройство найдено и удалено; иначе <c>false</c>.
  /// </returns>
  public static Task<bool> DeleteByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteByIdAsync<IPowerSupply>(id, cancellationToken);

  /// <summary>
  /// Удаляет все устройства из таблицы данных.
  /// </summary>
  public static Task<bool> DeleteAllAsync(CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteAllAsync<IPowerSupply>(cancellationToken);

  /// <summary>
  /// Создаёт runtime-объект внешнего источника питания на основе DTO.
  /// </summary>
  /// <param name="dto">DTO, содержащий данные устройства.</param>
  /// <returns>
  /// Готовый runtime-объект внешнего источника питания.
  /// </returns>
  public static IPowerSupply Build(DeviceDto dto) =>
    DeviceRuntime.Build<IPowerSupply>(dto);
}