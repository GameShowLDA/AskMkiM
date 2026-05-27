using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Validation
{
  public static class CkCommandValidator
  {
    public static void ValidateVshCompatibility(IReadOnlyList<BaseCommandModel> models)
    {
      if (!HasTwoBusStructure(models))
        return;

      foreach (var ckCommand in models.OfType<CkCommandModel>())
      {
        if (HasForbiddenTwoBusStructureError(ckCommand))
          continue;

        ckCommand.Errors.Add(CkErrors.ForbiddenForTwoBusStructure(
          ckCommand.StartLineNumber,
          $"{ckCommand.CommandNumber} {ckCommand.Mnemonic}"));
      }
    }

    private static bool HasTwoBusStructure(IEnumerable<BaseCommandModel> models)
    {
      return models
        .OfType<VshCommandModel>()
        .Any(vsh => vsh.BusStructure?.ContainsKey(BusStructureEnum.Type.Bus2) == true);
    }

    private static bool HasForbiddenTwoBusStructureError(CkCommandModel ckCommand)
    {
      return ckCommand.Errors.Any(error => error.Code == ErrorCode.Ck_ForbiddenForTwoBusStructure);
    }
  }
}
