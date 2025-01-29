using AppConfig.Config;

namespace AppConfig.Data.MeasurementError
{
  static internal class MeasurementErrorSettingsManager
  {
    /// <summary>
    /// Считывает параметры погрешностей и задаёт их в программе.
    /// </summary>
    static internal async Task ReadMeasurementErrorMode()
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
