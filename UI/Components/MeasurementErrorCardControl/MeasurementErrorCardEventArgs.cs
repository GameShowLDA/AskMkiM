using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;

namespace UI.Components.MeasurementErrorCardControl
{
  public class MeasurementErrorCardEventArgs : EventArgs
  {
    public MeasurementTypeCommand TypeCommand { get; }
    public double PercentageValue { get; }
    public double NumericValue { get; }

    public MeasurementErrorCardEventArgs(MeasurementTypeCommand typeCommand, double percentageValue, double numericValue)
    {
      TypeCommand = typeCommand;
      PercentageValue = percentageValue;
      NumericValue = numericValue;
    }
  }
}
