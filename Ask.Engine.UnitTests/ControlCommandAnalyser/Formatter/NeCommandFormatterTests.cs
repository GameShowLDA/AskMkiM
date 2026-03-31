using System.Reflection;
using System.Text;
using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.ComandBody;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser;
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

  [Fact(DisplayName = "Транслятор: исключение в parser превращается в ошибку модели и не роняет сборку")]
  public void BuildTranslation_WhenParserThrows_AddsErrorModel()
  {
    var manager = new CommandTranslationManager();
    var parsersField = typeof(CommandTranslationManager).GetField("_parsers", BindingFlags.Instance | BindingFlags.NonPublic);

    Assert.NotNull(parsersField);
    var parsers = Assert.IsType<List<ICommandParser>>(parsersField!.GetValue(manager));
    parsers.Insert(0, new ThrowingParser());

    var result = manager.BuildTranslation("10 ТЕСТ\n20 КЦ");

    Assert.NotNull(result);
    Assert.NotEmpty(result.Models);
    Assert.Contains(
        result.Models.SelectMany(model => model.Errors),
        error => error.Code == ErrorCode.Unknown
            && error.Description.Contains("разборе команды", StringComparison.OrdinalIgnoreCase));
  }

  [Fact(DisplayName = "Транслятор: исключение в SetSourseLines превращается в ошибку модели")]
  public void SetSourseLines_WhenBodyBuilderThrows_AddsErrorInsteadOfThrowing()
  {
    var manager = new CommandTranslationManager();
    var buildersField = typeof(CommandTranslationManager).GetField("_commandBodyBuilders", BindingFlags.Instance | BindingFlags.NonPublic);

    Assert.NotNull(buildersField);
    var builders = Assert.IsType<List<ICommandBody>>(buildersField!.GetValue(manager));
    builders.Insert(0, new ThrowingBodyBuilder());

    var model = new UnknownCommandModel
    {
      CommandNumber = "10",
      Mnemonic = "ТЕСТ",
      StartLineNumber = 10,
      SourceLines = new List<string> { "10 ТЕСТ" }
    };

    var exception = Record.Exception(() => manager.SetSourseLines(new List<BaseCommandModel> { model }));

    Assert.Null(exception);
    Assert.Contains(
        model.Errors,
        error => error.Code == ErrorCode.Unknown
            && error.Description.Contains("формировании исходных строк", StringComparison.OrdinalIgnoreCase));
    Assert.Equal("10 ТЕСТ", Assert.Single(model.SourceLines));
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

  private sealed class ThrowingParser : ICommandParser
  {
    public bool CanParse(MnemonicIdentifier mnemonic) =>
        string.Equals(mnemonic.Mnemonic, "ТЕСТ", StringComparison.OrdinalIgnoreCase);

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines) =>
        throw new InvalidOperationException("Parser crash");
  }

  private sealed class ThrowingBodyBuilder : ICommandBody
  {
    public bool CanCreate(BaseCommandModel model) => model is UnknownCommandModel;

    public StringBuilder Create(BaseCommandModel model, StringBuilder newSourseLines) =>
        throw new InvalidOperationException("Body builder crash");
  }
}
