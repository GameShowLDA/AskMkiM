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

    /// <summary>
    /// Светлая custom-тема интерфейса.
    /// </summary>
    [Display(Name = "Светлая тема (custom)")]
    LightCustom = 3,

    /// <summary>
    /// Тёмная custom-тема интерфейса.
    /// </summary>
    [Display(Name = "Тёмная тема (custom)")]
    DarkCustom = 2,
  }
}
