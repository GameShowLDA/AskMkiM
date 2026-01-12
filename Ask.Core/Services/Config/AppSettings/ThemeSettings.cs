using Ask.Core.Services.Config.Base;
using Ask.Core.Shared.Metadata.Enums.UiEnums;

namespace Ask.Core.Services.Config.AppSettings
{
  public static class ThemeSettings
  {
    private static ThemeMode _currentTheme = ThemeMode.Dark;

    public static ThemeMode CurrentTheme => _currentTheme;

    public static event Action<ThemeMode>? ThemeChanged;

    /// <summary>
    /// Загружает язык из конфигурации при запуске.
    /// </summary>
    public static async Task InitializeAsync()
    {
      var themeItem = await UserInterfaceConfig.GetTheme();
      _currentTheme = themeItem;
      ThemeChanged?.Invoke(themeItem);
    }

    /// <summary>
    /// Устанавливает новый язык и сохраняет его в конфигурации.
    /// </summary>
    public static async Task SetThemeAsync(ThemeMode theme)
    {
      if (theme == _currentTheme)
        return;

      _currentTheme = theme;
      ThemeChanged?.Invoke(_currentTheme);
    }
  }
}
