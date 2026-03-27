using System.Reflection;
using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Formatter;

public sealed class NeCommandFormatterTests : IDisposable
{
  public NeCommandFormatterTests()
  {
    CommandsModel.Clear();
  }

  [Fact(DisplayName = "НЭ parser: блок точек без полярности превращается в ошибку, а не в падение")]
  public void Parse_WithMissingPolarity_AddsValidationError()
  {
    var model = new NeCommandModel
    {
      CommandNumber = "10",
      StartLineNumber = 10
    };

    var rmCommand = CreateRmCommand();
    string remainder = "0.5<В<0.8 *X1,X2* *-X3,X4*";

    var scheme = NeSchemeManager.Parse(
        model,
        rmCommand,
        10,
        "10",
        "НЭ",
        ref remainder,
        new List<string> { "10 НЭ 0.5<В<0.8 *X1,X2* *-X3,X4*" });

    Assert.NotNull(scheme);

    var error = Assert.Single(model.Errors);
    Assert.Equal(ErrorCode.Ne_CannotParseParameters, error.Code);
    Assert.Contains("не указана полярность", error.Description, StringComparison.OrdinalIgnoreCase);
  }

  [Fact(DisplayName = "Транслятор: падение formatter переводится в ошибку модели и не роняет сборку")]
  public void BuildFormattedText_WhenFormatterThrows_AddsErrorAndUsesFallbackText()
  {
    var method = typeof(CommandTranslationManager).GetMethod(
        "BuildFormattedText",
        BindingFlags.Instance | BindingFlags.NonPublic);

    Assert.NotNull(method);

    var manager = new CommandTranslationManager();
    var model = new NeCommandModel
    {
      CommandNumber = "10",
      StartLineNumber = 10,
      SourceLines = new List<string> { "10 НЭ *+X1,X2*" },
      AlgorithmKey = null!,
      Scheme = new SchemeModel(new List<GroupModel>())
    };

    var formattedText = Assert.IsType<string>(
        method!.Invoke(manager, new object[] { new List<BaseCommandModel> { model } }));

    var error = Assert.Single(model.Errors);
    Assert.Equal(ErrorCode.Unknown, error.Code);
    Assert.Contains("Не удалось отформатировать команду 10 НЭ", error.Description);
    Assert.Equal("10 НЭ *+X1,X2*", formattedText);
  }

  public void Dispose()
  {
    CommandsModel.Clear();
  }

  private static RmCommandModel CreateRmCommand()
  {
    return new RmCommandModel
    {
      CommandNumber = "1",
      StartLineNumber = 1,
      PointsMap = new Dictionary<string, string>
      {
        ["X1"] = "1.1.1",
        ["X2"] = "1.1.2",
        ["X3"] = "1.1.3",
        ["X4"] = "1.1.4"
      }
    };
  }
}
