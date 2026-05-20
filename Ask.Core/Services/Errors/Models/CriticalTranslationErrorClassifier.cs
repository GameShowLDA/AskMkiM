namespace Ask.Core.Services.Errors.Models
{
  public static class CriticalTranslationErrorClassifier
  {
    public static bool IsCriticalStructural(ErrorItem? error)
    {
      return error?.Code is
        ErrorCode.Gen_FirstMustBeOk or
        ErrorCode.Gen_LastMustBeKc or
        ErrorCode.Gen_MissingRequiredCommand or
        ErrorCode.Gen_DuplicateCommand or
        ErrorCode.Gen_MissingPointsMap or
        ErrorCode.Vsh_InvalidVshBusStructure or
        ErrorCode.Vsh_NoneVshBusStructure;
    }
  }
}
