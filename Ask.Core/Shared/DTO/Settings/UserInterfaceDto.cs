using Ask.Core.Shared.Metadata.Enums.UiEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ask.Core.Shared.DTO.Settings;

/// <summary>
/// DTO настроек пользовательского интерфейса.
/// Содержит параметры отображения и поведения UI без привязки к источнику данных.
/// </summary>
[Table("UserInterface")]
public class UserInterfaceDto
{
  /// <summary>
  /// Идентификатор записи настроек.
  /// </summary>
  [Key]
  public int Id { get; set; }

  /// <summary>
  /// Выбранный язык интерфейса.
  /// </summary>
  public string Language { get; set; } = string.Empty;

  /// <summary>
  /// Текущая тема оформления интерфейса.
  /// </summary>
  public ThemeMode Theme { get; set; }

  /// <summary>
  /// Включает подсветку синтаксиса в редакторе.
  /// </summary>
  public bool UseSyntaxHighlighting { get; set; }

  /// <summary>
  /// Включает фоновую подсветку тела команды в протоколе выполнения.
  /// </summary>
  public bool UseCommandBodyBackgroundHighlighting { get; set; }

  /// <summary>
  /// Включает подсветку строк цепей и точек в протоколе выполнения.
  /// </summary>
  public bool UseChainPointBodyBackgroundHighlighting { get; set; }

  /// <summary>
  /// Заменяет текст верхнего меню на иконки.
  /// </summary>
  public bool UseTopMenuIcons { get; set; }

  /// <summary>
  /// Флаг, указывающий, нужно ли автоматически сворачивать завершённые блоки команд в протоколе выполнения.
  /// </summary>
  public bool UseCommandAutoCollapse { get; set; }
}
