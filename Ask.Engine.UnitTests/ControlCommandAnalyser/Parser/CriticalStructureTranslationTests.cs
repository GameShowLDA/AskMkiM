using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Parser;

public class CriticalStructureTranslationTests : IDisposable
{
  [Fact]
  public void Analyze_WithCriticalStructureError_SkipsSecondaryPostAnalysis()
  {
    var models = new List<BaseCommandModel>
    {
      new UpCommandModel
      {
        CommandNumber = "10",
        SourceLines = new List<string> { "10 \u0423\u041F 999" },
        StartLineNumber = 1,
        TargetLabel = "999"
      },
      new KscCommandModel
      {
        CommandNumber = "20",
        SourceLines = new List<string> { "20 \u041A\u0426" },
        StartLineNumber = 2
      }
    };

    CommandPostAnalyzer.Analyze(models);
    var errors = models.SelectMany(model => model.Errors).ToArray();

    Assert.Contains(errors, error => error.Code == ErrorCode.Gen_FirstMustBeOk);
    Assert.Contains(errors, error =>
      error.Code == ErrorCode.Gen_MissingRequiredCommand &&
      error.Description.Contains("\u041E\u041A", StringComparison.OrdinalIgnoreCase));
    Assert.DoesNotContain(errors, error => error.Code == ErrorCode.Up_UpLabelNotFound);
  }

  public void Dispose()
  {
    CommandsModel.Clear();
  }
}
