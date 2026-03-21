using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Device.Communication.Ethernet.Udp;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Execution;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  /// <summary>
  /// Исполнитель команды "ОК".
  /// </summary>
  internal class OkCommandExecutor : CommandExecutorBase, ICommandExecutor
  {
    public string Mnemonic => EnumExtensions.GetDisplayOrganizationalInfo(OrganizationalComands.OK).DisplayName;

    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      await DeviceCommandSender.ResetAllSystem();
      context.CommandExecutionManager.ClearErrorsMethod();

      var command = GetRequiredCommand<OkCommandModel>(context);
      SetActiveLine(context, command);

      command.ProtocolModel = new ProtocolModel();
      command.ProtocolModel.ProgramPath = command.ObjectName;

      await context.Console.ShowMessageAsync(new ShowMessageModel($"Выполнение программы контроля для \"{command.ObjectName}({command.ObjectCode})\"", type: ShowMessageModel.MessageType.Command)
      {
        IsControlProgramCommandHeader = true
      }, IsBlockStart: true);
    }
  }
}
