using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;

namespace Ask.DataBase.Engine.Static.Devices;

/// <summary>
/// Статический фасад для работы с быстрыми измерителями.
/// Предоставляет типобезопасный доступ к операциям <see cref="DeviceRuntime"/>
/// для устройств типа <see cref="IMultimeter"/>.
/// </summary>
/// <remarks>
/// Является обёрткой над универсальным движком устройств и не содержит
/// собственной бизнес-логики.
/// </remarks>
public static class FastMeters
{
  /// <summary>
  /// Получает быстрый измеритель по его идентификатору.
  /// </summary>
  /// <param name="id">Уникальный идентификатор устройства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Найденное устройство или <c>null</c>, если устройство не существует.
  /// </returns>
  public static Task<IMultimeter?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByIdAsync<IMultimeter>(id, cancellationToken);

  /// <summary>
  /// Получает все быстрые измерители.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Список устройств. Если устройства отсутствуют — возвращается пустой список.
  /// </returns>
  public static Task<List<IMultimeter>> GetAllAsync(CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetAllAsync<IMultimeter>(cancellationToken);

  /// <summary>
  /// Получает быстрый измеритель по его номеру.
  /// </summary>
  /// <param name="number">Номер устройства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Найденное устройство или <c>null</c>, если устройство не найдено.
  /// </returns>
  public static Task<IMultimeter?> GetByNumberAsync(int number, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByNumberAsync<IMultimeter>(number, cancellationToken);

  /// <summary>
  /// Получает список быстрых измерителей, относящихся к указанному шасси.
  /// </summary>
  /// <param name="numberChassis">Номер шасси.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Список устройств. Если устройства отсутствуют — возвращается пустой список.
  /// </returns>
  public static Task<List<IMultimeter>> GetDevicesByNumberChassisAsync(
    int numberChassis,
    CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetDevicesByNumberChassisAsync<IMultimeter>(numberChassis, cancellationToken);

  /// <summary>
  /// Получает быстрый измеритель по номеру шасси и номеру устройства.
  /// </summary>
  /// <param name="numberChassis">Номер шасси.</param>
  /// <param name="number">Номер устройства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Найденное устройство или <c>null</c>, если соответствующее устройство не найдено.
  /// </returns>
  public static Task<IMultimeter?> GetDeviceByNumberChassisAsync(
    int numberChassis,
    int number,
    CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetDeviceByNumberChassisAsync<IMultimeter>(numberChassis, number, cancellationToken);

  /// <summary>
  /// Создаёт новый быстрый измеритель.
  /// </summary>
  /// <param name="device">Экземпляр устройства для создания.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Созданный runtime-объект устройства с актуальными данными.
  /// </returns>
  public static Task<IMultimeter> CreateAsync(IMultimeter device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.CreateAsync(device, cancellationToken);

  /// <summary>
  /// Создаёт набор быстрых измерителей.
  /// </summary>
  /// <param name="devices">Коллекция устройств для создания.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Список созданных runtime-объектов устройств с актуальными данными.
  /// </returns>
  public static Task<List<IMultimeter>> CreateRangeAsync(IEnumerable<IMultimeter> devices, CancellationToken cancellationToken = default) =>
    DeviceRuntime.CreateRangeAsync(devices, cancellationToken);

  /// <summary>
  /// Обновляет существующий быстрый измеритель.
  /// </summary>
  /// <param name="device">Экземпляр устройства с обновлёнными данными.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Обновлённый runtime-объект устройства.
  /// </returns>
  public static Task<IMultimeter> UpdateAsync(IMultimeter device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.UpdateAsync(device, cancellationToken);

  /// <summary>
  /// Удаляет быстрый измеритель.
  /// </summary>
  /// <param name="device">Экземпляр устройства для удаления.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// <c>true</c>, если устройство успешно удалено; иначе <c>false</c>.
  /// </returns>
  public static Task<bool> DeleteAsync(IMultimeter device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteAsync(device, cancellationToken);

  /// <summary>
  /// Удаляет быстрый измеритель по идентификатору.
  /// </summary>
  /// <param name="id">Уникальный идентификатор устройства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// <c>true</c>, если устройство найдено и удалено; иначе <c>false</c>.
  /// </returns>
  public static Task<bool> DeleteByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteByIdAsync<IMultimeter>(id, cancellationToken);

  /// <summary>
  /// Удаляет все быстрые измерители из таблицы данных.
  /// </summary>
  public static Task<bool> DeleteAllAsync(CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteAllAsync<IMultimeter>(cancellationToken);

  /// <summary>
  /// Создаёт runtime-объект стойки на основе DTO.
  /// </summary>
  /// <param name="dto">DTO, содержащий данные быстрого измерителя.</param>
  /// <returns>
  /// Готовый runtime-объект стойки.
  /// </returns>
  public static IMultimeter Build(DeviceDto dto) =>
    DeviceRuntime.Build<IMultimeter>(dto);
}