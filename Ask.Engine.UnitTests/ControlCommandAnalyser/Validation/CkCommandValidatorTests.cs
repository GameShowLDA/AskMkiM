using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Validation;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Validation;

public class CkCommandValidatorTests
{
  [Fact(DisplayName = "СК валидатор: ВШ 2Ш запрещает команду СК")]
  public void ValidateVshCompatibility_WithTwoBusStructure_AddsCkError()
  {
    var ck = new CkCommandModel
    {
      CommandNumber = "30",
      StartLineNumber = 3
    };

    var models = new List<BaseCommandModel>
    {
      new VshCommandModel
      {
        CommandNumber = "20",
        StartLineNumber = 2,
        BusStructure = new Dictionary<BusStructureEnum.Type, List<int?>>
        {
          { BusStructureEnum.Type.Bus2, new List<int?>() }
        }
      },
      ck
    };

    CkCommandValidator.ValidateVshCompatibility(models);

    Assert.Contains(ck.Errors, error => error.Code == ErrorCode.Ck_ForbiddenForTwoBusStructure);
  }

  [Fact(DisplayName = "СК валидатор: ВШ 4Ш допускает команду СК")]
  public void ValidateVshCompatibility_WithFourBusStructure_DoesNotAddCkError()
  {
    var ck = new CkCommandModel
    {
      CommandNumber = "30",
      StartLineNumber = 3
    };

    var models = new List<BaseCommandModel>
    {
      new VshCommandModel
      {
        CommandNumber = "20",
        StartLineNumber = 2,
        BusStructure = new Dictionary<BusStructureEnum.Type, List<int?>>
        {
          { BusStructureEnum.Type.Bus4, new List<int?>() }
        }
      },
      ck
    };

    CkCommandValidator.ValidateVshCompatibility(models);

    Assert.DoesNotContain(ck.Errors, error => error.Code == ErrorCode.Ck_ForbiddenForTwoBusStructure);
  }
}
