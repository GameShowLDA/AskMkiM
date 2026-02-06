using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Engine.ControlCommandExecutor.Execution
{
  /// <summary>
  /// Менеджер точек останова команд.
  /// </summary>
  internal sealed class BreakpointManager
  {
    /// <summary>
    /// Коллекция команд управляющей программы, в которой производится установка и снятие точек останова.
    /// </summary>
    private readonly CommandCollection _commands;

    public BreakpointManager(CommandCollection commands)
    {
      _commands = commands;

      EventAggregator.Subscribe<BreakpointEvents.BreakpointSet>(OnSet);
      EventAggregator.Subscribe<BreakpointEvents.BreakpointRemoved>(OnRemoved);
    }

    /// <summary>
    /// Обрабатывает событие установки точки останова.
    /// Устанавливает флаг точки останова у соответствующей команды в коллекции.
    /// </summary>
    /// <param name="e">
    /// Событие установки точки останова, содержащее номер команды.
    /// </param>
    private void OnSet(BreakpointEvents.BreakpointSet e)
    {
      var cmd = _commands.FindByNumber(e.CommandNumber);
      if (cmd != null)
        cmd.HasBreakpoint = true;
    }

    /// <summary>
    /// Обрабатывает событие удаления точки останова.
    /// Снимает флаг точки останова у соответствующей команды в коллекции.
    /// </summary>
    /// <param name="e">
    /// Событие удаления точки останова, содержащее номер команды.
    /// </param>
    private void OnRemoved(BreakpointEvents.BreakpointRemoved e)
    {
      var cmd = _commands.FindByNumber(e.CommandNumber);
      if (cmd != null)
        cmd.HasBreakpoint = false;
    }
  }
}
