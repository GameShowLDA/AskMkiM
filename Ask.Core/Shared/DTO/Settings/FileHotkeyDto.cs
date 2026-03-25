namespace Ask.Core.Shared.DTO.Settings;

/// <summary>
/// DTO горячей клавиши.
/// Представляет привязку действия к комбинации клавиш без логики обработки.
/// </summary>
public class FileHotkeyDto
{
  /// <summary>
  /// Уникальное логическое имя действия.
  /// </summary>
  public string ActionName { get; set; } = string.Empty;

  /// <summary>
  /// Комбинация клавиш (например, Ctrl+Shift+S).
  /// </summary>
  public string KeyCombination { get; set; } = string.Empty;

  /// <summary>
  /// Признак активности горячей клавиши.
  /// </summary>
  public bool IsEnabled { get; set; }

  /// <summary>
  /// Описание действия для отображения в интерфейсе.
  /// </summary>
  public string? Description { get; set; }
}