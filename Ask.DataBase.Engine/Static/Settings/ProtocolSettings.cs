using Ask.Core.Shared.DTO.Settings;
using Ask.Core.Shared.Entity.Settings;
using Ask.DataBase.Engine.Mapping;
using Ask.DataBase.Provider.Services.Settings;

namespace Ask.DataBase.Engine.Static.Settings;

/// <summary>
/// Статический фасад для чтения и сохранения настроек протокола через новую БД.
/// </summary>
public static class ProtocolSettings
{
  private static readonly SettingsProtocolDtoService Service = new();

  /// <summary>
  /// Возвращает сохранённые настройки протокола в виде доменной модели.
  /// </summary>
  public static async Task<SettingsProtocolModel?> GetAsync(CancellationToken cancellationToken = default)
  {
    var dto = await Service.GetProtocolAsync(cancellationToken);
    return dto == null ? null : ReflectionMapper.Map<SettingsProtocolDto, SettingsProtocolModel>(dto);
  }

  /// <summary>
  /// Сохраняет настройки протокола и возвращает актуальную доменную модель.
  /// </summary>
  public static async Task<SettingsProtocolModel> SaveAsync(
    SettingsProtocolModel model,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(model);

    var dto = ReflectionMapper.Map<SettingsProtocolModel, SettingsProtocolDto>(model);
    var saved = await Service.SaveProtocolAsync(dto, cancellationToken);
    return ReflectionMapper.Map<SettingsProtocolDto, SettingsProtocolModel>(saved);
  }
}
