using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using AppConfig.DataBase.Models;
using AppConfig.DataBase.Services;
using NewCore.Base;
using NewCore.Interface;
using System.Windows;
using static Utilities.LoggerUtility;
using System.Management;
using NewCore.Device;
using Mode.Settings.DeviceConfig.Base.HandlerDevice;
using Mode.Settings.DeviceConfig.Base.BaseSettings;
using System.Windows.Controls;

namespace Mode.Settings.DeviceConfig.Base
{
  /// <summary>
  /// Класс для обработки данных устройств на основе типа T.
  /// </summary>
  static internal class BaseHandler<T> where T : class, IDevice
  {

   static private Dictionary<string, Parity> ValuePairs = new Dictionary<string, Parity>()
   {
     { "Чет", Parity.Even },
     { "Нечет", Parity.Odd },
     { "Нет", Parity.None },
     { "Маркер", Parity.Mark },
     { "Пробел", Parity.Space }
   };

    static private Dictionary<string, StopBits> StopBitsPairs = new Dictionary<string, StopBits>()
    {
       { "1", StopBits.One },
       { "1.5", StopBits.OnePointFive },
       { "2", StopBits.Two }
    };

    private static readonly Dictionary<Type, Type> _interfaceMappings = new()
    {
        { typeof(BreakdownTesterEntity), typeof(IBreakdownTester) },
        { typeof(ChassisManagerEntity), typeof(IChassisManager) },
        { typeof(FastMeterEntity), typeof(IFastMeter) },
        { typeof(PowerSourceModuleEntity), typeof(IPowerSourceModule) },
        { typeof(PrecisionMeterEntity), typeof(IPrecisionMeter) },
        { typeof(RackEntity), typeof(IRack) },
        { typeof(RelaySwitchModuleEntity), typeof(IRelaySwitchModule) },
        { typeof(SwitchingDeviceEntity), typeof(ISwitchingDevice) },
    };


    #region События.

    /// <summary>
    /// Событие, вызываемое после успешного сохранения устройства.
    /// </summary>
    static public event EventHandler<ChassisManagerEntity> ChassisManagerSaved;
    static public event EventHandler<BreakdownTesterEntity> BreakdownTesterSaved;
    static public event EventHandler<object> RequestSave;

    #endregion

    /// <summary>
    /// Обрабатывает данные устройства на основе выбранной модели.
    /// </summary>
    /// <param name="selectedModel">Выбранная модель устройства.</param>
    /// <param name="deviceModelMap">Словарь моделей устройств с соответствующими типами.</param>
    static internal bool ProcessDeviceData(string selectedModel, Dictionary<string, Type> deviceModelMap, IDataProcessor dataProcessor)
    {
      if (deviceModelMap.TryGetValue(selectedModel, out Type selectedType))
      {
        try
        {
          var instance = Activator.CreateInstance(selectedType);

          if (instance != null)
          {
            var type = DetermineInterface();
            return HandleDeviceByType(instance, type, dataProcessor);
          }
          else
          {
            MessageBox.Show($"Не удалось создать экземпляр класса {selectedType.Name}.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
          }
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Ошибка при создании устройства {selectedType.Name}: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
          return false;
        }
      }
      return false;
    }

    /// <summary>
    /// Определяет и вызывает соответствующий метод обработки устройства на основе его типа.
    /// </summary>
    /// <param name="instance">Экземпляр устройства.</param>
    /// <param name="type">Тип устройства для обработки.</param>
    static private bool HandleDeviceByType(object instance, Type type, IDataProcessor dataProcessor)
    {
      return dataProcessor.HandleData(instance);
    }

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

    static internal string GetConnectionDetails(BaseSettingsControl DefaultSettingControl, object instance)
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
    static IPAddress GetIPAddress(BaseSettingsControl DefaultSettingControl)
    {
      IPAddress ipAddress = null;

      if (int.TryParse(DefaultSettingControl.IPAddressPart3Input.Text, out int ipPart3) && int.TryParse(DefaultSettingControl.IPAddressPart4Input.Text, out int ipPart4))
      {
        ipAddress = IPAddress.Parse($"192.168.{ipPart3}.{ipPart4}");
      }
      else
      {
        throw new Exception("Реализовать подсветку ошибки для IP-адреса");
      }

      return ipAddress;
    }

    static SerialPortCustom GetSerialPort(BaseSettingsControl DefaultSettingControl)
    {
      string portName = DefaultSettingControl.COMPortSelectionBox.Text;
      ComboBoxItem selectedItem = DefaultSettingControl.BaudRateComboBoxControl.SelectedItem as ComboBoxItem;
      if (selectedItem != null && int.TryParse(selectedItem.Content.ToString(), out int baudRate))
      {
      }
      else
      {
        throw new ArgumentException("Системная ошибка преобразования значения.");
      } 


      Parity parity = new Parity();
      ValuePairs.TryGetValue(DefaultSettingControl.ParityComboBoxControl.SelectedItem.ToString(), out parity);

      StopBits stopBit = new StopBits();
      StopBitsPairs.TryGetValue(DefaultSettingControl.StopBitsComboBoxControl.SelectedItem.ToString(), out stopBit);

      selectedItem = DefaultSettingControl.BaudRateComboBoxControl.SelectedItem as ComboBoxItem;
      if (selectedItem != null && int.TryParse(selectedItem.Content.ToString(), out int dataBits))
      {
      }
      else
      {
        throw new ArgumentException("Системная ошибка преобразования значения.");
      }

      SerialPortCustom serialPortCustom = new SerialPortCustom(portName, baudRate, parity, dataBits, stopBit);
      return serialPortCustom;
    }
  }
}
