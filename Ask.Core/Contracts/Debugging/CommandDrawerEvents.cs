using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.EventInterfaces;

namespace Ask.Core.Contracts.Debugging
{
  /// <summary>
  /// Запрос на открытие Drawer выбора команды в UI.
  /// </summary>
  public sealed class OpenCommandDrawerRequest : IEvent
  {
    public Guid RequestId { get; }
    public IReadOnlyList<BaseCommandModel> Commands { get; }
    public BaseCommandModel BreakpointCommand { get; }

    public OpenCommandDrawerRequest(Guid requestId, IReadOnlyList<BaseCommandModel> commands, BaseCommandModel breakpointCommand)
    {
      RequestId = requestId;
      Commands = commands;
      BreakpointCommand = breakpointCommand;
    }
  }

  /// <summary>
  /// Результат работы Drawer выбора команды.
  /// </summary>
  public sealed class CommandDrawerResult : IEvent
  {
    public Guid RequestId { get; }
    public BaseCommandModel? SelectedCommand { get; }

    public CommandDrawerResult(Guid requestId, BaseCommandModel? selectedCommand)
    {
      RequestId = requestId;
      SelectedCommand = selectedCommand;
    }
  }
}
