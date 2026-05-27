using System.Text;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.ComandBody;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Serialization;

public class EhtCommandSerializationTests
{
  [Fact]
  public void ParameterPipeline_PreservesAmperageForSerialization()
  {
    var model = new EhtCommandModel
    {
      CommandNumber = "10"
    };
    var context = ParameterContext.Create("10", "\u042D\u0422", 1);

    var remainder = EhtParameterPipeline.Execute(
      model,
      "10<\u041E\u043C<20, 2 \u041E\u043C, 10 \u043C\u0410, 1\u0441",
      context);

    Assert.Equal(string.Empty, remainder);
    Assert.Equal(0.01, model.Amperage);
    Assert.Equal("\u0410", model.AmperageUnit);
    Assert.Equal("0.01 \u0410", model.AmperageSource);
  }

  [Fact]
  public void CommandBodyBuilder_WritesCableResistanceUnitAndAmperage()
  {
    var model = new EhtCommandModel
    {
      CommandNumber = "10",
      LowerLimitResistance = 10,
      LowerLimitResistanceSource = "10 \u041E\u043C",
      HigherLimitResistance = 20,
      HigherLimitResistanceSource = "20 \u041E\u043C",
      ResistanceUnit = "\u041E\u043C",
      TimeSource = "1\u0441",
      CabelResistance = 2,
      CabelResistanceSource = "2 \u041E\u043C",
      CabelResistanceUnit = "\u041E\u043C",
      Amperage = 0.01,
      AmperageSource = "0.01 \u0410",
      AmperageUnit = "\u0410"
    };

    var text = new EhtCommandBodyBuilder()
      .Create(model, new StringBuilder("10 \u042D\u0422  "))
      .ToString();

    Assert.Contains(", 2 \u041E\u043C", text);
    Assert.Contains(", 0.01 \u0410", text);
    Assert.DoesNotContain(", 2, 0.01", text);
  }
}
