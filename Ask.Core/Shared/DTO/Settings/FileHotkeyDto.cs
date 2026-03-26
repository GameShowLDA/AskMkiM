using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ask.Core.Shared.DTO.Settings;

/// <summary>
/// DTO горячей клавиши.
/// Представляет привязку действия к комбинации клавиш без логики обработки.
/// </summary>
[Table("FileHotkeys")]
public class FileHotkeyDto
{
  /// <summary>
  /// Уникальный идентификатор горячей клавиши.
  /// </summary>
  [Key]
  public int Id { get; set; }

  /// <summary>
  /// Уникальное логическое имя действия.
  /// </summary>
  [Required]
  [MaxLength(100)]
  public string ActionName { get; set; } = string.Empty;

  /// <summary>
  /// Комбинация клавиш (например, Ctrl+Shift+S).
  /// </summary>
  [Required]
  [MaxLength(50)]
  public string KeyCombination { get; set; } = string.Empty;

  /// <summary>
  /// Признак активности горячей клавиши.
  /// </summary>
  public bool IsEnabled { get; set; }

  /// <summary>
  /// Описание действия для отображения в интерфейсе.
  /// </summary>
  [MaxLength(255)]
  public string? Description { get; set; }
}
