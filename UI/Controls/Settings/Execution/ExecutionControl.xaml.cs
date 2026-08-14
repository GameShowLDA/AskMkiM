using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.DTO.Settings;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.RoleEnums;
using Ask.DataBase.Engine.Static;
using Ask.DataBase.Provider.Context;
using Ask.UI.Infrastructure.Localization;
using Ask.UI.Shared.Components;
using Message;
using Microsoft.EntityFrameworkCore;
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
    private sealed record ErroneousMeasurementOption(TypeErroneousMeasurement Value, string Title);
    private readonly List<HardwareErrorSimulationItem> _hardwareErrorSimulationItems = [];

    private sealed class HardwareErrorSimulationItem
    {
      public required DeviceDto Device { get; init; }

      public required SettingsCard Card { get; init; }

      public bool SavedValue { get; set; }
    }

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
      RootSettingsGroup.Visibility = IsRootRole() ? Visibility.Visible : Visibility.Collapsed;
      LoadErroneousMeasurementOptions();
      await LoadHardwareErrorSimulationCardsAsync();
      DefalultData();

      if (!_isInitialized)
      {
        StopInError.CheckedChanged += CheckedChanged;
        RepeatMeasurement.CheckedChanged += CheckedChanged;
        StepByStepMode.CheckedChanged += CheckedChanged;
        ErroneousMeasurementTypeSelect.ValueChanged += ErroneousMeasurementTypeChanged;
        IdleMode.CheckedChanged += IdleMode_CheckedChanged;

        CompatibilityModeCheckBox.CheckedChanged += CheckedChanged;
        DisablePowerCheck.CheckedChanged += CheckedChanged;
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
      await SaveHardwareErrorSimulationSettingsAsync();
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
      if (!ProtocolEquals(_baseExecutionModel, GetModel()) || HasHardwareErrorSimulationChanges())
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

    private void ErroneousMeasurementTypeChanged(object? sender, object? e)
    {
      CheckedChanged(sender, false);
    }

    /// <summary>
    /// Формирует модель протокола из текущих значений элементов UI.
    /// </summary>
    private SettingsExecutionDto GetModel()
    {
      var model = new SettingsExecutionDto()
      {
        StopOnError = StopInError.IsChecked,
        RepeatMeasurement = RepeatMeasurement.IsChecked,
        StepByStepMode = StepByStepMode.IsChecked,
        ErroneousMeasurementType = ErroneousMeasurementTypeSelect.SelectedValue is TypeErroneousMeasurement selectedType
          ? selectedType
          : _baseExecutionModel.ErroneousMeasurementType,
        IsHardwareErrorSimulationMode = _baseExecutionModel.IsHardwareErrorSimulationMode,
        IdleModeExecution = IdleMode.IsChecked,
        LegacyCompatibilityMode = CompatibilityModeCheckBox.IsChecked,
        DisablePowerCheck = IsRootRole()
          ? DisablePowerCheck.IsChecked
          : _baseExecutionModel.DisablePowerCheck,
      };
      return model;
    }

    /// <summary>
    /// Сравнивает две модели протокола по всем флагам.
    /// </summary>
    private static bool ProtocolEquals(SettingsExecutionDto a, SettingsExecutionDto b) =>
      a.IdleModeExecution == b.IdleModeExecution &&
      a.ErroneousMeasurementType == b.ErroneousMeasurementType &&
      a.IsHardwareErrorSimulationMode == b.IsHardwareErrorSimulationMode &&
      a.StepByStepMode == b.StepByStepMode &&
      a.StopOnError == b.StopOnError &&
      a.RepeatMeasurement == b.RepeatMeasurement &&
      a.LegacyCompatibilityMode == b.LegacyCompatibilityMode &&
      a.DisablePowerCheck == b.DisablePowerCheck;

    /// <summary>
    /// Заполняет элементы UI значениями из базовой (сохранённой) модели.
    /// </summary>
    private void DefalultData()
    {
      IdleMode.IsChecked = _baseExecutionModel.IdleModeExecution;
      ErroneousMeasurementTypeSelect.DefaultValue = _baseExecutionModel.ErroneousMeasurementType;
      ErroneousMeasurementTypeSelect.SelectedValue = _baseExecutionModel.ErroneousMeasurementType;
      foreach (var item in _hardwareErrorSimulationItems)
      {
        item.Card.IsChecked = item.SavedValue;
      }

      StepByStepMode.IsChecked = _baseExecutionModel.StepByStepMode;
      StopInError.IsChecked = _baseExecutionModel.StopOnError;
      RepeatMeasurement.IsChecked = _baseExecutionModel.RepeatMeasurement;
      CompatibilityModeCheckBox.IsChecked = _baseExecutionModel.LegacyCompatibilityMode;
      DisablePowerCheck.IsChecked = _baseExecutionModel.DisablePowerCheck;
    }

    private void LoadErroneousMeasurementOptions()
    {
      ErroneousMeasurementTypeSelect.ItemsSource = new[]
      {
        new ErroneousMeasurementOption(
          TypeErroneousMeasurement.None,
          LocalizationService.Get("settings.execution.measurementErrorSimulation.none")),
        new ErroneousMeasurementOption(
          TypeErroneousMeasurement.Rnd,
          LocalizationService.Get("settings.execution.measurementErrorSimulation.rnd")),
        new ErroneousMeasurementOption(
          TypeErroneousMeasurement.Low,
          LocalizationService.Get("settings.execution.measurementErrorSimulation.low")),
        new ErroneousMeasurementOption(
          TypeErroneousMeasurement.High,
          LocalizationService.Get("settings.execution.measurementErrorSimulation.high")),
      };
    }

    private async Task LoadHardwareErrorSimulationCardsAsync()
    {
      await using var context = new AppDbContext();
      var devices = new List<DeviceDto>();

      devices.AddRange(await context.ChassisManagers.AsNoTracking().ToListAsync());
      devices.AddRange(await context.Rack.AsNoTracking().ToListAsync());
      devices.AddRange(await context.RelaySwitchModules.AsNoTracking().ToListAsync());
      devices.AddRange(await context.SwitchingDevices.AsNoTracking().ToListAsync());
      devices.AddRange(await context.PowerSourceModules.AsNoTracking().ToListAsync());
      devices.AddRange(await context.FastMeters.AsNoTracking().ToListAsync());
      devices.AddRange(await context.BreakdownTesters.AsNoTracking().ToListAsync());
      devices.AddRange(await context.UninterruptiblePowerSupplies.AsNoTracking().ToListAsync());

      HardwareErrorSimulationCards.Children.Clear();
      _hardwareErrorSimulationItems.Clear();

      foreach (var device in devices
        .OrderBy(device => device.DeviceType)
        .ThenBy(device => device is AttachableDeviceDto attachable ? attachable.NumberChassis : device.Number)
        .ThenBy(device => device.Number))
      {
        var card = new SettingsCard
        {
          Title = GetDeviceCardTitle(device),
          Description = GetDeviceCardDescription(device),
          IsChecked = device.IsHardwareFailureSimulationEnabled,
          Margin = new Thickness(0, 6, 10, 0),
          VerticalAlignment = VerticalAlignment.Top,
        };

        card.CheckedChanged += CheckedChanged;
        HardwareErrorSimulationCards.Children.Add(card);
        _hardwareErrorSimulationItems.Add(new HardwareErrorSimulationItem
        {
          Device = device,
          Card = card,
          SavedValue = device.IsHardwareFailureSimulationEnabled,
        });
      }
    }

    private async Task SaveHardwareErrorSimulationSettingsAsync()
    {
      var changedItems = _hardwareErrorSimulationItems
        .Where(item => item.Card.IsChecked != item.SavedValue)
        .ToList();

      if (changedItems.Count == 0)
      {
        return;
      }

      await using var context = new AppDbContext();
      foreach (var item in changedItems)
      {
        item.Device.IsHardwareFailureSimulationEnabled = item.Card.IsChecked;
        context.Attach((object)item.Device);
        context.Entry((object)item.Device)
          .Property(nameof(DeviceDto.IsHardwareFailureSimulationEnabled))
          .IsModified = true;
      }

      await context.SaveChangesAsync();
      DeviceRuntime.ClearCache();

      foreach (var item in changedItems)
      {
        item.SavedValue = item.Card.IsChecked;
      }
    }

    private bool HasHardwareErrorSimulationChanges() =>
      _hardwareErrorSimulationItems.Any(item => item.Card.IsChecked != item.SavedValue);

    private static string GetDeviceCardTitle(DeviceDto device) =>
      device is AttachableDeviceDto attachable
        ? $"{device.Name} ({attachable.NumberChassis}.{device.Number})"
        : $"{device.Name} ({device.Number})";

    private static string GetDeviceCardDescription(DeviceDto device)
    {
      if (!string.IsNullOrWhiteSpace(device.Description))
      {
        return device.Description;
      }

      if (!string.IsNullOrWhiteSpace(device.ConnectionDetails))
      {
        return device.ConnectionDetails;
      }

      return $"ID: {device.Id}";
    }

    /// <summary>
    /// Проверяет, обладает ли текущая сессия ролью Root.
    /// </summary>
    /// <returns><see langword="true"/>, если активна роль Root.</returns>
    private static bool IsRootRole() => RoleAuthorizationConfig.CurrentRole == RoleType.Root;
  }
}
