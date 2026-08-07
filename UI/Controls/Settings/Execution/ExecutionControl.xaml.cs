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
using static Ask.LogLib.LoggerUtility;

namespace UI.Controls.Settings.Execution
{
  /// <summary>
  /// Логика взаимодействия для ExecutionControl.xaml
  /// </summary>
  public partial class ExecutionControl : UserControl
  {
    private static readonly HashSet<Type> HardwareFailureSimulationDeviceTypes =
    [
      typeof(IChassisManager),
      typeof(IRelaySwitchModule),
      typeof(ISwitchingDevice),
      typeof(IPowerSourceModule),
      typeof(IMultimeter),
      typeof(IBreakdownTester),
    ];

    private bool _isInitialized;
    private readonly List<HardwareFailureSimulationEntry> _hardwareFailureSimulationEntries = [];
    private readonly Action<DeviceConfigurationEvents.Changed> _deviceConfigurationChangedHandler;
    private CancellationTokenSource? _hardwareFailureRefreshCancellation;
    private bool _isDeviceConfigurationChangedSubscribed;
    private bool _isSavingHardwareFailureSimulation;
    private bool _refreshHardwareFailureSimulationAfterSave;

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
      _deviceConfigurationChangedHandler = OnDeviceConfigurationChanged;
      Loaded += ExecutionControl_Loaded;
      Unloaded += ExecutionControl_Unloaded;
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

      if (IsLoaded && !_isDeviceConfigurationChangedSubscribed)
      {
        EventAggregator.Subscribe(_deviceConfigurationChangedHandler);
        _isDeviceConfigurationChangedSubscribed = true;
      }
    }

    private void ExecutionControl_Unloaded(object sender, RoutedEventArgs e)
    {
      if (_isDeviceConfigurationChangedSubscribed)
      {
        EventAggregator.Unsubscribe(_deviceConfigurationChangedHandler);
        _isDeviceConfigurationChangedSubscribed = false;
      }

      _hardwareFailureRefreshCancellation?.Cancel();
      _hardwareFailureRefreshCancellation = null;
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

    private void OnDeviceConfigurationChanged(DeviceConfigurationEvents.Changed eventData)
    {
      if (eventData.DeviceType != null
        && !HardwareFailureSimulationDeviceTypes.Contains(eventData.DeviceType))
      {
        return;
      }

      if (!Dispatcher.CheckAccess())
      {
        _ = Dispatcher.InvokeAsync(() => OnDeviceConfigurationChanged(eventData));
        return;
      }

      if (!IsLoaded)
      {
        return;
      }

      if (_isSavingHardwareFailureSimulation)
      {
        _refreshHardwareFailureSimulationAfterSave = true;
        return;
      }

      QueueHardwareFailureSimulationRefresh();
    }

    private void QueueHardwareFailureSimulationRefresh()
    {
      _hardwareFailureRefreshCancellation?.Cancel();

      var cancellation = new CancellationTokenSource();
      _hardwareFailureRefreshCancellation = cancellation;
      _ = RefreshHardwareFailureSimulationDevicesAsync(cancellation);
    }

    private async Task RefreshHardwareFailureSimulationDevicesAsync(CancellationTokenSource cancellation)
    {
      try
      {
        await Task.Delay(100, cancellation.Token);
        await LoadHardwareFailureSimulationDevicesAsync(
          preserveUnsavedValues: true,
          cancellation.Token);
        CheckedChanged(sender: null, e: false);
      }
      catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
      {
      }
      catch (Exception exception)
      {
        LogException(exception, "Не удалось динамически обновить список симуляции сбоев оборудования.");
      }
      finally
      {
        if (ReferenceEquals(_hardwareFailureRefreshCancellation, cancellation))
        {
          _hardwareFailureRefreshCancellation = null;
        }

        cancellation.Dispose();
      }
    }

    private async Task LoadHardwareFailureSimulationDevicesAsync(
      bool preserveUnsavedValues = false,
      CancellationToken cancellationToken = default)
    {
      var chassisTask = ChassisManagers.GetAllAsync(cancellationToken);
      var relayTask = RelaySwitchModules.GetAllAsync(cancellationToken);
      var switchingTask = SwitchingDevices.GetAllAsync(cancellationToken);
      var powerSourceTask = PowerSourceModules.GetAllAsync(cancellationToken);
      var fastMeterTask = FastMeters.GetAllAsync(cancellationToken);
      var breakdownTesterTask = BreakdownTesters.GetAllAsync(cancellationToken);

      await Task.WhenAll(
        chassisTask,
        relayTask,
        switchingTask,
        powerSourceTask,
        fastMeterTask,
        breakdownTesterTask);

      cancellationToken.ThrowIfCancellationRequested();
      Dictionary<HardwareFailureSimulationDeviceKey, bool> unsavedValues = preserveUnsavedValues
        ? _hardwareFailureSimulationEntries
          .Where(entry => entry.Card.IsChecked != entry.SavedValue)
          .ToDictionary(entry => entry.Key, entry => entry.Card.IsChecked)
        : [];

      HardwareFailureSimulationCards.Children.Clear();
      _hardwareFailureSimulationEntries.Clear();

      AddHardwareFailureSimulationCards(chassisTask.Result, ChassisManagers.UpdateAsync, "Контроллер шасси", unsavedValues);
      AddHardwareFailureSimulationCards(relayTask.Result, RelaySwitchModules.UpdateAsync, "Модуль коммутации реле", unsavedValues);
      AddHardwareFailureSimulationCards(switchingTask.Result, SwitchingDevices.UpdateAsync, "Устройство коммутации", unsavedValues);
      AddHardwareFailureSimulationCards(powerSourceTask.Result, PowerSourceModules.UpdateAsync, "Модуль источника питания", unsavedValues);
      AddHardwareFailureSimulationCards(fastMeterTask.Result, FastMeters.UpdateAsync, "Быстрый измеритель", unsavedValues);
      AddHardwareFailureSimulationCards(breakdownTesterTask.Result, BreakdownTesters.UpdateAsync, "Пробойная установка", unsavedValues);
    }

    private void AddHardwareFailureSimulationCards<TDevice>(
      IEnumerable<TDevice> devices,
      Func<TDevice, CancellationToken, Task<TDevice>> updateAsync,
      string deviceKind,
      IReadOnlyDictionary<HardwareFailureSimulationDeviceKey, bool> unsavedValues)
      where TDevice : class, IDevice
    {
      foreach (var device in devices.OrderBy(item => item.Number))
      {
        var key = new HardwareFailureSimulationDeviceKey(typeof(TDevice), device.Id);
        bool savedValue = device.IsHardwareFailureSimulationEnabled;
        bool currentValue = unsavedValues.TryGetValue(key, out bool unsavedValue)
          ? unsavedValue
          : savedValue;

        var card = new SettingsCard
        {
          Title = BuildDeviceTitle(device, deviceKind),
          Description = BuildDeviceDescription(device, deviceKind),
          IsChecked = currentValue,
          Margin = new Thickness(0, 6, 10, 0),
          VerticalAlignment = VerticalAlignment.Top,
        };

        var entry = new HardwareFailureSimulationEntry(
          key,
          card,
          savedValue,
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
      _hardwareFailureRefreshCancellation?.Cancel();
      _isSavingHardwareFailureSimulation = true;

      try
      {
        foreach (var entry in _hardwareFailureSimulationEntries.Where(
          item => item.Card.IsChecked != item.SavedValue))
        {
          await entry.SaveAsync(entry.Card.IsChecked);
          entry.SavedValue = entry.Card.IsChecked;
        }
      }
      finally
      {
        _isSavingHardwareFailureSimulation = false;

        if (_refreshHardwareFailureSimulationAfterSave)
        {
          _refreshHardwareFailureSimulationAfterSave = false;
          QueueHardwareFailureSimulationRefresh();
        }
      }
    }

    private readonly record struct HardwareFailureSimulationDeviceKey(Type DeviceType, int DeviceId);

    private sealed class HardwareFailureSimulationEntry
    {
      public HardwareFailureSimulationEntry(
        HardwareFailureSimulationDeviceKey key,
        SettingsCard card,
        bool savedValue,
        Func<bool, Task> saveAsync)
      {
        Key = key;
        Card = card;
        SavedValue = savedValue;
        SaveAsync = saveAsync;
      }

      public HardwareFailureSimulationDeviceKey Key { get; }
      public SettingsCard Card { get; }
      public bool SavedValue { get; set; }
      public Func<bool, Task> SaveAsync { get; }
    }
  }
}
