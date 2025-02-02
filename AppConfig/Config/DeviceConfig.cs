using AppConfig.DataBase.Models;
using NewCore.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppConfig.Config
{
  /// <summary>
  /// Класс конфигурации устройств, содержащий списки всех зарегистрированных устройств
  /// </summary>
  public class DeviceConfig
  {
    // <summary>
    /// Список менеджеров шасси
    /// </summary>
    public List<ChassisManagerEntity> ChassisManagers { get; set; } = new();

    /// <summary>
    /// Список модулей коммутации реле
    /// </summary>
    public List<RelaySwitchModuleEntity> RelaySwitchModules { get; set; } = new();

    /// <summary>
    /// Список модулей источников питания
    /// </summary>
    public List<PowerSourceModuleEntity> PowerSourceModules { get; set; } = new();

    /// <summary>
    /// Список устройств коммутации
    /// </summary>
    public List<SwitchingDeviceEntity> SwitchingDevices { get; set; } = new();

    /// <summary>
    /// Список точных измерителей
    /// </summary>
    public List<PrecisionMeterEntity> PrecisionMeters { get; set; } = new();

    /// <summary>
    /// Список быстрых измерителей
    /// </summary>
    public List<FastMeterEntity> FastMeters { get; set; } = new();

    /// <summary>
    /// Список пробойных установок
    /// </summary>
    public List<BreakdownTesterEntity> BreakdownTesters { get; set; } = new();

  }
}
