using System.Windows.Input;
using static AppConfig.Config.ExecutionConfig;

namespace Mode.Settings.Execution
{
  partial class ExecutionControl
  {
    /// <summary>
    /// Устанавливает конфигурацию на основе данных, прочитанных из YAML-файла.
    /// </summary>
    private async void SetConfiguration()
    {
      idleMode.IsChecked = await GetIsIdleModeEnabled();
      stepByStep.IsChecked = await GetIsStepByStepModeEnabled();
      stopOnError.IsChecked = await GetIsStopOnErrorEnabled();
      isErrorSimulation.IsChecked = await GetIsErrorSimulationEnabled();
    }

    /// <summary>
    /// Проверяет, является ли введенный текст числовым значением.
    /// </summary>
    /// <param name="e">Аргументы события предварительного ввода текста.</param>
    /// <returns>True, если текст не является числовым; в противном случае - false.</returns>
    private bool CheckIsNumeric(TextCompositionEventArgs e)
    {
      if (!(double.TryParse(e.Text, out _)))
      {
        e.Handled = true;
        return true;
      }
      return false;
    }

    /// <summary>
    /// Сохраняет новые данные конфигурации в модель и применяет их.
    /// </summary>
    private async Task NewDataSaveAsync()
    {
      if (start)
      {
        await SetIdleMode((bool)idleMode.IsChecked);
        await SetStepByStepMode((bool)stepByStep.IsChecked);
        await SetStopOnError((bool)stopOnError.IsChecked);
        await SetIsErrorSimulationMode((bool)isErrorSimulation.IsChecked);

        await RewriteExecutionConfigAsync();
      }
    }
  }
}
