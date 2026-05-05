using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Parser;

public class SchemeManagerDeletionTests : IDisposable
{
  public SchemeManagerDeletionTests()
  {
    CommandsModel.Clear();
    CommandsModel.CommandModels.Add(CreateRmCommand());
  }

  [Fact(DisplayName = "SchemeManager removes a chain containing a point from SSIRT deletion list")]
  public void GetScheme_WithDeletionList_RemovesWholeContainingGroup()
  {
    var model = new TestCommandModel
    {
      CommandNumber = "10",
      Mnemonic = "ТСТ",
      StartLineNumber = 10
    };
    string remainder = "*X1,X2,X3*X4,X5*X6*~(X4)*";

    var scheme = SchemeManager.GetScheme(model, CommandsModel.GetRMModel(), 10, ref remainder);

    Assert.Empty(model.Errors);
    Assert.Equal("*X1,X2,X3*X4,X5*X6*~(X4)*", model.PointsSourse);
    Assert.Equal(new[] { "1.1.1", "1.1.2", "1.1.3", "1.1.6" }, GetPointAddresses(scheme));
  }

  [Fact(DisplayName = "SchemeManager applies SSIRT deletion after key C adds RM points")]
  public void GetScheme_WithKeyCAndDeletionList_RemovesPointsAddedByKeyC()
  {
    var model = new SiCommandModel
    {
      CommandNumber = "20",
      StartLineNumber = 20,
      AlgorithmKey = { "С" }
    };
    string remainder = "*X1*~(X3)*";

    var scheme = SchemeManager.GetScheme(model, CommandsModel.GetRMModel(), 20, ref remainder);

    Assert.Empty(model.Errors);
    Assert.Equal(new[] { "1.1.1", "1.1.2", "1.1.4", "1.1.5", "1.1.6" }, GetPointAddresses(scheme));
  }

  public void Dispose() => CommandsModel.Clear();

  private static string[] GetPointAddresses(SchemeModel scheme)
    => scheme.EnumeratePoints().Select(point => point.ToString()).ToArray();

  private static RmCommandModel CreateRmCommand()
  {
    return new RmCommandModel
    {
      CommandNumber = "30",
      StartLineNumber = 30,
      PointsMap = new Dictionary<string, string>
      {
        ["X1"] = "1.1.1",
        ["X2"] = "1.1.2",
        ["X3"] = "1.1.3",
        ["X4"] = "1.1.4",
        ["X5"] = "1.1.5",
        ["X6"] = "1.1.6"
      }
    };
  }

  private sealed class TestCommandModel : BaseCommandModel;
}
