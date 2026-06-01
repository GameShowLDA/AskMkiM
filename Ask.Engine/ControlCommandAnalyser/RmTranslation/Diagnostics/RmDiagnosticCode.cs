namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Diagnostics;

public enum RmDiagnosticCode
{
  UnexpectedCharacter,
  ExpectedEquals,
  ExpectedMachineAddress,
  InvalidMachineAddress,
  InvalidObjectAddress,
  InvalidRange,
  RangeLengthMismatch,
  DuplicateObjectAddress,
  DuplicateMachineAddress,
  DuplicateSynonym,
  SynonymObjectCollision,
  EmptyInput,
  EmptyExpression,
  UnexpectedToken
}
