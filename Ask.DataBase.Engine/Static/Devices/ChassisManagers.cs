using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Rack;

namespace Ask.DataBase.Engine.Static.Devices;

/// <summary>
/// Статический фасад для работы с менеджерами шасси.
/// Предоставляет типобезопасный доступ к операциям <see cref="DeviceRuntime"/>
/// для устройств типа <see cref="IChassisManager"/>.
/// </summary>
/// <remarks>
/// Является обёрткой над универсальным движком устройств и не содержит
/// собственной бизнес-логики.
/// </remarks>
public static class ChassisManagers
{
  /// <summary>
  /// Получает менеджер шасси по его идентификатору.
  /// </summary>
  /// <param name="id">Уникальный идентификатор устройства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Найденное устройство или <c>null</c>, если устройство не существует.
  /// </returns>
  public static Task<IChassisManager?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByIdAsync<IChassisManager>(id, cancellationToken);

  /// <summary>
  /// Получает все менеджеры шасси.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Список устройств. Если устройства отсутствуют — возвращается пустой список.
  /// </returns>
  public static Task<List<IChassisManager>> GetAllAsync(CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetAllAsync<IChassisManager>(cancellationToken);

  /// <summary>
  /// Получает менеджер шасси по его номеру.
  /// </summary>
  /// <param name="number">Номер устройства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Найденное устройство или <c>null</c>, если устройство не найдено.
  /// </returns>
  public static Task<IChassisManager?> GetByNumberAsync(int number, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByNumberAsync<IChassisManager>(number, cancellationToken);

  /// <summary>
  /// Создаёт новый менеджер шасси.
  /// </summary>
  /// <param name="device">Экземпляр устройства для создания.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Созданный runtime-объект устройства с актуальными данными.
  /// </returns>
  public static Task<IChassisManager> CreateAsync(IChassisManager device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.CreateAsync(device, cancellationToken);

  /// <summary>
  /// Создаёт набор стоек.
  /// </summary>
  /// <param name="devices">Коллекция устройств для создания.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Список созданных runtime-объектов устройств с актуальными данными.
  /// </returns>
  public static Task<List<IChassisManager>> CreateRangeAsync(IEnumerable<IChassisManager> devices, CancellationToken cancellationToken = default) =>
    DeviceRuntime.CreateRangeAsync(devices, cancellationToken);

  /// <summary>
  /// Обновляет существующий менеджер шасси.
  /// </summary>
  /// <param name="device">Экземпляр устройства с обновлёнными данными.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Обновлённый runtime-объект устройства.
  /// </returns>
  public static Task<IChassisManager> UpdateAsync(IChassisManager device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.UpdateAsync(device, cancellationToken);

  /// <summary>
  /// Удаляет менеджер шасси.
  /// </summary>
  /// <param name="device">Экземпляр устройства для удаления.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// <c>true</c>, если устройство успешно удалено; иначе <c>false</c>.
  /// </returns>
  public static Task<bool> DeleteAsync(IChassisManager device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteAsync(device, cancellationToken);

  /// <summary>
  /// Удаляет менеджер шасси по идентификатору.
  /// </summary>
  /// <param name="id">Уникальный идентификатор устройства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// <c>true</c>, если устройство найдено и удалено; иначе <c>false</c>.
  /// </returns>
  public static Task<bool> DeleteByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteByIdAsync<IChassisManager>(id, cancellationToken);

  /// <summary>
  /// Удаляет все устройства из таблицы данных.
  /// </summary>
  public static Task<bool> DeleteAllAsync(CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteAllAsync<IChassisManager>(cancellationToken);

  /// <summary>
  /// Создаёт runtime-объект менеджера шасси  на основе DTO.
  /// </summary>
  /// <param name="dto">DTO, содержащий данные устройства.</param>
  /// <returns>
  /// Готовый runtime-объект менеджера шасси.
  /// </returns>
  public static IChassisManager Build(DeviceDto dto) =>
    DeviceRuntime.Build<IChassisManager>(dto);
}