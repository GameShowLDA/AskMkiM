using Ask.Core.Services.Translator;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Vsh;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Parser.Vsh
{
  public class VshCommandParserTests
  {
    [Fact]
    public void CanParse_VshMnemonic_ReturnsTrue()
    {
      var parser = new VshCommandParser();

      Assert.True(parser.CanParse(new MnemonicIdentifier("ВШ")));
      Assert.False(parser.CanParse(new MnemonicIdentifier("ПТ")));
    }

    /*[Fact]
    public void Parse_2BusWithRack_ParsesCorrectly()
    {
      var parser = new VshCommandParser();

      var model = Assert.IsType<VshCommandModel>(
          parser.Parse(
              "30",
              "ВШ",
              1,
              new List<string>
              {
                "30 ВШ *2Ш:1*"
              }));


      Assert.NotNull(model.BusStructure);
      Assert.True(model.BusStructure.ContainsKey(BusStructureEnum.Type.Bus2));
      Assert.Contains(1, model.BusStructure[BusStructureEnum.Type.Bus2]);
    }

    [Fact]
    public void Parse_DuplicateRack_AddsWarning()
    {
      var parser = new VshCommandParser();

      var model = Assert.IsType<VshCommandModel>(
          parser.Parse(
              "30",
              "ВШ",
              1,
              new List<string>
              {
                "30 ВШ *2Ш:1,2Ш:1*"
              }));

      Assert.NotEmpty(model.Warnings);
    }*/

    [Fact]
    public void Parse_InvalidRack_AddsError()
    {
      var parser = new VshCommandParser();

      var model = Assert.IsType<VshCommandModel>(
          parser.Parse(
              "30",
              "ВШ",
              1,
              new List<string>
              {
                "30 ВШ *2Ш:999*"
              }));

      Assert.NotEmpty(model.Errors);
    }

    [Fact]
    public void Parse_EmptyBody_AddsError()
    {
      var parser = new VshCommandParser();

      var model = Assert.IsType<VshCommandModel>(
          parser.Parse(
              "30",
              "ВШ",
              1,
              new List<string>()));

      Assert.NotEmpty(model.Errors);
    }
  }
}
