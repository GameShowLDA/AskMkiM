namespace AppManager.Data.Theme
{
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
