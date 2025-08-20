using AppConfiguration.Parameter;

namespace AppConfiguration.Parameter
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
      var langItem = await ParameterConfig.GetLanguage();
      if (!string.IsNullOrWhiteSpace(langItem))
      {
        _currentLanguage = langItem.ToLower();
      }

      LanguageChanged?.Invoke(_currentLanguage);
    }

    /// <summary>
    /// Устанавливает новый язык и сохраняет его в конфигурации.
    /// </summary>
    public static async Task SetLanguageAsync(string lang)
    {
      if (string.Equals(_currentLanguage, lang, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(lang))
        return;


      _currentLanguage = lang.ToLower();
      LanguageChanged?.Invoke(_currentLanguage);
    }
  }
}
