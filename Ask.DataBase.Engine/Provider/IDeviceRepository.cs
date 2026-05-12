namespace Ask.DataBase.Engine.Provider;

/// <summary>
/// Контракт репозитория для работы с устройствами на уровне хранения данных.
/// Предоставляет методы получения данных без привязки к конкретной реализации источника.
/// </summary>
public interface IDeviceRepository
{
  /// <summary>
  /// Получает запись устройства по её идентификатору.
  /// </summary>
  /// <param name="id">Уникальный идентификатор устройства.</param>
  /// <returns>
  /// Объект устройства или <c>null</c>, если запись не найдена.
  /// </returns>
  Task<object?> GetByIdAsync(int id);
}
