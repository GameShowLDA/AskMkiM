using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Interface;
using Ask.Engine.ControlCommandExecutor.Execution;

namespace Ask.Engine.ControlCommandExecutor.BaseStrategies.Data
{
  internal class PairwiseFirstPointAltContext : ExecutorContext
  {
    internal double CabelResistance { get; set; } = 0;

    public PairwiseFirstPointAltContext() { }
    internal PairwiseFirstPointAltContext(
      CommandExecutionContext context,
      BaseCommandModel command,
      IHasScheme hasScheme,
      double value = 0,
      double lowerLimit = 0,
      double higherLimit = 0) : base(context, command, hasScheme, value, lowerLimit, higherLimit) { }
  }
}
