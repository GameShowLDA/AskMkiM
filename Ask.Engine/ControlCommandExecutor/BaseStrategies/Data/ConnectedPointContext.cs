using static Ask.Engine.ControlCommandExecutor.BaseStrategies.ConnectedPointChecker;

namespace Ask.Engine.ControlCommandExecutor.BaseStrategies.Data
{
  internal class ConnectedPointContext : ExecutorContext
  {
    internal PerformMeasurementAsync PerformMeasurementAsync { get; set; }
  }
}
