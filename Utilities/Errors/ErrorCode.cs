using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Utilities.Models;

namespace Utilities.Errors
{
  public enum ErrorCode
  {
    [ErrorCodeTag("GEN001")] Gen_FirstMustBeOk,
    [ErrorCodeTag("GEN002")] Gen_LastMustBeKc,
    [ErrorCodeTag("GEN003")] Gen_MissingRequiredCommand,
    [ErrorCodeTag("GEN004")] Gen_DuplicateCommand,
    [ErrorCodeTag("GEN005")] Gen_MissingPointsMap,
    [ErrorCodeTag("GEN006")] Gen_UnknownPoint,
    [ErrorCodeTag("GEN007")] Gen_DuplicateDestination,
    [ErrorCodeTag("GEN008")] Gen_UnknownCommand,
    [ErrorCodeTag("GEN009")] Gen_UnrecognizedParameters,

    [ErrorCodeTag("OK001")] Ok_CannotParseFirstLine,
    [ErrorCodeTag("OK002")] Ok_MissingObjectCode,
    [ErrorCodeTag("OK003")] Ok_MissingObjectName,
    [ErrorCodeTag("OK004")] Ok_EmptyCommandBody,
    [ErrorCodeTag("OK005")] Ok_CannotParseParameter,
    [ErrorCodeTag("OK006")] Ok_ParameterKeyTooLong,
    [ErrorCodeTag("OK007")] Ok_ParameterValueTooLong,
    [ErrorCodeTag("OK008")] Ok_DuplicateKey,
    [ErrorCodeTag("OK009")] Ok_ObjectCodeTooLong,
    [ErrorCodeTag("OK010")] Ok_ObjectNameTooLong,

    [ErrorCodeTag("RM001")] Rm_CannotParseExpression,
    [ErrorCodeTag("RM002")] Rm_EmptyLeftOrRight,
    [ErrorCodeTag("RM003")] Rm_MismatchedCounts,
    [ErrorCodeTag("RM004")] Rm_GroupMismatch,
    [ErrorCodeTag("RM005")] Rm_GroupTooShort,
    [ErrorCodeTag("RM006")] Rm_StepRangeMismatch,
    [ErrorCodeTag("RM007")] Rm_EmptyCommandBody,

    [ErrorCodeTag("SI001")] Si_CannotParseExpression,
    [ErrorCodeTag("SI002")] Si_CannotParseParameters,
    [ErrorCodeTag("SI003")] Si_EmptyPoints,
    [ErrorCodeTag("SI004")] Si_EmptyCommandBody,
  }

  /// <summary>
  /// Расширения для <see cref="ErrorCode"/>.
  /// </summary>
  public static class ErrorCodeExtensions
  {
    /// <summary>
    /// Возвращает строковой тэг (например, TRN001), заданный через атрибут <see cref="ErrorCodeTagAttribute"/>.
    /// </summary>
    public static string? GetTag(this ErrorCode code)
    {
      var member = typeof(ErrorCode).GetMember(code.ToString()).FirstOrDefault();
      var attr = member?.GetCustomAttribute<ErrorCodeTagAttribute>();
      return attr?.Tag;
    }
  }
}
