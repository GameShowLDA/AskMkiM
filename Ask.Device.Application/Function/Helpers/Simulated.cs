using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;

namespace NewCore.Function.Helpers
{
  internal class Simulated
  {
    private static readonly Random _rnd = new();

    internal static double GetSimulatedValue(double rangeFrom, double rangeTo, ElectricalTestFunction measurementTypeCommand)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        if (!ExecutionConfig.GetIsErrorSimulationEnabled().Result)
        {
          switch (measurementTypeCommand)
          {
            case ElectricalTestFunction.None:
              break;
            case ElectricalTestFunction.DielectricWithstandAC:
              return 30;
            case ElectricalTestFunction.DielectricWithstandDC:
              return 1;
            case ElectricalTestFunction.InsulationResistance:
              return 60000;

            case ElectricalTestFunction.ACVoltage:
            case ElectricalTestFunction.DCVoltage:
            case ElectricalTestFunction.Resistance:
            case ElectricalTestFunction.Capacitance:
            case ElectricalTestFunction.Continuity:
            case ElectricalTestFunction.Diode:
              return (rangeFrom + rangeTo) / 2;
          }
        }
        else
        {
          switch (measurementTypeCommand)
          {
            case ElectricalTestFunction.DielectricWithstandAC:
              return new Random().Next(0, 80);
            case ElectricalTestFunction.DielectricWithstandDC:
              return new Random().Next(0, 5);
            case ElectricalTestFunction.InsulationResistance:
              return new Random().Next(0, 60000);
          }
        }


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

        return min + (_rnd.NextDouble() * (max - min));
      }

      return -1;
    }
  }
}
