using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Execution;
using Ask.Engine.ControlCommandExecutor.Executors.Interface;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  internal class KscCommandExecutor : ICommandExecutor
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

      var command = context.Command as KscCommandModel;
      context.TranslationControl.SetActiveLine(command.FormattedStartLineNumber);

      if (!await ExecutionConfig.GetIsIdleModeEnabled())
      {
        await NewCore.Communication.DeviceCommandSender.ResetAllSystem();
      }

      await GetProtocol(context, command, protocolModel);
    }

    private async Task GetProtocol(CommandExecutionContext context, KscCommandModel command, ProtocolModel protocolModel)
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

      if (await ProtocolConfig.GetGenerateProtocol())
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
      protocolModel.Mode = await ExecutionConfig.GetIsIdleModeEnabled() ? "Холостой режим" : "Рабочий режим";
      // TODO: формирование протокола с ошибкой
      //ProtocolModel.GetPathProtocol(protocolModel); 
      FileInteractionEventAdapter.RaiseViewProtocol(protocolModel);
      EventAggregator.Unsubscribe<FileInteractionEvents.ProtocolInfoClose>(OnProtocolClose);
    }
  }
}
