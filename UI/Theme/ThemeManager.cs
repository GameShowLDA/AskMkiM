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

      var uri = theme == ThemeMode.Dark
          ? new Uri("/UI;component/Resources/Theme/dark.xaml", UriKind.Relative)
          : new Uri("/UI;component/Resources/Theme/light.xaml", UriKind.Relative);

      var newThemeDict = (ResourceDictionary)Application.LoadComponent(uri);
      LogInformation($"[ThemeManager] Словарь успешно загружен: {uri}");

      Application.Current.Resources.Clear();
      Application.Current.Resources.MergedDictionaries.Add(newThemeDict);
    }


    private static bool _initialized;

    public static void Initialize()
    {
      if (_initialized) return;
      _initialized = true;
      ThemeSettings.ThemeChanged += ApplyThemeAsync;
    }
  }
}
