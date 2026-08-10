namespace Ask.Device.ResponseProcessor.BreakdownTester.ResponseModels;

/// <summary>
/// Содержит результат измерения пробойной установки.
/// </summary>
public sealed record BreakdownMeasurementResponse(string Status, double Value, string Unit);
