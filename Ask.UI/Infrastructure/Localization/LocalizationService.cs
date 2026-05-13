using Ask.Core.Services.Config.AppSettings;
using System.Globalization;
using System.Resources;
using System.Windows;
using static Ask.LogLib.LoggerUtility;

namespace Ask.UI.Infrastructure.Localization
{

  /// <summary>
  /// Сервис локализации для получения строк из ресурсов по текущему языку.
  /// Поддерживает автоматическое переключение языка при изменении настроек.
  /// </summary>
  public static class LocalizationService
  {
    private static readonly ResourceManager _resourceManager =
        new ResourceManager(
          "Ask.UI.Resources.Localization.Language.Strings",
          typeof(LocalizationService).Assembly);

    static LocalizationService()
    {
      LanguageSettings.LanguageChanged += langCode =>
      {
        SetCulture(langCode);
        LocalizedString.RefreshAll();
      };

      SetCulture(LanguageSettings.CurrentLanguage);
    }

    /// <summary>
    /// Возвращает локализованную строку по ключу.
    /// Если ключ не найден — возвращает "!ключ!".
    /// </summary>
    public static string Get(string key)
    {
      try
      {
        var culture = ResolveCulture(LanguageSettings.CurrentLanguage);
        var result = _resourceManager.GetString(key, culture)
                     ?? _resourceManager.GetString(key, CultureInfo.InvariantCulture);
        return result ?? $"!{key}!";
      }
      catch
      {
        return $"!{key}!";
      }
    }

    /// <summary>
    /// Принудительно применяет текущий язык из настроек и обновляет все локализованные строки в UI.
    /// </summary>
    public static void RefreshCurrentLanguage()
    {
      SetCulture(LanguageSettings.CurrentLanguage);
      LocalizedString.RefreshAll();
    }

    private static bool _metadataOverridden = false;

    /// <summary>
    /// Принудительно задаёт культуру приложения.
    /// </summary>
    private static void SetCulture(string langCode)
    {
      try
      {
        var culture = ResolveCulture(langCode);

        void ApplyCulture()
        {
          CultureInfo.DefaultThreadCurrentCulture = culture;
          CultureInfo.DefaultThreadCurrentUICulture = culture;

          Thread.CurrentThread.CurrentCulture = culture;
          Thread.CurrentThread.CurrentUICulture = culture;

          if (!_metadataOverridden && Application.Current != null)
          {
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    System.Windows.Markup.XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

            _metadataOverridden = true;

            LogInformation(
              $"[LocalizationService] LanguageProperty metadata overridden for culture: {culture.Name}");
          }
          else if (_metadataOverridden)
          {
            LogInformation(
              $"[LocalizationService] LanguageProperty metadata already set, skipping override for: {culture.Name}");
          }

          LogInformation(
            $"[LocalizationService] UI Thread: {Thread.CurrentThread.ManagedThreadId}, Culture: {CultureInfo.CurrentUICulture.Name}");
        }

        if (Application.Current?.Dispatcher?.CheckAccess() == false)
        {
          Application.Current.Dispatcher.Invoke(ApplyCulture);
        }
        else
        {
          ApplyCulture();
        }
      }
      catch (Exception ex)
      {
        LogException(ex);
      }
    }

    /// <summary>
    /// Приводит значение языка к поддерживаемой культуре.
    /// </summary>
    private static CultureInfo ResolveCulture(string? langCode)
    {
      var normalized = LanguageSettings.NormalizeLanguageCode(langCode);
      return normalized == "en"
        ? CultureInfo.GetCultureInfo("en")
        : CultureInfo.GetCultureInfo("ru");
    }

    /// <summary>
    /// Возвращает список культур, для которых реально есть ресурсы.
    /// Обязательно добавляем нейтральный ресурс (Strings.resx), который у нас содержит русский.
    /// </summary>
    public static IReadOnlyList<CultureInfo> GetAvailableCultures()
    {
      var list = new List<CultureInfo>();

      try
      {
        if (_resourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, false) != null)
          list.Add(new CultureInfo("ru")); // нейтральные ресурсы — русский
      }
      catch { }

      foreach (var c in CultureInfo.GetCultures(CultureTypes.AllCultures))
      {
        if (string.IsNullOrEmpty(c.Name)) continue;
        try
        {
          var rs = _resourceManager.GetResourceSet(c, true, false);
          if (rs != null) list.Add(c);
        }
        catch { }
      }

      return list
        .OrderBy(c => c.Name.Length)
        .GroupBy(c => c.TwoLetterISOLanguageName)
        .Select(g => g.First())
        .ToList();
    }

    /// <summary>Красивое название культуры (с заглавной буквы, в её же языке).</summary>
    public static string GetDisplayName(CultureInfo culture)
    {
      var ti = culture.TextInfo;
      var name = culture.NativeName;
      return ti.ToTitleCase(name);
    }
  }
}
