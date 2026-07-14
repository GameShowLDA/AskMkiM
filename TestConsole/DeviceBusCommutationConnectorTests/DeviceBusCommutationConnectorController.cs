using System.Diagnostics;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.AskMkiM.Function.DeviceBusCommutation;
using Ask.Device.Runtime.Device.SwitchingDevice;

namespace TestConsole.DeviceBusCommutationConnectorTests
{
  public sealed class DeviceBusCommutationConnectorController
  {
    private const int DefaultTimeoutMs = 5000;
    private readonly DeviceBusCommutation _device;
    private readonly Action<string> _log;
    public DeviceBusCommutationConnectorController(DeviceBusCommutation? device = null, Action<string>? log = null)
    {
      _device = device ?? new DeviceBusCommutation();
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
    public IConnectionInfo ConnectionInfo => _device.ConnectionInfo;
    public async Task<DeviceBusCommutationCommandResult> ConnectAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
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

    public async Task<DeviceBusCommutationCommandResult> InitializeAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
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

    public Task<DeviceBusCommutationCommandResult> ResetAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      EnsureProtocolConfigured();

      return RunTimedAsync(
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

    public Task<DeviceBusCommutationCommandResult> DisconnectAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      EnsureProtocolConfigured();

      return RunTimedAsync(
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

    public Task<DeviceBusCommutationCommandResult> ConnectMultimeterAsync(SwitchingBusNew bus, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync($"CONNECT MULTIMETER {bus}", timeoutMs, _ => _device.ConnectorManager.ConnectMultimeter(bus), cancellationToken);
    }

    public Task<DeviceBusCommutationCommandResult> DisconnectMultimeterAsync(SwitchingBusNew bus, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync($"DISCONNECT MULTIMETER {bus}", timeoutMs, _ => _device.ConnectorManager.DisconnectMultimeter(bus), cancellationToken);
    }

    public Task<DeviceBusCommutationCommandResult> ConnectPintAsync(SwitchingBusNew bus, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync($"CONNECT PINT {bus}", timeoutMs, _ => _device.ConnectorManager.ConnectPINT(bus), cancellationToken);
    }

    public Task<DeviceBusCommutationCommandResult> DisconnectPintAsync(SwitchingBusNew bus, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync($"DISCONNECT PINT {bus}", timeoutMs, _ => _device.ConnectorManager.DisconnectPINT(bus), cancellationToken);
    }

    public Task<DeviceBusCommutationCommandResult> ConnectAdcAsync(SwitchingBusNew bus, bool reversePolarity, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync($"CONNECT ADC {bus} REVERSE={reversePolarity}", timeoutMs, _ => ConnectorManager.ConnectADC(bus, reversePolarity), cancellationToken);
    }

    public Task<DeviceBusCommutationCommandResult> DisconnectAdcAsync(SwitchingBusNew bus, bool reversePolarity, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync($"DISCONNECT ADC {bus} REVERSE={reversePolarity}", timeoutMs, _ => ConnectorManager.DisconnectADC(bus, reversePolarity), cancellationToken);
    }

    public Task<DeviceBusCommutationCommandResult> ConnectBreakdownTesterAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync("CONNECT BREAKDOWN TESTER", timeoutMs, _ => _device.ConnectorManager.ConnectBreakdownTester(), cancellationToken);
    }

    public Task<DeviceBusCommutationCommandResult> DisconnectBreakdownTesterAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync("DISCONNECT BREAKDOWN TESTER", timeoutMs, _ => _device.ConnectorManager.DisconnectBreakdownTester(), cancellationToken);
    }

    public Task<DeviceBusCommutationCommandResult> ConnectAllBusesAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync("CONNECT ALL BUSES", timeoutMs, _ => _device.ConnectorManager.ConnectAllBuses(), cancellationToken);
    }

    public Task<DeviceBusCommutationCommandResult> DisconnectAllBusesAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync("DISCONNECT ALL BUSES", timeoutMs, _ => _device.ConnectorManager.DisconnectAllBuses(), cancellationToken);
    }

    public Task<DeviceBusCommutationCommandResult> ConnectBreakdownTesterAndMultimeterAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync("CONNECT BREAKDOWN TESTER AND MULTIMETER", timeoutMs, _ => _device.ConnectorManager.ConnectBreakdownTesterAndMultimeter(), cancellationToken);
    }

    public Task<DeviceBusCommutationCommandResult> DisconnectBreakdownTesterAndMultimeterAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync("DISCONNECT BREAKDOWN TESTER AND MULTIMETER", timeoutMs, _ => _device.ConnectorManager.DisconnectBreakdownTesterAndMultimeter(), cancellationToken);
    }

    public Task<DeviceBusCommutationCommandResult> EnableDividerAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync("ENABLE DIVIDER", timeoutMs, _ => _device.ConnectorManager.EnableDivider(), cancellationToken);
    }

    public Task<DeviceBusCommutationCommandResult> DisableDividerAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return RunBoolOperationAsync("DISABLE DIVIDER", timeoutMs, _ => _device.ConnectorManager.DisableDivider(), cancellationToken);
    }

    public IReadOnlyList<DeviceConnectionInfo> GetConnectedDevices() => _device.ConnectorManager.GetConnectedDevices();

    private ConnectorManager ConnectorManager => _device.ConnectorManager as ConnectorManager
      ?? throw new InvalidOperationException("ConnectorManager is not the runtime DeviceBusCommutation.ConnectorManager implementation.");

    private Task<DeviceBusCommutationCommandResult> RunBoolOperationAsync(
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
        throw new InvalidOperationException("Set a valid IP address before sending DeviceBusCommutation commands.");
      }
    }

    private async Task<DeviceBusCommutationCommandResult> RunTimedAsync(
      string operation,
      int timeoutMs,
      Func<CancellationToken, Task<string>> action,
      CancellationToken cancellationToken)
    {
      var stopwatch = Stopwatch.StartNew();
      _log($"[DeviceBusCommutation] TX {operation}");

      using var timeoutCts = timeoutMs > 0
        ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
        : null;
      timeoutCts?.CancelAfter(timeoutMs);

      CancellationToken effectiveToken = timeoutCts?.Token ?? cancellationToken;

      try
      {
        string response = await action(effectiveToken);
        stopwatch.Stop();
        _log($"[DeviceBusCommutation] RX {operation}: {response} ({stopwatch.ElapsedMilliseconds} ms)");
        return new DeviceBusCommutationCommandResult(operation, response, stopwatch.Elapsed, true, false);
      }
      catch (OperationCanceledException ex) when (timeoutCts?.IsCancellationRequested == true && !cancellationToken.IsCancellationRequested)
      {
        stopwatch.Stop();
        _log($"[DeviceBusCommutation] TIMEOUT {operation} after {stopwatch.ElapsedMilliseconds} ms");
        return new DeviceBusCommutationCommandResult(operation, string.Empty, stopwatch.Elapsed, false, true, ex);
      }
      catch (TimeoutException ex)
      {
        stopwatch.Stop();
        _log($"[DeviceBusCommutation] TIMEOUT {operation} after {stopwatch.ElapsedMilliseconds} ms: {ex.Message}");
        return new DeviceBusCommutationCommandResult(operation, string.Empty, stopwatch.Elapsed, false, true, ex);
      }
      catch (Exception ex)
      {
        stopwatch.Stop();
        bool timedOut = timeoutMs > 0 && stopwatch.ElapsedMilliseconds >= timeoutMs;
        string state = timedOut ? "TIMEOUT" : "ERROR";
        _log($"[DeviceBusCommutation] {state} {operation} after {stopwatch.ElapsedMilliseconds} ms: {ex.Message}");
        return new DeviceBusCommutationCommandResult(operation, string.Empty, stopwatch.Elapsed, false, timedOut, ex);
      }
    }
  }
}
