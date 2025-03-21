using NewCore.Base.Device;
using NewCore.Base.Function.ModuleVoltageCurrentSource;
using NewCore.Base.Interface.Main;
using NewCore.Enum;
using NewCore.Function.ModuleVoltageCurrentSource;

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

      BusManager = new BusManager(this);
      CurrentManager = new CurrentManager(this);
      StateManager = new StateManager(this);
      VoltageManager = new VoltageManager(this);
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
    /// Менеджер управления состоянием модуля.
    /// </summary>
    public IStateManager StateManager { get; set; }

    /// <summary>
    /// Менеджер управления напряжением модуля.
    /// </summary>
    public IVoltageManager VoltageManager { get; set; }
  }
}
