namespace AppConfig.Data.Theme
{
  static internal class ThemeSettingsManager
  {
    /// <summary>
    /// Считывает параметры отображения данных в протоколе и задаёт их в программе.
    /// </summary>
    static internal async Task ReadThemeModeAsync()
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
