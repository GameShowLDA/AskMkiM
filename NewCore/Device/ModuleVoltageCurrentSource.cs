using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using NewCore.Base.Device;
using NewCore.Base.DeviceResponses;
using NewCore.Base.Function.ModuleVoltageCurrentSource;
using NewCore.Base.Interface.Main;
using NewCore.Enum;
using NewCore.Function.ModuleVoltageCurrentSource;
using NewCore.FunctionAdapters.ModuleVoltageCurrentSource;

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
      Name = "Модуль источника напряжения и тока";
      Description = "Предназначен для создания электрических параметров для проверки кабельных изделий, печатных плат, контроля функционирования релейно-коммутационных изделий и другой подобной аппаратуры, проведения испытаний изделий по программам контроля";

      DeviceType = DeviceEnum.DeviceType.PowerSourceModule;

      BusManager = new BusManagerAdapter(this);
      CurrentManager = new CurrentManagerAdapter(this);
      ConnectableManager = new StateManagerAdapter(this);
      VoltageManager = new VoltageManagerAdapter(this);
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
    public string? ResistanceCalibrationJson { get; set ; }

    /// <summary>
    /// Десериализованные калибровочные диапазоны сопротивления
    /// </summary>
    [NotMapped]
    public List<ResistanceCalibrationRange> ResistanceCalibration { get; set; } = new();
  }
}
