using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Execution;
using Ask.Engine.ControlCommandExecutor.Executors.Interface;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  internal class CkCommandExecutor : CommandExecutorBase, ICommandExecutor
  {
    public string Mnemonic => EnumExtensions.GetDisplayOrganizationalInfo(OrganizationalComands.CK).DisplayName;

    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      var command = GetRequiredCommand<CkCommandModel>(context);
      string nameCommand = $"{command.CommandNumber} {command.Mnemonic}";
      var message = BuildSourceLinesMessage(command);
      SetActiveLine(context, command);

      BreakpointHandler.Handle(command, context.Console);
      await context.Console.ShowMessageAsync(ExecutorMessageBuilder.BuildCommandExecutionMessage(nameCommand, message), IsBlockStart: true);

      var relayModules = EquipmentService.ValidRelayModules;
      foreach (var realysModule in relayModules)
      {
        BusConverter.TrySplitAbBus(realysModule.BusType, out SwitchingBus busA, out SwitchingBus busB);
        if (command.BusList.Contains(busA))
        {
          await realysModule.PointManager.DisconnectingAllPointFromBusA(context.Console);
        }
        else if (command.BusList.Contains(busB))
        {
          await realysModule.PointManager.DisconnectingAllPointFromBusB(context.Console);
        }
      }
    }
  }
}
