using System.Windows;
using System.Windows.Controls;
using DataBaseConfiguration.Services.Device;
using NewCore.Base.Interface.Main;
using static Utilities.LoggerUtility;

namespace UI.Components
{
  /// <summary>
  /// Панель выбора устройств для пользовательского интерфейса.
  /// Позволяет выбрать: управляющий шасси, модуль коммутации или устройство измерения.
  /// </summary>
  public partial class DeviceSelectorPanel : UserControl
  {
    public enum RelayDeviceType
    {
      Unknown,
      RelaySwitchModule,
      SwitchingDevice,
      PowerSourceModule
    }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="DeviceSelectorPanel"/>.
    /// Загружает все доступные устройства для выбора.
    /// </summary>
    public DeviceSelectorPanel()
    {
      InitializeComponent();
      ChassisData.DeviceSelected += OnChassisManagerSelected;
      SelectedDeviceInfoVisibility = Visibility.Collapsed;
      FastMeterSelectionVisibility = Visibility.Collapsed;

      LoadManagerChassis();
    }

    /// <summary>
    /// Загружает список всех доступных управляющих шасси и отображает их в поле выбора.
    /// </summary>
    private void LoadManagerChassis()
    {
      var devices = new ChassisManagerServices().GetAll();
      var names = new List<string>();

      foreach (var chassisManager in devices)
      {
        names.Add(chassisManager.Name + " " + chassisManager.Number);
      }

      ChassisData.ItemsSource = devices;
      ChassisData.DisplayFields = names;
    }

    /// <summary>
    /// Загружает все доступные модули коммутации и устройства УКШ, объединяет их в общий список.
    /// Отображает список в элементе выбора.
    /// </summary>
    private void LoadAllSelectableDevices()
    {
      if (ChassisData.SelectedItem is not IChassisManager selectedChassis)
        return;

      var chassisNumber = selectedChassis.Number;

      var devices = new RelaySwitchModuleServices().GetAll()
                      .Where(d => d.NumberChassis == chassisNumber);

      var uksh = new SwitchingDeviceServices().GetAll()
                      .Where(d => d.NumberChassis == chassisNumber);

      var mint = new PowerSourceModuleServices().GetAll()
                      .Where(d => d.NumberChassis == chassisNumber);

      var combined = new List<object>();
      var displayNames = new List<string>();

      foreach (var item in mint)
      {
        combined.Add(item);
        displayNames.Add($"{item.Name} {item.Number}");
      }

      foreach (var item in uksh)
      {
        combined.Add(item);
        displayNames.Add($"{item.Name} {item.Number}");
      }

      foreach (var item in devices)
      {
        combined.Add(item);
        displayNames.Add($"{item.Name} {item.Number}");
      }

      RelayData.ItemsSource = combined;
      RelayData.DisplayFields = displayNames;
    }

    /// <summary>
    /// Загружает список всех доступных измерителей и отображает их в поле выбора.
    /// </summary>
    private void LoadMeter()
    {
      if (ChassisData.SelectedItem is not IChassisManager selectedChassis)
        return;

      var chassisNumber = selectedChassis.Number;

      var devices = new FastMeterServices().GetAll()
                      .Where(d => d.NumberChassis == chassisNumber)
                      .ToList();

      var names = devices
        .Select(d => $"{d.Name} {d.Number}")
        .ToList();

      MeterData.ItemsSource = devices;
      MeterData.DisplayFields = names;
    }

    /// <summary>
    /// Возвращает выбранное устройство как IFastMeter, если возможно.
    /// </summary>
    /// <returns>Объект типа IFastMeter или null, если выбранное устройство не реализует IFastMeter.</returns>
    public IFastMeter? GetFastMeter()
    {
      return MeterData.SelectedItem as IFastMeter;
    }

    /// <summary>
    /// Возвращает выбранное устройство как IRelaySwitchModule, если возможно.
    /// </summary>
    public IRelaySwitchModule? GetRelayModule()
    {
      return RelayData.SelectedItem as IRelaySwitchModule;
    }

    /// <summary>
    /// Возвращает выбранное устройство как IChassisManager, если возможно.
    /// </summary>
    public IChassisManager? GetChassisManager()
    {
      return ChassisData.SelectedItem as IChassisManager;
    }

    /// <summary>
    /// Управляет видимостью панели, отображающей информацию о выбранном устройстве.
    /// </summary>
    public Visibility SelectedDeviceInfoVisibility
    {
      get => SelectedDeviceInfoBlock.Visibility;
      set => SelectedDeviceInfoBlock.Visibility = value;

    }
    /// <summary>
    /// Управляет видимостью элемента интерфейса, отображающего выбранный измеритель (FastMeter).
    /// </summary>
    public Visibility FastMeterSelectionVisibility
    {
      get => FastMeterSelectionControl.Visibility;
      set => FastMeterSelectionControl.Visibility = value;
    }

    /// <summary>
    /// Обрабатывает выбор управляющего шасси. После выбора делает видимыми блоки выбора устройства и измерителя.
    /// </summary>
    /// <param name="obj">Выбранное устройство (ожидается IChassisManager).</param>
    private void OnChassisManagerSelected(object obj)
    {
      if (obj is IChassisManager)
      {
        SelectedDeviceInfoVisibility = Visibility.Visible;
        FastMeterSelectionVisibility = Visibility.Visible;

        LoadAllSelectableDevices();
        LoadMeter();
      }
      else
      {
        SelectedDeviceInfoVisibility = Visibility.Collapsed;
        FastMeterSelectionVisibility = Visibility.Collapsed;
      }
    }

    public RelayDeviceType GetSelectedRelayDeviceType()
    {
      try
      {
        object? selected = null;

        if (RelayData.Dispatcher.CheckAccess())
        {
          selected = RelayData.SelectedItem;
        }
        else
        {
          RelayData.Dispatcher.Invoke(() =>
          {
            selected = RelayData.SelectedItem;
          });
        }

        return selected switch
        {
          IRelaySwitchModule => RelayDeviceType.RelaySwitchModule,
          ISwitchingDevice => RelayDeviceType.SwitchingDevice,
          IPowerSourceModule => RelayDeviceType.PowerSourceModule,
          _ => RelayDeviceType.Unknown
        };
      }
      catch (Exception ex)
      {
        LogException(ex);
        return RelayDeviceType.Unknown;
      }
    }


    public T? GetSelectedRelayDevice<T>() where T : class
    {
      var selected = RelayData.SelectedItem;

      return GetSelectedRelayDeviceType() switch
      {
        RelayDeviceType.RelaySwitchModule when typeof(T) == typeof(IRelaySwitchModule) => selected as T,
        RelayDeviceType.SwitchingDevice when typeof(T) == typeof(ISwitchingDevice) => selected as T,
        RelayDeviceType.PowerSourceModule when typeof(T) == typeof(IPowerSourceModule) => selected as T,
        _ => null
      };
    }
  }
}
