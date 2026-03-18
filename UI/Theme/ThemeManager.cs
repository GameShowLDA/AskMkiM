using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using System.Windows;
using static Ask.LogLib.LoggerUtility;

namespace UI.Theme
{
  public static class ThemeManager
  {
    private static ResourceDictionary? _currentThemeDict;

    public static void ApplyThemeAsync(ThemeMode theme)
    {
      LogInformation($"[ThemeManager] Вызван ApplyThemeAsync. Тема = {theme}");

      var uri = GetThemeUri(theme);

      var newThemeDict = (ResourceDictionary)Application.LoadComponent(uri);
      LogInformation($"[ThemeManager] Словарь успешно загружен: {uri}");

      Application.Current.Resources.Clear();
      Application.Current.Resources.MergedDictionaries.Add(newThemeDict);
    }

    private static Uri GetThemeUri(ThemeMode theme) =>
      theme switch
      {
        ThemeMode.Dark => new Uri("/UI;component/Resources/Theme/dark.xaml", UriKind.Relative),
        ThemeMode.Light => new Uri("/UI;component/Resources/Theme/light.xaml", UriKind.Relative),
        ThemeMode.DarkCustom => new Uri("/UI;component/Resources/Theme/dark.custom.xaml", UriKind.Relative),
        ThemeMode.LightCustom => new Uri("/UI;component/Resources/Theme/light.custom.xaml", UriKind.Relative),
        _ => new Uri("/UI;component/Resources/Theme/dark.xaml", UriKind.Relative),
      };

    private static bool _initialized;

    public static void Initialize()
    {
      if (_initialized) return;
      _initialized = true;
      ThemeSettings.ThemeChanged += ApplyThemeAsync;
    }
  }
}
