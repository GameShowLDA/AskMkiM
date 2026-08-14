using Ask.Core.Shared.DTO.Protocol;
using System.Windows.Media;

namespace Ask.Core.Services.Protocols;

/// <summary>
/// Содержит сериализуемое состояние сообщения протокола выполнения.
/// </summary>
public sealed record ExecutionProtocolMessageSnapshot(
  string? Header,
  string? Message,
  string? Time,
  string? Source,
  ShowMessageModel.MessageType? Status,
  bool IsDeviceMessage,
  bool ExecutionError,
  string? ExecutionErrorMessage,
  bool CanBeDeleted,
  bool IsControlProgramCommandHeader,
  bool IsStepModeCheckpoint,
  bool UseSuccessColorForEntireMessage,
  bool? CommandExecutionHasErrors,
  int IndentLevel,
  uint? HeaderColor,
  uint? MessageColor,
  uint? TimeColor,
  uint? HeaderBackgroundColor)
{
  public static ExecutionProtocolMessageSnapshot FromModel(ShowMessageModel model) => new(
    model.Header,
    model.Message,
    model.Time,
    model.DiagnosticSource ?? model.Debug,
    model.Status,
    model.IsDeviceMessage,
    model.ExecutionError,
    model.ExecutionErrorMessage,
    model.CanBeDeleted,
    model.IsControlProgramCommandHeader,
    model.IsStepModeCheckpoint,
    model.UseSuccessColorForEntireMessage,
    model.CommandExecutionHasErrors,
    model.IndentLevel,
    Pack(model.HeaderColor),
    Pack(model.MessageColor),
    Pack(model.TimeColor),
    Pack(model.HeaderBackgroundColor));

  public ShowMessageModel ToModel(bool includeDiagnostics)
  {
    var model = new ShowMessageModel
    {
      Header = Header ?? string.Empty,
      Message = Message ?? string.Empty,
      Time = Time ?? string.Empty,
      DiagnosticSource = Source,
      IsDeviceMessage = IsDeviceMessage,
      ExecutionError = ExecutionError,
      ExecutionErrorMessage = ExecutionErrorMessage,
      CanBeDeleted = CanBeDeleted,
      IsControlProgramCommandHeader = IsControlProgramCommandHeader,
      IsStepModeCheckpoint = IsStepModeCheckpoint,
      UseSuccessColorForEntireMessage = UseSuccessColorForEntireMessage,
      CommandExecutionHasErrors = CommandExecutionHasErrors,
      IndentLevel = IndentLevel,
      Status = Status,
      HeaderColor = Unpack(HeaderColor),
      MessageColor = Unpack(MessageColor),
      TimeColor = Unpack(TimeColor),
      HeaderBackgroundColor = Unpack(HeaderBackgroundColor)
    };

    if (includeDiagnostics && !string.IsNullOrWhiteSpace(Source))
      model.Debug = $"{Environment.NewLine}    ↳ [ОТЛАДКА ROOT] {BuildDiagnosticText()}";

    return model;
  }

  private string BuildDiagnosticText()
  {
    var attributes = new List<string> { $"тип={Status}", $"отступ={IndentLevel}" };
    if (IsDeviceMessage) attributes.Add("оборудование");
    if (ExecutionError) attributes.Add("ошибка выполнения");
    if (CanBeDeleted) attributes.Add("сокращаемая запись");
    if (IsControlProgramCommandHeader) attributes.Add("заголовок команды ПК");
    if (IsStepModeCheckpoint) attributes.Add("контрольная точка шага");
    if (CommandExecutionHasErrors.HasValue)
      attributes.Add($"результат команды={(CommandExecutionHasErrors.Value ? "БРАК" : "НОРМА")}");
    if (!string.IsNullOrWhiteSpace(ExecutionErrorMessage))
      attributes.Add($"ошибка для заключения={ExecutionErrorMessage}");

    return $"{Source}; {string.Join("; ", attributes)}";
  }

  private static uint? Pack(Color? color) => color.HasValue
    ? ((uint)color.Value.A << 24) | ((uint)color.Value.R << 16) | ((uint)color.Value.G << 8) | color.Value.B
    : null;

  private static Color? Unpack(uint? value) => value.HasValue
    ? Color.FromArgb(
      (byte)(value.Value >> 24),
      (byte)(value.Value >> 16),
      (byte)(value.Value >> 8),
      (byte)value.Value)
    : null;
}
