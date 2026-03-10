using Ask.Core.Services.Config.Base;
using System.Globalization;

namespace Ask.Core.Services.Config.AppSettings
{
  public static class LanguageSettings
  {
    private static string _currentLanguage = "ru";

    public static string CurrentLanguage => _currentLanguage;

    public static event Action<string>? LanguageChanged;

    /// <summary>
    /// Загружает язык из конфигурации при запуске.
    /// </summary>
    public static async Task InitializeAsync()
    {
      var langItem = await UserInterfaceConfig.GetLanguage();
      _currentLanguage = NormalizeLanguageCode(langItem);

      LanguageChanged?.Invoke(_currentLanguage);
    }

    /// <summary>
    /// Устанавливает новый язык и сохраняет его в конфигурации.
    /// </summary>
    public static async Task SetLanguageAsync(string lang)
    {
      var normalized = NormalizeLanguageCode(lang);
      if (string.Equals(_currentLanguage, normalized, StringComparison.OrdinalIgnoreCase))
      {
        return;
      }

      _currentLanguage = normalized;
      LanguageChanged?.Invoke(_currentLanguage);
    }

    /// <summary>
    /// Приводит значение языка к поддерживаемому коду культуры ("ru" или "en").
    /// </summary>
    public static string NormalizeLanguageCode(string? lang)
    {
      if (string.IsNullOrWhiteSpace(lang))
      {
        return "ru";
      }

      var value = lang.Trim();
      var lowered = value.ToLowerInvariant();

      if (lowered.StartsWith("en", StringComparison.Ordinal))
      {
        return "en";
      }

      if (lowered.StartsWith("ru", StringComparison.Ordinal))
      {
        return "ru";
      }

      if (lowered.Contains("english", StringComparison.Ordinal) || lowered.Contains("англ", StringComparison.Ordinal))
      {
        return "en";
      }

      if (lowered.Contains("russian", StringComparison.Ordinal) || lowered.Contains("рус", StringComparison.Ordinal))
      {
        return "ru";
      }

      try
      {
        var culture = CultureInfo.GetCultureInfo(value);
        var code = culture.TwoLetterISOLanguageName.ToLowerInvariant();
        if (code == "en" || code == "ru")
        {
          return code;
        }
      }
      catch
      {
        // Игнорируем некорректные или неизвестные значения языка.
      }

      return "ru";
    }
  }
}
