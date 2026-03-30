using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;

namespace Ask.DataBase.Engine.Contracts;

/// <summary>
/// Универсальный движок управления устройствами.
/// Обеспечивает полный цикл работы с устройствами:
/// получение, создание, обновление, удаление и пересборку runtime-объектов.
///
/// Движок абстрагирует источник данных (например, БД) и механизм построения
/// конкретных реализаций устройств, возвращая готовые объекты,
/// реализующие интерфейс <see cref="IDevice"/>.
///
/// Поддерживает:
/// - Получение устройств по различным критериям (Id, номер, шасси и т.д.)
/// - Массовое получение устройств
/// - Перезагрузку устройства с обходом кэша
/// - CRUD-операции
/// - Построение runtime-объекта устройства из <see cref="DeviceDto"/>
/// - Кэширование устройств и его очистку
///
/// Использует обобщения для возврата конкретного типа устройства,
/// что позволяет работать с различными реализациями через единый контракт.
/// </summary>
public interface IDeviceEngine
{
  /// <summary>
  /// Получает устройство по его уникальному идентификатору.
  /// </summary>
  /// <typeparam name="TDevice">Тип устройства.</typeparam>
  /// <param name="id">Уникальный идентификатор устройства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Готовый runtime-объект устройства или <c>null</c>, если устройство не найдено.
  /// </returns>
  Task<TDevice?> GetByIdAsync<TDevice>(int id, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice;

  /// <summary>
  /// Получает все устройства указанного типа.
  /// </summary>
  /// <typeparam name="TDevice">Тип устройства.</typeparam>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Список runtime-объектов устройств. Если устройства отсутствуют — возвращается пустой список.
  /// </returns>
  Task<List<TDevice>> GetAllAsync<TDevice>(CancellationToken cancellationToken = default)
    where TDevice : class, IDevice;

  /// <summary>
  /// Получает устройство по его номеру.
  /// </summary>
  /// <typeparam name="TDevice">Тип устройства.</typeparam>
  /// <param name="number">Номер устройства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Найденное устройство или <c>null</c>, если устройство с указанным номером отсутствует.
  /// </returns>
  Task<TDevice?> GetByNumberAsync<TDevice>(int number, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice;

  /// <summary>
  /// Получает список устройств, принадлежащих указанному шасси.
  /// </summary>
  /// <typeparam name="TDevice">Тип устройства.</typeparam>
  /// <param name="numberChassis">Номер шасси.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Список устройств, связанных с данным шасси. Если устройства отсутствуют — возвращается пустой список.
  /// </returns>
  Task<List<TDevice>> GetDevicesByNumberChassisAsync<TDevice>(
    int numberChassis,
    CancellationToken cancellationToken = default)
    where TDevice : class, IDevice;

  /// <summary>
  /// Получает устройство по номеру шасси и номеру устройства.
  /// </summary>
  /// <typeparam name="TDevice">Тип устройства.</typeparam>
  /// <param name="numberChassis">Номер шасси.</param>
  /// <param name="number">Номер устройства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Найденное устройство или <c>null</c>, если соответствующее устройство не найдено.
  /// </returns>
  Task<TDevice?> GetDeviceByNumberChassisAsync<TDevice>(
    int numberChassis,
    int number,
    CancellationToken cancellationToken = default)
    where TDevice : class, IDevice;

  /// <summary>
  /// Принудительно перезагружает устройство по идентификатору,
  /// игнорируя кэш и создавая новый runtime-объект.
  /// </summary>
  /// <typeparam name="TDevice">Тип устройства.</typeparam>
  /// <param name="id">Уникальный идентификатор устройства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Обновлённый runtime-объект устройства или <c>null</c>, если устройство не найдено.
  /// </returns>
  Task<TDevice?> ReloadByIdAsync<TDevice>(int id, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice;

  /// <summary>
  /// Создаёт новое устройство.
  /// </summary>
  /// <typeparam name="TDevice">Тип устройства.</typeparam>
  /// <param name="device">Экземпляр устройства для создания.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Созданный runtime-объект устройства (включая обновлённые данные, например Id).
  /// </returns>
  Task<TDevice> CreateAsync<TDevice>(TDevice device, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice;

  /// <summary>
  /// Создаёт набор устройств.
  /// </summary>
  /// <typeparam name="TDevice">Тип устройств.</typeparam>
  /// <param name="devices">Коллекция устройств для создания.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Список созданных runtime-объектов устройств с актуальными данными.
  /// </returns>
  Task<List<TDevice>> CreateRangeAsync<TDevice>(IEnumerable<TDevice> devices,CancellationToken cancellationToken = default)
    where TDevice : class, IDevice;

  /// <summary>
  /// Обновляет существующее устройство.
  /// </summary>
  /// <typeparam name="TDevice">Тип устройства.</typeparam>
  /// <param name="device">Экземпляр устройства с обновлёнными данными.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Обновлённый runtime-объект устройства.
  /// </returns>
  Task<TDevice> UpdateAsync<TDevice>(TDevice device, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice;

  /// <summary>
  /// Удаляет устройство по его идентификатору.
  /// </summary>
  /// <typeparam name="TDevice">Тип устройства.</typeparam>
  /// <param name="id">Уникальный идентификатор устройства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// <c>true</c>, если устройство успешно удалено; иначе <c>false</c>.
  /// </returns>
  Task<bool> DeleteAsync<TDevice>(TDevice device, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice;

  /// <summary>
  /// Удаляет устройство по его идентификатору.
  /// </summary>
  /// <typeparam name="TDevice">Тип устройства.</typeparam>
  /// <param name="id">Уникальный идентификатор устройства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// <c>true</c>, если устройство успешно удалено; иначе <c>false</c>.
  /// </returns>
  Task<bool> DeleteByIdAsync<TDevice>(int id, CancellationToken cancellationToken = default)
    where TDevice : class, IDevice;

  /// <summary>
  /// Удаляет все устрйоства из таблицы данных.
  /// </summary>
  /// <typeparam name="TDevice">Тип устройства.</typeparam>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// <c>true</c>, если устройства успешно удалены; иначе <c>false</c>.
  /// </returns>
  Task<bool> DeleteAllAsync<TDevice>(CancellationToken cancellationToken = default)
    where TDevice : class, IDevice;

  /// <summary>
  /// Создаёт runtime-объект устройства на основе DTO.
  /// </summary>
  /// <typeparam name="TDevice">Тип устройства.</typeparam>
  /// <param name="dto">DTO, содержащий данные устройства.</param>
  /// <returns>
  /// Готовый runtime-объект устройства.
  /// </returns>
  TDevice Build<TDevice>(DeviceDto dto)
    where TDevice : class, IDevice;

  /// <summary>
  /// Очищает внутренний кэш устройств.
  /// </summary>
  void ClearCache();
}
