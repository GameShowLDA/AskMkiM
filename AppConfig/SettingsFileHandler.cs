using System.Windows;
using AppConfig.Data.Device;
using AppConfig.Data.Execution;
using AppConfig.Data.MeasurementError;
using AppConfig.Data.Protocol;
using AppConfig.Data.Theme;
using Utilities.USB;

namespace AppConfig
{
  /// <summary>
  /// Класс для чтения настроек из файлов.
  /// </summary>
  static public class SettingsFileReader
  {

    static private USBMonitorService usbMonitorService = new USBMonitorService(Application.Current.Dispatcher);

    /// <summary>
    /// Считывает настройки программы из файлов.
    /// </summary>
    /// <returns>Задача, которая завершится после того, как все настройки будут считаны.</returns>
    static public async Task ReadAllSettingsAsync()
    {
      var executionTask = ExecutionSettingsManager.ReadExecutionModeAsync();
      var themeTask = ThemeSettingsManager.ReadThemeModeAsync();
      var protocolTask = ProtocolSettingsManager.ReadProtocolModeAsync();
      var measurementErrorTask = MeasurementErrorSettingsManager.ReadMeasurementErrorMode();
      var deviceConfigTask = DeviceSettingsManager.ReadDeviceModeAsync();
      await Task.WhenAll(executionTask, /*themeTask,*/ protocolTask, measurementErrorTask, deviceConfigTask);
    }

    static private async Task OnAdminRightsChanged(bool newRights)
    {
      await Config.SystemStateManager.SetAdminRights(newRights);
    }
  }
}
