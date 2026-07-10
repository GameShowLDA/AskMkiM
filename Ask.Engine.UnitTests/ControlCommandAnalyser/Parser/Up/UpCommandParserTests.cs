using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.Translator;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Up;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Parser.Up
{
  public class UpCommandParserTests
  {
    [Fact(DisplayName = "УП парсер: принимает только мнемонику УП")]
    public void CanParse_ReturnsTrueOnlyForUpMnemonic()
    {
      var parser = new UpCommandParser();

      Assert.True(parser.CanParse(new MnemonicIdentifier("УП")));
      Assert.False(parser.CanParse(new MnemonicIdentifier("ПТ")));
      Assert.False(parser.CanParse(new MnemonicIdentifier("РМ")));
    }

    [Fact(DisplayName = "УП парсер: корректно разбирает метку перехода")]
    public void Parse_WithValidLabel_ParsesTargetLabel()
    {
      var parser = new UpCommandParser();

      var model = Assert.IsType<UpCommandModel>(
          parser.Parse(
              "130",
              "УП",
              1,
              new List<string>
              {
                "130 УП 210"
              }));

      Assert.Equal("210", model.TargetLabel);
      Assert.Empty(model.Errors);
    }

    [Fact(DisplayName = "УП парсер: считывает метку со второй строки")]
    public void Parse_LabelOnSecondLine()
    {
      var parser = new UpCommandParser();

      var model = Assert.IsType<UpCommandModel>(
          parser.Parse(
              "130",
              "УП",
              1,
              new List<string>
              {
                "130 УП",
                "210"
              }));

      Assert.Equal("210", model.TargetLabel);
      Assert.Empty(model.Errors);
    }

    [Fact(DisplayName = "УП парсер: отсутствие метки приводит к ошибке")]
    public void Parse_WithoutLabel_AddsError()
    {
      var parser = new UpCommandParser();

      var model = Assert.IsType<UpCommandModel>(
          parser.Parse(
              "130",
              "УП",
              1,
              new List<string>
              {
                "130 УП"
              }));

      Assert.NotEmpty(model.Errors);
    }

    [Fact(DisplayName = "УП парсер: переход на самого себя запрещён")]
    public void Parse_SelfJump_AddsError()
    {
      var parser = new UpCommandParser();

      var model = Assert.IsType<UpCommandModel>(
          parser.Parse(
              "130",
              "УП",
              1,
              new List<string>
              {
                "130 УП 130"
              }));

      Assert.Contains(model.Errors,
          e => e.Code == ErrorCode.Up_SelfReferenceJump);
    }

    [Fact(DisplayName = "УП парсер: некорректная метка вызывает ошибку")]
    public void Parse_InvalidLabel_AddsError()
    {
      var parser = new UpCommandParser();

      var model = Assert.IsType<UpCommandModel>(
          parser.Parse(
              "130",
              "УП",
              1,
              new List<string>
              {
                "130 УП ABC"
              }));

      Assert.Contains(model.Errors,
          e => e.Code == ErrorCode.Up_MissingOrInvalidUpLabel);
    }
  }
}
