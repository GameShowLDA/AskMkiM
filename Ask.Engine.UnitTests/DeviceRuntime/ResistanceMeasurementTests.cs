using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device;

namespace Ask.Engine.UnitTests.DeviceRuntime;

public class ResistanceMeasurementTests
{
  [Fact]
  public async Task MeasureResistanceAsync_ReturnsSingleMeasurement()
  {
    var (meter, protocol) = CreateMeter(100d, 200d, 300d);

    double result = await meter.ResistanceManager.MeasureResistanceAsync(
      new MeasurementRange(100d, 90d, 110d));

    Assert.Equal(100d, result);
    Assert.Equal(1, protocol.MeasurementCount);
  }

  [Fact]
  public async Task MeasureResistanceAsync_FirstMeasurementOutsideRange_ReturnsSecondMeasurement()
  {
    var (meter, protocol) = CreateMeter(1_000d, 105d);

    double result = await meter.ResistanceManager.MeasureResistanceAsync(
      new MeasurementRange(100d, 90d, 110d));

    Assert.Equal(105d, result);
    Assert.Equal(2, protocol.MeasurementCount);
  }

  [Fact]
  public async Task MeasureResistanceAsync_BothMeasurementsOutsideRange_ReturnsSecondMeasurement()
  {
    var (meter, protocol) = CreateMeter(1_000d, 2_000d);

    double result = await meter.ResistanceManager.MeasureResistanceAsync(
      new MeasurementRange(100d, 90d, 110d));

    Assert.Equal(2_000d, result);
    Assert.Equal(2, protocol.MeasurementCount);
  }

  [Fact]
  public async Task MeasureResistanceAsync_ResponseDelay_AppliesToMeasurement()
  {
    var (meter, protocol) = CreateMeter(100d);

    await meter.ResistanceManager.MeasureResistanceAsync(
      new MeasurementRange(100d, 90d, 110d),
      responseDelay: 2_000d);

    Assert.Equal(new[] { 2_000d }, protocol.MeasurementResponseDelays);
  }

  private static (KeysightDevice Meter, ResistanceProtocolStub Protocol) CreateMeter(params double[] measurements)
  {
    var protocol = new ResistanceProtocolStub(measurements);
    var meter = new KeysightDevice
    {
      TypeMode = MultimeterTypeMode.Resistance,
      DeviceProtocol = protocol,
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
