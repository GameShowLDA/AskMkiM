using Ask.Core.Shared.DTO.Executor;

namespace Ask.UI.Features.ExecutionSelection.ViewModels
{
  public sealed class CommandPreviewViewModel
  {
    public BaseCommandModel Command { get; }
    public string Name { get; }
    public string FullCommandText { get; }

    public CommandPreviewViewModel(BaseCommandModel command)
    {
      Command = command;
      Name = $"{command.CommandNumber} {command.Mnemonic}".Trim();
      FullCommandText = string.IsNullOrWhiteSpace(command.CommandBody) ? string.Empty : command.CommandBody;
    }
  }
}
