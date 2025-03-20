using System.ComponentModel.DataAnnotations.Schema;
using NewCore.Base.Function.Breakdown;
using NewCore.Base.Interface.Main;
using NewCore.Enum;

namespace AppConfig.DataBase.Models
{
  /// <summary>
  /// Класс, представляющий сущность пробойной установки.
  /// </summary>
  public class BreakdownTesterEntity : IBreakdownTester
  {
    /// <inheritdoc />
    public int Id { get; set; }

    /// <inheritdoc />
    public int NumberChassis { get; set; }

    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
    public string Description { get; set; }

    /// <inheritdoc />
    public int Number { get; set; }

    /// <inheritdoc />
    public string ConnectionDetails { get; set; }

    /// <inheritdoc />
    public string DeviceClass { get; set; }

    /// <inheritdoc />
    public DeviceEnum.DeviceType DeviceType => DeviceEnum.DeviceType.BreakdownTester;

    /// <inheritdoc />
    [NotMapped]
    public IAcwModeBreakdown AcwManger { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IDcwModeBreakdown DcwManger { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IIrModeBreakdown IrManger { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public ISystemSettingsBreakdown SystemManger { get; set; }

    /// <summary>
    /// Метод инициализации пробойной установки.
    /// </summary>
    /// <returns>Возвращает true, если инициализация прошла успешно.</returns>
    public Task<bool> Initialize() => Task.FromResult(true);
  }
}
