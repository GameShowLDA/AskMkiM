using System.ComponentModel.DataAnnotations.Schema;
using NewCore.Base.Function.ManagerChassis;
using NewCore.Base.Interface.Additionally;
using NewCore.Base.Interface.Main;
using NewCore.Enum;

namespace AppConfig.DataBase.Models
{
  /// <summary>
  /// Класс, представляющий сущность менеджера шасси.
  /// </summary>
  public class ChassisManagerEntity : IChassisManager, IHeadUnit
  {
    /// <inheritdoc />
    public int Id { get; set; }

    /// <inheritdoc />
    public string Name { get; set; }
    
    /// <inheritdoc />
    public string Description { get; set; }

    /// <inheritdoc />
    public int Number { get; set; }

    /// <inheritdoc />
    public string ConnectionDetails { get; set; }

    /// <inheritdoc />
    public DeviceEnum.DeviceType DeviceType => DeviceEnum.DeviceType.ChassisManager;

    /// <inheritdoc />
    public string DeviceClass { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IStateManagerChassis StateManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IPowerManagerChassis PowerManager { get; set; }
  }
}
