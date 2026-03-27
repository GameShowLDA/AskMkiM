using Ask.Core.Services.App;
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
using DataBaseConfiguration.Services.Device;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using static Ask.LogLib.LoggerUtility;

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
    public event EventHandler<IDevice> DeleteEvent;
    public event EventHandler<IDevice> EditEvent;

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
    public void AddDevice(IDevice device)
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
    public void RemoveDevice(DeviceWrapper deviceWrapper)
    {
      if (Devices.Contains(deviceWrapper))
      {
        Devices.Remove(deviceWrapper);
        UpdateAddButtonVisibility();
        RemoveDeviceFromDatabase(deviceWrapper.Device);
        DeleteEvent?.Invoke(this, deviceWrapper.Device);
      }
    }

    /// <summary>
    /// Удаляет устройство из базы данных в зависимости от его типа.
    /// </summary>
    /// <param name="device">Экземпляр устройства.</param>
    private void RemoveDeviceFromDatabase(IDevice device)
    {
      switch (device)
      {
        case ISwitchingDevice:
          SwitchingDevices.DeleteAsync((ISwitchingDevice)device).GetAwaiter().GetResult();
          LogInformation("Удаляем устройство из SwitchingDeviceTable");
          break;

        case IFastMeter:
          FastMeters.DeleteAsync((IFastMeter)device).GetAwaiter().GetResult();
          LogInformation("Удаляем устройство из FastMeterTable");
          break;

        case IRelaySwitchModule:
          RelaySwitchModules.DeleteAsync((IRelaySwitchModule)device).GetAwaiter().GetResult();
          LogInformation("Удаляем устройство из RelaySwitchModuleTable");
          break;

        case IBreakdownTester:
          ServiceLocator.GetRequired<BreakdownTesterServices>().Delete((IBreakdownTester)device);
          LogInformation("Удаляем устройство из BreakdownTesterTable");
          break;

        case IChassisManager:
          ChassisManagers.DeleteAsync((IChassisManager)device).GetAwaiter().GetResult();
          LogInformation("Удаляем устройство из ChassisManagerTable");
          break;

        case IRack:
          new RackServices().Delete((IRack)device);
          LogInformation("Удаляем устройство из ChassisManagerTable");
          break;

        case IPowerSourceModule:
          new PowerSourceModuleServices().Delete((IPowerSourceModule)device);
          LogInformation("Удаляем устройство из ChassisManagerTable");
          break;

        case IUninterruptiblePowerSupply:
          new UninterruptiblePowerSupplyServices().Delete((IUninterruptiblePowerSupply)device);
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
    private void RemoveDeviceButton_Click(object sender, RoutedEventArgs e)
    {
      if (sender is Button button && button.CommandParameter is DeviceWrapper deviceWrapper)
      {
        RemoveDevice(deviceWrapper);
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
    public IDevice Device { get; }

    /// <summary>
    /// Получает отображаемое имя устройства.
    /// </summary>
    public string DisplayName => $"{Device.Name} ({Device.Number})";

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DeviceWrapper"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства.</param>
    public DeviceWrapper(IDevice device)
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
