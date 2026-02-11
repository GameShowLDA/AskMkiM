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
        new ResourceManager("UI.Localization.Strings", typeof(LocalizationService).Assembly);

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
        var result = _resourceManager.GetString(key, CultureInfo.CurrentUICulture);
        return result ?? $"!{key}!";
      }
      catch
      {
        return $"!{key}!";
      }
    }

    private static bool _metadataOverridden = false;

    /// <summary>
    /// Принудительно задаёт культуру приложения.
    /// </summary>
    private static void SetCulture(string langCode)
    {
      try
      {
        var culture = new CultureInfo(langCode);

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
    /// Возвращает список культур, для которых реально есть ресурсы "UI.Localization.Strings".
    /// Включает и нейтральный ресурс (Strings.resx), если он есть.
    /// </summary>
    public static IReadOnlyList<CultureInfo> GetAvailableCultures()
    {
      var list = new List<CultureInfo>();

      try
      {
        if (_resourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, false) != null)
        {
          list.Add(new CultureInfo(LanguageSettings.CurrentLanguage));
        }
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
