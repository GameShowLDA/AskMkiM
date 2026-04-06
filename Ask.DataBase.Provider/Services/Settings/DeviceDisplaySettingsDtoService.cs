using Ask.Core.Shared.DTO.Settings;
using Ask.DataBase.Provider.Services.Base;

namespace Ask.DataBase.Provider.Services.Settings;

/// <summary>
/// Сервис работы с DTO настроек отображения устройств.
/// Общая CRUD-логика берётся из базового сервиса,
/// а здесь остаются только предметные методы.
/// </summary>
public class DeviceDisplaySettingsDtoService : CrudService<DeviceDisplaySettingsDto>
{
  /// <summary>
  /// Возвращает сохранённые настройки отображения устройств.
  /// </summary>
  public Task<DeviceDisplaySettingsDto?> GetDeviceDisplayAsync(CancellationToken cancellationToken = default)
  {
    return GetFirstOrDefaultAsync(cancellationToken);
  }

  /// <summary>
  /// Сохраняет настройки отображения устройств.
  /// Если запись отсутствует, создаёт её; если существует, обновляет.
  /// </summary>
  public Task<DeviceDisplaySettingsDto> SaveDeviceDisplayAsync(
    DeviceDisplaySettingsDto session,
    CancellationToken cancellationToken = default)
  {
    return SaveSingleAsync(session, cancellationToken);
  }
}
