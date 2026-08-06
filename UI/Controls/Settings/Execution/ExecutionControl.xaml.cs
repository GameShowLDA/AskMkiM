using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Settings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.DataBase.Engine.Static.Devices;
using Ask.UI.Shared.Components;
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
    private readonly List<HardwareFailureSimulationEntry> _hardwareFailureSimulationEntries = [];

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
        }
        else
        {
          IdleMode.Visibility = Visibility.Visible;
        }
      });
    }

    private async void ExecutionControl_Loaded(object sender, RoutedEventArgs e)
    {
      _baseExecutionModel = await ExecutionConfig.GetExecitonModel();
      await LoadHardwareFailureSimulationDevicesAsync();
      DefalultData();

      if (!_isInitialized)
      {
        StopInError.CheckedChanged += CheckedChanged;
        StepByStepMode.CheckedChanged += CheckedChanged;
        MeasurementErrorSimulation.CheckedChanged += CheckedChanged;
        IdleMode.CheckedChanged += IdleMode_CheckedChanged;

        CompatibilityModeCheckBox.CheckedChanged += CheckedChanged;
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
      await SaveHardwareFailureSimulationDevicesAsync();
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
      if (!ProtocolEquals(_baseExecutionModel, GetModel()) || HasHardwareFailureSimulationChanges())
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
        IsErrorSimulationMode = MeasurementErrorSimulation.IsChecked,
        IdleModeExecution = IdleMode.IsChecked,
        LegacyCompatibilityMode = CompatibilityModeCheckBox.IsChecked,
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
      a.StopOnError == b.StopOnError &&
      a.LegacyCompatibilityMode == b.LegacyCompatibilityMode;

    /// <summary>
    /// Заполняет элементы UI значениями из базовой (сохранённой) модели.
    /// </summary>
    private void DefalultData()
    {
      IdleMode.IsChecked = _baseExecutionModel.IdleModeExecution;
      MeasurementErrorSimulation.IsChecked = _baseExecutionModel.IsErrorSimulationMode;
      StepByStepMode.IsChecked = _baseExecutionModel.StepByStepMode;
      StopInError.IsChecked = _baseExecutionModel.StopOnError;
      CompatibilityModeCheckBox.IsChecked = _baseExecutionModel.LegacyCompatibilityMode;

      foreach (var entry in _hardwareFailureSimulationEntries)
      {
        entry.Card.IsChecked = entry.SavedValue;
      }
    }

    private async Task LoadHardwareFailureSimulationDevicesAsync()
    {
      HardwareFailureSimulationCards.Children.Clear();
      _hardwareFailureSimulationEntries.Clear();

      var chassisTask = ChassisManagers.GetAllAsync();
      var relayTask = RelaySwitchModules.GetAllAsync();
      var switchingTask = SwitchingDevices.GetAllAsync();
      var powerSourceTask = PowerSourceModules.GetAllAsync();
      var fastMeterTask = FastMeters.GetAllAsync();
      var breakdownTesterTask = BreakdownTesters.GetAllAsync();

      await Task.WhenAll(
        chassisTask,
        relayTask,
        switchingTask,
        powerSourceTask,
        fastMeterTask,
        breakdownTesterTask);

      AddHardwareFailureSimulationCards(chassisTask.Result, ChassisManagers.UpdateAsync, "Контроллер шасси");
      AddHardwareFailureSimulationCards(relayTask.Result, RelaySwitchModules.UpdateAsync, "Модуль коммутации реле");
      AddHardwareFailureSimulationCards(switchingTask.Result, SwitchingDevices.UpdateAsync, "Устройство коммутации");
      AddHardwareFailureSimulationCards(powerSourceTask.Result, PowerSourceModules.UpdateAsync, "Модуль источника питания");
      AddHardwareFailureSimulationCards(fastMeterTask.Result, FastMeters.UpdateAsync, "Быстрый измеритель");
      AddHardwareFailureSimulationCards(breakdownTesterTask.Result, BreakdownTesters.UpdateAsync, "Пробойная установка");
    }

    private void AddHardwareFailureSimulationCards<TDevice>(
      IEnumerable<TDevice> devices,
      Func<TDevice, CancellationToken, Task<TDevice>> updateAsync,
      string deviceKind)
      where TDevice : class, IDevice
    {
      foreach (var device in devices.OrderBy(item => item.Number))
      {
        var card = new SettingsCard
        {
          Title = BuildDeviceTitle(device, deviceKind),
          Description = BuildDeviceDescription(device, deviceKind),
          IsChecked = device.IsHardwareFailureSimulationEnabled,
          Margin = new Thickness(0, 6, 10, 0),
          VerticalAlignment = VerticalAlignment.Top,
        };

        var entry = new HardwareFailureSimulationEntry(
          card,
          device.IsHardwareFailureSimulationEnabled,
          async enabled =>
          {
            device.IsHardwareFailureSimulationEnabled = enabled;
            await updateAsync(device, CancellationToken.None);
          });

        card.CheckedChanged += CheckedChanged;
        _hardwareFailureSimulationEntries.Add(entry);
        HardwareFailureSimulationCards.Children.Add(card);
      }
    }

    private static string BuildDeviceTitle(IDevice device, string deviceKind)
    {
      string deviceName = string.IsNullOrWhiteSpace(device.Name) ? deviceKind : device.Name;
      return device is IChassisManager ? deviceName : $"{deviceName} ({device.Number})";
    }

    private static string BuildDeviceDescription(IDevice device, string deviceKind)
    {
      if (device is IAttachableDevice attachableDevice)
      {
        return $"{deviceKind}. Шасси {attachableDevice.NumberChassis}, устройство {device.Number}.";
      }

      return $"{deviceKind}. Устройство {device.Number}.";
    }

    private bool HasHardwareFailureSimulationChanges() =>
      _hardwareFailureSimulationEntries.Any(entry => entry.Card.IsChecked != entry.SavedValue);

    private async Task SaveHardwareFailureSimulationDevicesAsync()
    {
      foreach (var entry in _hardwareFailureSimulationEntries.Where(
        item => item.Card.IsChecked != item.SavedValue))
      {
        await entry.SaveAsync(entry.Card.IsChecked);
        entry.SavedValue = entry.Card.IsChecked;
      }
    }

    private sealed class HardwareFailureSimulationEntry
    {
      public HardwareFailureSimulationEntry(
        SettingsCard card,
        bool savedValue,
        Func<bool, Task> saveAsync)
      {
        Card = card;
        SavedValue = savedValue;
        SaveAsync = saveAsync;
      }

      public SettingsCard Card { get; }
      public bool SavedValue { get; set; }
      public Func<bool, Task> SaveAsync { get; }
    }
  }
}
