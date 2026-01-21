using Ask.Engine.ControlCommandAnalyser.Model;
using static Ask.Engine.ControlCommandExecutor.BaseStrategies.NodeFullChecker;

namespace Ask.Engine.ControlCommandExecutor.BaseStrategies.Data
{
  internal class NodeFullContext : ExecutorContext
  {
    internal PerformMeasurementAsync PerformMeasurementAsync { get; set; }

    internal VoltageEnum.Type VoltageType = VoltageEnum.Type.DCW;

  }
}
