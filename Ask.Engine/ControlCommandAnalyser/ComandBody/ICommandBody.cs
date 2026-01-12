using Ask.Engine.ControlCommandAnalyser.Model;
using System.Text;

namespace Ask.Engine.ControlCommandAnalyser.ComandBody
{
  /// <summary>
  /// Интерфейс парсера для команды.
  /// </summary>
  public interface ICommandBody
  {
    /// <summary>
    /// Проверяет, подходит ли форматтер для данной модели (по типу/мнемонике).
    /// </summary>
    bool CanCreate(BaseCommandModel model);

    /// <summary>
    /// Собирает тело команды.
    /// </summary>
    StringBuilder Create(BaseCommandModel model, StringBuilder newSourseLines);
  }
}
