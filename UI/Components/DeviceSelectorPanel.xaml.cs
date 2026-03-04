using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using DataBaseConfiguration.Services.Device;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using static Ask.Core.Services.EventCore.Events.SystemStateEvents;
using static Ask.LogLib.LoggerUtility;

namespace UI.Components
{
  /// <summary>
  /// Панель выбора устройств для пользовательского интерфейса.
  /// Позволяет выбрать: управляющий шасси, модуль коммутации или устройство измерения.
  /// </summary>
  public partial class DeviceSelectorPanel : UserControl, IDeviceSelector
  {
    /// <summary>
    /// Прямая ссылка на элемент ChoiceDevice, используемый для отображения
    /// и выбора вариантов перечислений в блоке самоконтроля.
    /// Является прокси-доступом к визуальному элементу PartData.
    /// </summary>
    public ChoiceDevice PartDataControl;

    private bool _isHasDevice = false;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="DeviceSelectorPanel"/>.
    /// Загружает все доступные устройства для выбора.
    /// </summary>
    public DeviceSelectorPanel()
    {
      InitializeComponent();
      EventAggregator.Subscribe<SystemStateEvents.AdminRightsChanged>(e => ChangePartVisible(e));
      RelayData.DeviceSelected += RelayData_DeviceSelected;

      SelectedDeviceInfoVisibility = Visibility.Visible;
      SelfControlPartSelectionVisibility = Visibility.Collapsed;

      LoadAllSelectableDevices();

      PartDataControl = PartData;
    }

    private void ChangePartVisible(AdminRightsChanged e)
    {
      Dispatcher.Invoke(() =>
      {
        if (_isHasDevice)
        {
          SelfControlPartSelectionVisibility = e.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
          SelfControlPartSelectionVisibility = Visibility.Collapsed;
        }
      });
    }

    #region IDeviceSelector

    /// <summary>
    /// Безопасно определяет тип выбранного устройства и возвращает его экземпляр
    /// в соответствии с его фактическим интерфейсом (IRelaySwitchModule,
    /// ISwitchingDevice, IPowerSourceModule, IBreakdownTester).
    /// Гарантирует выполнение в UI-потоке.
    /// </summary>
    /// <returns>Экземпляр устройства соответствующего типа либо null.</returns>
    public object? GetSelectedRelayDeviceByTypeSafe()
    {
      object? device = null;

      void TryGet()
      {
        var type = GetSelectedRelayDeviceType();

        device = type switch
        {
          DeviceType.RelaySwitchModule => GetSelectedRelayDevice<IRelaySwitchModule>(),
          DeviceType.SwitchingDevice => GetSelectedRelayDevice<ISwitchingDevice>(),
          DeviceType.PowerSourceModule => GetSelectedRelayDevice<IPowerSourceModule>(),
          DeviceType.BreakdownTester => GetSelectedRelayDevice<IBreakdownTester>(),
          _ => null
        };
      }

      if (Dispatcher.CheckAccess())
        TryGet();
      else
        Dispatcher.Invoke(TryGet);

      return device;
    }

    /// <summary>
    /// Возвращает тип выбранного устройства на основе его фактической
    /// реализации интерфейсов.
    /// </summary>
    /// <returns>
    /// Значение DeviceType:
    /// RelaySwitchModule, SwitchingDevice, PowerSourceModule, BreakdownTester
    /// или Unknown.
    /// </returns>
    public DeviceType GetSelectedRelayDeviceType()
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
          IRelaySwitchModule => DeviceType.RelaySwitchModule,
          ISwitchingDevice => DeviceType.SwitchingDevice,
          IPowerSourceModule => DeviceType.PowerSourceModule,
          IBreakdownTester => DeviceType.BreakdownTester,
          _ => DeviceType.Unknown
        };
      }
      catch (Exception ex)
      {
        LogException(ex);
        return DeviceType.Unknown;
      }
    }

    /// <summary>
    /// Возвращает выбранное значение enum для блока самоконтроля в непараметризованном виде.
    /// Гарантирует выполнение в UI-потоке.
    /// </summary>
    /// <returns>Экземпляр Enum или null, если ничего не выбрано.</returns>
    public Enum? GetSelectedSelfControlEnumUntypedSafe()
    {
      Enum? result = null;

      void TryGet()
      {
        if (PartDataControl.SelectedItem is EnumDisplayItem item)
          result = item.Value;
      }

      if (Dispatcher.CheckAccess())
        TryGet();
      else
        Dispatcher.Invoke(TryGet);

      return result;
    }

    /// <summary>
    /// Возвращает выбранное устройство как IFastMeter, если возможно.
    /// </summary>
    /// <returns>Объект типа IFastMeter или null, если выбранное устройство не реализует IFastMeter.</returns>
    public IFastMeter GetFastMeterSafe()
    {
      IFastMeter? result = new FastMeterServices().GetAll().FirstOrDefault();

      return result;
    }

    #endregion

    /// <summary>
    /// Обрабатывает событие выбора устройства. Определяет конкретный интерфейс устройства,
    /// получает тип enum для самоконтроля и заполняет UI элементами выбора.
    /// </summary>
    /// <param name="obj">Параметр события (не используется).</param>
    private void RelayData_DeviceSelected(object obj)
    {
      var selectedDevice = RelayData.SelectedItem;

      if (selectedDevice is ISwitchingDevice switchingDevice && switchingDevice.SelfTestManager is ISelfTestCheckerDeviceBusCommutation checker)
      {
        var enumType = checker.GetTestTypeEnum();
        SetSelfControlEnum(enumType);
      }
      else if (selectedDevice is IPowerSourceModule powerSource && powerSource.SelfTestManager is ISelfTestCheckerModuleVoltageCurrentSource checker2)
      {
        var enumType = checker2.GetTestTypeEnum();
        SetSelfControlEnum(enumType);
      }
      else if (selectedDevice is IRelaySwitchModule relayModule && relayModule.SelfTestManager is ISelfTestCheckerModuleRelayControl checker3)
      {
        var enumType = checker3.GetTestTypeEnum();
        SetSelfControlEnum(enumType);
      }
      else if (selectedDevice is IBreakdownTester breakdownTester && breakdownTester.SelfTestManager is ISelfTestCheckerBreakdownTester checker4)
      {
        var enumType = checker4.GetTestTypeEnum();
        SetSelfControlEnum(enumType);
      }
      _isHasDevice = true;

      if (Ask.Core.Services.Config.AppSettings.AdminConfig.GetAdminRights())
      {
        SelfControlPartSelectionVisibility = Visibility.Visible;
      }
    }

    /// <summary>
    /// Загружает все доступные модули коммутации и устройства УКШ, объединяет их в общий список.
    /// Отображает список в элементе выбора.
    /// </summary>
    private void LoadAllSelectableDevices()
    {
      var sources = new List<IEnumerable<dynamic>>
    {
        new PowerSourceModuleServices().GetAll(),
        new SwitchingDeviceServices().GetAll(),
        new RelaySwitchModuleServices().GetAll(),
        new BreakdownTesterServices().GetAll()
    };

      var combined = new List<object>();
      var displayNames = new List<string>();

      foreach (var list in sources)
      {
        foreach (var item in list)
        {
          combined.Add(item);
          displayNames.Add($"{item.Name} {item.NumberChassis}.{item.Number}");
        }
      }

      RelayData.ItemsSource = combined;
      RelayData.DisplayFields = displayNames;
    }


    /// <summary>
    /// Возвращает выбранное устройство как IRelaySwitchModule, если возможно.
    /// </summary>
    public IRelaySwitchModule? GetRelayModule()
    {
      return RelayData.SelectedItem as IRelaySwitchModule;
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
    public Visibility SelfControlPartSelectionVisibility
    {
      get => SelfControlPartSelector.Visibility;
      set => SelfControlPartSelector.Visibility = value;
    }

    public T? GetSelectedRelayDevice<T>() where T : class
    {
      var selected = RelayData.SelectedItem;

      return GetSelectedRelayDeviceType() switch
      {
        DeviceType.RelaySwitchModule when typeof(T) == typeof(IRelaySwitchModule) => selected as T,
        DeviceType.SwitchingDevice when typeof(T) == typeof(ISwitchingDevice) => selected as T,
        DeviceType.PowerSourceModule when typeof(T) == typeof(IPowerSourceModule) => selected as T,
        DeviceType.BreakdownTester when typeof(T) == typeof(IBreakdownTester) => selected as T,
        _ => null
      };
    }

    /// <summary>
    /// Устанавливает перечисление в поле выбора "Тип проверки".
    /// Работает с любым enum.
    /// </summary>
    /// <param name="enumType">Тип перечисления.</param>
    /// <param name="defaultSelected">Значение, которое нужно выбрать по умолчанию (необязательно).</param>
    public void SetSelfControlEnum(Type enumType)
    {
      if (!enumType.IsEnum)
        throw new ArgumentException("Тип должен быть перечислением (enum).", nameof(enumType));

      var values = Enum.GetValues(enumType).Cast<Enum>();

      var items = values.Select(e =>
      {
        var field = enumType.GetField(e.ToString());
        var attr = field?.GetCustomAttribute<DescriptionAttribute>();
        var desc = attr?.Description ?? e.ToString();
        return new EnumDisplayItem { Description = desc, Value = e };
      }).ToList();

      PartData.ItemsSource = items;
      PartData.DisplayFields = items.Select(i => i.Description).ToList();
      PartData.SelectedItem = items.First();
    }

    /// <summary>
    /// Возвращает выбранное значение enum из панели самоконтроля, приведённое к указанному типу.
    /// </summary>
    /// <typeparam name="TEnum">Тип перечисления.</typeparam>
    /// <returns>Значение перечисления или null, если выбранное значение несовместимо.</returns>
    public TEnum? GetSelectedSelfControlEnum<TEnum>() where TEnum : struct, Enum
    {
      if (PartData.SelectedItem is EnumDisplayItem item && item.Value is TEnum enumValue)
        return enumValue;

      return null;
    }
  }
}
