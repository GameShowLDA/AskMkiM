using Ask.Core.Shared.DTO.Settings;
using Ask.Core.Shared.Entity.Settings;
using Ask.DataBase.Engine.Mapping;
using Ask.DataBase.Provider.Services.Settings;

namespace Ask.DataBase.Engine.Static.Settings;

/// <summary>
/// Статический фасад для чтения и сохранения настроек пользовательского интерфейса.
/// </summary>
public static class UserInterfaceSettings
{
  private static readonly UserInterfaceDtoService Service = new();

  /// <summary>
  /// Возвращает сохранённые настройки пользовательского интерфейса.
  /// </summary>
  public static async Task<UserInterfaceDto?> GetAsync(CancellationToken cancellationToken = default)
  {
    var dto = await Service.GetUserInterfaceAsync(cancellationToken);
    return dto == null ? null : ReflectionMapper.Map<UserInterfaceDto, UserInterfaceDto>(dto);
  }

  /// <summary>
  /// Сохраняет настройки пользовательского интерфейса.
  /// </summary>
  public static async Task<UserInterfaceDto> SaveAsync(
    UserInterfaceDto model,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(model);

    var dto = ReflectionMapper.Map<UserInterfaceDto, UserInterfaceDto>(model);
    var saved = await Service.SaveUserInterfaceAsync(dto, cancellationToken);
    return ReflectionMapper.Map<UserInterfaceDto, UserInterfaceDto>(saved);
  }
}
