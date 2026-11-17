using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Mode;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device;
using Ask.Device.Runtime.Function.GPT.Managment;

namespace Ask.Device.Emulator.UnitTests.BreakdownTester;

public sealed class MeasureManagmentTests
{
  [Theory]
  [InlineData(BreakdownTypeMode.ACW, ElectricalTestFunction.DielectricWithstandAC)]
  [InlineData(BreakdownTypeMode.DCW, ElectricalTestFunction.DielectricWithstandDC)]
  [InlineData(BreakdownTypeMode.IR, ElectricalTestFunction.InsulationResistance)]
  public async Task MeasureAsync_RestoresSelectedTimeAcrossOneHundredRuns(
    BreakdownTypeMode mode, ElectricalTestFunction function)
  {
    using var executionMode = new TestExecutionMode(idleMode: false);
    using var protocol = new MeasurementProtocol($"{mode},PASS ,0.016kV,002.6004 mA ,T=000.4S");
    var time = new StubTimeManager();
    var device = new GPT79904 { Mode = mode, DeviceProtocol = protocol, Time = time };
    var manager = new MeasureManagment(device, 0, time.GetTestTimeAsync, time.GetRampTimeAsync, false);
    var random = new Random(1020);
    for (int run = 0; run < 100; run++)
    {
      double selected = run == 0 ? 60 : Math.Round(0.1 + random.NextDouble() * 59.9, 1);
      time.SetTargetTime(selected);
      await time.SetTestTimeAsync(selected);
      time.SentTimes.Clear();
      await manager.MeasureAsync(function, new MeasurementRange(0, 0, 50), waitFullTime: true);
      Assert.Equal(new[] { selected }, time.SentTimes);
      Assert.Equal(selected, time.GetTargetTime());
    }
    Assert.Equal(100, protocol.MeasurementQueries);
  }

  [Fact]
  public async Task IrPreliminaryFailure_RestoresOriginalTimeForFullMeasurement()
  {
    using var executionMode = new TestExecutionMode(idleMode: false);
    using var protocol = new MeasurementProtocol("IR,PASS ,0.016kV,002.6004 mA ,T=000.4S")
    { FirstResponse = "IR,FAIL ,0.016kV,002.6004 mA ,T=000.4S" };
    var time = new StubTimeManager();
    time.SetTargetTime(17.5);
    await time.SetTestTimeAsync(17.5);
    time.SentTimes.Clear();
    var device = new GPT79904 { Mode = BreakdownTypeMode.IR, DeviceProtocol = protocol, Time = time };
    var manager = new MeasureManagment(device, 0, time.GetTestTimeAsync, time.GetRampTimeAsync, false);
    await manager.MeasureAsync(ElectricalTestFunction.InsulationResistance, new MeasurementRange(0, 0, 50));
    Assert.Equal(new[] { 1d, 17.5d }, time.SentTimes);
    Assert.Equal(17.5, time.GetTargetTime());
    Assert.Equal(2, protocol.MeasurementQueries);
  }

  [Theory]
  [InlineData(BreakdownTypeMode.ACW, ElectricalTestFunction.DielectricWithstandAC, "FAIL", BreakdownMeasurementStatus.Fail)]
  [InlineData(BreakdownTypeMode.DCW, ElectricalTestFunction.DielectricWithstandDC, "FAIL", BreakdownMeasurementStatus.Fail)]
  [InlineData(BreakdownTypeMode.ACW, ElectricalTestFunction.DielectricWithstandAC, "PASS", BreakdownMeasurementStatus.Pass)]
  [InlineData(BreakdownTypeMode.DCW, ElectricalTestFunction.DielectricWithstandDC, "PASS", BreakdownMeasurementStatus.Pass)]
  public async Task MeasureAsync_PreservesDeviceStatusWhenRounding(
    BreakdownTypeMode mode, ElectricalTestFunction function, string status, BreakdownMeasurementStatus expected)
  {
    using var executionMode = new TestExecutionMode(idleMode: false);
    using var protocol = new MeasurementProtocol($"{mode},{status} ,0.016kV,002.6004 mA ,T=000.4S");
    var device = new GPT79904 { Mode = mode, DeviceProtocol = protocol, Time = new StubTimeManager() };
    var manager = new MeasureManagment(device, 0, () => Task.FromResult(3d), () => Task.FromResult(1d), false);

    var result = await manager.MeasureAsync(function, new MeasurementRange(0, 0, 50));

    Assert.Equal(expected, result.Status);
    Assert.Equal(2.6, result.Value);
    Assert.Equal("mA", result.Unit);
    Assert.Equal(1, protocol.MeasurementQueries);
  }

  private sealed class MeasurementProtocol(string response) : IDeviceProtocol, IDisposable
  {
    public SemaphoreSlim OperationLock { get; set; } = new(1, 1);
    public int MeasurementQueries { get; private set; }
    public string? FirstResponse { get; init; }

    public Task<string> QueryAsync(string command, double responseDelay = 0, int timeout = 0,
      int port = 0, int delayBeforeCall = 0, CancellationToken cancellationToken = default)
    {
      if (command == "MEAS ?")
      {
        MeasurementQueries++;
        return Task.FromResult(MeasurementQueries == 1 ? FirstResponse ?? response : response);
      }

      if (FirstResponse != null && command == "MANU:IR:RLOS 1M")
        return Task.FromResult(string.Empty);
      if (FirstResponse != null && command == "MANU:IR:RLOS ?")
        return Task.FromResult("1M");

      Assert.Equal("FUNC:TEST ON", command);
      return Task.FromResult(string.Empty);
    }

    public void Dispose() => OperationLock.Dispose();
  }

  private sealed class StubTimeManager : ITimeManager
  {
    private double target = 3;
    private double current = 3;
    public List<double> SentTimes { get; } = [];
    public Task<(bool Success, string Message)> SetTestTimeAsync(double value, IUserInteractionService? userMessageService = null)
    {
      SentTimes.Add(value);
      current = value;
      return Task.FromResult((true, string.Empty));
    }
    public Task<double> GetTestTimeAsync() => Task.FromResult(current);
    public Task<(bool Success, string Message)> SetRampTimeAsync(double value, IUserInteractionService? userMessageService = null)
      => Task.FromResult((true, string.Empty));
    public Task<double> GetRampTimeAsync() => Task.FromResult(1d);
    public void SetTargetTime(double time) => target = time;
    public double GetTargetTime() => target;
  }
}
