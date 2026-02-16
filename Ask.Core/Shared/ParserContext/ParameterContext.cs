using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;

namespace Ask.Core.Shared.ParserContext
{
  public record ParameterContext(
    string CommandNumber,
    string Mnemonic,
    int LineNumber,
    IBreakdownTester? Breakdown = null,
    IFastMeter? Fastmeter = null)
  {
    public string CommandId => $"{CommandNumber} {Mnemonic}";

    public static ParameterContext Create(
        string number,
        string mnemonic,
        int line)
        => new(number, mnemonic, line);

    public ParameterContext WithBreakdown(IBreakdownTester breakdown)
        => this with { Breakdown = breakdown };

    public ParameterContext WithFastMeter(IFastMeter fastmeter)
        => this with { Fastmeter = fastmeter };
  }
}
