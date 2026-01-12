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
  public class UpCommandExecutor : ICommandExecutor
  {
    public string Mnemonic => EnumExtensions.GetDisplayOrganizationalInfo(OrganizationalComands.UP).DisplayName;


    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      var up = (UpCommandModel)context.Command;

      if (CommandExecutionState.LastCuResult == MessageBoxResult.No)
      {
        context.JumpToCommandNumber?.Invoke(up.TargetLabel);
      }

      CommandExecutionState.LastCuResult = MessageBoxResult.None;
    }
  }
}
