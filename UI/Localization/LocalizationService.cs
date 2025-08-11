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



  }
}
