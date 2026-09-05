using Ask.Core.Services.Errors.Models;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model.Ks;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Validation;

public sealed class AllowedKeysAttributeTests
{
  [Fact]
  public void ValidateKeysAndAttachErrors_UnknownAlgorithmKey_AddsNotRecognizedError()
  {
    var model = new KsCommandModel
    {
      StartLineNumber = 7,
      AlgorithmKey = { "НЕИЗВЕСТНЫЙ" }
    };

    AllowedKeysAttribute.ValidateKeysAndAttachErrors(model);

    var error = Assert.Single(model.Errors);
    Assert.Equal(ErrorCode.Key_NotRecognized, error.Code);
    Assert.Equal(7, error.SourceLineNumber);
    Assert.Contains("НЕИЗВЕСТНЫЙ", error.Description);
  }
}
