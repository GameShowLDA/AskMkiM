using AppManager.Abstract;
using Utilities.FilesUtility;

namespace AppManager.Data.MeasurementError
{
  /// <summary>
  /// Класс для управления файлами, содержащими данные о погрешностях измерений.
  /// Обеспечивает операции чтения, записи, добавления и удаления погрешностей измерений.
  /// </summary>
  public class MeasurementErrorFileManage : ConfigurationManagerBase<List<MeasurementErrorModel>>
  {
    private readonly JsonUtility<MeasurementErrorModel> _jsonUtility;

    /// <summary>
    /// Читает данные из конфигурационного файла, содержащего список погрешностей измерений.
    /// </summary>
    /// <returns>Список объектов типа <see cref="MeasurementErrorModel"/>.</returns>
    public override async Task<List<MeasurementErrorModel>> ReadFileAsync() => await _jsonUtility.ReadAsync();

    /// <summary>
    /// Перезаписывает данные о списке погрешностей измерений в конфигурационный файл.
    /// </summary>
    /// <param name="data">Список погрешностей измерений для записи в файл.</param>
    public override async Task RewriteFileAsync(List<MeasurementErrorModel> data) => await _jsonUtility.RewriteAsync(data);

    /// <summary>
    /// Добавляет новую погрешность измерений в файл.
    /// </summary>
    /// <param name="data">Модель погрешности измерения, которую нужно добавить.</param>
    public async Task CreateAsync(MeasurementErrorModel data) => await _jsonUtility.CreateAsync(data);

    /// <summary>
    /// Удаляет погрешность измерений из файла.
    /// </summary>
    /// <param name="data">Модель погрешности измерения, которую нужно удалить.</param>
    public async Task DeleteAsync(MeasurementErrorModel data) => await _jsonUtility.DeleteAsync(data);

    /// <summary>
    /// Конструктор класса <see cref="MeasurementErrorFileManage"/>.
    /// </summary>
    /// <param name="pathFile">Путь к конфигурационному файлу, содержащему погрешности измерений.</param>
    public MeasurementErrorFileManage(string pathFile) : base(pathFile)
    {
      _jsonUtility = new JsonUtility<MeasurementErrorModel>(pathFile);
    }
  }
}
