using System.Net;
using Core.Abstract;
using Core.Model;

namespace Core.ConfigCollector
{
  /// <summary>
  /// Часть класса, представляющая информацию различных значений конфигурации.
  /// </summary>
  static public partial class ConfigCollector
  {
    #region Менеджер Шасси.

    /// <summary>
    /// Возвращает номер Менеджера шасси.
    /// </summary>
    /// <returns>Если Менеджер шасси записан, то возвращает Номер модуля. Иначе null.</returns>
    static public string GetManagerShassyNumber()
    {
      if (ManagerShassy != null)
      {
        return ManagerShassy.Number;
      }

      return null;
    }

    /// <summary>
    /// Возвращает Ip Менеджера шасси.
    /// </summary>
    /// <returns>Если Менеджер шасси записан, то возвращает Ip. Иначе null.</returns>
    static public IPAddress GetManagerShassyIp()
    {
      if (ManagerShassy != null)
      {
        return ManagerShassy.IPAddress;
      }

      return null;
    }

    /// <summary>
    /// Возвращает объект менеджера шасси.
    /// </summary>
    /// <returns></returns>
    static public ManagerShassy.Model GetManagerShassy()
    {
      return ManagerShassy;
    }

    /// <summary>
    /// Возвращает объект менеджера шасси.
    /// </summary>
    /// <returns></returns>
    static public ManagerShassy.Model GetManagerShassy(string number)
    {
      if (ManagerShassy.Number == number)
      {
        return ManagerShassy;
      }
      else
      {
        return null;
      }
    }

    #endregion

    #region Устройство коммутации шин.

    /// <summary>
    /// Возвращает Ip УКШ.
    /// </summary>
    /// <returns>Если УКШ записан, то возвращает Ip. Иначе null.</returns>
    static public IPAddress GetDeviceBusCommutationIp()
    {
      if (DeviceBusCommunication != null)
      {
        return DeviceBusCommunication.IPAddress;
      }

      return null;
    }

    /// <summary>
    /// Возвращает номер УКШ.
    /// </summary>
    /// <returns>Если УКШ записан, то возвращает Номер модуля. Иначе null.</returns>
    static public string GetDeviceBusCommutationNumber()
    {
      if (DeviceBusCommunication != null)
      {
        return DeviceBusCommunication.Number;
      }

      return null;
    }

    /// <summary>
    /// Возвращает УКШ.
    /// </summary>
    /// <returns>Возвращает УКШ.</returns>
    static public DeviceBusCommutation.Model GetDeviceBusCommutation()
    {
      return DeviceBusCommunication;
    }
    #endregion

    #region МИНТ.

    /// <summary>
    /// Возвращает IP-адрес источника напряжения и тока модуля.
    /// </summary>
    /// <returns>
    /// IP-адрес источника напряжения и тока модуля. Если модуль не инициализирован или равен null, возвращает null.
    /// </returns>
    static public IPAddress GetIpModuleVoltageCurrentSource()
    {
      if (ModuleVoltageCurrentSource != null)
      {
        return ModuleVoltageCurrentSource.IPAddress;
      }
      else
      {
        return null;
      }
    }

    /// <summary>
    /// Возвращает источник напряжения и тока модуля..
    /// </summary>
    /// <returns>Если УКШ записан, то возвращает Номер модуля. Иначе null.</returns>
    static public ModuleVoltageCurrentSource.Model GetModuleVoltageCurrentSource()
    {
      return ModuleVoltageCurrentSource;
    }

    #endregion

    /// <summary>
    /// Возвращает список адресов MKR.
    /// </summary>
    /// <returns> Возвращает список адресов MKR.</returns>
    static public List<string> GetAllIpMkr()
    {
      List<string> list = new List<string>();
      if (ModuleRelayControlsList != null)
      {
        foreach (DeviceModel item in ModuleRelayControlsList)
        {
          list.Add(item.IPAddress.ToString());
        }

        return list;
      }

      return null;
    }

    /// <summary>
    /// Возвращает активную конфигурацию модулей МКР.
    /// </summary>
    /// <returns>Модели МКР.</returns>
    static public List<ModuleRelayControl.Model> GetMkrModels()
    {
      if (ModuleRelayControlsList != null)
      {
        return ModuleRelayControlsList;
      }
      else
      {
        return new List<ModuleRelayControl.Model>();
      }
    }

    /// <summary>
    /// Возвращает модель мультиметра(быстрого).
    /// </summary>
    /// <returns></returns>
    static public MeterBase GetFastMeter()
    {
      return FastMeter;
    }

    /// <summary>
    /// Возвращает модель мультиметра(быстрого).
    /// </summary>
    /// <returns></returns>
    static public MeterBase GetAccurateMeter()
    {
      return AccurateMeter;
    }

    /// <summary>
    /// Возвращает список всех заданных устройств.
    /// </summary>
    /// <returns></returns>
    static public List<DeviceModel> GetAllDevices()
    {

      List<DeviceModel> deviceModels = new List<DeviceModel>();
      var managerShassy = GetManagerShassy();
      var deviceBusCommutation = GetDeviceBusCommutation();
      var fastMeter = GetFastMeter();
      var accurateMeter = GetAccurateMeter();
      var moduleCommutationRelay = GetMkrModels();
      var moduleVoltage = GetModuleVoltageCurrentSource();

      if (managerShassy != null)
      {
        deviceModels.Add(managerShassy);
      }

      if (deviceBusCommutation != null)
      {
        deviceModels.Add(deviceBusCommutation);
      }

      if (fastMeter != null)
      {
        deviceModels.Add(fastMeter);
      }

      if (accurateMeter != null)
      {
        deviceModels.Add(accurateMeter);
      }

      if (moduleVoltage != null)
      {
        deviceModels.Add(moduleVoltage);
      }

      foreach (var deviceModel in moduleCommutationRelay)
      {
        if (deviceModel != null)
        {
          deviceModels.Add(deviceModel);
        }
      }

      return deviceModels;
    }
  }
}
