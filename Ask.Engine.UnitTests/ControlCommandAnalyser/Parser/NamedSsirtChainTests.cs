using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Model.Pr;
using Ask.Engine.ControlCommandAnalyser.Parser;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Parser;

public class NamedSsirtChainTests
{
  [Fact]
  public void ParsePoints_WithNamedChains_StoresChainNamesAndParsesPoints()
  {
    var model = new PrCommandModel
    {
      CommandNumber = "80",
      StartLineNumber = 80
    };
    var rm = CreateRmCommand();

    var (scheme, errors) = PointParser.ParsePoints(
      "*1_1сеть1=Х7/1,Х7/3,Х7/5*1_2сеть2=Х7/2,Х7/4,Х7/6*",
      model,
      rm);

    Assert.Empty(errors);
    Assert.NotNull(scheme);
    Assert.Equal(new[] { "1_1сеть1", "1_2сеть2" }, scheme!.GroupModels.Select(group => group.ChainName).ToArray());
    Assert.Equal(new[] { "1.1.1", "1.1.3", "1.1.5", "1.1.2", "1.1.4", "1.1.6" },
      scheme.EnumeratePoints().Select(point => point.ToString()).ToArray());
  }

  [Fact]
  public void ParsePoints_WithoutChainNames_KeepsChainNameEmpty()
  {
    var model = new PrCommandModel
    {
      CommandNumber = "80",
      StartLineNumber = 80
    };
    var rm = CreateRmCommand();

    var (scheme, errors) = PointParser.ParsePoints("*Х7/1,Х7/3*", model, rm);

    Assert.Empty(errors);
    Assert.NotNull(scheme);
    Assert.Null(scheme!.GroupModels.Single().ChainName);
  }

  [Fact(DisplayName = "ССИРТ: соседние отсутствующие точки объединяются в одну ошибку диапазона")]
  public void ParsePoints_WithMissingRange_ReturnsSingleRangeError()
  {
    var model = CreatePrCommand();
    var rm = CreateNumericRmCommand(100);

    var (_, errors) = PointParser.ParsePoints("*1-110*", model, rm);

    var error = Assert.Single(errors);
    Assert.Equal(ErrorCode.Gen_UnknownPoint, error.Code);
    Assert.Equal("Точки 101-110 не найдены в РМ.", error.Description);
    Assert.Equal("1-110", error.SourceFragment);

    const string sourceLine = "80 ПР 10<Ом<20 *1-110*";
    Assert.True(IssueSelectionHintResolver.TryResolve(error, sourceLine, out var hint));
    Assert.Equal("1-110", sourceLine.Substring(hint.StartIndex, hint.Length));
  }

  [Fact(DisplayName = "ССИРТ: раздельные отсутствующие диапазоны остаются раздельными ошибками")]
  public void ParsePoints_WithSeparatedMissingRanges_ReturnsOneErrorPerRange()
  {
    var model = CreatePrCommand();
    var rm = CreateNumericRmCommand(100);

    var (_, errors) = PointParser.ParsePoints("*1-105,108-110*", model, rm);

    Assert.Collection(
      errors,
      error =>
      {
        Assert.Equal("Точки 101-105 не найдены в РМ.", error.Description);
        Assert.Equal("1-105", error.SourceFragment);
      },
      error =>
      {
        Assert.Equal("Точки 108-110 не найдены в РМ.", error.Description);
        Assert.Equal("108-110", error.SourceFragment);
      });
  }

  [Fact(DisplayName = "ССИРТ: отсутствующий диапазон точек с префиксом объединяется")]
  public void ParsePoints_WithPrefixedMissingRange_ReturnsSingleRangeError()
  {
    var model = CreatePrCommand();
    var rm = CreatePrefixedRmCommand("X1/", 100);

    var (_, errors) = PointParser.ParsePoints("*X1/1-110*", model, rm);

    var error = Assert.Single(errors);
    Assert.Equal("Точки X1/101-X1/110 не найдены в РМ.", error.Description);
    Assert.Equal("X1/1-110", error.SourceFragment);
  }

  [Fact(DisplayName = "Точки шин: отсутствующий диапазон объединяется в одну ошибку")]
  public void ParseBusPoints_WithMissingRange_ReturnsSingleRangeError()
  {
    var rm = CreateNumericRmCommand(100);

    var (_, errors) = PointParser.ParseBusPoints(
      "*A1:101-110*", rm, 80, "80 ПТ");

    var error = Assert.Single(errors);
    Assert.Equal("Точки 101-110 не найдены в РМ.", error.Description);
    Assert.Equal("101-110", error.SourceFragment);
  }

  [Fact(DisplayName = "ОТ/ПТ: одинаковые ошибки конфигурации модуля объединяются для диапазона точек")]
  public void CollapseBusPointErrors_WithSameFailure_ReturnsSingleRangeError()
  {
    var errors = Enumerable.Range(101, 10)
      .Select(point => GeneralErrors.BusPointConfiguration(
        "Модуль 1.2 не найден в конфигурации.",
        point.ToString(),
        80,
        "80 ПТ"))
      .ToList();

    CommandPostAnalyzer.CollapseBusPointErrors(errors, "*A1:1-110*");

    var error = Assert.Single(errors);
    Assert.Equal(ErrorCode.Gen_BusPointConfiguration, error.Code);
    Assert.Equal(
      "Модуль 1.2 не найден в конфигурации. Затронуты точки 101-110.",
      error.Description);
    Assert.Equal("1-110", error.SourceFragment);
  }

  [Fact(DisplayName = "ОТ/ПТ: разные причины ошибки конфигурации остаются отдельными")]
  public void CollapseBusPointErrors_WithDifferentFailures_ReturnsSeparateRanges()
  {
    var errors = new List<ErrorItem>();
    errors.AddRange(Enumerable.Range(101, 3)
      .Select(point => GeneralErrors.BusPointConfiguration(
        "Модуль 1.2 не найден в конфигурации.",
        point.ToString(),
        80,
        "80 ПТ")));
    errors.AddRange(Enumerable.Range(104, 2)
      .Select(point => GeneralErrors.BusPointConfiguration(
        "Модуль 1.3 не поддерживает шину A1.",
        point.ToString(),
        80,
        "80 ПТ")));

    CommandPostAnalyzer.CollapseBusPointErrors(errors, "*A1:101-105*");

    Assert.Collection(
      errors,
      error =>
      {
        Assert.Contains("точки 101-103", error.Description);
        Assert.Equal("101-105", error.SourceFragment);
      },
      error =>
      {
        Assert.Contains("точки 104-105", error.Description);
        Assert.Equal("101-105", error.SourceFragment);
      });
  }

  [Fact(DisplayName = "РМ: повторы машинных адресов между частями объединяются в диапазон")]
  public void Analyze_WithDuplicateDestinationsAcrossParts_ReturnsSingleRangeError()
  {
    var rm = new RmCommandModel
    {
      CommandNumber = "30",
      StartLineNumber = 30,
      PointsSourse = "A1-A2=1.1.1-2 B1-B2=1.1.1-2",
      PointsMap = new Dictionary<string, string>
      {
        ["A1"] = "1.1.1",
        ["A2"] = "1.1.2",
        ["B1"] = "1.1.1",
        ["B2"] = "1.1.2"
      }
    };
    var models = new List<BaseCommandModel>
    {
      new OkCommandModel { CommandNumber = "10", StartLineNumber = 10 },
      rm,
      new KscCommandModel { CommandNumber = "40", StartLineNumber = 40 }
    };

    CommandPostAnalyzer.Analyze(models);

    var error = Assert.Single(
      rm.Errors,
      item => item.Code == ErrorCode.Gen_DuplicateDestination);
    Assert.Equal(
      "Точки назначения 1.1.1-1.1.2 используются более одного раза.",
      error.Description);
    Assert.Equal("1.1.1-2", error.SourceFragment);
  }

  private static PrCommandModel CreatePrCommand() => new()
  {
    CommandNumber = "80",
    StartLineNumber = 80
  };

  private static RmCommandModel CreateNumericRmCommand(int count)
  {
    var rm = new RmCommandModel
    {
      CommandNumber = "30",
      StartLineNumber = 30
    };

    for (int point = 1; point <= count; point++)
      rm.PointsMap[point.ToString()] = $"1.1.{point}";

    return rm;
  }

  private static RmCommandModel CreatePrefixedRmCommand(string prefix, int count)
  {
    var rm = new RmCommandModel
    {
      CommandNumber = "30",
      StartLineNumber = 30
    };

    for (int point = 1; point <= count; point++)
      rm.PointsMap[$"{prefix}{point}"] = $"1.1.{point}";

    return rm;
  }

  private static RmCommandModel CreateRmCommand()
  {
    return new RmCommandModel
    {
      CommandNumber = "30",
      StartLineNumber = 30,
      PointsMap = new Dictionary<string, string>
      {
        ["Х7/1"] = "1.1.1",
        ["Х7/2"] = "1.1.2",
        ["Х7/3"] = "1.1.3",
        ["Х7/4"] = "1.1.4",
        ["Х7/5"] = "1.1.5",
        ["Х7/6"] = "1.1.6"
      }
    };
  }
}
