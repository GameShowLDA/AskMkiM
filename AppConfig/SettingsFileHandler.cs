using System.Windows;
using AppConfig.Data.Device;
using AppConfig.Data.Execution;
using AppConfig.Data.MeasurementError;
using AppConfig.Data.Protocol;
using AppConfig.Data.Theme;
using Utilities.USB;
using static Utilities.LoggerUtility;

namespace AppConfig
{
  /// <summary>
  /// Класс для чтения настроек из файлов.
  /// </summary>
  static public class SettingsFileReader
  {

    /// <summary>
    /// Считывает настройки программы из файлов.
    /// </summary>
    /// <returns>Задача, которая завершится после того, как все настройки будут считаны.</returns>
    static public async Task ReadAllSettingsAsync()
    {
      try
      {
        var executionTask = ExecutionSettingsManager.ReadExecutionModeAsync();
        var themeTask = ThemeSettingsManager.ReadThemeModeAsync();
        var protocolTask = ProtocolSettingsManager.ReadProtocolModeAsync();
        var measurementErrorTask = MeasurementErrorSettingsManager.ReadMeasurementErrorMode();
        var deviceTask = DeviceSettingsManager.ReadDeviceConfigAsync();

        await Task.WhenAll(executionTask, themeTask, protocolTask, measurementErrorTask, deviceTask);
      }
      catch (Exception ex)
      {
        var stackTrace = new System.Diagnostics.StackTrace();
        var callingFrame = stackTrace.GetFrame(1);
        var method = callingFrame.GetMethod();
        var className = method.DeclaringType.FullName;
        var methodName = method.Name;

        LogError($"Ошибка в методе {className}.{methodName}: {ex.Message}");
      }
    }
  }
}
