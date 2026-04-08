using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Shared.DTO.Devices.FastMeter;
using Ask.Core.Shared.DTO.Devices.PowerSourceModule;
using Ask.Core.Shared.DTO.Devices.Rack;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Devices.SwitchingDevice;
using Ask.Core.Shared.DTO.Devices.UninterruptiblePowerSupply;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Rack;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;
using Ask.DataBase.Engine.Static.Devices;
using Ask.DataBase.Provider.Services.Devices;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Ask.LogLib.LoggerUtility;
using static UI.Controls.Settings.DeviceConfig.DeviceConfigNotifications;

namespace UI.Controls.Settings.DeviceConfig.Controls
{
  /// <summary>
  /// Логика взаимодействия для DeviceListControl.xaml.
  /// </summary>
  public partial class DeviceListControl : UserControl
  {
    /// <summary>
    /// Событие, вызываемое при нажатии кнопки добавления устройства.
    /// </summary>
    public event EventHandler PlusEvent;

    /// <summary>
    /// Событие, вызываемое при удалении устройства.
    /// </summary>
    public event EventHandler<DeviceDto> DeleteEvent;
    public event EventHandler<DeviceDto> EditEvent;

    /// <summary>
    /// Свойство зависимости для заголовка списка устройств.
    /// </summary>
    public static readonly DependencyProperty DeviceTitleProperty =
        DependencyProperty.Register(
            nameof(DeviceTitle),
            typeof(string),
            typeof(DeviceListControl),
            new PropertyMetadata("Название устройства"));

    public static readonly DependencyProperty IsSingleDeviceOnlyProperty =
        DependencyProperty.Register(
            nameof(IsSingleDeviceOnly),
            typeof(bool),
            typeof(DeviceListControl),
            new PropertyMetadata(false, OnIsSingleDeviceOnlyChanged));

    /// <summary>
    /// Получает или задает заголовок списка устройств.
    /// </summary>
    public string DeviceTitle
    {
      get => (string)GetValue(DeviceTitleProperty);
      set => SetValue(DeviceTitleProperty, value);
    }

    public bool IsSingleDeviceOnly
    {
      get => (bool)GetValue(IsSingleDeviceOnlyProperty);
      set => SetValue(IsSingleDeviceOnlyProperty, value);
    }

    /// <summary>
    /// Коллекция устройств (IDevice + DisplayName).
    /// </summary>
    public ObservableCollection<DeviceWrapper> Devices { get; set; } = new();

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DeviceListControl"/>.
    /// </summary>
    public DeviceListControl()
    {
      InitializeComponent();
      DataContext = this;
      DeviceList.ItemsSource = Devices;
    }

    /// <summary>
    /// Добавляет устройство в список.
    /// </summary>
    /// <param name="device">Экземпляр устройства.</param>
    public void AddDevice(DeviceDto device)
    {
      Devices.Add(new DeviceWrapper(device));
      UpdateAddButtonVisibility();
    }

    /// <summary>
    /// Отчищает список устройств.
    /// </summary>
    public void ClearItems()
    {
      Devices.Clear();
      UpdateAddButtonVisibility();
    }

    /// <summary>
    /// Удаляет устройство из списка и базы данных.
    /// </summary>
    /// <param name="deviceWrapper">Экземпляр <see cref="DeviceWrapper"/>.</param>
    public async Task RemoveDeviceAsync(DeviceWrapper deviceWrapper)
    {
      if (!Devices.Contains(deviceWrapper))
      {
        return;
      }

      try
      {
        await RemoveDeviceFromDatabaseAsync(deviceWrapper.Device);
        Devices.Remove(deviceWrapper);
        UpdateAddButtonVisibility();
        ShowDeleted(deviceWrapper.Device);
        DeleteEvent?.Invoke(this, deviceWrapper.Device);
      }
      catch (Exception ex)
      {
        LogException(ex, $"Ошибка удаления устройства {deviceWrapper.DisplayName}");
        ShowDeleteError(deviceWrapper.Device, ex);
      }
    }

    /// <summary>
    /// Удаляет устройство из базы данных в зависимости от его типа.
    /// </summary>
    /// <param name="device">Экземпляр устройства.</param>
    private async Task RemoveDeviceFromDatabaseAsync(DeviceDto device)
    {
      switch (device)
      {
        case SwitchingDeviceDto:
          var switchingDevice = SwitchingDevices.Build(device);
          await SwitchingDevices.DeleteAsync(switchingDevice);
          LogInformation("Удаляем устройство из SwitchingDeviceTable");
          break;

        case FastMeterDto:
          var fastMeter = FastMeters.Build(device);
          await FastMeters.DeleteAsync(fastMeter);
          LogInformation("Удаляем устройство из FastMeterTable");
          break;

        case RelaySwitchModuleDto:
          var relaySwitchModule = RelaySwitchModules.Build(device);
          await RelaySwitchModules.DeleteAsync(relaySwitchModule);
          LogInformation("Удаляем устройство из RelaySwitchModuleTable");
          break;

        case BreakdownTesterDto:
          var breakdownTester = BreakdownTesters.Build(device);
          await BreakdownTesters.DeleteAsync(breakdownTester);
          LogInformation("Удаляем устройство из BreakdownTesterTable");
          break;

        case ChassisManagerDto:
          var chassisManager = ChassisManagers.Build(device);
          await ChassisManagers.DeleteAsync(chassisManager);
          LogInformation("Удаляем устройство из ChassisManagerTable");
          break;

        case RackDto:
          var rack = Racks.Build(device);
          await Racks.DeleteAsync(rack);
          LogInformation("Удаляем устройство из ChassisManagerTable");
          break;

        case PowerSourceModuleDto:
          var powerSourceModule = PowerSourceModules.Build(device);
          await PowerSourceModules.DeleteAsync(powerSourceModule);
          LogInformation("Удаляем устройство из ChassisManagerTable");
          break;

        case UninterruptiblePowerSupplyDto:
          var uninterruptiblePowerSupply = UninterruptiblePowerSupplies.Build(device);
          await UninterruptiblePowerSupplies.DeleteAsync(uninterruptiblePowerSupply);
          LogInformation("Удаляем устройство из UninterruptiblePowerSuppliesTable");
          break;

        default:
          LogInformation($"Неизвестный интерфейс {device.GetType().Name}, удаление не выполнено.");
          throw new NotFiniteNumberException();
      }
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки удаления устройства.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private async void RemoveDeviceButton_Click(object sender, RoutedEventArgs e)
    {
      if (sender is Button button && button.CommandParameter is DeviceWrapper deviceWrapper)
      {
        await RemoveDeviceAsync(deviceWrapper);
      }
    }

    private void EditDeviceButton_Click(object sender, RoutedEventArgs e)
    {
      if (sender is Button button && button.CommandParameter is DeviceWrapper deviceWrapper)
      {
        EditEvent?.Invoke(this, deviceWrapper.Device);
      }
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки добавления устройства.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void PlusPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      if (IsSingleDeviceOnly && Devices.Count > 0)
      {
        return;
      }

      PlusEvent?.Invoke(this, e);
    }

    private static void OnIsSingleDeviceOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is DeviceListControl control)
      {
        control.UpdateAddButtonVisibility();
      }
    }

    private void UpdateAddButtonVisibility()
    {
      if (AddButtonContainer == null)
      {
        return;
      }

      AddButtonContainer.Visibility = IsSingleDeviceOnly && Devices.Count > 0
          ? Visibility.Collapsed
          : Visibility.Visible;
    }
  }

  /// <summary>
  /// Обертка для устройства, содержащая его отображаемое имя.
  /// </summary>
  public class DeviceWrapper
  {
    /// <summary>
    /// Получает экземпляр устройства.
    /// </summary>
    public DeviceDto Device { get; }

    /// <summary>
    /// Получает отображаемое имя устройства.
    /// </summary>
    public string DisplayName => $"{Device.Name} ({Device.Number})";

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DeviceWrapper"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства.</param>
    public DeviceWrapper(DeviceDto device)
    {
      Device = device;
    }

    /// <summary>
    /// Возвращает строковое представление объекта.
    /// </summary>
    /// <returns>Строковое представление объекта.</returns>
    public override string ToString()
    {
      return DisplayName;
    }
  }
}
