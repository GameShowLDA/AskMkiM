using Ask.Core.Shared.Interfaces.DeviceInterfaces.Rack;

namespace Ask.DataBase.Engine.Static.Devices;

/// <summary>
/// Статический фасад для работы со стойками.
/// Предоставляет типобезопасный доступ к операциям <see cref="DeviceRuntime"/>
/// для устройств типа <see cref="IRack"/>.
/// </summary>
/// <remarks>
/// Является обёрткой над универсальным движком устройств и не содержит
/// собственной бизнес-логики.
/// </remarks>
public static class Racks
{
  /// <summary>
  /// Получает стойку по её идентификатору.
  /// </summary>
  /// <param name="id">Уникальный идентификатор устройства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Найденное устройство или <c>null</c>, если устройство не существует.
  /// </returns>
  public static Task<IRack?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByIdAsync<IRack>(id, cancellationToken);

  /// <summary>
  /// Получает все стойки.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Список устройств. Если устройства отсутствуют — возвращается пустой список.
  /// </returns>
  public static Task<List<IRack>> GetAllAsync(CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetAllAsync<IRack>(cancellationToken);

  /// <summary>
  /// Получает стойку по её номеру.
  /// </summary>
  /// <param name="number">Номер устройства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Найденное устройство или <c>null</c>, если устройство не найдено.
  /// </returns>
  public static Task<IRack?> GetByNumberAsync(int number, CancellationToken cancellationToken = default) =>
    DeviceRuntime.GetByNumberAsync<IRack>(number, cancellationToken);

  /// <summary>
  /// Создаёт новую стойку.
  /// </summary>
  /// <param name="device">Экземпляр устройства для создания.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Созданный runtime-объект устройства с актуальными данными.
  /// </returns>
  public static Task<IRack> CreateAsync(IRack device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.CreateAsync(device, cancellationToken);

  /// <summary>
  /// Обновляет существующую стойку.
  /// </summary>
  /// <param name="device">Экземпляр устройства с обновлёнными данными.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Обновлённый runtime-объект устройства.
  /// </returns>
  public static Task<IRack> UpdateAsync(IRack device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.UpdateAsync(device, cancellationToken);

  /// <summary>
  /// Удаляет стойку.
  /// </summary>
  /// <param name="device">Экземпляр устройства для удаления.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// <c>true</c>, если устройство успешно удалено; иначе <c>false</c>.
  /// </returns>
  public static Task<bool> DeleteAsync(IRack device, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteAsync(device, cancellationToken);

  /// <summary>
  /// Удаляет стойку по идентификатору.
  /// </summary>
  /// <param name="id">Уникальный идентификатор устройства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// <c>true</c>, если устройство найдено и удалено; иначе <c>false</c>.
  /// </returns>
  public static Task<bool> DeleteByIdAsync(int id, CancellationToken cancellationToken = default) =>
    DeviceRuntime.DeleteByIdAsync<IRack>(id, cancellationToken);
}