using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;

namespace Ask.DataBase.Engine.Static.Devices;

/// <summary>
/// Статический фасад для работы с модулями коммутации реле.
/// Предоставляет типобезопасный доступ к операциям <see cref="DeviceRuntime"/>
/// для устройств типа <see cref="IRelaySwitchModule"/>.
/// </summary>
/// <remarks>
/// Является обёрткой над универсальным движком устройств.
/// Содержит как базовые CRUD-операции, так и специализированные методы,
/// связанные с управлением параметрами релейного модуля.
/// </remarks>
public static class RelaySwitchModules
{
  /// <summary>
  /// Получает модуль коммутации реле по его идентификатору.
  /// </summary>
  public static Task<IRelaySwitchModule?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByIdAsync<IRelaySwitchModule>(id, cancellationToken);

  /// <summary>
  /// Получает все модули коммутации реле.
  /// </summary>
  public static Task<List<IRelaySwitchModule>> GetAllAsync(CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetAllAsync<IRelaySwitchModule>(cancellationToken);

  /// <summary>
  /// Получает модуль коммутации реле по его номеру.
  /// </summary>
  public static Task<IRelaySwitchModule?> GetByNumberAsync(int number, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByNumberAsync<IRelaySwitchModule>(number, cancellationToken);

  /// <summary>
  /// Получает список модулей коммутации реле, относящихся к указанному шасси.
  /// </summary>
  public static Task<List<IRelaySwitchModule>> GetDevicesByNumberChassisAsync(
    int numberChassis,
    CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetDevicesByNumberChassisAsync<IRelaySwitchModule>(numberChassis, cancellationToken);

  /// <summary>
  /// Получает модуль коммутации реле по номеру шасси и номеру устройства.
  /// </summary>
  public static Task<IRelaySwitchModule?> GetDeviceByNumberChassisAsync(
    int numberChassis,
    int number,
    CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetDeviceByNumberChassisAsync<IRelaySwitchModule>(numberChassis, number, cancellationToken);

  /// <summary>
  /// Создаёт новый модуль коммутации реле.
  /// </summary>
  public static Task<IRelaySwitchModule> CreateAsync(IRelaySwitchModule device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.CreateAsync(device, cancellationToken);

  /// <summary>
  /// Обновляет существующий модуль коммутации реле.
  /// </summary>
  public static Task<IRelaySwitchModule> UpdateAsync(IRelaySwitchModule device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.UpdateAsync(device, cancellationToken);

  /// <summary>
  /// Удаляет модуль коммутации реле.
  /// </summary>
  public static Task<bool> DeleteAsync(IRelaySwitchModule device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteAsync(device, cancellationToken);

  /// <summary>
  /// Удаляет модуль коммутации реле по идентификатору.
  /// </summary>
  public static Task<bool> DeleteByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteByIdAsync<IRelaySwitchModule>(id, cancellationToken);

  /// <summary>
  /// Обновляет сопротивление переключения для модуля,
  /// найденного по номеру шасси и номеру устройства.
  /// </summary>
  /// <param name="numberChassis">Номер шасси.</param>
  /// <param name="number">Номер устройства.</param>
  /// <param name="value">Новое значение сопротивления.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Обновлённый экземпляр устройства или <c>null</c>, если устройство не найдено.
  /// </returns>
  /// <remarks>
  /// Метод выполняет:
  /// 1. Поиск устройства
  /// 2. Изменение свойства <see cref="IRelaySwitchModule.SwitchResistance"/>
  /// 3. Сохранение изменений через <see cref="UpdateAsync"/>
  /// </remarks>
  public static async Task<IRelaySwitchModule?> UpdateResistanceAsync(
    int numberChassis,
    int number,
    double value,
    CancellationToken cancellationToken = default)
  {
    var device = await GetDeviceByNumberChassisAsync(numberChassis, number, cancellationToken);
    if (device == null)
    {
      return null;
    }

    device.SwitchResistance = value;
    return await UpdateAsync(device, cancellationToken);
  }
}