using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;

namespace Ask.DataBase.Engine.Static.Devices;

/// <summary>
/// Статический фасад для работы с модулями источников питания.
/// Предоставляет типобезопасный доступ к операциям <see cref="DeviceRuntime"/>
/// для устройств типа <see cref="IPowerSourceModule"/>.
/// </summary>
/// <remarks>
/// Является обёрткой над универсальным движком устройств и не содержит
/// собственной бизнес-логики.
/// </remarks>
public static class PowerSourceModules
{
  /// <summary>
  /// Получает модуль источника питания по его идентификатору.
  /// </summary>
  /// <param name="id">Уникальный идентификатор устройства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Найденное устройство или <c>null</c>, если устройство не существует.
  /// </returns>
  public static Task<IPowerSourceModule?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByIdAsync<IPowerSourceModule>(id, cancellationToken);

  /// <summary>
  /// Получает все модули источников питания.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Список устройств. Если устройства отсутствуют — возвращается пустой список.
  /// </returns>
  public static Task<List<IPowerSourceModule>> GetAllAsync(CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetAllAsync<IPowerSourceModule>(cancellationToken);

  /// <summary>
  /// Получает модуль источника питания по его номеру.
  /// </summary>
  /// <param name="number">Номер устройства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Найденное устройство или <c>null</c>, если устройство не найдено.
  /// </returns>
  public static Task<IPowerSourceModule?> GetByNumberAsync(int number, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByNumberAsync<IPowerSourceModule>(number, cancellationToken);

  /// <summary>
  /// Получает список модулей источников питания, относящихся к указанному шасси.
  /// </summary>
  /// <param name="numberChassis">Номер шасси.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Список устройств. Если устройства отсутствуют — возвращается пустой список.
  /// </returns>
  public static Task<List<IPowerSourceModule>> GetDevicesByNumberChassisAsync(
    int numberChassis,
    CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetDevicesByNumberChassisAsync<IPowerSourceModule>(numberChassis, cancellationToken);

  /// <summary>
  /// Получает модуль источника питания по номеру шасси и номеру устройства.
  /// </summary>
  /// <param name="numberChassis">Номер шасси.</param>
  /// <param name="number">Номер устройства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Найденное устройство или <c>null</c>, если соответствующее устройство не найдено.
  /// </returns>
  public static Task<IPowerSourceModule?> GetDeviceByNumberChassisAsync(
    int numberChassis,
    int number,
    CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetDeviceByNumberChassisAsync<IPowerSourceModule>(numberChassis, number, cancellationToken);

  /// <summary>
  /// Создаёт новый модуль источника питания.
  /// </summary>
  /// <param name="device">Экземпляр устройства для создания.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Созданный runtime-объект устройства с актуальными данными.
  /// </returns>
  public static Task<IPowerSourceModule> CreateAsync(IPowerSourceModule device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.CreateAsync(device, cancellationToken);

  /// <summary>
  /// Создаёт набор модулей источника питания.
  /// </summary>
  /// <param name="devices">Коллекция устройств для создания.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Список созданных runtime-объектов устройств с актуальными данными.
  /// </returns>
  public static Task<List<IPowerSourceModule>> CreateRangeAsync(IEnumerable<IPowerSourceModule> devices, CancellationToken cancellationToken = default) =>
    DeviceRuntime.CreateRangeAsync(devices, cancellationToken);

  /// <summary>
  /// Обновляет существующий модуль источника питания.
  /// </summary>
  /// <param name="device">Экземпляр устройства с обновлёнными данными.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Обновлённый runtime-объект устройства.
  /// </returns>
  public static Task<IPowerSourceModule> UpdateAsync(IPowerSourceModule device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.UpdateAsync(device, cancellationToken);

  /// <summary>
  /// Удаляет модуль источника питания.
  /// </summary>
  /// <param name="device">Экземпляр устройства для удаления.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// <c>true</c>, если устройство успешно удалено; иначе <c>false</c>.
  /// </returns>
  public static Task<bool> DeleteAsync(IPowerSourceModule device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteAsync(device, cancellationToken);

  /// <summary>
  /// Удаляет модуль источника питания по идентификатору.
  /// </summary>
  /// <param name="id">Уникальный идентификатор устройства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// <c>true</c>, если устройство найдено и удалено; иначе <c>false</c>.
  /// </returns>
  public static Task<bool> DeleteByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteByIdAsync<IPowerSourceModule>(id, cancellationToken);

  /// <summary>
  /// Удаляет все устройства из таблицы данных.
  /// </summary>
  public static Task<bool> DeleteAllAsync(CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteAllAsync<IPowerSourceModule>(cancellationToken);

  /// <summary>
  /// Создаёт runtime-объект стойки на основе DTO.
  /// </summary>
  /// <param name="dto">DTO, содержащий данные устройства.</param>
  /// <returns>
  /// Готовый runtime-объект стойки.
  /// </returns>
  public static IPowerSourceModule Build(DeviceDto dto) =>
    DeviceRuntime.Build<IPowerSourceModule>(dto);
}