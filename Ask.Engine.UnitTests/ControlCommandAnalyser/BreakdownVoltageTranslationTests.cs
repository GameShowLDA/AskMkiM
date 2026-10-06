using Ask.Core.Shared.ParserContext;
using Ask.Device.Runtime.Device;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.Pi;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.Si;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser;

public sealed class BreakdownVoltageTranslationTests
{
  [Theory]
  [InlineData("125В", true)]
  [InlineData("0.125кВ", true)]
  [InlineData("150В", true)]
  [InlineData("126В", false)]
  [InlineData("49В", false)]
  [InlineData("1001В", false)]
  public void SiUsesStepAndExceptions(string text, bool valid)
  {
    using var device = new GPT79904();
    var model = new SiCommandModel();
    new SiVoltageProcessor().Process(model, text, new ParameterContext("10", "СИ", 1, device));
    Assert.Equal(valid, model.Errors.Count == 0);
  }

  [Theory]
  [InlineData("650В", true)]
  [InlineData("652В", false)]
  [InlineData("649В", false)]
  [InlineData("49В", false)]
  [InlineData("900В +", true)]
  [InlineData("902В +", false)]
  [InlineData("899В +", false)]
  public void PiUsesSavedDeviceBoundsAndStep(string text, bool valid)
  {
    using var device = new GPT79904();
    device.AcwManger.VoltageRange.MaxVoltage = 650;
    device.DcwManger.VoltageRange.MaxVoltage = 900;
    var model = new PiCommandModel();
    new PiVoltageProcessor().Process(model, text, new ParameterContext("10", "ПИ", 1, device));
    Assert.Equal(valid, model.Errors.Count == 0);
  }
}
