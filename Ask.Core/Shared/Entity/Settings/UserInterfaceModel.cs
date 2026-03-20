using Ask.Core.Shared.Metadata.Enums.UiEnums;
using System.ComponentModel.DataAnnotations;

namespace Ask.Core.Shared.Entity.Settings
{
  public class UserInterfaceModel
  {
    [Key]
    public int Id { get; set; } = 1;

    /// <summary>
    /// Выбранный язык интерфейса программы.
    /// </summary>
    public string Language { get; set; }

    /// <summary>
    /// Выбранная тема оформления интерфейса программы.
    /// </summary>
    public ThemeMode Theme { get; set; }

    /// <summary>
    /// Флаг, указывающий, нужно ли включить подсветку синтаксиса в редакторе.
    /// </summary>
    public bool UseSyntaxHighlighting { get; set; }

    /// <summary>
    /// Флаг, указывающий, нужно ли выделять тело команды фоновым цветом в протоколе выполнения.
    /// </summary>
    public bool UseCommandBodyBackgroundHighlighting { get; set; } = true;

    /// <summary>
    /// Флаг, указывающий, нужно ли выделять строки цепей и точек фоновым цветом в протоколе выполнения.
    /// </summary>
    public bool UseChainPointBodyBackgroundHighlighting { get; set; } = true;

    /// <summary>
    /// Флаг, указывающий, нужно ли заменять текст верхнего меню на иконки.
    /// </summary>
    public bool UseTopMenuIcons { get; set; }
  }
}
