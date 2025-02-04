using NewCore.Enum;
using NewCore.Interface;

namespace AppConfig.DataBase.Models
{
  /// <summary>
  /// Класс, представляющий сущность пробойной установки.
  /// </summary>
  public class BreakdownTesterEntity : IBreakdownTester
  {
    public int Id { get; set; }
    /// <summary>
    /// Номер менеджера шасси, к которому подключена пробойная установка.
    /// </summary>
    public int NumberChassis { get; set; }

    /// <summary>
    /// Название пробойной установки.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Описание пробойной установки.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Уникальный номер устройства.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Детали подключения установки (IP-адрес, COM-порт и т. д.).
    /// </summary>
    public string ConnectionDetails { get; set; }

    /// <summary>
    /// Тип устройства, всегда BreakdownTester.
    /// </summary>
    public DeviceEnum.DeviceType DeviceType => DeviceEnum.DeviceType.BreakdownTester;

    /// <summary>
    /// Метод инициализации пробойной установки.
    /// </summary>
    /// <returns>Возвращает true, если инициализация прошла успешно.</returns>
    public Task<bool> Initialize() => Task.FromResult(true);
  }
}
