using Ask.Core.Contracts.Debugging;
using Ask.Core.Services.App;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.HotkeysEnums;
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
        return command;
	
      if (!command.IsBreakpointEnabled)
        return command;

      await ShowBreakpointCommandHeaderAsync(command, userInteractionService).ConfigureAwait(false);
      StepControlManager.EnableStepModeByBreakpoint(command, true);
      var cancellationToken = userInteractionService.GetCancellationToken();
      var shouldOpenDrawer = await WaitForBreakpointActionAsync(command, cancellationToken).ConfigureAwait(false);
      if (!shouldOpenDrawer)
      {
        return command;
      }

      var selected = await OpenDrawerAndWaitSelectionAsync(command, commands, cancellationToken).ConfigureAwait(false);

      // Drawer closed via F4 without selection: stay on current command and continue in normal flow.
      return selected ?? command;
    }

    private static Task ShowBreakpointCommandHeaderAsync(BaseCommandModel command, IUserInteractionService userInteractionService)
    {
      var commandName = $"{command.CommandNumber} {command.Mnemonic}".Trim();
      var commandBody = string.IsNullOrWhiteSpace(command.CommandBody) ? "<пусто>" : command.CommandBody;

      var header = new ShowMessageModel(
        header: $"\r\nСработала точка останова на команде {commandName}",
        headerColor: ShowMessageModel.SuccessMessage.TitleColor,
        message: $"{commandBody}",
        type: ShowMessageModel.MessageType.Command)
        {
          IndentLevel = 1
        };

      return userInteractionService.ShowMessageAsync(
        header,
        IsBlockStart: true,
        SkipStepModeCheck: true,
        skipPause: true);
    }

    private static async Task<bool> WaitForBreakpointActionAsync(BaseCommandModel command, CancellationToken cancellationToken)
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

      Action<ControlButtonPressed>? onControlPressed = null;
      onControlPressed = e =>
      {
        if (e.Button == ExecutionControlButton.Run)
        {
          StepControlManager.DisableStepMode();
          tcs.TrySetResult(false);
          return;
        }

        if (e.Button is ExecutionControlButton.StepOver or ExecutionControlButton.StepInto)
        {
          if (e.Button == ExecutionControlButton.StepOver)
          {
            StepControlManager.RequestStepOverUntilNextControlCommand();
          }
          else
          {
            StepControlManager.SetStepIntoMode();
          }

          tcs.TrySetResult(false);
        }
      };

      EventAggregator.Subscribe(onF4Pressed);
      EventAggregator.Subscribe(onControlPressed);

      try
      {
        using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken)))
        {
          return await tcs.Task.ConfigureAwait(false);
        }
      }
      finally
      {
        EventAggregator.Unsubscribe(onF4Pressed);
        EventAggregator.Unsubscribe(onControlPressed);
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
