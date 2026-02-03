using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Pr;
using Ask.Engine.ControlCommandExecutor.Execution;
using Ask.Engine.ControlCommandExecutor.Executors.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  internal class VshCommandExecutor : ICommandExecutor
  {
    public string Mnemonic => EnumExtensions.GetDisplayOrganizationalInfo(OrganizationalComands.VSH).DisplayName;

    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      var command = context.Command as VshCommandModel;

      string nameCommand = $"{command.CommandNumber} {command.Mnemonic}";
      string message = string.Empty;

      foreach (var str in command.SourceLines)
      {
        message += "\r\n  " + str;
      }

      await context.Console.ShowMessageAsync(ExecutorMessageBuilder.BuildCommandExecutionMessage(nameCommand, message), IsBlockStart: true);

      await ConnectBlocksToBus(context.Console);
    }

    public async Task ConnectBlocksToBus(IUserInteractionService userInteractionService)
    {
      var rsm = EquipmentService.ValidRelayModules;
      foreach (var module in rsm)
      {
        var busses = await GetBusses(module);
        await module.BusManager.ConnectBusAsync(busses.bus1, userInteractionService);
        await module.BusManager.ConnectBusAsync(busses.bus2, userInteractionService);
      }
    }

    private async Task<(SwitchingBus bus1, SwitchingBus bus2)> GetBusses(IRelaySwitchModule relaySwitch)
    {
      var busStruct = relaySwitch.BusType;
      switch (busStruct)
      {
        case SwitchingBusNew.AB1:
          return (SwitchingBus.A1, SwitchingBus.B1);
        case SwitchingBusNew.AB2:
          return (SwitchingBus.A2, SwitchingBus.B2);
        case SwitchingBusNew.AB3:
          return (SwitchingBus.A3, SwitchingBus.B3);
        case SwitchingBusNew.AB4:
          return (SwitchingBus.A4, SwitchingBus.B4);
      }

      throw new NotImplementedException();
    }
  }
}
