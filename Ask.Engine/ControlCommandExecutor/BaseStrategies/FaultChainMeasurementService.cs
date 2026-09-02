using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using Ask.Engine.ControlCommandExecutor.Execution;

namespace Ask.Engine.ControlCommandExecutor.BaseStrategies
{
  internal static class FaultChainMeasurementService
  {
    internal delegate Task<(bool Result, double Value)> PerformMeasurementAsync(
      double value,
      IUserInteractionService messageService,
      CancellationToken cancellationToken,
      double errorResistance,
      VoltageEnum.Type type = VoltageEnum.Type.DCW,
      string? points = null);

    public static async Task<AlgorithmExecutionResult> MeasureAsync(
      ExecutorContext context,
      IReadOnlyList<ChainModel> chainParts,
      string chainDisplay,
      PerformMeasurementAsync performMeasurementAsync,
      VoltageEnum.Type voltageType)
    {
      if (chainParts == null || chainParts.Count < 2)
      {
        return BuildResult(context, chainDisplay, 0);
      }

      var firstPart = chainParts[0];
      var otherParts = chainParts.Skip(1).ToList();
      var switchResistance = GetSwitchResistance(firstPart);

      try
      {
        await DisconnectFromBothBusesAsync(chainParts, context);

        await DeviceManager.RelayModule.ChainManager.ConnectChainToBusAAsync(firstPart, context.MessageService, context.IsPolarityReversed);

        foreach (var part in otherParts)
        {
          await DeviceManager.RelayModule.ChainManager.ConnectChainToBusBAsync(part, context.MessageService, context.IsPolarityReversed);
        }

        var measured = await performMeasurementAsync(
          context.Value,
          context.MessageService,
          context.MessageService.GetCancellationToken(),
          switchResistance,
          voltageType,
          chainDisplay);

        return BuildResult(context, chainDisplay, measured.Value);
      }
      finally
      {
        await DisconnectFromBothBusesAsync(chainParts, context);
      }
    }

    private static AlgorithmExecutionResult BuildResult(
      ExecutorContext context,
      string chainDisplay,
      double value)
    {
      return MeasurementMessages.BuildFaultChainResult(
        context.TypeCommand,
        new MeasurementRange(value, context.LowerLimit, context.HigherLimit),
        chainDisplay);
    }

    private static double GetSwitchResistance(ChainModel chain)
    {
      var point = chain.PointModels.FirstOrDefault();
      var module = point != null ? EquipmentService.GetModuleByPoint(point) : null;
      return module?.SwitchResistance ?? 0;
    }

    private static async Task DisconnectFromBothBusesAsync(
      IReadOnlyList<ChainModel> chainParts,
      ExecutorContext context)
    {
      foreach (var part in chainParts)
      {
        await DeviceManager.RelayModule.ChainManager.DisconnectChainFromBusAAsync(part, context.MessageService, context.IsPolarityReversed);
        await DeviceManager.RelayModule.ChainManager.DisconnectChainFromBusBAsync(part, context.MessageService, context.IsPolarityReversed);
      }
    }
  }
}
