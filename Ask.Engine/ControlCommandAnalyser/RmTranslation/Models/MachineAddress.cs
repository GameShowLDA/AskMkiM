namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Models;

public readonly record struct MachineAddress(int Rack, int Block, int Point) : IComparable<MachineAddress>
{
  public int CompareTo(MachineAddress other)
  {
    var rack = Rack.CompareTo(other.Rack);
    if (rack != 0)
      return rack;

    var block = Block.CompareTo(other.Block);
    return block != 0 ? block : Point.CompareTo(other.Point);
  }

  public override string ToString() => $"{Rack}.{Block}.{Point}";
}
