using AppConfiguration;
using DataBaseConfiguration.Services;
using DataBaseConfiguration.Services.Device;
using NewCore.Base.Device;
using NewCore.Base.Interface.Main;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Mode.Settings.DeviceConfig.Controls
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

    /// <summary>
    /// Свойство зависимости для заголовка списка устройств.
    /// </summary>
    public static readonly DependencyProperty DeviceTitleProperty =
        DependencyProperty.Register(
            nameof(DeviceTitle),
            typeof(string),
            typeof(DeviceListControl),
            new PropertyMetadata("Название устройства"));

    /// <summary>
    /// Получает или задает заголовок списка устройств.
    /// </summary>
    public string DeviceTitle
    {
      get => (string)GetValue(DeviceTitleProperty);
      set => SetValue(DeviceTitleProperty, value);
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
    }

    /// <summary>
    /// Отчищает список устройств.
    /// </summary>
    public void ClearItems()
    {
      Devices.Clear();
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
          new SwitchingDeviceServices().Delete((ISwitchingDevice)device);
          Utilities.LoggerUtility.LogInformation("Удаляем устройство из SwitchingDeviceTable");
          break;

        case IFastMeter:
          new FastMeterServices().Delete((IFastMeter)device);
          Utilities.LoggerUtility.LogInformation("Удаляем устройство из FastMeterTable");
          break;

        case IPrecisionMeter:
          new PrecisionMeterServices().Delete((IPrecisionMeter)device);
          Utilities.LoggerUtility.LogInformation("Удаляем устройство из PrecisionMeterTable");
          break;

        case IRelaySwitchModule:
          new RelaySwitchModuleServices().Delete((IRelaySwitchModule)device);
          Utilities.LoggerUtility.LogInformation("Удаляем устройство из RelaySwitchModuleTable");
          break;

        case IBreakdownTester:
          ServiceLocator.GetRequired<BreakdownTesterServices>().Delete((IBreakdownTester)device);
          Utilities.LoggerUtility.LogInformation("Удаляем устройство из BreakdownTesterTable");
          break;

        case IChassisManager:
          new ChassisManagerServices().Delete((IChassisManager)device);
          Utilities.LoggerUtility.LogInformation("Удаляем устройство из ChassisManagerTable");
          break;

        case IRack:
          new RackServices().Delete((IRack)device);
          Utilities.LoggerUtility.LogInformation("Удаляем устройство из ChassisManagerTable");
          break;

        case IPowerSourceModule:
          new PowerSourceModuleServices().Delete((IPowerSourceModule)device);
          Utilities.LoggerUtility.LogInformation("Удаляем устройство из ChassisManagerTable");
          break;

        default:
          Utilities.LoggerUtility.LogInformation($"Неизвестный интерфейс {device.GetType().Name}, удаление не выполнено.");
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

    /// <summary>
    /// Обрабатывает нажатие кнопки добавления устройства.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void PlusPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      PlusEvent?.Invoke(this, e);
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
