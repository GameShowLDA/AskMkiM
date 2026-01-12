using Ask.Engine.ControlCommandAnalyser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Ask.Engine.ControlCommandExecutor.BaseStrategies.NodeAccumulationChecker;

namespace Ask.Engine.ControlCommandExecutor.BaseStrategies.Data
{
  internal class NodeAccumulationContext : ExecutorContext
  {
    internal PerformMeasurementAsync PerformMeasurementAsync { get; set; }

    internal VoltageEnum.Type VoltageType = VoltageEnum.Type.DCW;
  }
}
