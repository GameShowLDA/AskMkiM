using AppConfiguration.Enums;

namespace UI.Components.MeasurementErrorCard
{
  public class MeasurementErrorCardEventArgs : EventArgs
  {
    public TypeCommand TypeCommand { get; }
    public double PercentageValue { get; }
    public double NumericValue { get; }

    public MeasurementErrorCardEventArgs(TypeCommand typeCommand, double percentageValue, double numericValue)
    {
      TypeCommand = typeCommand;
      PercentageValue = percentageValue;
      NumericValue = numericValue;
    }
  }
}
