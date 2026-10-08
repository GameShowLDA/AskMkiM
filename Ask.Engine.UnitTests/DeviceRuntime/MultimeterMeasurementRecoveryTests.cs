using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Device.Runtime.Device;
using Ask.Device.Runtime.Function.Base.Multimeter.Measurements.Common;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Moq;

namespace Ask.Engine.UnitTests.DeviceRuntime;

public sealed class MultimeterMeasurementRecoveryTests
{
  [Fact]
  public async Task Capacitance_CancellationStopsRecovery()
  {
    bool idle = ExecutionConfig.GetIsIdleModeEnabled();
    try
    {
      ExecutionConfig.SetIdleMode(false);
      using var cancellation = new CancellationTokenSource(200);
      var device = new KeysightDevice { TypeMode = MultimeterTypeMode.Capacitance };
      device.ConnectionInfo.IsConnected = true;
      using var protocol = new RecoveryProtocol(device, device.CapacitanceCommands.Measure, int.MaxValue, measurementTimeout: device.CapacitanceCommands.Timeout) { BlockRead = true };
      device.DeviceProtocol = protocol;
      await Assert.ThrowsAnyAsync<OperationCanceledException>(() => MeasurementBase.MeasureAsync(device,
        device.CapacitanceCommands, new MeasurementRange(1, -1, -1), cancellationToken: cancellation.Token));
      Assert.Equal(1, protocol.Reads);
      Assert.Equal(0, protocol.Probes);
    }
    finally { ExecutionConfig.SetIdleMode(idle); }
  }

  [Fact]
  public async Task BooleanContinuity_UsesRecoveryAndKeepsInitializationHidden()
  {
    bool idle = ExecutionConfig.GetIsIdleModeEnabled();
    bool visible = DeviceDisplayConfig.GetExecutionParametersVisibility();
    try
    {
      ExecutionConfig.SetIdleMode(false);
      DeviceDisplayConfig.SetExecutionParametersVisibility(true);
      var device = new KeysightDevice { TypeMode = MultimeterTypeMode.Continuity };
      device.ConnectionInfo.IsConnected = true;
      using var protocol = new RecoveryProtocol(device, device.ContinuityCommands.Measure, 4, measurementTimeout: device.ContinuityCommands.Timeout);
      device.DeviceProtocol = protocol;
      var output = new Mock<IUserInteractionService>();
      Assert.True(await device.ContinuityManager.CheckContinuityAsync(true, output.Object));
      Assert.Equal(4, protocol.Reads);
      Assert.Equal(1, protocol.Probes);
      var messages = output.Invocations.Where(x => x.Method.Name == "ShowMessageAsync")
        .Select(x => (ShowMessageModel)x.Arguments[0]).ToArray();
      Assert.Contains(messages, x => x.Message.EndsWith("(Попытка 4/4)"));
      Assert.DoesNotContain(messages, x => x.Message.Contains("Инициализация"));
      Assert.DoesNotContain(protocol.AllCommands, x => x.Contains("BEEP"));
    }
    finally
    {
      ExecutionConfig.SetIdleMode(idle);
      DeviceDisplayConfig.SetExecutionParametersVisibility(visible);
    }
  }

  [Fact]
  public async Task IdleMeasurement_DoesNotQueryRealProtocolOrProbe()
  {
    bool idle = ExecutionConfig.GetIsIdleModeEnabled();
    try
    {
      ExecutionConfig.SetIdleMode(true);
      var device = new KeysightDevice();
      var protocol = new Mock<IDeviceProtocol>(MockBehavior.Strict);
      device.DeviceProtocol = protocol.Object;
      var result = await MultimeterMeasurementQuery.QueryAsync(device, "MEAS:RES?", "123", "Сопротивление", 1000);
      Assert.Equal("123", result.Response);
      Assert.Equal(1, result.Attempt);
      protocol.VerifyNoOtherCalls();
    }
    finally { ExecutionConfig.SetIdleMode(idle); }
  }

  [Theory]
  [InlineData("AC", false)]
  [InlineData("DC", false)]
  [InlineData("RES", false)]
  [InlineData("CAP", false)]
  [InlineData("CONT", false)]
  [InlineData("DIOD", false)]
  [InlineData("AC", true)]
  [InlineData("DC", true)]
  [InlineData("RES", true)]
  [InlineData("CAP", true)]
  [InlineData("CONT", true)]
  [InlineData("DIOD", true)]
  public async Task EveryMode_TwoFailuresProbeOnceThenTwoReads(string mode, bool usb)
  {
    bool idle = ExecutionConfig.GetIsIdleModeEnabled();
    try
    {
      ExecutionConfig.SetIdleMode(false);
      IMultimeter device = usb ? new MultimeterB7783() : new KeysightDevice();
      IMeasurementProfile[] profiles = device switch
      {
        KeysightDevice m => [m.ACVCommands, m.DCVCommands, m.ResistanceCommands, m.CapacitanceCommands, m.ContinuityCommands, m.DiodeCommands],
        MultimeterB7783 m => [m.ACVCommands, m.DCVCommands, m.ResistanceCommands, m.CapacitanceCommands, m.ContinuityCommands, m.DiodeCommands],
        _ => throw new InvalidOperationException(),
      };
      var profile = profiles[Array.IndexOf(new[] { "AC", "DC", "RES", "CAP", "CONT", "DIOD" }, mode)];
      device.TypeMode = profile.TypeMode;
      device.ConnectionInfo.IsConnected = true;
      using var protocol = new RecoveryProtocol(device, profile.Measure, successOnRead: 4, measurementTimeout: profile.Timeout);
      device.DeviceProtocol = protocol;
      double result = await MeasurementBase.MeasureAsync(device, profile, new MeasurementRange(1, -1, -1));
      Assert.True(result > 0);
      Assert.Equal(new[] { profile.Measure, profile.Measure, "*IDN?", profile.Measure, profile.Measure }, protocol.Exchanges);
      Assert.Equal(4, protocol.Reads);
      Assert.Equal(1, protocol.Probes);
    }
    finally { ExecutionConfig.SetIdleMode(idle); }
  }

  [Theory]
  [InlineData(1)]
  [InlineData(2)]
  [InlineData(3)]
  [InlineData(4)]
  public async Task Query_StopsImmediatelyOnSuccessfulRead(int successOnRead)
  {
    bool idle = ExecutionConfig.GetIsIdleModeEnabled();
    try
    {
      ExecutionConfig.SetIdleMode(false);
      var device = new KeysightDevice();
      using var protocol = new RecoveryProtocol(device, "MEAS:RES?", successOnRead);
      device.DeviceProtocol = protocol;
      var result = await MultimeterMeasurementQuery.QueryAsync(device, "MEAS:RES?", "1", "Сопротивление", 1000);
      Assert.Equal(successOnRead, protocol.Reads);
      Assert.Equal(successOnRead > 2 ? 1 : 0, protocol.Probes);
      Assert.Equal(successOnRead, result.Attempt);
      Assert.Equal(successOnRead > 2 ? 4 : 2, result.MaxAttempts);
    }
    finally { ExecutionConfig.SetIdleMode(idle); }
  }

  [Theory]
  [InlineData("timeout", 2)]
  [InlineData("empty", 2)]
  [InlineData("success", 4)]
  public async Task Query_FailedRecoveryCannotStartAnotherRecovery(string probeOutcome, int expectedReads)
  {
    bool idle = ExecutionConfig.GetIsIdleModeEnabled();
    try
    {
      ExecutionConfig.SetIdleMode(false);
      var device = new KeysightDevice();
      using var protocol = new RecoveryProtocol(device, "MEAS:RES?", int.MaxValue, probeOutcome);
      device.DeviceProtocol = protocol;
      var error = await Assert.ThrowsAsync<DeviceNoResponseException>(() =>
        MultimeterMeasurementQuery.QueryAsync(device, "MEAS:RES?", "1", "Сопротивление", 1000));
      Assert.Equal(expectedReads, protocol.Reads);
      Assert.Equal(1, protocol.Probes);
      Assert.Contains(expectedReads == 4 ? "Попытка 4/4" : "Попытка 2/2", error.Operation);
    }
    finally { ExecutionConfig.SetIdleMode(idle); }
  }

  [Fact]
  public async Task Query_CancellationDuringReadPreventsProbeAndNextRead()
  {
    bool idle = ExecutionConfig.GetIsIdleModeEnabled();
    try
    {
      ExecutionConfig.SetIdleMode(false);
      using var cancellation = new CancellationTokenSource(200);
      var device = new KeysightDevice();
      using var protocol = new RecoveryProtocol(device, "MEAS:RES?", int.MaxValue) { BlockRead = true };
      device.DeviceProtocol = protocol;
      await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
        MultimeterMeasurementQuery.QueryAsync(device, "MEAS:RES?", "1", "Сопротивление", 1000, cancellationToken: cancellation.Token));
      Assert.Equal(1, protocol.Reads);
      Assert.Equal(0, protocol.Probes);
    }
    finally { ExecutionConfig.SetIdleMode(idle); }
  }

  private sealed class RecoveryProtocol(IMultimeter device, string measurement, int successOnRead,
    string probeOutcome = "success", int measurementTimeout = 1000) : IDeviceProtocol, IDisposable
  {
    public SemaphoreSlim OperationLock { get; set; } = new(1, 1);
    public List<string> Exchanges { get; } = [];
    public List<string> AllCommands { get; } = [];
    public bool BlockRead { get; init; }
    public int Reads { get; private set; }
    public int Probes { get; private set; }

    public Task<string> QueryAsync(string command, double responseDelay = 0, int timeout = 0,
      int port = 0, int delayBeforeCall = 0, CancellationToken cancellationToken = default)
    {
      AllCommands.Add(command);
      if (command == "SYSTEM:ERROR?" || command == "SYST:ERR?")
        return Task.FromResult("+0,\"No error\"");
      if (command == measurement || command == "*IDN?")
      {
        Assert.Equal(command == "*IDN?" ? 1000 : measurementTimeout, timeout);
        Exchanges.Add(command);
        if (command == "*IDN?")
        {
          Probes++;
          Assert.Equal(2, Reads);
          Assert.Equal(1, Probes);
          return probeOutcome switch
          {
            "timeout" => Task.FromException<string>(new DeviceNoResponseException(device, command, timeout)),
            "empty" => Task.FromResult(string.Empty),
            _ => Task.FromResult("Keysight Technologies,34465A,TEST,1.0"),
          };
        }
        Reads++;
        if (BlockRead) return WaitForCancellationAsync(cancellationToken);
        return Reads < successOnRead
          ? Task.FromException<string>(new DeviceNoResponseException(device, command, timeout))
          : Task.FromResult("1");
      }
      return Task.FromResult(string.Empty);
    }

    private static async Task<string> WaitForCancellationAsync(CancellationToken cancellationToken)
    {
      await Task.Delay(Timeout.Infinite, cancellationToken);
      return string.Empty;
    }

    public void Dispose() => OperationLock.Dispose();
  }
}
