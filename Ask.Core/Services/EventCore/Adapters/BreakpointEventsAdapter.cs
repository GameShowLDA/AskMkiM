using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;

namespace Ask.Core.Services.EventCore.Adapters
{
  /// <summary>
  /// Адаптер для генерации событий <see cref="BreakpointEvents"/>,
  /// обеспечивающий единый стиль вызова публикации событий.
  /// </summary>
  public static class BreakpointEventAdapter
  {
    /// <summary>
    /// Генерирует событие установки точки останова на строку.
    /// </summary>
    /// <param name="lineNumber">На какой строке находится команда.</param>
    /// <param name="commandNumber">Какую метку имеет команда.</param>
    public static void RaiseBreakpointSet(int lineNumber, int commandNumber) =>
      EventAggregator.Publish(new BreakpointEvents.BreakpointSet(lineNumber, commandNumber));

    /// <summary>
    /// Генерирует событие снятия точки останова со строки.
    /// </summary>
    /// <param name="lineNumber">На какой строке находится команда.</param>
    /// <param name="commandNumber">Какую метку имеет команда.</param>
    public static void RaiseBreakpointRemoved(int lineNumber, int commandNumber) =>
      EventAggregator.Publish(new BreakpointEvents.BreakpointRemoved(lineNumber, commandNumber));

    /// <summary>
    /// Генерирует событие включения точки останова на строке.
    /// </summary>
    /// <param name="lineNumber">На какой строке находится команда.</param>
    /// <param name="commandNumber">Какую метку имеет команда.</param>
    public static void RaiseBreakpointOn(int lineNumber, int commandNumber) =>
      EventAggregator.Publish(new BreakpointEvents.BreakpointOn(lineNumber, commandNumber));

    /// <summary>
    /// Генерирует событие выключения точки останова на строке.
    /// </summary>
    /// <param name="lineNumber">На какой строке находится команда.</param>
    /// <param name="commandNumber">Какую метку имеет команда.</param>
    public static void RaiseBreakpointOff(int lineNumber, int commandNumber) =>
      EventAggregator.Publish(new BreakpointEvents.BreakpointOff(lineNumber, commandNumber));
  }
}