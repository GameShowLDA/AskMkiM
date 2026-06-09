using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Formatter.Base
{
  /// <summary>
  /// Интерфейс форматтера команды: превращает модель в строки для вывода.
  /// </summary>
  public interface ICommandFormatter
  {
    /// <summary>
    /// Проверяет, подходит ли форматтер для данной модели (по типу/мнемонике).
    /// </summary>
    bool CanFormat(BaseCommandModel model);

    /// <summary>
    /// Возвращает строки для вывода по данной модели (развёрнутые/человеко-понятные).
    /// </summary>
    IEnumerable<string> Format(BaseCommandModel model);
  }
}
