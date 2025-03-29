using AppConfiguration.Base;
using Utilities.FilesUtility;

namespace AppConfiguration.Execution
{
  /// <summary>
  /// Класс для управления файлами конфигурации, связанными с данными типа <see cref="ExecutionModel"/>.
  /// Выполняет операции чтения и записи в YAML файл, а также обеспечивает работу с конфигурацией модели выполнения.
  /// </summary>
  internal class ExecutionFileManager : ConfigurationManagerBase<ExecutionModel>
  {
    private readonly YamlUtility<ExecutionModel> _yamlHelper;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ExecutionFileManager"/> с заданным путем к файлу.
    /// </summary>
    /// <param name="pathFile">Путь к YAML файлу, в котором будет храниться конфигурация модели выполнения.</param>
    internal ExecutionFileManager(string pathFile) : base(pathFile)
    {
      _yamlHelper = new YamlUtility<ExecutionModel>(pathFile);
    }

    /// <summary>
    /// Читает данные из YAML файла и десериализует их в объект типа <see cref="ExecutionModel"/>.
    /// </summary>
    /// <returns>Объект типа <see cref="ExecutionModel"/>, содержащий данные из конфигурационного файла.</returns>
    public override async Task<ExecutionModel> ReadFileAsync() => await _yamlHelper.ReadAsync();

    /// <summary>
    /// Перезаписывает данные в YAML файл, сериализуя переданный объект <see cref="ExecutionModel"/>.
    /// </summary>
    /// <param name="executionModel">Объект <see cref="ExecutionModel"/> с новыми данными для записи в файл.</param>
    public override async Task RewriteFileAsync(ExecutionModel executionModel) => await _yamlHelper.RewriteAsync(executionModel);
  }
}
