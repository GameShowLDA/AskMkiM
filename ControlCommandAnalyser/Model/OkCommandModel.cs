using System.Collections.Generic;

namespace ControlCommandAnalyser.Model.Ok
{
  /// <summary>
  /// Модель команды ОК (объект контроля).
  /// </summary>
  public class OkCommandModel : BaseCommandModel
  {
    /// <summary>
    /// Обозначение объекта контроля (обязательно, до 39 символов).
    /// </summary>
    public string ObjectCode { get; set; } = string.Empty;

    /// <summary>
    /// Наименование объекта контроля (до 39 символов). Может отсутствовать.
    /// </summary>
    public string? ObjectName { get; set; }

    /// <summary>
    /// Словарь параметров. Ключ — идентификатор параметра (ПРИМ/ПРИМЕЧ/ПРИМЕЧАНИЕ = "ПРИМ").
    /// </summary>
    public Dictionary<string, List<string>> Parameters { get; set; } = new();
  }
}
