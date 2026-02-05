using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Execution;
using Ask.Engine.ControlCommandExecutor.Executors.Interface;
using System.Windows;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  internal class UpCommandExecutor : CommandExecutorBase, ICommandExecutor
  {
    public string Mnemonic => EnumExtensions.GetDisplayOrganizationalInfo(OrganizationalComands.UP).DisplayName;

    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      var command = GetRequiredCommand<UpCommandModel>(context);

      if (CommandExecutionState.LastCuResult == MessageBoxResult.No)
      {
        context.JumpToCommandNumber?.Invoke(command.TargetLabel);
      }

      CommandExecutionState.LastCuResult = MessageBoxResult.None;
    }
  }
}
