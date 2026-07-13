using Ask.Core.Services.Translator;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Oc;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Parser.Oc
{
  public class OcCommandParserTests
  {
    [Fact]
    public void CanParse_OcMnemonic_ReturnsTrue()
    {
      var parser = new OcCommandParser();

      Assert.True(parser.CanParse(new MnemonicIdentifier("ОС")));
      Assert.False(parser.CanParse(new MnemonicIdentifier("СК")));
    }

    [Fact]
    public void Parse_CreatesModel()
    {
      var parser = new OcCommandParser();

      var model = Assert.IsType<OcCommandModel>(
          parser.Parse(
              "10",
              "ОС",
              5,
              new List<string>
              {
                        "10 ОС"
              }));

      Assert.Equal("10", model.CommandNumber);
      Assert.Equal(5, model.StartLineNumber);
      Assert.Single(model.SourceLines);
      Assert.Equal("10 ОС", model.SourceLines[0]);
    }
  }
}
