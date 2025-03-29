using AppManager.Abstract;
using Utilities.FilesUtility;

namespace AppManager.Data.Protocol
{
  internal class ProtocolFileManager : ConfigurationManagerBase<ProtocolModel>
  {
    private readonly YamlUtility<ProtocolModel> _yamlHelper;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ProtocolModel"/> с заданным путем к файлу.
    /// </summary>
    /// <param name="pathFile">Путь к YAML файлу, в котором будет храниться конфигурация модели выполнения.</param>
    internal ProtocolFileManager(string pathFile) : base(pathFile)
    {
      _yamlHelper = new YamlUtility<ProtocolModel>(pathFile);
    }

    /// <summary>
    /// Читает данные из YAML файла и десериализует их в объект типа <see cref="ProtocolModel"/>.
    /// </summary>
    /// <returns>Объект типа <see cref="ProtocolModel"/>, содержащий данные из конфигурационного файла.</returns>
    public override async Task<ProtocolModel> ReadFileAsync() => await _yamlHelper.ReadAsync();

    /// <summary>
    /// Перезаписывает данные в YAML файл, сериализуя переданный объект <see cref="ProtocolModel"/>.
    /// </summary>
    /// <param name="protocolModel">Объект <see cref="ProtocolModel"/> с новыми данными для записи в файл.</param>
    public override async Task RewriteFileAsync(ProtocolModel protocolModel) => await _yamlHelper.RewriteAsync(protocolModel);
  }
}
