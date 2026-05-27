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
