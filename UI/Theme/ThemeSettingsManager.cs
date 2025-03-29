using AppManager;

namespace UI.Theme
{
  /// <summary>
  /// Класс для управления настройками темы оформления приложения.
  /// Позволяет загружать и применять параметры отображения.
  /// </summary>
  static public class ThemeSettingsManager
  {
    /// <summary>
    /// Считывает параметры отображения данных в протоколе и задаёт их в программе.
    /// </summary>
    static public async Task ReadThemeModeAsync()
    {
      ThemeFileManager themeFileManager = new ThemeFileManager(FileLocations.ColorConfigPath);
      if (!await themeFileManager.CreateFileIfNotExistsAsync())
      {
        return;
      }

      await ThemeScheme.LoadSettingsColor(themeFileManager);
    }
  }
}
