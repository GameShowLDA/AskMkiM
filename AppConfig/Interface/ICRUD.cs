using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppConfig.Interface
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
    /// <returns>Объект типа <typeparamref name="T"/>.</returns>
    T GetById(int id);

    /// <summary>
    /// Создает новую сущность.
    /// </summary>
    void Create(T entity);

    /// <summary>
    /// Обновляет существующую сущность.
    /// </summary>
    void Update(T entity);

    /// <summary>
    /// Удаляет сущность.
    /// </summary>
    void Delete(int id);
  }
}
