using Ask.Core.Services.App;
using Ask.Core.Shared.DTO.Settings;
using System.IO;

namespace Ask.Core.Services.Config.AppSettings;

/// <summary>
/// Загружает фиксированные задержки оборудования из YAML-файла.
/// </summary>
public sealed class DelaySettingsFileService
{
  /// <summary>
  /// Имя файла настроек задержек.
  /// </summary>
  public const string SettingsFileName = "delaySettings.yaml";

  private readonly string _filePath;
  private readonly YamlService<DelaySettings> _yamlService;

  /// <summary>
  /// Создаёт сервис для каталога приложения.
  /// </summary>
  public DelaySettingsFileService()
    : this(AppDomain.CurrentDomain.BaseDirectory)
  {
  }

  /// <summary>
  /// Создаёт сервис для заданного каталога приложения.
  /// </summary>
  /// <param name="applicationDirectory">Каталог приложения, содержащий подкаталог Settings.</param>
  public DelaySettingsFileService(string applicationDirectory)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(applicationDirectory);

    _filePath = Path.Combine(
      Path.GetFullPath(applicationDirectory),
      "Settings",
      SettingsFileName);
    _yamlService = new YamlService<DelaySettings>(_filePath);
  }

  /// <summary>
  /// Загружает настройки задержек и записывает значения по умолчанию в отсутствующий или пустой файл.
  /// </summary>
  /// <returns>Загруженные настройки задержек.</returns>
  public DelaySettings Load()
  {
    if (!File.Exists(_filePath) || string.IsNullOrWhiteSpace(File.ReadAllText(_filePath)))
    {
      var defaultSettings = new DelaySettings();
      _yamlService.Save(defaultSettings);
      return defaultSettings;
    }

    DelaySettings settings = _yamlService.Load();
    settings.BreakdownTester ??= new BreakdownTesterDelaySettings();
    settings.ModuleRelayControl ??= new ModuleRelayControlDelaySettings();
    return settings;
  }

  /// <summary>
  /// Сохраняет настройки задержек в YAML-файл.
  /// </summary>
  /// <param name="settings">Настройки задержек оборудования.</param>
  /// <exception cref="ArgumentNullException">
  /// Выбрасывается, если <paramref name="settings"/> равен <see langword="null"/>.
  /// </exception>
  public void Save(DelaySettings settings)
  {
    ArgumentNullException.ThrowIfNull(settings);
    settings.BreakdownTester ??= new BreakdownTesterDelaySettings();
    settings.ModuleRelayControl ??= new ModuleRelayControlDelaySettings();
    _yamlService.Save(settings);
  }
}
