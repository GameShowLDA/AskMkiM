using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Execution;
using Ask.Engine.ControlCommandExecutor.Executors.Interface;
using DataBaseConfiguration.Services.Device;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  internal class OtCommandExecutor : ICommandExecutor
  {
    public string Mnemonic => EnumExtensions.GetDisplayOrganizationalInfo(OrganizationalComands.OT).DisplayName;

    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      var command = context.Command as OtCommandModel;
      context.TranslationControl.SetActiveLine(command.FormattedStartLineNumber);

      string nameCommand = $"{command.CommandNumber} {command.Mnemonic}";

      string message = string.Empty;

      foreach (var str in command.SourceLines)
      {
        message += "\r\n  " + str;
      }

      BreakpointHandler.Handle(command, context.Console);
      await context.Console.ShowMessageAsync(ExecutorMessageBuilder.BuildCommandExecutionMessage(nameCommand, message), IsBlockStart: true);

      foreach (var item in command.BusPointsDictionary.Keys)
      {
        await DisconnectedPoint(item, command.BusPointsDictionary[item], context.Console);
      }

      if (command.Time > 0)
      {
        await Delay(command.Time, context.Console);

        foreach (var item in command.BusPointsDictionary.Keys)
        {
          await ConnectedPoint(item, command.BusPointsDictionary[item], context.Console);
        }
      }
    }

    private async Task ConnectedPoint(SwitchingBus bus, List<PointModel> pointModels, IUserInteractionService interactionService)
    {
      var uniqueModules = pointModels.Select(p => (p.DeviceNumber, p.ModuleNumber)).Distinct().ToList();
      foreach (var item in uniqueModules)
      {
        var mkr = new RelaySwitchModuleServices().GetDevicesByNumberChassis(item.DeviceNumber).Where(x => x.Number == item.ModuleNumber).FirstOrDefault();
        await mkr.BusManager.ConnectBusAsync(bus);
      }

      foreach (var item in pointModels)
      {
        var pointBus = bus.ToString().StartsWith("A") ? BusPoint.A : BusPoint.B;
        var mkr = new RelaySwitchModuleServices().GetDevicesByNumberChassis(item.DeviceNumber).Where(x => x.Number == item.ModuleNumber).FirstOrDefault();
        await mkr.PointManager.ConnectRelayAsync(pointBus, item.PointNumber, interactionService);
      }
    }

    private async Task DisconnectedPoint(SwitchingBus bus, List<PointModel> pointModels, IUserInteractionService interactionService)
    {
      var uniqueModules = pointModels.Select(p => (p.DeviceNumber, p.ModuleNumber)).Distinct().ToList();

      foreach (var item in pointModels)
      {
        var pointBus = bus.ToString().StartsWith("A") ? BusPoint.A : BusPoint.B;
        var mkr = new RelaySwitchModuleServices().GetDevicesByNumberChassis(item.DeviceNumber).Where(x => x.Number == item.ModuleNumber).FirstOrDefault();
        await mkr.PointManager.DisconnectRelayAsync(pointBus, item.PointNumber, interactionService);
      }

      foreach (var item in uniqueModules)
      {
        var mkr = new RelaySwitchModuleServices().GetDevicesByNumberChassis(item.DeviceNumber).Where(x => x.Number == item.ModuleNumber).FirstOrDefault();
        await mkr.BusManager.DisconnectBusAsync(bus);
      }
    }

    private async Task Delay(double? time, IUserInteractionService interactionService)
    {
      await interactionService.ShowMessageAsync(new ShowMessageModel("Задержка перед включением", message: $"{time}сек.") { IndentLevel = 2 });
      time *= 1000;
      await Task.Delay(Convert.ToInt32(time));
    }
  }
}
