using System.IO.Ports;
using System.Net;
using System.Windows;
using AppConfig.DataBase.Models;
using Mode.Settings.DeviceConfig.Base.BaseSettingsConfig;
using NewCore.Base;
using NewCore.Base.Device;
using NewCore.Base.Interface.Main;

namespace Mode.Settings.DeviceConfig.Base
{
  /// <summary>
  /// Класс для обработки данных устройств на основе типа T.
  /// </summary>
  static internal class BaseHandler<T> where T : class, IDevice
  {

    static public Dictionary<string, Parity> ValuePairs = new Dictionary<string, Parity>()
   {
     { "Чет", Parity.Even },
     { "Нечет", Parity.Odd },
     { "Нет", Parity.None },
     { "Маркер", Parity.Mark },
     { "Пробел", Parity.Space }
   };

    static public Dictionary<string, StopBits> StopBitsPairs = new Dictionary<string, StopBits>()
    {
       { "1", StopBits.One },
       { "1.5", StopBits.OnePointFive },
       { "2", StopBits.Two }
    };

    /// <summary>
    /// Определяет, какой интерфейс устройства реализует переданный экземпляр.
    /// </summary>
    /// <param name="instance">Экземпляр устройства.</param>
    /// <returns>Тип интерфейса устройства.</returns>
    static Type GetDeviceInterface(object instance)
    {
      var interfaceMappings = new Dictionary<Type, Type>
    {
        { typeof(BreakdownTesterEntity), typeof(IBreakdownTester) },
        { typeof(ChassisManagerEntity), typeof(IChassisManager) },
        { typeof(FastMeterEntity), typeof(IFastMeter) },
        { typeof(PowerSourceModuleEntity), typeof(IPowerSourceModule) },
        { typeof(PrecisionMeterEntity), typeof(IPrecisionMeter) },
        { typeof(RelaySwitchModuleEntity), typeof(IRelaySwitchModule) },
        { typeof(SwitchingDeviceEntity), typeof(ISwitchingDevice) },
        { typeof(RackEntity), typeof(IRack) }
    };

      Type instanceType = instance.GetType();

      foreach (var mapping in interfaceMappings)
      {
        if (mapping.Key.IsAssignableFrom(instanceType))
        {
          return mapping.Value; // Возвращаем соответствующий интерфейс
        }
      }

      throw new InvalidOperationException($"Не удалось определить интерфейс для типа {instanceType.Name}.");
    }


    ///// <summary>
    ///// Обрабатывает данные устройства на основе выбранной модели.
    ///// </summary>
    ///// <param name="selectedModel">Выбранная модель устройства.</param>
    ///// <param name="deviceModelMap">Словарь моделей устройств с соответствующими типами.</param>
    //static internal bool ProcessDeviceData(string selectedModel, Dictionary<string, Type> deviceModelMap, IDataProcessor dataProcessor)
    //{
    //  if (deviceModelMap.TryGetValue(selectedModel, out Type selectedType))
    //  {
    //    try
    //    {
    //      var instance = Activator.CreateInstance(selectedType);

    //      if (instance != null)
    //      {
    //        var type = DetermineInterface();
    //        return HandleDeviceByType(instance, type, dataProcessor);
    //      }
    //      else
    //      {
    //        MessageBox.Show($"Не удалось создать экземпляр класса {selectedType.Name}.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
    //        return false;
    //      }
    //    }
    //    catch (Exception ex)
    //    {
    //      MessageBox.Show($"Ошибка при создании устройства {selectedType.Name}: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
    //      return false;
    //    }
    //  }
    //  return false;
    //}

    ///// <summary>
    ///// Определяет и вызывает соответствующий метод обработки устройства на основе его типа.
    ///// </summary>
    ///// <param name="instance">Экземпляр устройства.</param>
    ///// <param name="type">Тип устройства для обработки.</param>
    //static private bool HandleDeviceByType(object instance, Type type, IDataProcessor dataProcessor)
    //{
    //  return dataProcessor.HandleData(instance);
    //}

    /// <summary>
    /// Проверяет, реализует ли тип T известные интерфейсы устройств, и возвращает соответствующий тип.
    /// </summary>
    /// <returns>Тип интерфейса устройства.</returns> F
    static private Type DetermineInterface()
    {
      var interfaceMappings = new Dictionary<Type, string>
      {
        { typeof(IChassisManager), nameof(IChassisManager) },
        { typeof(IBreakdownTester), nameof(IBreakdownTester) },
        { typeof(IFastMeter), nameof(IFastMeter) },
        { typeof(IPowerSourceModule), nameof(IPowerSourceModule) },
        { typeof(IPrecisionMeter), nameof(IPrecisionMeter) },
        { typeof(IRelaySwitchModule), nameof(IRelaySwitchModule) },
        { typeof(ISwitchingDevice), nameof(ISwitchingDevice) },
        { typeof(IRack), nameof(IRack) }
      };

      foreach (var interfaceType in interfaceMappings.Keys)
      {
        if (interfaceType.IsAssignableFrom(typeof(T)))
        {
          return interfaceType;
        }
      }

      throw new InvalidOperationException(
          $"Тип {typeof(T).Name} не принадлежит к известным интерфейсам ({string.Join(", ", interfaceMappings.Values)})."
      );
    }

    static internal string GetConnectionDetails(DeviceSettingsControl DefaultSettingControl, object instance)
    {
      if (instance is DeviceWithIP deviceWithIP)
      {
        return GetIPAddress(DefaultSettingControl).ToString();
      }
      else if (instance is DeviceWithCOM deviceWithCOM)
      {
        return GetSerialPort(DefaultSettingControl).ToString();
      }
      else
      {
        MessageBox.Show("Устройство не принадлежит к известным типам (DeviceWithIP или DeviceWithCOM).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }

      return null;
    }

    /// <summary>
    /// Возвращает IP адрес.
    /// </summary>
    /// <param name="DefaultSettingControl"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    static IPAddress GetIPAddress(DeviceSettingsControl DefaultSettingControl)
    {
      IPAddress ipAddress = null;

      if (DefaultSettingControl.IpPart1Value != -1 &&
        DefaultSettingControl.IpPart2Value != -1 &&
        DefaultSettingControl.IpPart3Value != -1 &&
        DefaultSettingControl.IpPart4Value != -1)
      {
        ipAddress = IPAddress.Parse($"{DefaultSettingControl.IpPart1Value}.{DefaultSettingControl.IpPart2Value}.{DefaultSettingControl.IpPart3Value}.{DefaultSettingControl.IpPart4Value}");
      }
      else
      {
        throw new Exception("Реализовать подсветку ошибки для IP-адреса");
      }

      return ipAddress;
    }

    static SerialPortCustom GetSerialPort(DeviceSettingsControl DefaultSettingControl)
    {
      string portName = DefaultSettingControl.PortName;
      int baudRate;
      if ((baudRate = DefaultSettingControl.BaudRateValue) == -1)
      {
        throw new ArgumentException("Системная ошибка преобразования значения.");
      }

      Parity parity = DefaultSettingControl.ParityValue;
      StopBits stopBit = DefaultSettingControl.StopBitsValue;

      int dataBits;
      if ((dataBits = DefaultSettingControl.DataBitsValue) == -1)
      {
        throw new ArgumentException("Системная ошибка преобразования значения.");
      }

      SerialPortCustom serialPortCustom = new SerialPortCustom(portName, baudRate, parity, dataBits, stopBit);
      return serialPortCustom;
    }

    public static int GetNumber(DeviceSettingsControl DefaultSettingControl)
    {
      return DefaultSettingControl.NumberDevice;
    }
  }
}
