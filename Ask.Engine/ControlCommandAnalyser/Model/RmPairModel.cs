namespace Ask.Engine.ControlCommandAnalyser.Model
{
  public class RmPairModel
  {
    public string OkPoint { get; set; }
    public string? Synonym { get; set; }
    public string AskInput { get; set; }
    public int? PartNumber { get; set; }

    public override string ToString()
    {
      return Synonym != null
          ? $"{OkPoint}({Synonym}) => {AskInput}"
          : $"{OkPoint} => {AskInput}";
    }
  }

}
