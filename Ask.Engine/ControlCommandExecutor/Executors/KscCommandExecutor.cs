using Ask.Core.Services.App;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;
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
      StepControlManager.EnableStepMode(false);
      EventAggregator.Subscribe<FileInteractionEvents.ProtocolInfoClose>(OnProtocolClose);

      var command = GetRequiredCommand<KscCommandModel>(context);
      SetActiveLine(context, command);

      var nameCommand = $"{command.CommandNumber} {command.Mnemonic}";
      var message = BuildSourceLinesMessage(command);


      var relayModules = EquipmentService.ValidRelayModules;
      var switchingDevice = EquipmentService.ValidSwitchingDevice;
      var unique = context.GetUniqueMeasurementDevices();

      foreach (var item in relayModules)
      {
        await item.ConnectableManager.ResetAsync(context.Console);
      }

      if (switchingDevice != null)
      {
        await switchingDevice.ConnectableManager.ResetAsync(context.Console);
      }

      if (unique.Contains(MeasurementDevice.Multimeter))
      {
        var meter = EquipmentService.GetFastMeterOrThrow(context.Console);
        await meter.ConnectableManager.ResetAsync(context.Console);
      }

      if (unique.Contains(MeasurementDevice.BreakdownTester))
      {
        var breakDown = await EquipmentService.GetBreakdownTesterOrThrow(context.Console);
        await breakDown.ConnectableManager.ResetAsync(context.Console);
      }


      GetProtocol(context, command, protocolModel);
      EventAggregator.Unsubscribe<FileInteractionEvents.ProtocolInfoClose>(OnProtocolClose);
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
      FileInteractionEventAdapter.RaiseViewProtocol(protocolModel);
      EventAggregator.Unsubscribe<FileInteractionEvents.ProtocolInfoClose>(OnProtocolClose);
    }
  }
}
