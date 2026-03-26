using System.Linq.Expressions;

namespace Ask.DataBase.Provider.Contracts;

/// <summary>
/// Универсальный контракт для CRUD-операций над DTO.
/// Содержит общий набор методов чтения, создания, обновления, удаления
/// и поиска по произвольному свойству.
/// </summary>
/// <typeparam name="T">Тип DTO, с которым работает сервис.</typeparam>
public interface ICrudService<T> where T : class
{
  /// <summary>
  /// Возвращает все записи указанного типа.
  /// </summary>
  Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Возвращает запись по идентификатору.
  /// </summary>
  Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

  /// <summary>
  /// Создаёт новую запись.
  /// </summary>
  Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default);

  /// <summary>
  /// Обновляет существующую запись.
  /// </summary>
  Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);

  /// <summary>
  /// Удаляет запись по экземпляру DTO.
  /// </summary>
  Task DeleteAsync(T entity, CancellationToken cancellationToken = default);

  /// <summary>
  /// Удаляет запись по идентификатору.
  /// Возвращает <c>true</c>, если запись была найдена и удалена.
  /// </summary>
  Task<bool> DeleteByIdAsync(int id, CancellationToken cancellationToken = default);

  /// <summary>
  /// Ищет записи по значению произвольного свойства.
  /// </summary>
  /// <typeparam name="TProperty">Тип свойства.</typeparam>
  /// <param name="propertyExpression">Выражение доступа к свойству, например <c>x => x.Number</c>.</param>
  /// <param name="value">Искомое значение свойства.</param>
  Task<List<T>> FindByPropertyAsync<TProperty>(
    Expression<Func<T, TProperty>> propertyExpression,
    TProperty value,
    CancellationToken cancellationToken = default);
}
