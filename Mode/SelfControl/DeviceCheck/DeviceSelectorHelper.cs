using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewCore.Base.Interface.Main;
using UI.Components;
using UI.Controls.ProtocolNew;
using Utilities;
using static UI.Components.DeviceSelectorPanel;

namespace Mode.SelfControl.DeviceCheck
{
  /// <summary>
  /// Вспомогательный класс для безопасной работы с DeviceSelectorPanel в ProtocolUI.
  /// </summary>
  public static class DeviceSelectorHelper
  {
    /// <summary>
    /// Безопасно извлекает InputField из ProtocolUI.ContentView.
    /// </summary>
    /// <param name="protocolUI">Элемент ProtocolUI.</param>
    /// <returns>InputField или null, если не удалось извлечь.</returns>
    public static DeviceSelectorPanel? GetInputFieldSafe(this ProtocolUI protocolUI)
    {
      if (protocolUI == null)
      {
        return null;
      }

      DeviceSelectorPanel? result = null;

      void TryGet()
      {
        if (protocolUI.ContentView is DeviceSelectorPanel inputField)
        {
          result = inputField;
        }
      }

      if (protocolUI.Dispatcher.CheckAccess())
      {
        TryGet();
      }
      else
        protocolUI.Dispatcher.Invoke(TryGet);

      return result;
    }

    /// <summary>
    /// Безопасно извлекает IFastMeter из DeviceSelectorPanel.
    /// </summary>
    /// <param name="protocolUI">Экземпляр ProtocolUI.</param>
    /// <returns>IFastMeter или null, если не удалось извлечь.</returns>
    public static IFastMeter? GetFastMeterSafe(this DeviceSelectorPanel panel)
    {
      IFastMeter? result = null;

      void TryGet()
      {
        result = panel.GetFastMeter();
      }

      if (panel.Dispatcher.CheckAccess())
        TryGet();
      else
        panel.Dispatcher.Invoke(TryGet);

      return result;

      return result;
    }

    /// <summary>
    /// Безопасно извлекает IRelaySwitchModule из DeviceSelectorPanel.
    /// </summary>
    /// <param name="protocolUI">Экземпляр ProtocolUI.</param>
    /// <returns>IRelaySwitchModule или null.</returns>
    public static IRelaySwitchModule? GetRelaySwitchModuleSafe(this DeviceSelectorPanel panel)
    {
      IRelaySwitchModule? result = null;

      void TryGet()
      {
        result = panel.GetRelayModule();
      }

      if (panel.Dispatcher.CheckAccess())
        TryGet();
      else
        panel.Dispatcher.Invoke(TryGet);

      return result;
    }

    /// <summary>
    /// Безопасно извлекает IChassisManager из DeviceSelectorPanel.
    /// </summary>
    /// <param name="protocolUI">Экземпляр ProtocolUI.</param>
    /// <returns>IChassisManager или null.</returns>
    public static IChassisManager? GetChassisManagerSafe(this DeviceSelectorPanel panel)
    {
      IChassisManager? result = null;

      void TryGet()
      {
        result = panel.GetChassisManager();
      }

      if (panel.Dispatcher.CheckAccess())
        TryGet();
      else
        panel.Dispatcher.Invoke(TryGet);

      return result;
    }


    /// <summary>
    /// Безопасно извлекает выбранное устройство из DeviceSelectorPanel,
    /// возвращает приведённый тип и соответствующий RelayDeviceType.
    /// </summary>
    /// <typeparam name="T">Ожидаемый интерфейс (например, IRelaySwitchModule).</typeparam>
    /// <param name="panel">Экземпляр DeviceSelectorPanel.</param>
    /// <returns>Кортеж: (объект типа T или null, тип устройства).</returns>
    public static (T? Device, RelayDeviceType Type) GetSelectedRelayDeviceWithTypeSafe<T>(this DeviceSelectorPanel panel) where T : class
    {
      T? device = null;
      RelayDeviceType type = RelayDeviceType.Unknown;

      void TryGet()
      {
        type = panel.GetSelectedRelayDeviceType();

        device = type switch
        {
          RelayDeviceType.RelaySwitchModule when typeof(T) == typeof(IRelaySwitchModule) => panel.GetSelectedRelayDevice<IRelaySwitchModule>() as T,
          RelayDeviceType.SwitchingDevice when typeof(T) == typeof(ISwitchingDevice) => panel.GetSelectedRelayDevice<ISwitchingDevice>() as T,
          RelayDeviceType.PowerSourceModule when typeof(T) == typeof(IPowerSourceModule) => panel.GetSelectedRelayDevice<IPowerSourceModule>() as T,
          _ => null
        };
      }

      if (panel.Dispatcher.CheckAccess())
        TryGet();
      else
        panel.Dispatcher.Invoke(TryGet);

      return (device, type);
    }

    /// <summary>
    /// Безопасно извлекает выбранное устройство из DeviceSelectorPanel, приведённое к нужному интерфейсу по типу устройства.
    /// </summary>
    /// <param name="panel">Экземпляр DeviceSelectorPanel.</param>
    /// <returns>Объект нужного интерфейса (IRelaySwitchModule, ISwitchingDevice, IPowerSourceModule) или null.</returns>
    public static object? GetSelectedRelayDeviceByTypeSafe(this DeviceSelectorPanel panel)
    {
      object? device = null;

      void TryGet()
      {
        var type = panel.GetSelectedRelayDeviceType();

        device = type switch
        {
          RelayDeviceType.RelaySwitchModule => panel.GetSelectedRelayDevice<IRelaySwitchModule>(),
          RelayDeviceType.SwitchingDevice => panel.GetSelectedRelayDevice<ISwitchingDevice>(),
          RelayDeviceType.PowerSourceModule => panel.GetSelectedRelayDevice<IPowerSourceModule>(),
          _ => null
        };
      }

      if (panel.Dispatcher.CheckAccess())
        TryGet();
      else
        panel.Dispatcher.Invoke(TryGet);

      return device;
    }

    /// <summary>
    /// Безопасно извлекает выбранное значение перечисления из поля "Тип проверки".
    /// </summary>
    /// <typeparam name="TEnum">Тип перечисления.</typeparam>
    /// <param name="panel">Экземпляр <see cref="DeviceSelectorPanel"/>.</param>
    /// <returns>Выбранное значение enum или null.</returns>
    public static TEnum? GetSelectedSelfControlEnumSafe<TEnum>(this DeviceSelectorPanel panel)
      where TEnum : struct, Enum
    {
      TEnum? result = null;

      void TryGet()
      {
        result = panel.GetSelectedSelfControlEnum<TEnum>();
      }

      if (panel.Dispatcher.CheckAccess())
        TryGet();
      else
        panel.Dispatcher.Invoke(TryGet);

      return result;
    }

    /// <summary>
    /// Безопасно извлекает выбранное значение enum из PartData, не зная точного типа.
    /// </summary>
    /// <param name="panel">Экземпляр <see cref="DeviceSelectorPanel"/>.</param>
    /// <returns>Значение перечисления (System.Enum) или null.</returns>
    public static System.Enum? GetSelectedSelfControlEnumUntypedSafe(this DeviceSelectorPanel panel)
    {
      System.Enum? result = null;

      void TryGet()
      {
        if (panel.PartDataControl.SelectedItem is EnumDisplayItem item)
          result = item.Value;
      }

      if (panel.Dispatcher.CheckAccess())
        TryGet();
      else
        panel.Dispatcher.Invoke(TryGet);

      return result;
    }

  }
}
