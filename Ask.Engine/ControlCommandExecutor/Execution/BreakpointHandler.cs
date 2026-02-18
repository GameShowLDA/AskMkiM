using Ask.Core.Contracts.Debugging;
using Ask.Core.Services.App;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Static.Messages;
using static Ask.Core.Services.EventCore.Events.ExecutionEvents;

namespace Ask.Engine.ControlCommandExecutor.Execution
{
  /// <summary>
  /// Сервис обработки точек останова.
  /// Выполняет необходимые действия при достижении команды,
  /// у которой установлен флаг HasBreakpoint.
  /// </summary>
  internal static class BreakpointHandler
  {
    /// <summary>
    /// Основной метод обработки точки останова.
    /// </summary>
    public static async Task<BaseCommandModel?> OnBreakpointHitAsync(
      BaseCommandModel command,
      IReadOnlyList<BaseCommandModel> commands,
      IUserInteractionService userInteractionService)
    {
      if (!command.HasBreakpoint)
      {
        return command;
      }

      await ShowBreakpointCommandHeaderAsync(command, userInteractionService).ConfigureAwait(false);
      StepControlManager.EnableStepModeByBreakpoint(command, true);
      await WaitForBreakpointF4Async(command, userInteractionService.GetCancellationToken()).ConfigureAwait(false);
      return await OpenDrawerAndWaitSelectionAsync(command, commands, userInteractionService.GetCancellationToken()).ConfigureAwait(false);
    }

    private static Task ShowBreakpointCommandHeaderAsync(BaseCommandModel command, IUserInteractionService userInteractionService)
    {
      var commandName = $"{command.CommandNumber} {command.Mnemonic}".Trim();
      var message = string.IsNullOrWhiteSpace(command.CommandBody) ? null : command.CommandBody;
      var header = ExecutorMessageBuilder.BuildCommandExecutionMessage(commandName, message);

      // Header must be visible before we stop on breakpoint waiting for F4.
      return userInteractionService.ShowMessageAsync(
        header,
        IsBlockStart: true,
        SkipStepModeCheck: true,
        skipPause: true);
    }

    private static async Task WaitForBreakpointF4Async(BaseCommandModel command, CancellationToken cancellationToken)
    {
      var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

      Action<BreakpointF4Pressed>? onF4Pressed = null;
      onF4Pressed = e =>
      {
        if (!IsSameCommand(command, e.CommandInfo))
        {
          return;
        }

        tcs.TrySetResult(true);
      };

      EventAggregator.Subscribe(onF4Pressed);

      try
      {
        using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken)))
        {
          await tcs.Task.ConfigureAwait(false);
        }
      }
      finally
      {
        EventAggregator.Unsubscribe(onF4Pressed);
      }
    }

    private static async Task<BaseCommandModel?> OpenDrawerAndWaitSelectionAsync(
      BaseCommandModel breakpointCommand,
      IReadOnlyList<BaseCommandModel> commands,
      CancellationToken cancellationToken)
    {
      var requestId = Guid.NewGuid();
      var tcs = new TaskCompletionSource<BaseCommandModel?>(TaskCreationOptions.RunContinuationsAsynchronously);

      Action<CommandDrawerResult>? onResult = null;
      onResult = e =>
      {
        if (e.RequestId != requestId)
        {
          return;
        }

        tcs.TrySetResult(e.SelectedCommand);
      };

      EventAggregator.Subscribe(onResult);

      try
      {
        CommandDrawerEventAdapter.RaiseOpenRequest(requestId, commands, breakpointCommand);
        using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken)))
        {
          return await tcs.Task.ConfigureAwait(false);
        }
      }
      catch (TaskCanceledException)
      {
        return null;
      }
      finally
      {
        EventAggregator.Unsubscribe(onResult);
      }
    }

    private static bool IsSameCommand(BaseCommandModel source, IExecutionCommandInfo target)
    {
      return string.Equals(source.CommandNumber, target.CommandNumber, StringComparison.Ordinal) &&
             string.Equals(source.Mnemonic, target.Mnemonic, StringComparison.Ordinal) &&
             string.Equals(source.CommandBody, target.CommandBody, StringComparison.Ordinal);
    }
  }
}
