using Ask.Core.Services.App;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Engine.ControlCommandAnalyser.Model;
using static Ask.Core.Services.EventCore.Events.ExecutionEvents;
using static Ask.Core.Services.EventCore.Events.Message;

namespace Ask.Engine.ControlCommandExecutor.Execution
{
  /// <summary>
  /// —ервис обработки точек останова.
  /// ¬ыполн€ет необходимые действи€ при достижении команды,
  /// у которой установлен флаг HasBreakpoint.
  /// </summary>
  internal class BreakpointHandler
  {
    /// <summary>
    /// ќсновной метод обработки точки останова.
    /// ¬ыполн€етс€ тогда, когда логика анализа дошла до команды,
    /// содержащей установленную точку останова.
    /// </summary>
    /// <param name="command">ћодель команды, дл€ которой требуетс€ обработка точки останова.</param>
    static public void Handle(BaseCommandModel command, IUserInteractionService userInteractionService)
    {
      if (!command.HasBreakpoint)
        return;

      OnBreakpointHit(command);
    }

    /// <summary>
    /// ¬ызываетс€ при срабатывании точки останова.
    /// ћожно подписатьс€, логировать или остановить выполнение.
    /// </summary>
    private static void OnBreakpointHit(BaseCommandModel command)
    {
      StepControlManager.EnableStepModeByBreakpoint(command, true);
    }
  }
}
