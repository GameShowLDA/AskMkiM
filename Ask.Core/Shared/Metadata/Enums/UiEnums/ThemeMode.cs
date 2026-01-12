using System.ComponentModel.DataAnnotations;

namespace Ask.Core.Shared.Metadata.Enums.UiEnums
{
  /// <summary>
  /// Перечисление тем оформления интерфейса.
  /// </summary>
  public enum ThemeMode
  {
    /// <summary>
    /// Светлая тема интерфейса.
    /// </summary>
    [Display(Name = "Светлая тема")]
    Light = 1,

    /// <summary>
    /// Тёмная тема интерфейса.
    /// </summary>
    [Display(Name = "Тёмная тема")]
    Dark = 0,
  }
}
