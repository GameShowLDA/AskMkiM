using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Execution;
using Ask.Engine.ControlCommandExecutor.Executors.Interface;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  /// <summary>
  /// Исполнитель команды "ОК".
  /// </summary>
  public class OkCommandExecutor : ICommandExecutor
  {
    public string Mnemonic => EnumExtensions.GetDisplayOrganizationalInfo(OrganizationalComands.OK).DisplayName;

    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      if (!await ExecutionConfig.GetIsIdleModeEnabled())
      {
        await NewCore.Communication.DeviceCommandSender.ResetAllSystem();
      }
      
      context.CommandExecutionManager.ClearErrorsMethod();

      var command = context.Command as OkCommandModel;
      context.TranslationControl.SetActiveLine(command.FormattedStartLineNumber);
      command.ProtocolModel = new ProtocolModel();
      command.ProtocolModel.ProgramPath = command.ObjectName;

      await context.Console.ShowMessageAsync(new ShowMessageModel($"Выполнение программы контроля для \"{command.ObjectName}({command.ObjectCode})\"", type: ShowMessageModel.MessageType.Command), IsBlockStart: true);
    }
  }
}
