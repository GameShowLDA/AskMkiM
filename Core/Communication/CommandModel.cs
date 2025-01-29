using System.Windows.Controls;

namespace Core.Communication
{
  /// <summary>
  /// Represents a command model with associated properties.
  /// </summary>
  public class CommandModel
  {
    /// <summary>
    /// Получает или устанавливает основной элемент управления панели, связанный с командой.
    /// </summary>
    public Control MainPanel { get; set; }

    /// <summary>
    /// Получает или устанавливает имя команды.
    /// </summary>
    public string Name { get; set; }
  }
}
