using Ask.Core.Shared.DTO.Settings;
using Ask.DataBase.Provider.Services.Base;

namespace Ask.DataBase.Provider.Services.Settings;

/// <summary>
/// Сервис работы с DTO настроек выполнения.
/// Общая CRUD-логика берётся из базового сервиса,
/// а здесь остаются только предметные методы.
/// </summary>
public class SettingsExecutionDtoService : CrudService<SettingsExecutionDto>
{
  /// <summary>
  /// Возвращает сохранённые настройки выполнения.
  /// </summary>
  public Task<SettingsExecutionDto?> GetExecutionAsync(CancellationToken cancellationToken = default)
  {
    return GetFirstOrDefaultAsync(cancellationToken);
  }

  /// <summary>
  /// Сохраняет настройки выполнения.
  /// Если запись отсутствует, создаёт её; если существует, обновляет.
  /// </summary>
  public Task<SettingsExecutionDto> SaveExecutionAsync(
    SettingsExecutionDto value,
    CancellationToken cancellationToken = default)
  {
    return SaveSingleAsync(value, cancellationToken);
  }
}
