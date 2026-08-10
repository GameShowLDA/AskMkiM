using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Protocol.Messages.EntryPoints;

namespace Ask.Device.ResponseProcessor.BreakdownTester.ResponseProcessing;

/// <summary>
/// Централизует сообщения протокола пробойной установки.
/// </summary>
public static class BreakdownTesterMessages
{
  public static Task PublishOperationResultAsync(
    IBreakdownTester device,
    string operation,
    bool result,
    int indentLevel,
    IUserInteractionService? outputService = null,
    bool isStepCheckpoint = false)
    => DeviceMessages.PublishOperationResultAsync(
      device, operation, result, indentLevel, outputService, isStepCheckpoint);

  public static Task PublishOperationResultAsync(
    IBreakdownTester device,
    string operation,
    string? detail,
    bool result,
    int indentLevel,
    IUserInteractionService? outputService = null,
    bool isStepCheckpoint = false)
    => DeviceMessages.PublishOperationResultAsync(
      device, operation, detail, result, indentLevel, outputService, isStepCheckpoint);

  public static Task PublishDeviceHealthCheckTitleAsync(
    IBreakdownTester device,
    IUserInteractionService outputService)
    => EquipmentMessages.PublishDeviceHealthCheckTitleAsync(device, outputService);

  public static Task PublishInformationAsync(
    string header,
    IUserInteractionService outputService,
    string? message = null,
    int indentLevel = 0)
    => SelfTestMessages.PublishInformationAsync(header, outputService, message, indentLevel);

  public static Task PublishResultAsync(
    string header,
    bool result,
    IUserInteractionService outputService,
    string? message = null,
    int indentLevel = 0,
    string? executionErrorMessage = null)
    => SelfTestMessages.PublishResultAsync(
      header, result, outputService, message, indentLevel,
      executionErrorMessage: executionErrorMessage);

  public static Task PublishMeasurementResultAsync(
    CheckType checkType,
    Enum measurementUnit,
    MeasurementRange measurementRange,
    bool result,
    string? measurementTarget,
    string? executionErrorMessage,
    IUserInteractionService outputService)
    => MeasurementMessages.PublishResultAsync(
      checkType,
      measurementUnit,
      measurementRange,
      result,
      measurementTarget,
      executionErrorMessage,
      outputService);

  public static Task PublishMeasurementErrorAsync(
    CheckType checkType,
    Enum measurementUnit,
    MeasurementRange measurementRange,
    bool result,
    IUserInteractionService outputService,
    bool showAllowedRange = false,
    int indentLevel = 2,
    string? executionErrorMessage = null)
    => MeasurementMessages.PublishErrorAsync(
      checkType,
      measurementUnit,
      measurementRange,
      result,
      outputService,
      showAllowedRange,
      indentLevel,
      executionErrorMessage);
}
