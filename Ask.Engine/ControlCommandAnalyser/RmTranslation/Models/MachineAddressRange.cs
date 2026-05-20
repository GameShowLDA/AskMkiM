namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Models;

public sealed record MachineAddressRange(MachineAddress Start, MachineAddress End, int Step = 1)
{
  public int Count
  {
    get
    {
      if (Step == 0 || Start.Rack != End.Rack || Start.Block != End.Block)
        return 0;

      var distance = End.Point - Start.Point;
      if (distance == 0)
        return 1;

      if (Math.Sign(distance) != Math.Sign(Step))
        return 0;

      return (Math.Abs(distance) / Math.Abs(Step)) + 1;
    }
  }

  public IReadOnlyList<MachineAddress> Expand()
  {
    var result = new List<MachineAddress>();
    if (Step == 0 || Start.Rack != End.Rack || Start.Block != End.Block)
      return result;

    var direction = End.Point >= Start.Point ? 1 : -1;
    var step = Math.Abs(Step) * direction;

    for (var current = Start.Point; direction > 0 ? current <= End.Point : current >= End.Point; current += step)
      result.Add(new MachineAddress(Start.Rack, Start.Block, current));

    return result;
  }
}
