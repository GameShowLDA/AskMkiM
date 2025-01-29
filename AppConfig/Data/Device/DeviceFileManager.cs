using AppConfig.Abstract;
using Utilities.FilesUtility;

namespace AppConfig.Data.Device
{
  public class DeviceFileManager : ConfigurationManagerBase<List<object>>
  {
    private readonly JsonUtility<object> _jsonUtility;

    /// <summary>
    /// Читает данные из конфигурационного файла, содержащего список устройств.
    /// </summary>
    /// <returns>Список объектов типа <see cref="DeviceModel"/>.</returns>
    public override async Task<List<object>> ReadFileAsync() => await _jsonUtility.ReadAsync();

    /// <summary>
    /// Перезаписывает данные о списке устройств в конфигурационный файл.
    /// </summary>
    /// <param name="data">Список устройств измерений для записи в файл.</param>
    public override async Task RewriteFileAsync(List<object> data) => await _jsonUtility.RewriteAsync(data);

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DeviceFileManager"/> с заданным путем к файлу.
    /// </summary>
    /// <param name="pathFile">Путь к YAML файлу, в котором будет храниться конфигурация модели выполнения.</param>
    public DeviceFileManager(string pathFile) : base(pathFile)
    {
      _jsonUtility = new JsonUtility<object>(pathFile);
    }

  }
}
