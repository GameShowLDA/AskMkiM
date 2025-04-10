using System.IO.Ports;
using System.Net;
using System.Windows;
using DataBaseConfiguration.Models;
using DataBaseConfiguration.Models.Device;
using Mode.Settings.DeviceConfig.Base.BaseSettingsConfig;
using NewCore.Base;
using NewCore.Base.Device;
using NewCore.Base.Interface.Main;

namespace Mode.Settings.DeviceConfig.Base
{
  /// <summary>
  /// Класс для обработки данных устройств на основе типа T.
  /// </summary>
  /// <typeparam name="T">Тип устройства, реализующего интерфейс IDevice.</typeparam>
  internal static class BaseHandler<T> where T : class, IDevice
  {
    /// <summary>
    /// Словарь, содержащий соответствие строковых значений и четности порта.
    /// </summary>
    public static readonly Dictionary<string, Parity> ValuePairs = new Dictionary<string, Parity>
    {
      ["Чет"] = Parity.Even,
      ["Нечет"] = Parity.Odd,
      ["Нет"] = Parity.None,
      ["Маркер"] = Parity.Mark,
      ["Пробел"] = Parity.Space,
    };

    /// <summary>
    /// Словарь, содержащий соответствие строковых значений и количества стоп-бит.
    /// </summary>
    public static readonly Dictionary<string, StopBits> StopBitsPairs = new Dictionary<string, StopBits>
    {
      ["1"] = StopBits.One,
      ["1.5"] = StopBits.OnePointFive,
      ["2"] = StopBits.Two,
    };

    /// <summary>
    /// Определяет, какой интерфейс устройства реализует переданный экземпляр.
    /// </summary>
    /// <param name="instance">Экземпляр устройства.</param>
    /// <returns>Тип интерфейса устройства.</returns>
    private static Type GetDeviceInterface(object instance)
    {
      var interfaceMappings = new Dictionary<Type, Type>
      {
        [typeof(BreakdownTesterEntity)] = typeof(IBreakdownTester),
        [typeof(ChassisManagerEntity)] = typeof(IChassisManager),
        [typeof(FastMeterEntity)] = typeof(IFastMeter),
        [typeof(PowerSourceModuleEntity)] = typeof(IPowerSourceModule),
        [typeof(PrecisionMeterEntity)] = typeof(IPrecisionMeter),
        [typeof(RelaySwitchModuleEntity)] = typeof(IRelaySwitchModule),
        [typeof(SwitchingDeviceEntity)] = typeof(ISwitchingDevice),
        [typeof(RackEntity)] = typeof(IRack),
      };

      Type instanceType = instance.GetType();

      foreach (var mapping in interfaceMappings)
      {
        if (mapping.Key.IsAssignableFrom(instanceType))
        {
          return mapping.Value;
        }
      }

      throw new InvalidOperationException($"Не удалось определить интерфейс для типа {instanceType.Name}.");
    }

    /// <summary>
    /// Проверяет, реализует ли тип T известные интерфейсы устройств, и возвращает соответствующий тип.
    /// </summary>
    /// <returns>Тип интерфейса устройства.</returns>
    private static Type DetermineInterface()
    {
      var interfaceMappings = new Dictionary<Type, string>
      {
        [typeof(IChassisManager)] = nameof(IChassisManager),
        [typeof(IBreakdownTester)] = nameof(IBreakdownTester),
        [typeof(IFastMeter)] = nameof(IFastMeter),
        [typeof(IPowerSourceModule)] = nameof(IPowerSourceModule),
        [typeof(IPrecisionMeter)] = nameof(IPrecisionMeter),
        [typeof(IRelaySwitchModule)] = nameof(IRelaySwitchModule),
        [typeof(ISwitchingDevice)] = nameof(ISwitchingDevice),
        [typeof(IRack)] = nameof(IRack),
      };

      foreach (var interfaceType in interfaceMappings.Keys)
      {
        if (interfaceType.IsAssignableFrom(typeof(T)))
        {
          return interfaceType;
        }
      }

      throw new InvalidOperationException(
          $"Тип {typeof(T).Name} не принадлежит к известным интерфейсам ({string.Join(", ", interfaceMappings.Values)}).");
    }

    /// <summary>
    /// Получает параметры подключения устройства.
    /// </summary>
    /// <param name="defaultSettingControl">Элемент управления настройками устройства.</param>
    /// <param name="instance">Экземпляр устройства.</param>
    /// <returns>Строка с параметрами подключения.</returns>
    public static string GetConnectionDetails(DeviceSettingsControl defaultSettingControl, object instance)
    {
      if (instance is DeviceWithIP)
      {
        return GetIPAddress(defaultSettingControl).ToString();
      }

      if (instance is DeviceWithCOM)
      {
        return GetSerialPort(defaultSettingControl).ToString();
      }

      MessageBox.Show("Устройство не принадлежит к известным типам (DeviceWithIP или DeviceWithCOM).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      return null;
    }

    /// <summary>
    /// Возвращает IP-адрес, указанный в настройках устройства.
    /// </summary>
    /// <param name="defaultSettingControl">Элемент управления настройками устройства.</param>
    /// <returns>Объект IPAddress.</returns>
    /// <exception cref="Exception">Выбрасывается, если введенные данные невалидны.</exception>
    private static IPAddress GetIPAddress(DeviceSettingsControl defaultSettingControl)
    {
      if (defaultSettingControl.IpPart1Value != -1 &&
          defaultSettingControl.IpPart2Value != -1 &&
          defaultSettingControl.IpPart3Value != -1 &&
          defaultSettingControl.IpPart4Value != -1)
      {
        return IPAddress.Parse($"{defaultSettingControl.IpPart1Value}.{defaultSettingControl.IpPart2Value}.{defaultSettingControl.IpPart3Value}.{defaultSettingControl.IpPart4Value}");
      }

      throw new Exception("Реализовать подсветку ошибки для IP-адреса.");
    }

    /// <summary>
    /// Возвращает параметры COM-порта, указанные в настройках устройства.
    /// </summary>
    /// <param name="defaultSettingControl">Элемент управления настройками устройства.</param>
    /// <returns>Объект SerialPortCustom.</returns>
    /// <exception cref="ArgumentException">Выбрасывается, если данные невалидны.</exception>
    private static SerialPortCustom GetSerialPort(DeviceSettingsControl defaultSettingControl)
    {
      string portName = defaultSettingControl.PortName;
      int baudRate = defaultSettingControl.BaudRateValue;

      if (baudRate == -1)
      {
        throw new ArgumentException("Системная ошибка преобразования значения.");
      }

      Parity parity = defaultSettingControl.ParityValue;
      StopBits stopBits = defaultSettingControl.StopBitsValue;
      int dataBits = defaultSettingControl.DataBitsValue;

      if (dataBits == -1)
      {
        throw new ArgumentException("Системная ошибка преобразования значения.");
      }

      return new SerialPortCustom(portName, baudRate, parity, dataBits, stopBits);
    }

    /// <summary>
    /// Получает номер устройства из настроек.
    /// </summary>
    /// <param name="defaultSettingControl">Элемент управления настройками устройства.</param>
    /// <returns>Номер устройства.</returns>
    public static int GetNumber(DeviceSettingsControl defaultSettingControl)
    {
      return defaultSettingControl.NumberDevice;
    }
  }
}
