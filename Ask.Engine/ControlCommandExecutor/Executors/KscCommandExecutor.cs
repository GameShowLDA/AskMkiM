using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Execution;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  internal class KscCommandExecutor : CommandExecutorBase, ICommandExecutor
  {
    public string Mnemonic => EnumExtensions.GetDisplayOrganizationalInfo(OrganizationalComands.KSC).DisplayName;

    private void OnProtocolClose(FileInteractionEvents.ProtocolInfoClose e)
    {
      OnProtocolInfoClosing(e.Number, e.Executor, e.Agent, e.Customer, e.Protocol);
    }

    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {

      EventAggregator.Unsubscribe<FileInteractionEvents.ProtocolInfoClose>(OnProtocolClose);
      EventAggregator.Subscribe<FileInteractionEvents.ProtocolInfoClose>(OnProtocolClose);

      var command = GetRequiredCommand<KscCommandModel>(context);
      var nameCommand = $"{command.CommandNumber} {command.Mnemonic}";
      var message = BuildSourceLinesMessage(command);
      SetActiveLine(context, command);


      if (!ExecutionConfig.GetIsIdleModeEnabled())
      {
        await NewCore.Communication.DeviceCommandSender.ResetAllSystem();
      }

      GetProtocol(context, command, protocolModel);
    }

    private void GetProtocol(CommandExecutionContext context, KscCommandModel command, ProtocolModel protocolModel)
    {
      protocolModel.Designation = command.OkCommandModel.ObjectCode;
      protocolModel.ControlObjectName = command.OkCommandModel.ControlObjectName;
      protocolModel.Date = DateTime.Now.Date;
      protocolModel.EndTime = DateTime.Now;

      var opkPath = context.OpkFilePath;
      protocolModel.ProgramPath = string.IsNullOrWhiteSpace(opkPath)
          ? string.Empty
          : opkPath;
      protocolModel.ProgramName = string.IsNullOrWhiteSpace(opkPath)
          ? "Название программы контроля"
          : Path.GetFileName(opkPath);

      if (ProtocolConfig.GetGenerateProtocol())
      {
        FileInteractionEventAdapter.RaiseGetProtocolInfo(protocolModel);
      }
    }

    private async void OnProtocolInfoClosing(string number, string executor, string agent, string customer, ProtocolModel protocolModel)
    {
      protocolModel.Number = number;
      protocolModel.Executor = executor;
      protocolModel.Agent = agent;
      protocolModel.Customer = customer;
      protocolModel.Mode = ExecutionConfig.GetIsIdleModeEnabled() ? "Холостой режим" : "Рабочий режим";
      // TODO: формирование протокола с ошибкой
      //ProtocolModel.GetPathProtocol(protocolModel); 
      FileInteractionEventAdapter.RaiseViewProtocol(protocolModel);
      EventAggregator.Unsubscribe<FileInteractionEvents.ProtocolInfoClose>(OnProtocolClose);
    }
  }
}
