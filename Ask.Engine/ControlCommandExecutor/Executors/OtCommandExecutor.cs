using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Execution;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  internal class OtCommandExecutor : CommandExecutorBase, ICommandExecutor
  {
    public string Mnemonic => EnumExtensions.GetDisplayOrganizationalInfo(OrganizationalComands.OT).DisplayName;

    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {
      var command = GetRequiredCommand<OtCommandModel>(context);
      SetActiveLine(context, command);

      var nameCommand = $"{command.CommandNumber} {command.Mnemonic}";
      var message = BuildSourceLinesMessage(command);

      await context.Console.ShowMessageAsync(ExecutorMessageBuilder.BuildCommandExecutionMessage(nameCommand, message), IsBlockStart: true);

      foreach (var item in command.BusPointsDictionary.Keys)
      {
        await DisconnectPointsAsync(item, command.BusPointsDictionary[item], context.Console);
      }

      if (command.Time > 0)
      {
        await DelayAsync(command.Time, context.Console);

        foreach (var item in command.BusPointsDictionary.Keys)
        {
          await ConnectPointsAsync(item, command.BusPointsDictionary[item], context.Console);
        }
      }
    }

    private async Task ConnectPointsAsync(SwitchingBus bus, List<PointModel> pointModels, IUserInteractionService interactionService)
    {
      var uniqueModules = pointModels.Select(p => (p.DeviceNumber, p.ModuleNumber)).Distinct().ToList();

      foreach (var item in uniqueModules)
      {
        var module = GetModuleOrThrow(new PointModel
        {
          DeviceNumber = item.DeviceNumber,
          ModuleNumber = item.ModuleNumber,
          PointNumber = 0
        });
        await module.BusManager.ConnectBusAsync(bus);
      }

      foreach (var item in pointModels)
      {
        var pointBus = bus.ToString().StartsWith("A") ? BusPoint.A : BusPoint.B;
        var module = GetModuleOrThrow(item);
        await module.PointManager.ConnectRelayAsync(pointBus, item.PointNumber, interactionService);
      }
    }

    private async Task DisconnectPointsAsync(SwitchingBus bus, List<PointModel> pointModels, IUserInteractionService interactionService)
    {
      var uniqueModules = pointModels.Select(p => (p.DeviceNumber, p.ModuleNumber)).Distinct().ToList();

      foreach (var item in pointModels)
      {
        var pointBus = bus.ToString().StartsWith("A") ? BusPoint.A : BusPoint.B;
        var module = GetModuleOrThrow(item);
        await module.PointManager.DisconnectRelayAsync(pointBus, item.PointNumber, interactionService);
      }

      foreach (var item in uniqueModules)
      {
        var module = GetModuleOrThrow(new PointModel
        {
          DeviceNumber = item.DeviceNumber,
          ModuleNumber = item.ModuleNumber,
          PointNumber = 0
        });
        await module.BusManager.DisconnectBusAsync(bus);
      }
    }

    private static Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.IRelaySwitchModule GetModuleOrThrow(PointModel point)
    {
      return EquipmentService.GetModuleByPoint(point)
        ?? throw new InvalidOperationException(
          $"Модуль коммутации не найден для [{point.DeviceNumber}.{point.ModuleNumber}]. " +
          "Проверьте выполнение РМ и валидацию точек.");
    }

    private async Task DelayAsync(double? time, IUserInteractionService interactionService)
    {
      await interactionService.ShowMessageAsync(new ShowMessageModel("Задержка перед включением", message: $"{time}сек.") { IndentLevel = 2 });
      var delay = Convert.ToInt32(time * 1000);
      await Task.Delay(delay);
    }
  }
}
