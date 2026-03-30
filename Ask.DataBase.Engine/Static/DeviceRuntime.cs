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
  public static Task<TDevice> CreateAsync<TDevice>(TDevice device, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice =>
    Engine.CreateAsync(device, cancellationToken);

  /// <summary>
  /// Обновляет существующее устройство.
  /// </summary>
  public static Task<TDevice> UpdateAsync<TDevice>(TDevice device, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice =>
    Engine.UpdateAsync(device, cancellationToken);

  /// <summary>
  /// Удаляет устройство.
  /// </summary>
  public static Task<bool> DeleteAsync<TDevice>(TDevice device, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice =>
    Engine.DeleteAsync(device, cancellationToken);

  /// <summary>
  /// Удаляет устройство по идентификатору.
  /// </summary>
  public static Task<bool> DeleteByIdAsync<TDevice>(int id, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice =>
    Engine.DeleteByIdAsync<TDevice>(id, cancellationToken);

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
}