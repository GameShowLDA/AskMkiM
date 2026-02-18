using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Interface;
using Ask.Engine.ControlCommandExecutor.Execution;
using static Ask.Engine.ControlCommandExecutor.BaseStrategies.NodeFullChecker;

namespace Ask.Engine.ControlCommandExecutor.BaseStrategies.Data
{
  internal class NodeFullContext : ExecutorContext
  {
    internal PerformMeasurementAsync PerformMeasurementAsync { get; set; }

    internal VoltageEnum.Type VoltageType = VoltageEnum.Type.DCW;

    public NodeFullContext() { }
    internal NodeFullContext(
      CommandExecutionContext context,
      BaseCommandModel command,
      IHasScheme hasScheme,
      double value = 0,
      double lowerLimit = 0,
      double higherLimit = 0) : base(context, command, hasScheme, value) { }
  }
}
