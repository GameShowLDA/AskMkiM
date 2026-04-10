using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Settings;
using Message;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UI.Controls.Settings.Execution
{
  /// <summary>
  /// Логика взаимодействия для ExecutionControl.xaml
  /// </summary>
  public partial class ExecutionControl : UserControl
  {
    private bool _isInitialized;

    /// <summary>
    /// Базовая (сохранённая) модель выполнения, считанная при загрузке.
    /// Используется как эталон для сравнения с текущими значениями UI.
    /// </summary>
    private SettingsExecutionDto _baseExecutionModel { get; set; }

    /// <summary>
    /// Глобальный флаг наличия несохранённых изменений в разделе.
    /// <para>True — есть отличия от сохранённой модели; False — всё совпадает.</para>
    /// </summary>
    public bool HasUnsavedChanges { get; private set; }

    public ExecutionControl()
    {
      InitializeComponent();
      Loaded += ExecutionControl_Loaded;
      EventAggregator.Subscribe<SystemStateEvents.PowerChanged>(e => ChangeVisible(e.IsPowered));
      ChangeVisible(SystemStateManager.GetIsActivePower());
    }

    private void ChangeVisible(bool isPowered)
    {
      Dispatcher.Invoke(() =>
      {
        if (isPowered)
        {
          IdleMode.Visibility = Visibility.Collapsed;
          ErrorSimulation.Visibility = Visibility.Collapsed;
        }
        else
        {
          IdleMode.Visibility = Visibility.Visible;
          ErrorSimulation.Visibility = Visibility.Visible;
        }
      });
    }

    private async void ExecutionControl_Loaded(object sender, RoutedEventArgs e)
    {
      _baseExecutionModel = await ExecutionConfig.GetExecitonModel();
      DefalultData();

      if (!_isInitialized)
      {
        StopInError.CheckedChanged += CheckedChanged;
        StepByStepMode.CheckedChanged += CheckedChanged;
        ErrorSimulation.CheckedChanged += CheckedChanged;
        IdleMode.CheckedChanged += IdleMode_CheckedChanged;

        Success.PreviewMouseDown += Success_PreviewMouseDown;
        Error.PreviewMouseDown += Error_PreviewMouseDown;
        _isInitialized = true;
      }

      Error.Visibility = Visibility.Collapsed;
      Success.Visibility = Visibility.Collapsed;
      HasUnsavedChanges = false;
    }
    /// <summary>
    /// Клик по галочке «сохранить»: сохраняет текущую модель,
    /// перечитывает базу и скрывает индикаторы изменений.
    /// </summary>
    private async void Success_PreviewMouseDown(object sender, MouseButtonEventArgs e) => await SaveData();

    public async Task SaveData()
    {
      await ExecutionConfig.SaveExecutionModel(GetModel());
      _baseExecutionModel = await ExecutionConfig.GetExecitonModel();

      Error.Visibility = Visibility.Collapsed;
      Success.Visibility = Visibility.Collapsed;
      HasUnsavedChanges = false;
    }

    /// <summary>
    /// Клик по кресту «отменить»: откатывает значения к сохранённой модели
    /// и скрывает индикаторы изменений.
    /// </summary>
    private void Error_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      DefalultData();

      Error.Visibility = Visibility.Collapsed;
      Success.Visibility = Visibility.Collapsed;
      HasUnsavedChanges = false;
    }

    private void IdleMode_CheckedChanged(object? sender, bool e)
    {
      if (SystemStateManager.GetIsActivePower() && IdleMode.IsChecked)
      {
        MessageBoxCustom.Show("Отключите питание системы для перехода в холостой режим!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        IdleMode.IsChecked = false;
        return;
      }

      CheckedChanged(sender, e);
    }

    /// <summary>
    /// Унифицированный обработчик изменений любого переключателя.
    /// Сравнивает текущую модель с сохранённой и показывает/скрывает индикаторы.
    /// </summary>
    private void CheckedChanged(object? sender, bool e)
    {
      if (!ProtocolEquals(_baseExecutionModel, GetModel()))
      {
        Error.Visibility = Visibility.Visible;
        Success.Visibility = Visibility.Visible;
        HasUnsavedChanges = true;
      }
      else
      {
        Error.Visibility = Visibility.Collapsed;
        Success.Visibility = Visibility.Collapsed;
        HasUnsavedChanges = false;
      }
    }

    /// <summary>
    /// Формирует модель протокола из текущих значений элементов UI.
    /// </summary>
    private SettingsExecutionDto GetModel()
    {
      var model = new SettingsExecutionDto()
      {
        StopOnError = StopInError.IsChecked,
        StepByStepMode = StepByStepMode.IsChecked,
        IsErrorSimulationMode = ErrorSimulation.IsChecked,
        IdleModeExecution = IdleMode.IsChecked
      };
      return model;
    }

    /// <summary>
    /// Сравнивает две модели протокола по всем флагам.
    /// </summary>
    private static bool ProtocolEquals(SettingsExecutionDto a, SettingsExecutionDto b) =>
      a.IdleModeExecution == b.IdleModeExecution &&
      a.IsErrorSimulationMode == b.IsErrorSimulationMode &&
      a.StepByStepMode == b.StepByStepMode &&
      a.StopOnError == b.StopOnError;

    /// <summary>
    /// Заполняет элементы UI значениями из базовой (сохранённой) модели.
    /// </summary>
    private void DefalultData()
    {
      IdleMode.IsChecked = _baseExecutionModel.IdleModeExecution;
      ErrorSimulation.IsChecked = _baseExecutionModel.IsErrorSimulationMode;
      StepByStepMode.IsChecked = _baseExecutionModel.StepByStepMode;
      StopInError.IsChecked = _baseExecutionModel.StopOnError;
    }
  }
}
