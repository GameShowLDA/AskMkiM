using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataBaseConfiguration.Models.MeasurementError;

namespace UI.Components.MeasurementErrorCard
{
  public class MeasurementErrorCardEventArgs : EventArgs
  {
    public MeasurementErrorEntity.TypeCommand TypeCommand { get; }
    public double PercentageValue { get; }
    public double NumericValue { get; }

    public MeasurementErrorCardEventArgs(MeasurementErrorEntity.TypeCommand typeCommand, double percentageValue, double numericValue)
    {
      TypeCommand = typeCommand;
      PercentageValue = percentageValue;
      NumericValue = numericValue;
    }
  }
}
