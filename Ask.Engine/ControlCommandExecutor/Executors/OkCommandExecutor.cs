using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Execution;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  /// <summary>
  /// Исполнитель команды "ОК".
  /// </summary>
  internal class OkCommandExecutor : CommandExecutorBase, ICommandExecutor
  {
    public string Mnemonic => EnumExtensions.GetCommandOrganizationalInfo(OrganizationalComands.OK).DisplayName;

    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      EquipmentService.ClearUsedDevices();
      context.CommandExecutionManager.ClearErrorsMethod();

      var command = GetRequiredCommand<OkCommandModel>(context);
      SetActiveLine(context, command);

      command.ProtocolModel = new ProtocolModel();
      command.ProtocolModel.ProgramPath = command.ObjectName;

      await CommandMessages.PublishControlProgramStartAsync(context.Console, command.ObjectName, command.ObjectCode);
    }
  }
}
