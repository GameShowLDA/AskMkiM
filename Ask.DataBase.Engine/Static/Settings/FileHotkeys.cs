using Ask.Core.Shared.DTO.Settings;
using Ask.DataBase.Provider.Services.Settings;

namespace Ask.DataBase.Engine.Static.Settings;

/// <summary>
/// Статический фасад для горячих клавиш из новой БД.
/// </summary>
public static class FileHotkeys
{
  private static readonly FileHotkeyDtoService Service = new();

  /// <summary>
  /// Возвращает все записи горячих клавиш.
  /// </summary>
  public static Task<List<FileHotkeyDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
    Service.GetAllAsync(cancellationToken);

  /// <summary>
  /// Возвращает только включённые горячие клавиши.
  /// </summary>
  public static async Task<List<FileHotkeyDto>> GetEnabledAsync(CancellationToken cancellationToken = default)
  {
    var hotkeys = await Service.GetAllAsync(cancellationToken);
    return hotkeys.Where(x => x.IsEnabled).ToList();
  }

  /// <summary>
  /// Создаёт запись горячей клавиши.
  /// </summary>
  public static Task<FileHotkeyDto> CreateAsync(FileHotkeyDto dto, CancellationToken cancellationToken = default) =>
    Service.CreateAsync(dto, cancellationToken);

  /// <summary>
  /// Обновляет запись горячей клавиши.
  /// </summary>
  public static Task<FileHotkeyDto> UpdateAsync(FileHotkeyDto dto, CancellationToken cancellationToken = default) =>
    Service.UpdateAsync(dto, cancellationToken);

  /// <summary>
  /// Удаляет запись горячей клавиши.
  /// </summary>
  public static Task DeleteAsync(FileHotkeyDto dto, CancellationToken cancellationToken = default) =>
    Service.DeleteAsync(dto, cancellationToken);

  /// <summary>
  /// Удаляет запись горячей клавиши по идентификатору.
  /// </summary>
  public static Task<bool> DeleteByIdAsync(int id, CancellationToken cancellationToken = default) =>
    Service.DeleteByIdAsync(id, cancellationToken);
}
