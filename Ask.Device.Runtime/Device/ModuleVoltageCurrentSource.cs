using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule.Capabilities;
using Ask.Device.Communication.Ethernet;
using NewCore.Base.Device;
using NewCore.Base.DeviceResponses;
using NewCore.Function.ModuleVoltageCurrentSource.SelfCheck;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewCore.Device
{
  /// <summary>
  /// Класс, представляющий модуль источника напряжения и тока.
  /// </summary>
  public class ModuleVoltageCurrentSource : DeviceWithIP, IPowerSourceModule
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ModuleVoltageCurrentSource"/>.
    /// </summary>
    public ModuleVoltageCurrentSource()
    {
      Name = "Модуль МиНТ";
      Description = "Предназначен для создания электрических параметров для проверки кабельных изделий, печатных плат, контроля функционирования релейно-коммутационных изделий и другой подобной аппаратуры, проведения испытаний изделий по программам контроля";

      DeviceType = Ask.Core.Shared.Metadata.Enums.DeviceEnums.DeviceType.PowerSourceModule;

      BusManager = new Function.ModuleVoltageCurrentSource.BusManager(this);
      CurrentManager = new Function.ModuleVoltageCurrentSource.CurrentManager(this);
      ConnectableManager = new Function.ModuleVoltageCurrentSource.StateManager(this);
      VoltageManager = new Function.ModuleVoltageCurrentSource.VoltageManager(this);
      SelfTestManager = new SelfTestManager();
      DeviceClass = GetType().FullName;
    }

    /// <summary>
    /// Получает или задает номер шасси, к которому подключен модуль.
    /// </summary>
    public int NumberChassis { get; set; }

    /// <summary>
    /// Менеджер управления шинами модуля.
    /// </summary>
    public IBusManager BusManager { get; set; }

    /// <summary>
    /// Менеджер управления током модуля.
    /// </summary>
    public ICurrentManager CurrentManager { get; set; }

    /// <summary>
    /// Менеджер управления напряжением модуля.
    /// </summary>
    public IVoltageManager VoltageManager { get; set; }
    public ISelfTestCheckerModuleVoltageCurrentSource SelfTestManager { get; set; }
    public string? ResistanceCalibrationJson { get; set; }

    /// <summary>
    /// Десериализованные калибровочные диапазоны сопротивления
    /// </summary>
    [NotMapped]
    public List<ResistanceCalibrationRange> ResistanceCalibration { get; set; } = new();
  }
}
