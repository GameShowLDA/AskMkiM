using Message;
using System.Windows;
using System.Windows.Controls;
using static AppConfiguration.SystemState.SystemStateManager;
using static AppConfiguration.Execution.ExecutionConfig;

namespace UI.Controls.Settings.Execution
{
  /// <summary>
  /// Логика взаимодействия для ExecutionControl.xaml
  /// </summary>
  public partial class ExecutionControl : UserControl
  {
    private bool _canSave;

    public ExecutionControl()
    {
      InitializeComponent();
      Loaded += ExecutionControl_Loaded;
      Unloaded += ExecutionControl_Unloaded;
    }

    private void ExecutionControl_Unloaded(object sender, RoutedEventArgs e)
    {
      _canSave = false;
      IdleMode.CheckedChanged -= IdleMode_CheckedChanged;
      StepByStepMode.CheckedChanged -= StepByStepMode_CheckedChanged;
      ErrorSimulation.CheckedChanged -= ErrorSimulation_CheckedChanged;
      StopInError.CheckedChanged -= StopInError_CheckedChanged;
    }

    private async void ExecutionControl_Loaded(object? sender, RoutedEventArgs e)
    {
      _canSave = false;
      await SetConfiguration();

      IdleMode.CheckedChanged += IdleMode_CheckedChanged;
      StepByStepMode.CheckedChanged += StepByStepMode_CheckedChanged;
      ErrorSimulation.CheckedChanged += ErrorSimulation_CheckedChanged;
      StopInError.CheckedChanged += StopInError_CheckedChanged;

      _canSave = true;
    }

    private async void StopInError_CheckedChanged(object? sender, bool e) => await NewDataSaveAsync();
    private async void StepByStepMode_CheckedChanged(object? s, bool e) => await NewDataSaveAsync();
    private async void ErrorSimulation_CheckedChanged(object? s, bool e) => await NewDataSaveAsync();

    private async void IdleMode_CheckedChanged(object? sender, bool isChecked)
    {
      // isChecked — это новое значение из SettingsCard.CheckedChanged
      if (await GetIsActivePower() && isChecked)
      {
        MessageBoxCustom.Show("Отключите питание системы для перехода в холостой режим!",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        IdleMode.IsChecked = false; // откатываем
        return;
      }
      await NewDataSaveAsync();
    }


    /// <summary>
    /// Сохраняет новые данные конфигурации в модель и применяет их.
    /// </summary>
    private async Task NewDataSaveAsync()
    {
      if (!_canSave) return;

      await SetIdleMode((bool)IdleMode.IsChecked);
      await SetStepByStepMode((bool)StepByStepMode.IsChecked);
      await SetStopOnError((bool)StopInError.IsChecked);
      await SetIsErrorSimulationMode((bool)ErrorSimulation.IsChecked);

      await RewriteExecutionConfigAsync();
    }

    /// <summary>Читает конфигурацию и проставляет значения в UI.</summary>
    private async Task SetConfiguration()
    {
      IdleMode.IsChecked = await GetIsIdleModeEnabled();
      StepByStepMode.IsChecked = await GetIsStepByStepModeEnabled();
      StopInError.IsChecked = await GetIsStopOnErrorEnabled();
      ErrorSimulation.IsChecked = await GetIsErrorSimulationEnabled();
    }
  }
}
