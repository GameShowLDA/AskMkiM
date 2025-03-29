namespace AppManager.Interface
{
  /// <summary>
  /// Обобщенный интерфейс для базовых операций CRUD (создание, чтение, обновление, удаление).
  /// </summary>
  /// <typeparam name="T">Тип сущности, с которой работает репозиторий.</typeparam>
  internal interface ICRUD<T>
  {
    /// <summary>
    /// Получает список всех сущностей.
    /// </summary>
    /// <returns>Список всех сущностей типа <typeparamref name="T"/>.</returns>
    List<T> GetAll();

    /// <summary>
    /// Получает сущность по ее идентификатору.
    /// </summary>
    /// <param name="id">Уникальный номер сущности.</param>
    /// <returns>Объект типа <typeparamref name="T"/>.</returns>
    T GetById(int id);

    /// <summary>
    /// Создает новую сущность.
    /// </summary>
    /// <param name="entity">Сущность для обновления.</param>
    void Create(T entity);

    /// <summary>
    /// Обновляет существующую сущность.
    /// </summary>
    /// <param name="entity">Сущность для обновления.</param>
    void Update(T entity);

    /// <summary>
    /// Удаляет сущность.
    /// </summary>
    /// <param name="entity">Сущность для удаления.</param>
    void Delete(T entity);
  }
}
