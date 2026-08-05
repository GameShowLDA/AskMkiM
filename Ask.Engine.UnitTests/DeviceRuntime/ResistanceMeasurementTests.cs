using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device;

namespace Ask.Engine.UnitTests.DeviceRuntime;

public class ResistanceMeasurementTests
{
  [Fact]
  public async Task MeasureResistanceAsync_TwoCorrectAndOneFalse_ReturnsAverageOfCorrectMeasurements()
  {
    var (meter, protocol) = CreateMeter(100d, 1_000d, 110d);

    double result = await meter.ResistanceManager.MeasureResistanceAsync(
      new MeasurementRange(100d, 90d, 120d));

    Assert.Equal(105d, result);
    Assert.Equal(3, protocol.MeasurementCount);
  }

  [Fact]
  public async Task MeasureResistanceAsync_AllThreeCorrect_UsesAllMeasurements()
  {
    var (meter, protocol) = CreateMeter(90d, 100d, 110d);

    double result = await meter.ResistanceManager.MeasureResistanceAsync(
      new MeasurementRange(100d, 90d, 110d));

    Assert.Equal(100d, result);
    Assert.Equal(3, protocol.MeasurementCount);
  }

  [Fact]
  public async Task MeasureResistanceAsync_OnlyOneCorrect_ReturnsValueOutsideRange()
  {
    var (meter, protocol) = CreateMeter(100d, 1_000d, 2_000d);
    var range = new MeasurementRange(100d, 90d, 110d);

    double result = await meter.ResistanceManager.MeasureResistanceAsync(range);

    Assert.False(result >= range.LowerBound && result <= range.UpperBound);
    Assert.Equal(3, protocol.MeasurementCount);
  }

  [Fact]
  public async Task MeasureResistanceAsync_CustomCounts_UsesTheirSumAndRequiredCorrectCount()
  {
    var (meter, protocol) = CreateMeter(95d, 100d, 1_000d, 105d);

    double result = await meter.ResistanceManager.MeasureResistanceAsync(
      new MeasurementRange(100d, 90d, 110d),
      userMessageService: null,
      correctMeasurementCount: 3,
      falseMeasurementCount: 1);

    Assert.Equal(100d, result);
    Assert.Equal(4, protocol.MeasurementCount);
  }

  [Fact]
  public async Task MeasureResistanceAsync_ResponseDelay_AppliesToEveryMeasurement()
  {
    var (meter, protocol) = CreateMeter(95d, 100d, 105d);

    await meter.ResistanceManager.MeasureResistanceAsync(
      new MeasurementRange(100d, 90d, 110d),
      responseDelay: 2_000d);

    Assert.Equal(new[] { 2_000d, 2_000d, 2_000d }, protocol.MeasurementResponseDelays);
  }

  [Theory]
  [InlineData(0, 1, "correctMeasurementCount")]
  [InlineData(2, -1, "falseMeasurementCount")]
  public async Task MeasureResistanceAsync_InvalidCounts_Throws(
    int correctMeasurementCount,
    int falseMeasurementCount,
    string parameterName)
  {
    var (meter, _) = CreateMeter();

    var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
      meter.ResistanceManager.MeasureResistanceAsync(
        new MeasurementRange(100d, 90d, 110d),
        userMessageService: null,
        correctMeasurementCount,
        falseMeasurementCount));

    Assert.Equal(parameterName, exception.ParamName);
  }

  private static (KeysightDevice Meter, ResistanceProtocolStub Protocol) CreateMeter(params double[] measurements)
  {
    var protocol = new ResistanceProtocolStub(measurements);
    var meter = new KeysightDevice
    {
      TypeMode = MultimeterTypeMode.Resistance,
      DeviceProtocol = protocol
    };
    meter.ConnectionInfo.IsConnected = true;

    return (meter, protocol);
  }

  private sealed class ResistanceProtocolStub : IDeviceProtocol
  {
    private readonly Queue<string> _measurements;

    public ResistanceProtocolStub(IEnumerable<double> measurements)
    {
      _measurements = new Queue<string>(
        measurements.Select(value => value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    }

    public int MeasurementCount { get; private set; }

    public List<double> MeasurementResponseDelays { get; } = new();

    public SemaphoreSlim OperationLock { get; set; } = new(1, 1);

    public Task<string> QueryAsync(
      string command,
      double responseDelay = 0,
      int timeout = 0,
      int port = 0,
      int delayBeforeCall = 0,
      CancellationToken cancellationToken = default)
    {
      if (command == "MEAS:RES?")
      {
        MeasurementCount++;
        MeasurementResponseDelays.Add(responseDelay);
        return Task.FromResult(_measurements.Dequeue());
      }

      return Task.FromResult(string.Empty);
    }
  }
}
