using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;

namespace Ask.DataBase.Engine.Static.Devices;

/// <summary>
/// Статический фасад для работы с источниками бесперебойного питания.
/// Предоставляет типобезопасный доступ к операциям <see cref="DeviceRuntime"/>
/// для устройств типа <see cref="IUninterruptiblePowerSupply"/>.
/// </summary>
/// <remarks>
/// Является обёрткой над универсальным движком устройств и не содержит
/// собственной бизнес-логики.
/// </remarks>
public static class UninterruptiblePowerSupplies
{
  /// <summary>
  /// Получает источник бесперебойного питания по его идентификатору.
  /// </summary>
  /// <param name="id">Уникальный идентификатор устройства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Найденное устройство или <c>null</c>, если устройство не существует.
  /// </returns>
  public static Task<IUninterruptiblePowerSupply?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByIdAsync<IUninterruptiblePowerSupply>(id, cancellationToken);

  /// <summary>
  /// Получает все источники бесперебойного питания.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Список устройств. Если устройства отсутствуют — возвращается пустой список.
  /// </returns>
  public static Task<List<IUninterruptiblePowerSupply>> GetAllAsync(CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetAllAsync<IUninterruptiblePowerSupply>(cancellationToken);

  /// <summary>
  /// Получает источник бесперебойного питания по его номеру.
  /// </summary>
  /// <param name="number">Номер устройства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Найденное устройство или <c>null</c>, если устройство не найдено.
  /// </returns>
  public static Task<IUninterruptiblePowerSupply?> GetByNumberAsync(int number, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByNumberAsync<IUninterruptiblePowerSupply>(number, cancellationToken);

  /// <summary>
  /// Получает список источников бесперебойного питания, относящихся к указанному шасси.
  /// </summary>
  /// <param name="numberChassis">Номер шасси.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Список устройств. Если устройства отсутствуют — возвращается пустой список.
  /// </returns>
  public static Task<List<IUninterruptiblePowerSupply>> GetDevicesByNumberChassisAsync(
    int numberChassis,
    CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetDevicesByNumberChassisAsync<IUninterruptiblePowerSupply>(numberChassis, cancellationToken);

  /// <summary>
  /// Получает источник бесперебойного питания по номеру шасси и номеру устройства.
  /// </summary>
  /// <param name="numberChassis">Номер шасси.</param>
  /// <param name="number">Номер устройства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Найденное устройство или <c>null</c>, если соответствующее устройство не найдено.
  /// </returns>
  public static Task<IUninterruptiblePowerSupply?> GetDeviceByNumberChassisAsync(
    int numberChassis,
    int number,
    CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetDeviceByNumberChassisAsync<IUninterruptiblePowerSupply>(numberChassis, number, cancellationToken);

  /// <summary>
  /// Создаёт новый источник бесперебойного питания.
  /// </summary>
  /// <param name="device">Экземпляр устройства для создания.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Созданный runtime-объект устройства с актуальными данными.
  /// </returns>
  public static Task<IUninterruptiblePowerSupply> CreateAsync(IUninterruptiblePowerSupply device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.CreateAsync(device, cancellationToken);

  /// <summary>
  /// Создаёт набор источников бесперебойного питания.
  /// </summary>
  /// <param name="devices">Коллекция устройств для создания.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Список созданных runtime-объектов устройств с актуальными данными.
  /// </returns>
  public static Task<List<IUninterruptiblePowerSupply>> CreateRangeAsync(IEnumerable<IUninterruptiblePowerSupply> devices, CancellationToken cancellationToken = default) =>
    DeviceRuntime.CreateRangeAsync(devices, cancellationToken);

  /// <summary>
  /// Обновляет существующий источник бесперебойного питания.
  /// </summary>
  /// <param name="device">Экземпляр устройства с обновлёнными данными.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Обновлённый runtime-объект устройства.
  /// </returns>
  public static Task<IUninterruptiblePowerSupply> UpdateAsync(IUninterruptiblePowerSupply device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.UpdateAsync(device, cancellationToken);

  /// <summary>
  /// Удаляет источник бесперебойного питания.
  /// </summary>
  /// <param name="device">Экземпляр устройства для удаления.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// <c>true</c>, если устройство успешно удалено; иначе <c>false</c>.
  /// </returns>
  public static Task<bool> DeleteAsync(IUninterruptiblePowerSupply device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteAsync(device, cancellationToken);

  /// <summary>
  /// Удаляет источник бесперебойного питания по идентификатору.
  /// </summary>
  /// <param name="id">Уникальный идентификатор устройства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// <c>true</c>, если устройство найдено и удалено; иначе <c>false</c>.
  /// </returns>
  public static Task<bool> DeleteByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteByIdAsync<IUninterruptiblePowerSupply>(id, cancellationToken);

  /// <summary>
  /// Удаляет все источники бесперебойного питания из таблицы данных.
  /// </summary>
  public static Task<bool> DeleteAllAsync(CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteAllAsync<IUninterruptiblePowerSupply>(cancellationToken);

  /// <summary>
  /// Создаёт runtime-объект источника бесперебойного питания на основе DTO.
  /// </summary>
  /// <param name="dto">DTO, содержащий данные устройства.</param>
  /// <returns>
  /// Готовый runtime-объект источника бесперебойного питания.
  /// </returns>
  public static IUninterruptiblePowerSupply Build(DeviceDto dto) =>
    DeviceRuntime.Build<IUninterruptiblePowerSupply>(dto);
}