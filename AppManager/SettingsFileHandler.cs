using AppManager.Data.Execution;
using AppManager.Data.MeasurementError;
using AppManager.Data.Protocol;
using AppManager.Data.Theme;
using AppManager.DataBase;
using Microsoft.EntityFrameworkCore;
using static Utilities.LoggerUtility;

namespace AppManager
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
        var protocolTask = ProtocolSettingsManager.ReadProtocolModeAsync();
        var measurementErrorTask = MeasurementErrorSettingsManager.ReadMeasurementErrorMode();
        var db = InitializeDB();

        await Task.WhenAll(executionTask, protocolTask, measurementErrorTask, db);
        await ThemeSettingsManager.ReadThemeModeAsync();
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

    /// <summary>
    /// Инициализирует базу данных, применяя все ожидающие миграции.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию инициализации базы данных.</returns>
    static public async Task InitializeDB()
    {
      var dbContextFactory = new DbContextFactory();
      using (var scope = dbContextFactory.CreateDbContext(new string[0]))
      {
        await scope.Database.MigrateAsync();
      }
    }
  }
}
