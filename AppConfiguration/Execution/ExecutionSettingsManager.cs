using AppConfiguration.Base;

namespace AppConfiguration.Execution
{
  static internal class ExecutionSettingsManager
  {
    /// <summary>
    /// Считывает параметры выполнения режимов и задаёт их в программе.
    /// </summary>
    static internal async Task ReadExecutionModeAsync()
    {
      ExecutionFileManager executionFileManager = new ExecutionFileManager(FileLocations.ExecutionConfigPath);

      if (!await executionFileManager.CreateFileIfNotExistsAsync())
      {
        return;
      }

      ExecutionModel executionModel = await executionFileManager.ReadFileAsync();
      if (executionModel == null)
      {
        return;
      }

      await ConfigModel.SetExecutionModelAsync(executionModel);
    }
  }
}
