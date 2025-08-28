using AppConfiguration.Parameter;
using System.Globalization;
using System.Resources;
using System.Threading;
using System.Windows;
using UI.Localization;

namespace UI.Localization
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
      // Подписка на смену языка
      LanguageSettings.LanguageChanged += langCode =>
      {
        SetCulture(langCode);
        LocalizedString.RefreshAll();
      };

      // Установка культуры при запуске
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
          // Устанавливаем культуру для всех новых потоков
          CultureInfo.DefaultThreadCurrentCulture = culture;
          CultureInfo.DefaultThreadCurrentUICulture = culture;

          // Устанавливаем культуру для текущего (UI) потока
          Thread.CurrentThread.CurrentCulture = culture;
          Thread.CurrentThread.CurrentUICulture = culture;

          // Один раз переопределяем WPF-механизм локализации
          if (!_metadataOverridden && Application.Current != null)
          {
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    System.Windows.Markup.XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

            _metadataOverridden = true;

            Utilities.LoggerUtility.LogInformation(
              $"[LocalizationService] LanguageProperty metadata overridden for culture: {culture.Name}");
          }
          else if (_metadataOverridden)
          {
            Utilities.LoggerUtility.LogInformation(
              $"[LocalizationService] LanguageProperty metadata already set, skipping override for: {culture.Name}");
          }

          // Подтверждение потока
          Utilities.LoggerUtility.LogInformation(
            $"[LocalizationService] UI Thread: {Thread.CurrentThread.ManagedThreadId}, Culture: {CultureInfo.CurrentUICulture.Name}");
        }

        // 💥 Оборачиваем только если нужно
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
        Utilities.LoggerUtility.LogException(ex);
      }
    }

    /// <summary>
    /// Возвращает список культур, для которых реально есть ресурсы "UI.Localization.Strings".
    /// Включает и нейтральный ресурс (Strings.resx), если он есть.
    /// </summary>
    public static IReadOnlyList<CultureInfo> GetAvailableCultures()
    {
      var list = new List<CultureInfo>();

      // 1) Попытка получить нейтральный ресурс (Strings.resx внутри основной сборки)
      try
      {
        if (_resourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, false) != null)
        {
          // Нейтральная культура не имеет имени, подставим язык из настроек как базовый
          // (или можешь захардкодить "ru"/"en" под свой проект)
          list.Add(new CultureInfo(LanguageSettings.CurrentLanguage));
        }
      }
      catch { /* ignore */ }

      // 2) Перебор всех культур и запрос ResourceSet (возвращает null, если сателлита нет)
      foreach (var c in CultureInfo.GetCultures(CultureTypes.AllCultures))
      {
        if (string.IsNullOrEmpty(c.Name)) continue; // пропускаем InvariantCulture
        try
        {
          var rs = _resourceManager.GetResourceSet(c, true, false);
          if (rs != null) list.Add(c);
        }
        catch { /* ignore */ }
      }

      // Уберём дубли по имени (ru, ru-RU и т.п. оставим один — с более «общим» именем)
      return list
        .OrderBy(c => c.Name.Length)
        .GroupBy(c => c.TwoLetterISOLanguageName)   // "ru", "en"
        .Select(g => g.First())
        .ToList();
    }

    /// <summary>Красивое название культуры (с заглавной буквы, в её же языке).</summary>
    public static string GetDisplayName(CultureInfo culture)
    {
      var ti = culture.TextInfo;
      var name = culture.NativeName; // например, "русский (Россия)"
                                     // Нормализуем регистр первой буквы
      return ti.ToTitleCase(name);
    }
  }
}
