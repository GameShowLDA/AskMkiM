using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;
using static Ask.Engine.ControlCommandExecutor.BaseStrategies.NodeAccumulationChecker;

namespace Ask.Engine.ControlCommandExecutor.BaseStrategies.Data
{
  internal class PairwiseFirstPointContext : ExecutorContext
  {
    internal PerformMeasurementAsync PerformMeasurementAsync { get; set; }

    internal VoltageEnum.Type VoltageType = VoltageEnum.Type.DCW;
  }
}
