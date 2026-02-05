using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Execution;
using Ask.Engine.ControlCommandExecutor.Executors.Interface;

namespace Ask.Engine.ControlCommandExecutor.Executors
{
  internal class RmCommandExecutor : CommandExecutorBase, ICommandExecutor
  {
    public string Mnemonic => EnumExtensions.GetDisplayOrganizationalInfo(OrganizationalComands.RM).DisplayName;

    public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
    {

      var command = GetRequiredCommand<RmCommandModel>(context);
      SetActiveLine(context, command);
      var message = BuildSourceLinesMessage(command);

      await context.Console.ShowMessageAsync(new ShowMessageModel($"\r\nРабочее место", headerColor: ShowMessageModel.SuccessMessage.TitleColor, message: message, type: ShowMessageModel.MessageType.Command) { IndentLevel = 1 }, IsBlockStart: true);
      var points = command.GetAllDestinationPoints();

      List<PointModel> pointsModel = PointModel.ConvertToPointModels(points);
      await EquipmentService.AnalyzePoints(pointsModel, command.PointsMap, context.Console);

      var unique = context.GetUniqueMeasurementDevices();

      List<IAttachableDevice> devices = new();
      if (EquipmentService.ValidRelayModules != null)
      {
        devices.AddRange(EquipmentService.ValidRelayModules);
      }

      if (EquipmentService.ValidSwitchingDevice != null)
      {
        devices.Add(EquipmentService.ValidSwitchingDevice);
      }

      if (unique.Contains(MeasurementDevice.Multimeter))
      {
        var meter = EquipmentService.GetFastMeterOrThrow(context.Console);
        await meter.ConnectableManager.InitializeAsync(context.Console);
        devices.Add(meter);
      }

      if (unique.Contains(MeasurementDevice.BreakdownTester))
      {
        var breakDown = await EquipmentService.GetBreakdownTesterOrThrow(context.Console);
        await breakDown.ConnectableManager.InitializeAsync(context.Console);
        devices.Add(breakDown);
      }

      ExecutionEventAdapter.RaiseDevicesChanged(devices);
    }
  }
}
