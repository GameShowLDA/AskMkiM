using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Validation;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Validation;

public class ViCommandValidatorTests
{
  [Fact]
  public void ValidateVshCompatibility_BusPairOutsideStructure_AddsError()
  {
    var vi = new ViCommandModel
    {
      CommandNumber = "30",
      StartLineNumber = 3,
      PowerSupplyChassisNumber = 1,
      PositiveBus = SwitchingBus.A2,
      NegativeBus = SwitchingBus.B2
    };
    var vsh = new VshCommandModel
    {
      CommandNumber = "20",
      BusStructure = new Dictionary<BusStructureEnum.Type, List<int?>>
      {
        [BusStructureEnum.Type.Bus2] = new() { 1 }
      }
    };
    var models = new List<BaseCommandModel> { vsh, vi };

    ViCommandValidator.ValidateVshCompatibility(models);

    Assert.Contains(vi.Errors, error => error.Code == ErrorCode.Vi_InvalidBus);
  }

  [Fact]
  public void ValidateVshCompatibility_AvailableBusPair_DoesNotAddError()
  {
    var vi = new ViCommandModel
    {
      CommandNumber = "30",
      PowerSupplyChassisNumber = 1,
      PositiveBus = SwitchingBus.A2,
      NegativeBus = SwitchingBus.B2
    };
    var vsh = new VshCommandModel
    {
      CommandNumber = "20",
      BusStructure = new Dictionary<BusStructureEnum.Type, List<int?>>
      {
        [BusStructureEnum.Type.Bus4] = new() { 1 }
      }
    };
    var models = new List<BaseCommandModel> { vsh, vi };

    ViCommandValidator.ValidateVshCompatibility(models);

    Assert.Empty(vi.Errors);
  }
}
