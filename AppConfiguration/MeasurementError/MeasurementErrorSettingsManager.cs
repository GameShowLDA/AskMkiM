using AppConfiguration.Base;

namespace AppConfiguration.MeasurementError
{
  static public class MeasurementErrorSettingsManager
  {
    /// <summary>
    /// Считывает параметры погрешностей и задаёт их в программе.
    /// </summary>
    static public async Task ReadMeasurementErrorMode()
    {
      MeasurementErrorFileManage measurementErrorFileManage = new MeasurementErrorFileManage(FileLocations.MeasurementErrorConfigPath);

      if (!await measurementErrorFileManage.CreateFileIfNotExistsAsync())
      {
        return;
      }

      List<MeasurementErrorModel> measurementErrorModel = await measurementErrorFileManage.ReadFileAsync();
      if (measurementErrorModel == null)
      {
        return;
      }

      ConfigModel.SetMeasurementErrorModels(measurementErrorModel);
    }
  }
}
