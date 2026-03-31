using Ask.Core.Shared.DTO.Settings;
using Ask.Core.Shared.Entity.Settings;
using Ask.DataBase.Engine.Mapping;
using Ask.DataBase.Provider.Services.Settings;

namespace Ask.DataBase.Engine.Static.Settings;

/// <summary>
/// Статический фасад для чтения и сохранения настроек отображения устройств.
/// </summary>
public static class DeviceDisplaySettings
{
  private static readonly DeviceDisplaySettingsDtoService Service = new();

  /// <summary>
  /// Возвращает сохранённые настройки отображения устройств.
  /// </summary>
  public static async Task<DeviceDisplaySettingsModel?> GetAsync(CancellationToken cancellationToken = default)
  {
    var dto = await Service.GetDeviceDisplayAsync(cancellationToken);
    return dto == null ? null : ReflectionMapper.Map<DeviceDisplaySettingsDto, DeviceDisplaySettingsModel>(dto);
  }

  /// <summary>
  /// Сохраняет настройки отображения устройств.
  /// </summary>
  public static async Task<DeviceDisplaySettingsModel> SaveAsync(
    DeviceDisplaySettingsModel model,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(model);

    var dto = ReflectionMapper.Map<DeviceDisplaySettingsModel, DeviceDisplaySettingsDto>(model);
    var saved = await Service.SaveDeviceDisplayAsync(dto, cancellationToken);
    return ReflectionMapper.Map<DeviceDisplaySettingsDto, DeviceDisplaySettingsModel>(saved);
  }
}
