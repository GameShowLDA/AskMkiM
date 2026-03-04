using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Execution;
using System.Windows;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  internal class UpCommandExecutor : CommandExecutorBase, ICommandExecutor
  {
    public string Mnemonic => EnumExtensions.GetDisplayOrganizationalInfo(OrganizationalComands.UP).DisplayName;

    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      var command = GetRequiredCommand<UpCommandModel>(context);
      SetActiveLine(context, command);

      var nameCommand = $"{command.CommandNumber} {command.Mnemonic}";
      var message = BuildSourceLinesMessage(command);

      await context.Console.ShowMessageAsync(ExecutorMessageBuilder.BuildCommandExecutionMessage(nameCommand, message), IsBlockStart: true);

      if (CommandExecutionState.LastRejectFlag)
      {
        context.JumpToCommandNumber?.Invoke(command.TargetLabel);
      }

      CommandExecutionState.LastCuResult = MessageBoxResult.None;
      CommandExecutionState.LastRejectFlag = false;
    }
  }
}
