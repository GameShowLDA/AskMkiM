using Ask.Core.Shared.DTO.Settings;
using Ask.DataBase.Engine.Mapping;
using Ask.DataBase.Provider.Services.Settings;

namespace Ask.DataBase.Engine.Static.Settings;

/// <summary>
/// Статический фасад для чтения и сохранения настроек выполнения через новую БД.
/// </summary>
public static class ExecutionSettings
{
  private static readonly SettingsExecutionDtoService Service = new();

  /// <summary>
  /// Возвращает сохранённые настройки выполнения.
  /// </summary>
  public static async Task<SettingsExecutionDto?> GetAsync(CancellationToken cancellationToken = default)
  {
    var dto = await Service.GetExecutionAsync(cancellationToken);
    return dto == null ? null : ReflectionMapper.Map<SettingsExecutionDto, SettingsExecutionDto>(dto);
  }

  /// <summary>
  /// Сохраняет настройки выполнения и возвращает актуальную доменную модель.
  /// </summary>
  public static async Task<SettingsExecutionDto> SaveAsync(
    SettingsExecutionDto model,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(model);

    var dto = ReflectionMapper.Map<SettingsExecutionDto, SettingsExecutionDto>(model);
    var saved = await Service.SaveExecutionAsync(dto, cancellationToken);
    return ReflectionMapper.Map<SettingsExecutionDto, SettingsExecutionDto>(saved);
  }
}
