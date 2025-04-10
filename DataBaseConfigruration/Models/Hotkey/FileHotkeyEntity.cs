using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBaseConfiguration.Models.Hotkey
{
  /// <summary>
  /// Модель горячей клавиши, связанной с действиями управления файлами.
  /// </summary>
  [Table("FileHotkeys")]
  public class FileHotkeyEntity : IHotkeyBinding
  {
    /// <summary>
    /// Уникальный идентификатор горячей клавиши.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Уникальное логическое имя действия, к которому привязана комбинация клавиш.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ActionName { get; set; } = string.Empty;

    /// <summary>
    /// Комбинация клавиш в строковом виде (например, Ctrl+Shift+S).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string KeyCombination { get; set; } = string.Empty;

    /// <summary>
    /// Флаг, указывающий, активна ли комбинация клавиш.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Описание действия для пользовательского интерфейса.
    /// </summary>
    [MaxLength(255)]
    public string? Description { get; set; }

    /// <summary>
    /// Область применения горячей клавиши (например, Global, Editor).
    /// </summary>
    [Required]
    public HotkeyScope Scope { get; set; } = HotkeyScope.Global;
  }
}
