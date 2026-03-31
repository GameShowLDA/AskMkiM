using Ask.Core.Shared.DTO.Settings;
using Ask.DataBase.Provider.Services.Base;

namespace Ask.DataBase.Provider.Services.Settings;

/// <summary>
/// Сервис работы с DTO настроек пользовательского интерфейса.
/// Общая CRUD-логика берётся из базового сервиса,
/// а здесь остаются только предметные методы.
/// </summary>
public class UserInterfaceDtoService : CrudService<UserInterfaceDto>
{
  /// <summary>
  /// Возвращает сохранённые настройки пользовательского интерфейса.
  /// </summary>
  public Task<UserInterfaceDto?> GetUserInterfaceAsync(CancellationToken cancellationToken = default)
  {
    return GetFirstOrDefaultAsync(cancellationToken);
  }

  /// <summary>
  /// Сохраняет настройки пользовательского интерфейса.
  /// Если запись отсутствует, создаёт её; если существует, обновляет.
  /// </summary>
  public Task<UserInterfaceDto> SaveUserInterfaceAsync(
    UserInterfaceDto value,
    CancellationToken cancellationToken = default)
  {
    return SaveSingleAsync(value, cancellationToken);
  }
}
