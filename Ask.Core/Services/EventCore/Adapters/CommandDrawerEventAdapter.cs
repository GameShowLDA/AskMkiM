using Ask.Core.Contracts.Debugging;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Executor;

namespace Ask.Core.Services.EventCore.Adapters
{
  /// <summary>
  /// Адаптер публикации событий Drawer выбора команды.
  /// </summary>
  public static class CommandDrawerEventAdapter
  {
    public static void RaiseOpenRequest(Guid requestId, IReadOnlyList<BaseCommandModel> commands, BaseCommandModel breakpointCommand)
      => EventAggregator.Publish(new OpenCommandDrawerRequest(requestId, commands, breakpointCommand));

    public static void RaiseResult(Guid requestId, BaseCommandModel? selectedCommand)
      => EventAggregator.Publish(new CommandDrawerResult(requestId, selectedCommand));
  }
}
