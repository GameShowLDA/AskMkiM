using Ask.Device.ResponseProcessor.Multimeter.ResponseProcessing;
using Ask.Device.ResponseProcessor.Multimeter.ResponseModels;

namespace Ask.Device.Emulator.UnitTests.Multimeter;

public sealed class MultimeterResponseProcessorTests
{
  [Theory(DisplayName = "Мультиметр: измерительное значение разбирается в форматах приборов")]
  [InlineData("+1.25000000E+01", 12.5)]
  [InlineData("-2,5 V", -2.5)]
  [InlineData("10.75 OHM", 10.75)]
  public void MeasurementResponse_ValidNumber_ReturnsValue(string response, double expected)
  {
    bool result = MultimeterResponseProcessor.TryParseMeasurement(response, out var measurement);

    Assert.True(result);
    Assert.Equal(expected, measurement!.Value);
  }

  [Theory(DisplayName = "Мультиметр: некорректное измерение отклоняется")]
  [InlineData("")]
  [InlineData("No value")]
  public void MeasurementResponse_InvalidValue_ReturnsFalse(string response)
    => Assert.False(MultimeterResponseProcessor.TryParseMeasurement(response, out _));

  [Theory(DisplayName = "Мультиметр: все поддерживаемые ответы перегрузки распознаются как отдельное состояние")]
  [InlineData("+9.90000000E+37")]
  [InlineData("9.9E37 OHM")]
  [InlineData("OL")]
  [InlineData("OVL")]
  [InlineData("OVLD")]
  [InlineData("OVLOAD")]
  [InlineData("OVERLOAD")]
  [InlineData("\"overload\"")]
  public void MeasurementResponse_Overload_ReturnsOverloadState(string response)
  {
    bool parsed = MultimeterResponseProcessor.TryParseMeasurement(response, out var measurement);

    Assert.True(parsed);
    Assert.Equal(MeasurementState.Overload, measurement!.State);
    Assert.True(double.IsPositiveInfinity(measurement.Value));
  }

  [Theory(DisplayName = "Мультиметр: текущий режим проверяется без учёта регистра")]
  [InlineData("\"VOLT:AC\"", "VOLT:AC", true)]
  [InlineData("CONF:RES", "res", true)]
  [InlineData("CONF:VOLT:DC", "VOLT:AC", false)]
  public void ModeResponse_ReturnsExpectedResult(string response, string mode, bool expected)
    => Assert.Equal(expected, MultimeterResponseProcessor.CheckMode(response, mode));

  [Theory(DisplayName = "Мультиметр: системная ошибка проверяется по числовому коду")]
  [InlineData("+0,\"No error\"", true)]
  [InlineData("0", true)]
  [InlineData("-113,\"Undefined header\"", false)]
  [InlineData("invalid", false)]
  public void InstrumentError_ReturnsExpectedResult(string response, bool expected)
    => Assert.Equal(expected, MultimeterResponseProcessor.CheckNoInstrumentError(response, out _));

  [Theory(DisplayName = "Мультиметр: ответ прозвонки сопоставляется с ожидаемым состоянием цепи")]
  [InlineData("+1.00000000E+00", true, true)]
  [InlineData("+9.90000000E+37", false, true)]
  [InlineData("+9.90000000E+37", true, false)]
  public void ContinuityResponse_ReturnsExpectedResult(
    string response,
    bool expectedClosed,
    bool expectedResult)
  {
    Assert.True(MultimeterResponseProcessor.TryCheckContinuity(
      response, expectedClosed, out bool matchesExpected));
    Assert.Equal(expectedResult, matchesExpected);
  }
}
