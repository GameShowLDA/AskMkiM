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

    public Task<string> QueryAsync(string command, double responseDelay = 0, int timeout = 0,
      int port = 0, int delayBeforeCall = 0, CancellationToken cancellationToken = default)
    {
      if (command == "MEAS ?")
      {
        MeasurementQueries++;
        return Task.FromResult(response);
      }

      Assert.Equal("FUNC:TEST ON", command);
      return Task.FromResult(string.Empty);
    }

    public void Dispose() => OperationLock.Dispose();
  }

  private sealed class StubTimeManager : ITimeManager
  {
    public Task<(bool Success, string Message)> SetTestTimeAsync(double value, IUserInteractionService? userMessageService = null)
      => Task.FromResult((true, string.Empty));
    public Task<double> GetTestTimeAsync() => Task.FromResult(3d);
    public Task<(bool Success, string Message)> SetRampTimeAsync(double value, IUserInteractionService? userMessageService = null)
      => Task.FromResult((true, string.Empty));
    public Task<double> GetRampTimeAsync() => Task.FromResult(1d);
    public void SetTargetTime(double time) { }
    public double GetTargetTime() => 3;
  }
}
