using Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseModels;

namespace Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseProcessing.Checkers;

/// <summary>
/// Проверяет подтверждение обычной команды МКР.
/// </summary>
internal static class CommandResponseChecker
{
  internal static bool Check(
    string response,
    int chassisNumber,
    int moduleNumber,
    string expectedAnswer,
    bool requireHardwareVerification = false)
  {
    RelayVerificationResponse? model = ResponseDeserializer.Deserialize<RelayVerificationResponse>(response);
    return model != null &&
      ModuleResponseIdentityChecker.Check(model, chassisNumber, moduleNumber) &&
      string.Equals(model.Answer, expectedAnswer, StringComparison.Ordinal) &&
      (!requireHardwareVerification || model.Checked);
  }
}
