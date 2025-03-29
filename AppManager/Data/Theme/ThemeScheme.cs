using System.Windows;
using System.Windows.Media;
using static Utilities.LoggerUtility;

namespace AppManager.Data.Theme
{
  /// <summary>
  /// Класс для работы с темами приложения, включая загрузку, сохранение и преобразование цветовых настроек.
  /// </summary>
  static internal class ThemeScheme
  {
    /// <summary>
    /// Загружает настройки цветов для темы приложения.
    /// </summary>
    /// <param name="themeManager">Менеджер для работы с файлами темы.</param>
    public static async Task<ThemeModel> LoadSettingsColor(ThemeFileManager themeManager)
    {
      try
      {
        ThemeModel themeModel = await themeManager.ReadFileAsync();
        bool hasError = false;

        if (themeModel != null)
        {
          foreach (var property in themeModel.GetType().GetProperties())
          {
            var propertyValue = property.GetValue(themeModel);
            if (propertyValue == null)
            {
              hasError = true;
            }
          }
        }
        else
        {
          hasError = true;
        }

        if (hasError)
        {
          themeModel = GetDarkTheme();
          await themeManager.RewriteFileAsync(themeModel);
        }

        ApplyThemeToResources(themeModel);

        return themeModel;
      }
      catch (Exception ex)
      {
        LogError(ex.Message);
        throw;
      }
    }

    /// <summary>
    /// Применяет цвета из <see cref="ThemeModel"/> в ресурсы приложения.
    /// </summary>
    /// <param name="themeModel">Модель темы с цветами.</param>
    private static void ApplyThemeToResources(ThemeModel themeModel)
    {
      var resourceDictionary = new ResourceDictionary
      {
        Source = new Uri("pack://application:,,,/UI;component/Style.xaml"),
      };

      if (!Application.Current.Resources.MergedDictionaries.Contains(resourceDictionary))
      {
        Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
        LogInformation("Ресурс Style.xaml был добавлен в MergedDictionaries.");
      }

      if (Application.Current.Resources.MergedDictionaries.Contains(resourceDictionary))
      {
        resourceDictionary["PrimaryColor"] = FromHex(themeModel.PrimaryColor);
        resourceDictionary["SecondaryColor"] = FromHex(themeModel.SecondaryColor);
        resourceDictionary["ForegroundColor"] = FromHex(themeModel.ForegroundColor);
        resourceDictionary["ActiveColor"] = FromHex(themeModel.ActiveColor);
        resourceDictionary["IsCheckedColor"] = FromHex(themeModel.IsCheckedColor);
        LogInformation("Ресурсы успешно обновлены.");
      }
      else
      {
        LogError("Ошибка: Ресурс не найден в MergedDictionaries.");
      }

      Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
    }

    /// <summary>
    /// Преобразует строку шестнадцатеричного представления цвета в объект Color.
    /// </summary>
    /// <param name="hex">Строка шестнадцатеричного представления цвета (с или без "#", 6 или 8 символов).</param>
    /// <returns>Объект Color, соответствующий входной строке.</returns>
    /// <exception cref="ArgumentNullException">Выбрасывается, если входная строка пуста или null.</exception>
    public static Color FromHex(string hex)
    {
      if (string.IsNullOrEmpty(hex))
      {
        throw new ArgumentNullException(nameof(hex), "Hex строка не может быть пустой.");
      }

      hex = hex.Replace("#", "");
      byte a = 255, r = 0, g = 0, b = 0;

      if (hex.Length == 6)
      {
        r = Convert.ToByte(hex.Substring(0, 2), 16);
        g = Convert.ToByte(hex.Substring(2, 2), 16);
        b = Convert.ToByte(hex.Substring(4, 2), 16);
      }
      else if (hex.Length == 8)
      {
        a = Convert.ToByte(hex.Substring(0, 2), 16);
        r = Convert.ToByte(hex.Substring(2, 2), 16);
        g = Convert.ToByte(hex.Substring(4, 2), 16);
        b = Convert.ToByte(hex.Substring(6, 2), 16);
      }

      return Color.FromArgb(a, r, g, b);
    }

    /// <summary>
    /// Возвращает экземпляр темной темы с заранее заданными цветами.
    /// </summary>
    /// <returns>Объект <see cref="ThemeModel"/> с настройками для темной темы.</returns>
    static public ThemeModel GetDarkTheme()
    {
      return new ThemeModel
      {
        PrimaryColor = "#465060",
        SecondaryColor = "#303843",
        ForegroundColor = "#f3f0f9",
        ActiveColor = "#1ca3e9",
        IsCheckedColor = "#1f242b",
      };
    }

    /// <summary>
    /// Возвращает экземпляр светлой темы с заранее заданными цветами.
    /// </summary>
    /// <returns>Объект <see cref="ThemeModel"/> с настройками для светлой темы.</returns>
    static public ThemeModel GetLightTheme()
    {
      return new ThemeModel
      {
        PrimaryColor = "#E0F7FA",
        SecondaryColor = "#AEEEEE",
        ForegroundColor = "#002B36",
        ActiveColor = "#FF4081",
        IsCheckedColor = "#B2EBF2",
      };
    }
  }
}
