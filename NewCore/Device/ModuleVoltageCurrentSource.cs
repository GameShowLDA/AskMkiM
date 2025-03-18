using System.Net;
using NewCore.Base.Device;
using NewCore.Base.Function.ModuleVoltageCurrentSource;
using NewCore.Base.Interface.Main;
using NewCore.Function.ModuleVoltageCurrentSource;

namespace NewCore.Device
{
  public class ModuleVoltageCurrentSource : DeviceWithIP, IPowerSourceModule
  {

    public ModuleVoltageCurrentSource()
    {
      Name = "Модуль источника напряжения и тока";
      Description = "Предназначен для создания электрических параметров для проверки кабельных изделий, печатных плат, контроля функционирования релейно-коммутационных изделий и другой подобной аппаратуры, проведения испытаний изделий по программам контроля";
      BusManager = new BusManager(this);
      CurrentManager = new CurrentManager(this);
      StateManager = new StateManager(this);
      VoltageManager = new VoltageManager(this);
      DeviceClass = GetType().FullName;
    }

    public int NumberChassis { get; set; }
    public IBusManager BusManager { get; set; }
    public ICurrentManager CurrentManager { get; set; }
    public IStateManager StateManager { get; set; }
    public IVoltageManager VoltageManager { get; set; }
  }
}
