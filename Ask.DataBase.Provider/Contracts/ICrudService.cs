using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using System.Linq.Expressions;

namespace Ask.DataBase.Provider.Contracts;

/// <summary>
/// Универсальный контракт для CRUD-операций над DTO.
/// Предоставляет асинхронные методы для чтения, создания, обновления,
/// удаления и поиска сущностей по произвольному свойству.
/// </summary>
/// <typeparam name="T">Тип DTO, с которым работает сервис.</typeparam>
public interface ICrudService<T> where T : class
{
  /// <summary>
  /// Возвращает все записи указанного типа.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Список всех записей. Если записи отсутствуют — возвращается пустой список.
  /// </returns>
  Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Возвращает запись по идентификатору.
  /// </summary>
  /// <param name="id">Уникальный идентификатор записи.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Найденная запись или <c>null</c>, если запись не существует.
  /// </returns>
  Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

  /// <summary>
  /// Создаёт новую запись.
  /// </summary>
  /// <param name="entity">DTO для создания.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Созданная запись с актуализированными данными (например, с присвоенным идентификатором).
  /// </returns>
  Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default);

  /// <summary>
  /// Создаёт набор записей.
  /// </summary>
  /// <param name="entities">Коллекция DTO для создания.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Список созданных записей с актуальными данными (например, с присвоенными идентификаторами).
  /// </returns>
  Task<List<T>> CreateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

  /// <summary>
  /// Обновляет существующую запись.
  /// </summary>
  /// <param name="entity">DTO с обновлёнными данными.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Обновлённая запись.
  /// </returns>
  Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);

  /// <summary>
  /// Удаляет запись по экземпляру DTO.
  /// </summary>
  /// <param name="entity">DTO для удаления.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Задача, представляющая асинхронную операцию удаления.
  /// </returns>
  Task DeleteAsync(T entity, CancellationToken cancellationToken = default);

  /// <summary>
  /// Удаляет запись по идентификатору.
  /// </summary>
  /// <param name="id">Уникальный идентификатор записи.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// <c>true</c>, если запись была найдена и удалена; иначе <c>false</c>.
  /// </returns>
  Task<bool> DeleteByIdAsync(int id, CancellationToken cancellationToken = default);

  /// <summary>
  /// Удаляет все устрйоства из таблицы данных.
  /// </summary>
  /// <typeparam name="TDevice">Тип устройства.</typeparam>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// <c>true</c>, если устройства успешно удалены; иначе <c>false</c>.
  /// </returns>
  Task<bool> DeleteAllAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Ищет записи по значению произвольного свойства.
  /// </summary>
  /// <typeparam name="TProperty">Тип свойства.</typeparam>
  /// <param name="propertyExpression">
  /// Выражение доступа к свойству (например, <c>x => x.Number</c>).
  /// </param>
  /// <param name="value">Искомое значение свойства.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>
  /// Список записей, удовлетворяющих условию. Если совпадений нет — возвращается пустой список.
  /// </returns>
  Task<List<T>> FindByPropertyAsync<TProperty>(
    Expression<Func<T, TProperty>> propertyExpression,
    TProperty value,
    CancellationToken cancellationToken = default);
}