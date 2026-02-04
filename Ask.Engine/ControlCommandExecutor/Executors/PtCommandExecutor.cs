using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Execution;
using Ask.Engine.ControlCommandExecutor.Executors.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  internal class PtCommandExecutor : ICommandExecutor
  {
    public string Mnemonic => EnumExtensions.GetDisplayOrganizationalInfo(OrganizationalComands.PT).DisplayName;

    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      var command = context.Command as PtCommandModel;
      context.TranslationControl.SetActiveLine(command.FormattedStartLineNumber);

      string nameCommand = $"{command.CommandNumber} {command.Mnemonic}";

      string message = string.Empty;

      foreach (var str in command.SourceLines)
      {
        message += "\r\n  " + str;
      }


      BreakpointHandler.Handle(command, context.Console);
      await context.Console.ShowMessageAsync(ExecutorMessageBuilder.BuildCommandExecutionMessage(nameCommand, message), IsBlockStart: true);

      var messgaeService = context.Console;

      foreach (var key in command.BusPointsDictionary.Keys)
      {
        await messgaeService.ShowMessageAsync(new ShowMessageModel($"Шина {key}"));

        foreach (var item in command.BusPointsDictionary[key])
        {
          await messgaeService.ShowMessageAsync(new ShowMessageModel($"Точка {item}"));
        }
      }

      // await ConnectedPoint(command.BusPointsDictionary);
      // await Delay(command.Time);
      // await DisconnectedPoint();
    }

    private async Task ConnectedPoint(SwitchingBus bus, List<PointModel> pointModels)
    { 
    
    }

    private async Task DisconnectedPoint(SwitchingBus bus, List<PointModel> pointModels)
    {

    }

    private async Task Delay(double? time)
    {
      if (time > 0)
      {
        time *= 1000;
        await Task.Delay(Convert.ToInt32(time));
      }
    }
  }
}
