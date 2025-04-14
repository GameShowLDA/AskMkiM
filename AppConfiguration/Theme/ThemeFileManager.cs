using AppConfiguration.Base;
using Utilities.FilesUtility;

namespace AppConfiguration.Theme
{
  /// <summary>
  /// Менеджер файлов тем, реализующий функциональность загрузки и записи конфигурации темы.
  /// Наследуется от <see cref="ConfigurationManagerBase{ThemeModel}"/>.
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
