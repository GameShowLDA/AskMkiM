using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device.RelaySwitchModule;
using System.Diagnostics;

namespace TestConsole.ModuleRelayControlTests
{
  public sealed class ModuleRelayControlController
  {
    private const int DefaultTimeoutMs = 5000;
    private readonly ModuleRelayControl _device;
    private readonly Action<string> _log;

    public ModuleRelayControlController(ModuleRelayControl? device = null, Action<string>? log = null)
    {
      _device = device ?? new ModuleRelayControl();
      _log = log ?? Console.WriteLine;
    }

    public string Name => _device.Name;
    public string ConnectionDetails
    {
      get => _device.ConnectionDetails;
      set => _device.ConnectionDetails = value;
    }

    public int NumberChassis
    {
      get => _device.NumberChassis;
      set => _device.NumberChassis = value;
    }

    public int Number
    {
      get => _device.Number;
      set => _device.Number = value;
    }

    public int PointCount
    {
      get => _device.PointCount;
      set => _device.PointCount = value;
    }

    public IConnectionInfo ConnectionInfo => _device.ConnectionInfo;


    public async Task<ModuleRelayControlCommandResult> ConnectAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      EnsureProtocolConfigured();

      return await RunTimedAsync(
        "CONNECT",
        timeoutMs,
        async token =>
        {
          var result = await _device.ConnectableManager.ConnectAsync();
          token.ThrowIfCancellationRequested();
          _device.ConnectionInfo.IsConnected = result.Connect;
          return result.Connect ? result.Answer : throw new InvalidOperationException(result.Answer);
        },
        cancellationToken);
    }

    public async Task<ModuleRelayControlCommandResult> InitializeAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      EnsureProtocolConfigured();

      return await RunTimedAsync(
        "INIT",
        timeoutMs,
        async token =>
        {
          var result = await _device.ConnectableManager.InitializeAsync();
          token.ThrowIfCancellationRequested();
          _device.ConnectionInfo.IsConnected = result.Connect;
          return result.Connect ? result.Answer : throw new InvalidOperationException(result.Answer);
        },
        cancellationToken);
    }

    public async Task<ModuleRelayControlCommandResult> ResetAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      EnsureProtocolConfigured();

      return await RunTimedAsync(
        "RESET",
        timeoutMs,
        async token =>
        {
          bool result = await _device.ConnectableManager.ResetAsync();
          token.ThrowIfCancellationRequested();
          if (result)
          {
            _device.ConnectionInfo.IsConnected = false;
          }

          return result.ToString();
        },
        cancellationToken);
    }

    public async Task<ModuleRelayControlCommandResult> DisconnectAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      EnsureProtocolConfigured();

      return await RunTimedAsync(
        "DISCONNECT",
        timeoutMs,
        async token =>
        {
          bool result = await _device.ConnectableManager.DisconnectAsync();
          token.ThrowIfCancellationRequested();
          _device.ConnectionInfo.IsConnected = false;
          return result.ToString();
        },
        cancellationToken);
    }

    public Task<ModuleRelayControlCommandResult> ConnectBusAsync(SwitchingBus bus, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync($"CONNECT BUS {bus}", timeoutMs, _ => _device.BusManager.ConnectBusAsync(bus), cancellationToken);
    }

    public Task<ModuleRelayControlCommandResult> DisconnectBusAsync(SwitchingBus bus, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync($"DISCONNECT BUS {bus}", timeoutMs, _ => _device.BusManager.DisconnectBusAsync(bus), cancellationToken);
    }

    public Task<ModuleRelayControlCommandResult> ConnectMeterAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync("CONNECT METER", timeoutMs, _ => _device.MeterManager.ConnectMeterAsync(), cancellationToken);
    }

    public Task<ModuleRelayControlCommandResult> DisconnectMeterAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync("DISCONNECT METER", timeoutMs, _ => _device.MeterManager.DisconnectMeterAsync(), cancellationToken);
    }

    public Task<ModuleRelayControlCommandResult> GetMeterResponseAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync("GET METER RESPONSE", timeoutMs, _ => _device.MeterManager.GetMeterResponseAsync(), cancellationToken);
    }

    public Task<ModuleRelayControlCommandResult> ConnectRelayAsync(BusPoint bus, int point, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync($"CONNECT RELAY {point} TO {bus}", timeoutMs, _ => _device.PointManager.ConnectRelayAsync(bus, point), cancellationToken);
    }

    public Task<ModuleRelayControlCommandResult> DisconnectRelayAsync(BusPoint bus, int point, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync($"DISCONNECT RELAY {point} FROM {bus}", timeoutMs, _ => _device.PointManager.DisconnectRelayAsync(bus, point), cancellationToken);
    }

    public Task<ModuleRelayControlCommandResult> ConnectRelayVerifiedAsync(BusPoint bus, int point, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync($"CONNECT RELAY VERIFIED {point} TO {bus}", timeoutMs, _ => _device.PointManager.ConnectRelayVerifiedAsync(bus, point), cancellationToken);
    }

    public Task<ModuleRelayControlCommandResult> DisconnectRelayVerifiedAsync(BusPoint bus, int point, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync($"DISCONNECT RELAY VERIFIED {point} FROM {bus}", timeoutMs, _ => _device.PointManager.DisconnectRelayVerifiedAsync(bus, point), cancellationToken);
    }

    public Task<ModuleRelayControlCommandResult> ConnectRelayGroupAsync(BusPoint bus, int firstPoint, int lastPoint, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync($"CONNECT RELAY GROUP {firstPoint}-{lastPoint} TO {bus}", timeoutMs, _ => _device.PointManager.ConnectRelayGroupAsync(bus, firstPoint, lastPoint), cancellationToken);
    }

    public Task<ModuleRelayControlCommandResult> DisconnectRelayGroupAsync(BusPoint bus, int firstPoint, int lastPoint, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync($"DISCONNECT RELAY GROUP {firstPoint}-{lastPoint} FROM {bus}", timeoutMs, _ => _device.PointManager.DisconnectRelayGroupAsync(bus, firstPoint, lastPoint), cancellationToken);
    }

    public Task<ModuleRelayControlCommandResult> CheckPointAsync(int point, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      EnsureProtocolConfigured();

      return RunTimedAsync(
        $"CHECK POINT {point}",
        timeoutMs,
        token => _device.PointManager.CheckPoint(point),
        cancellationToken);
    }

    public Task<ModuleRelayControlCommandResult> ConnectPointToNewBusAsync(BusPoint bus, int point, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync($"CONNECT POINT {point} TO NEW BUS {bus}", timeoutMs, _ => _device.PointManager.ConnectingPointToNewBus(bus, point), cancellationToken);
    }

    public Task<ModuleRelayControlCommandResult> DisconnectAllPointsAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync("DISCONNECT ALL POINTS", timeoutMs, _ => _device.PointManager.DisconnectingAllPoint(), cancellationToken);
    }

    public Task<ModuleRelayControlCommandResult> DisconnectAllPointsFromBusAAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync("DISCONNECT ALL POINTS FROM A", timeoutMs, _ => _device.PointManager.DisconnectingAllPointFromBusA(), cancellationToken);
    }

    public Task<ModuleRelayControlCommandResult> DisconnectAllPointsFromBusBAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync("DISCONNECT ALL POINTS FROM B", timeoutMs, _ => _device.PointManager.DisconnectingAllPointFromBusB(), cancellationToken);
    }

    public IReadOnlyList<BusConnectionInfo> GetConnectedBuses() => _device.BusManager.GetConnectedBuses();

    public IReadOnlyList<PointConnectionInfo> GetConnectedPoints() => _device.PointManager.GetConnectedPoints();

    private Task<ModuleRelayControlCommandResult> RunBoolOperationAsync(
      string operation,
      int timeoutMs,
      Func<CancellationToken, Task<bool>> action,
      CancellationToken cancellationToken)
    {
      EnsureProtocolConfigured();

      return RunTimedAsync(
        operation,
        timeoutMs,
        async token =>
        {
          bool result = await action(token);
          token.ThrowIfCancellationRequested();
          return result.ToString();
        },
        cancellationToken);
    }

    private void EnsureProtocolConfigured()
    {
      if (_device.DeviceProtocol == null)
      {
        throw new InvalidOperationException("Set a valid IP address before sending ModuleRelayControl commands.");
      }
    }

    private async Task<ModuleRelayControlCommandResult> RunTimedAsync(
      string operation,
      int timeoutMs,
      Func<CancellationToken, Task<string>> action,
      CancellationToken cancellationToken)
    {
      var stopwatch = Stopwatch.StartNew();
      _log($"[ModuleRelayControl] TX {operation}");

      using var timeoutCts = timeoutMs > 0
        ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
        : null;
      timeoutCts?.CancelAfter(timeoutMs);

      CancellationToken effectiveToken = timeoutCts?.Token ?? cancellationToken;

      try
      {
        string response = await action(effectiveToken);
        stopwatch.Stop();
        _log($"[ModuleRelayControl] RX {operation}: {response} ({stopwatch.ElapsedMilliseconds} ms)");
        return new ModuleRelayControlCommandResult(operation, response, stopwatch.Elapsed, true, false);
      }
      catch (OperationCanceledException ex) when (timeoutCts?.IsCancellationRequested == true && !cancellationToken.IsCancellationRequested)
      {
        stopwatch.Stop();
        _log($"[ModuleRelayControl] TIMEOUT {operation} after {stopwatch.ElapsedMilliseconds} ms");
        return new ModuleRelayControlCommandResult(operation, string.Empty, stopwatch.Elapsed, false, true, ex);
      }
      catch (TimeoutException ex)
      {
        stopwatch.Stop();
        _log($"[ModuleRelayControl] TIMEOUT {operation} after {stopwatch.ElapsedMilliseconds} ms: {ex.Message}");
        return new ModuleRelayControlCommandResult(operation, string.Empty, stopwatch.Elapsed, false, true, ex);
      }
      catch (Exception ex)
      {
        stopwatch.Stop();
        bool timedOut = timeoutMs > 0 && stopwatch.ElapsedMilliseconds >= timeoutMs;
        string state = timedOut ? "TIMEOUT" : "ERROR";
        _log($"[ModuleRelayControl] {state} {operation} after {stopwatch.ElapsedMilliseconds} ms: {ex.Message}");
        return new ModuleRelayControlCommandResult(operation, string.Empty, stopwatch.Elapsed, false, timedOut, ex);
      }
    }
  }
}
