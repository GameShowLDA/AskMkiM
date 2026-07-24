using Ask.Core.Contracts.Debugging;
using Ask.Core.Services.Devices;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Engine.ControlCommandExecutor.Execution;

/// <summary>
/// Выбирает целевую команду и подготавливает оборудование к переходу.
/// </summary>
internal static class CommandJumpService
{
  /// <summary>
  /// Открывает панель выбора команды и ожидает результат пользователя.
  /// </summary>
  /// <param name="currentCommand">Текущая команда.</param>
  /// <param name="commands">Команды программы контроля.</param>
  /// <param name="cancellationToken">Токен отмены ожидания.</param>
  /// <returns>Выбранная команда или <see langword="null"/>, если выбор отменён.</returns>
  public static async Task<BaseCommandModel?> SelectAsync(
    BaseCommandModel currentCommand,
    IReadOnlyList<BaseCommandModel> commands,
    CancellationToken cancellationToken)
  {
    var requestId = Guid.NewGuid();
    var completionSource =
      new TaskCompletionSource<BaseCommandModel?>(TaskCreationOptions.RunContinuationsAsynchronously);

    Action<CommandDrawerResult>? resultHandler = null;
    resultHandler = result =>
    {
      if (result.RequestId == requestId)
      {
        completionSource.TrySetResult(result.SelectedCommand);
      }
    };

    EventAggregator.Subscribe(resultHandler);

    try
    {
      CommandDrawerEventAdapter.RaiseOpenRequest(requestId, commands, currentCommand);
      using (cancellationToken.Register(() => completionSource.TrySetCanceled(cancellationToken)))
      {
        return await completionSource.Task.ConfigureAwait(false);
      }
    }
    catch (TaskCanceledException)
    {
      return null;
    }
    finally
    {
      EventAggregator.Unsubscribe(resultHandler);
    }
  }

  /// <summary>
  /// Выводит сообщение о переходе и сбрасывает оборудование программы контроля.
  /// </summary>
  /// <param name="targetCommand">Целевая команда.</param>
  /// <param name="interactionService">Сервис взаимодействия с пользователем.</param>
  public static async Task PrepareAsync(
    BaseCommandModel targetCommand,
    IUserInteractionService interactionService)
  {
    var commandName = $"{targetCommand.CommandNumber} {targetCommand.Mnemonic}".Trim();
    var commandBody = string.IsNullOrWhiteSpace(targetCommand.CommandBody)
      ? "<пусто>"
      : targetCommand.CommandBody;

    await interactionService.ShowMessageAsync(
      new ShowMessageModel(
        header: $"\r\nПереход к команде {commandName}",
        message: commandBody,
        type: ShowMessageModel.MessageType.Command)
      {
        IndentLevel = 1
      },
      IsBlockStart: true,
      SkipStepModeCheck: true,
      skipPause: true).ConfigureAwait(false);

    await DeviceResetService.ResetDevicesAsync(
      EquipmentService.GetAllDevices(),
      interactionService);
  }
}
