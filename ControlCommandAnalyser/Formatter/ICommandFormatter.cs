using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Model;

namespace ControlCommandAnalyser.Formatter
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
