using AppConfig.Abstract;
using Utilities.FilesUtility;

namespace UI.Theme
{
  /// <summary>
  /// Класс для управления конфигурационным файлом темы оформления приложения.
  /// Реализует чтение и запись данных в формате YAML.
  /// </summary>
  internal class ThemeFileManager : ConfigurationManagerBase<ThemeModel>
  {
    private readonly YamlUtility<ThemeModel> _yamlHelper;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ThemeFileManager"/> с заданным путем к файлу.
    /// </summary>
    /// <param name="pathFile">Путь к YAML файлу, в котором будет храниться конфигурация модели выполнения.</param>
    internal ThemeFileManager(string pathFile) : base(pathFile)
    {
      _yamlHelper = new YamlUtility<ThemeModel>(pathFile);
    }

    /// <summary>
    /// Читает данные из YAML файла и десериализует их в объект типа <see cref="ThemeModel"/>.
    /// </summary>
    /// <returns>Объект типа <see cref="ThemeModel"/>, содержащий данные из конфигурационного файла.</returns>
    public override async Task<ThemeModel> ReadFileAsync() => await _yamlHelper.ReadAsync();

    /// <summary>
    /// Перезаписывает данные в YAML файл, сериализуя переданный объект <see cref="ThemeModel"/>.
    /// </summary>
    /// <param name="executionModel">Объект <see cref="ThemeModel"/> с новыми данными для записи в файл.</param>
    public override async Task RewriteFileAsync(ThemeModel executionModel) => await _yamlHelper.RewriteAsync(executionModel);
  }
}
