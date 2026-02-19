using Ask.Core.Services.Config.AppSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewCore.Function.Helpers
{
  internal class Simulated
  {
    private static readonly Random _rnd = new();

    internal static double GetSimulatedValue(double rangeFrom, double rangeTo)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        if (!ExecutionConfig.GetIsErrorSimulationEnabled().Result)
          return (rangeFrom + rangeTo) / 2;

        double min = rangeFrom / 2;
        double max = rangeTo == double.MaxValue
            ? rangeTo
            : rangeTo * 2;

        if (min > max)
          (min, max) = (max, min);

        if (double.IsInfinity(min) || double.IsInfinity(max))
          return rangeFrom;

        if (Math.Abs(max - min) < double.Epsilon)
          return min;

        return min + _rnd.NextDouble() * (max - min);
      }

      return -1;
    }
  }
}
