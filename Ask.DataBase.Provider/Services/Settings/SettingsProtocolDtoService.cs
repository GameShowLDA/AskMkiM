using Ask.Core.Shared.DTO.Settings;
using Ask.DataBase.Provider.Services.Base;

namespace Ask.DataBase.Provider.Services.Settings;

/// <summary>
/// Сервис работы с DTO настроек протокола.
/// Общая CRUD-логика берётся из базового сервиса,
/// а здесь остаются только предметные методы.
/// </summary>
public class SettingsProtocolDtoService : CrudService<SettingsProtocolDto>
{
  /// <summary>
  /// Возвращает сохранённые настройки протокола.
  /// </summary>
  public Task<SettingsProtocolDto?> GetProtocolAsync(CancellationToken cancellationToken = default)
  {
    return GetFirstOrDefaultAsync(cancellationToken);
  }

  /// <summary>
  /// Сохраняет настройки протокола.
  /// Если запись отсутствует, создаёт её; если существует, обновляет.
  /// </summary>
  public Task<SettingsProtocolDto> SaveProtocolAsync(
    SettingsProtocolDto session,
    CancellationToken cancellationToken = default)
  {
    return SaveSingleAsync(session, cancellationToken);
  }
}
