using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.ResponseProcessor.BreakdownTester.ResponseProcessing.Checkers;
using Ask.Protocol.Messages.EntryPoints;

namespace Ask.Device.ResponseProcessor.BreakdownTester.ResponseProcessing;

/// <summary>
/// Предоставляет единую точку входа для обработки ответов пробойной установки.
/// </summary>
public static class BreakdownTesterResponseProcessor
{
  public static bool CheckInitialization(string response)
    => !string.IsNullOrWhiteSpace(response);

  public static bool CheckInitialization(string response, string expectedIdentifier)
    => CheckInitialization(response)
      && response.Contains(expectedIdentifier, StringComparison.OrdinalIgnoreCase);

  public static bool CheckMode(string response, string expectedMode)
    => ModeResponseChecker.Check(response, expectedMode);

  public static bool TryParseNumber(string response, out double value)
    => NumericResponseChecker.TryParse(response, out value);

  public static bool TryParseState(string response, out bool state)
    => StateResponseChecker.TryParse(response, out state);

  public static bool TryParseMeasurement(
    string response,
    out BreakdownMeasurementResponse? result)
    => MeasurementResponseChecker.TryParse(response, out result);

  public static bool IsTestInProgress(string response)
    => response.Contains("TEST", StringComparison.OrdinalIgnoreCase)
      && !IsTestStopped(response);

  public static bool IsTestFailed(string response)
    => response.Contains("FAIL", StringComparison.OrdinalIgnoreCase);

  public static bool IsTestStopped(string response)
    => response.Contains("TEST OFF", StringComparison.OrdinalIgnoreCase);

  public static Task PublishConnectionResultAsync(
    IBreakdownTester device,
    bool result,
    string? error,
    IUserInteractionService? outputService = null)
    => EquipmentMessages.PublishConnectionResultAsync(device, result, error, outputService);

  public static Task PublishDisconnectionResultAsync(
    IBreakdownTester device,
    bool result,
    IUserInteractionService? outputService = null)
    => EquipmentMessages.PublishDisconnectionResultAsync(device, result, outputService: outputService);

  public static Task PublishInitializationResultAsync(
    IBreakdownTester device,
    bool result,
    string? error,
    IUserInteractionService? outputService = null)
    => EquipmentMessages.PublishInitializationResultAsync(device, result, error, outputService);

  public static Task PublishResetResultAsync(
    IBreakdownTester device,
    bool result,
    IUserInteractionService? outputService = null)
    => EquipmentMessages.PublishResetResultAsync(device, result, outputService: outputService);
}
