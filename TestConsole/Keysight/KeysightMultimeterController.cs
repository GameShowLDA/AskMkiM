using System.Diagnostics;
using System.Globalization;
using System.Net;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Device.Runtime.Device.Multimeters;

namespace TestConsole.Keysight
{
  public sealed class KeysightMultimeterController
  {
    private const int DefaultTimeoutMs = 5000;
    private const int MeasurementTimeoutMs = 10000;
    private readonly KeysightDevice _device;
    private readonly Action<string> _log;


    public KeysightMultimeterController(KeysightDevice? device = null, Action<string>? log = null)
    {
      _device = device ?? new KeysightDevice();
      _log = log ?? Console.WriteLine;
      _device.IPAddress = IPAddress.Parse("192.168.1.119");
    }

    public string Name => _device.Name;

    public string ConnectionDetails
    {
      get => _device.ConnectionDetails;
      set => _device.ConnectionDetails = value;
    }
    public IConnectionInfo ConnectionInfo => _device.ConnectionInfo;

    public string ConnectionStatus => _device.ConnectionInfo.GetConnectionStatus();

    public async Task<KeysightCommandResult> InitializeAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return await RunTimedAsync(
        "INIT",
        timeoutMs,
        async token =>
        {
          var result = await _device.ConnectableManager.InitializeAsync();
          token.ThrowIfCancellationRequested();
          return result.Connect ? result.Answer : throw new InvalidOperationException(result.Answer);
        },
        cancellationToken);
    }

    public async Task<KeysightCommandResult> ConnectAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return await RunTimedAsync(
        "CONNECT",
        timeoutMs,
        async token =>
        {
          var result = await _device.ConnectableManager.ConnectAsync();
          token.ThrowIfCancellationRequested();
          return result.Connect ? result.Answer : throw new InvalidOperationException(result.Answer);
        },
        cancellationToken);
    }

    public async Task<bool> DisconnectAsync(CancellationToken cancellationToken = default)
    {
      var stopwatch = Stopwatch.StartNew();
      _log("[Keysight] DISCONNECT");

      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        bool result = await _device.ConnectableManager.DisconnectAsync();
        _log($"[Keysight] DISCONNECT -> {result} ({stopwatch.ElapsedMilliseconds} ms)");
        return result;
      }
      catch (Exception ex)
      {
        _log($"[Keysight] DISCONNECT ERROR ({stopwatch.ElapsedMilliseconds} ms): {ex.Message}");
        throw;
      }
    }

    public Task<KeysightCommandResult> IdentifyAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return QueryAsync("*IDN?", timeoutMs: timeoutMs, cancellationToken: cancellationToken);
    }

    public Task<KeysightCommandResult> ResetAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return QueryAsync("*RST", timeoutMs: timeoutMs, cancellationToken: cancellationToken);
    }

    public Task<KeysightCommandResult> ClearStatusAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return QueryAsync("*CLS", timeoutMs: timeoutMs, cancellationToken: cancellationToken);
    }

    public async Task<KeysightCommandResult> SetResistanceModeAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      await EnsureConnectedAsync(timeoutMs, cancellationToken);

      return await RunTimedAsync(
        "SET RESISTANCE MODE",
        timeoutMs,
        async token =>
        {
          bool result = await _device.ResistanceManager.SetResistanceModeAsync();
          token.ThrowIfCancellationRequested();
          return result ? _device.ConnectionInfo.GetConnectionStatus() : "Resistance mode was not confirmed.";
        },
        cancellationToken);
    }

    public async Task<KeysightCommandResult> SetContinuityModeAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      await EnsureConnectedAsync(timeoutMs, cancellationToken);

      return await RunTimedAsync(
        "SET CONTINUITY MODE",
        timeoutMs,
        async token =>
        {
          bool result = await _device.ContinuityManager.SetContinuityModeAsync();
          token.ThrowIfCancellationRequested();
          return result ? _device.ConnectionInfo.GetConnectionStatus() : "Continuity mode was not confirmed.";
        },
        cancellationToken);
    }

    public async Task<KeysightCommandResult> SetDiodeModeAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      await EnsureConnectedAsync(timeoutMs, cancellationToken);

      return await RunTimedAsync(
        "SET DIODE MODE",
        timeoutMs,
        async token =>
        {
          bool result = await _device.DiodeManager.SetDiodeModeAsync();
          token.ThrowIfCancellationRequested();
          return result ? _device.ConnectionInfo.GetConnectionStatus() : "Diode mode was not confirmed.";
        },
        cancellationToken);
    }

    public async Task<KeysightCommandResult> SetDcVoltageModeAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      await EnsureConnectedAsync(timeoutMs, cancellationToken);

      return await RunTimedAsync(
        "SET DC VOLTAGE MODE",
        timeoutMs,
        async token =>
        {
          bool result = await _device.DcVoltageManager.SetDCVoltageModeAsync();
          token.ThrowIfCancellationRequested();
          return result ? _device.ConnectionInfo.GetConnectionStatus() : "DC voltage mode was not confirmed.";
        },
        cancellationToken);
    }

    public async Task<KeysightCommandResult> SetResistanceRangeAsync(double range, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      await EnsureConnectedAsync(timeoutMs, cancellationToken);

      return await RunTimedAsync(
        $"SET RESISTANCE RANGE {FormatRange(range)}",
        timeoutMs,
        async token =>
        {
          bool result = await _device.ResistanceManager.SetResistanceRangeAsync(range);
          token.ThrowIfCancellationRequested();
          return result ? _device.ConnectionInfo.GetConnectionStatus() : "Resistance range was not confirmed.";
        },
        cancellationToken);
    }

    public async Task<KeysightCommandResult> SetDcVoltageRangeAsync(double range, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      await EnsureConnectedAsync(timeoutMs, cancellationToken);

      return await RunTimedAsync(
        $"SET DC VOLTAGE RANGE {FormatRange(range)}",
        timeoutMs,
        async token =>
        {
          bool result = await _device.DcVoltageManager.SetDCVoltageRangeAsync(range);
          token.ThrowIfCancellationRequested();
          return result ? _device.ConnectionInfo.GetConnectionStatus() : "DC voltage range was not confirmed.";
        },
        cancellationToken);
    }

    public async Task<KeysightCommandResult> SetAcVoltageModeAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      await EnsureConnectedAsync(timeoutMs, cancellationToken);

      return await RunTimedAsync(
        "SET AC VOLTAGE MODE",
        timeoutMs,
        async token =>
        {
          bool result = await _device.AcVoltageManager.SetACVoltageModeAsync();
          token.ThrowIfCancellationRequested();
          return result ? _device.ConnectionInfo.GetConnectionStatus() : "AC voltage mode was not confirmed.";
        },
        cancellationToken);
    }

    public async Task<KeysightCommandResult> SetAcVoltageRangeAsync(double range, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      await EnsureConnectedAsync(timeoutMs, cancellationToken);

      return await RunTimedAsync(
        $"SET AC VOLTAGE RANGE {FormatRange(range)}",
        timeoutMs,
        async token =>
        {
          bool result = await _device.AcVoltageManager.SetACVoltageRangeAsync(range);
          token.ThrowIfCancellationRequested();
          return result ? _device.ConnectionInfo.GetConnectionStatus() : "AC voltage range was not confirmed.";
        },
        cancellationToken);
    }

    public async Task<KeysightCommandResult> SetCapacitanceModeAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      await EnsureConnectedAsync(timeoutMs, cancellationToken);

      return await RunTimedAsync(
        "SET CAPACITANCE MODE",
        timeoutMs,
        async token =>
        {
          bool result = await _device.CapacitanceManager.SetCapacitanceModeAsync();
          token.ThrowIfCancellationRequested();
          return result ? _device.ConnectionInfo.GetConnectionStatus() : "Capacitance mode was not confirmed.";
        },
        cancellationToken);
    }

    public async Task<KeysightCommandResult> SetCapacitanceRangeAsync(double range, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      await EnsureConnectedAsync(timeoutMs, cancellationToken);

      return await RunTimedAsync(
        $"SET CAPACITANCE RANGE {FormatRange(range)}",
        timeoutMs,
        async token =>
        {
          bool result = await _device.CapacitanceManager.SetCapacitanceRangeAsync(range);
          token.ThrowIfCancellationRequested();
          return result ? _device.ConnectionInfo.GetConnectionStatus() : "Capacitance range was not confirmed.";
        },
        cancellationToken);
    }

    public async Task<KeysightCommandResult> CheckContinuityAsync(bool expectedOutcome, int timeoutMs = MeasurementTimeoutMs, CancellationToken cancellationToken = default)
    {
      await EnsureConnectedAsync(timeoutMs, cancellationToken);

      return await RunTimedAsync(
        $"CHECK CONTINUITY (expected {expectedOutcome})",
        timeoutMs,
        async token =>
        {
          bool result = await _device.ContinuityManager.CheckContinuityAsync(expectedOutcome);
          token.ThrowIfCancellationRequested();
          return result.ToString();
        },
        cancellationToken);
    }

    public async Task<double> MeasureResistanceAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, int timeoutMs = MeasurementTimeoutMs, CancellationToken cancellationToken = default)
    {
      await EnsureConnectedAsync(timeoutMs, cancellationToken);
      cancellationToken.ThrowIfCancellationRequested();
      return await _device.ResistanceManager.MeasureResistanceAsync(param, rangeFrom, rangeTo);
    }

    public async Task<double> MeasureDcVoltageAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, int timeoutMs = MeasurementTimeoutMs, CancellationToken cancellationToken = default)
    {
      await EnsureConnectedAsync(timeoutMs, cancellationToken);
      cancellationToken.ThrowIfCancellationRequested();
      return await _device.DcVoltageManager.MeasureDCVoltageAsync(param, rangeFrom, rangeTo);
    }

    public async Task<double> MeasureAcVoltageAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, int timeoutMs = MeasurementTimeoutMs, CancellationToken cancellationToken = default)
    {
      await EnsureConnectedAsync(timeoutMs, cancellationToken);
      cancellationToken.ThrowIfCancellationRequested();
      return await _device.AcVoltageManager.MeasureACVoltageAsync(param, rangeFrom, rangeTo);
    }

    public async Task<double> MeasureCapacitanceAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, int timeoutMs = MeasurementTimeoutMs, CancellationToken cancellationToken = default)
    {
      await EnsureConnectedAsync(timeoutMs, cancellationToken);
      cancellationToken.ThrowIfCancellationRequested();
      return await _device.CapacitanceManager.MeasureCapacitanceAsync(param, rangeFrom, rangeTo);
    }

    public async Task<double> MeasureContinuityResistanceAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, int timeoutMs = MeasurementTimeoutMs, CancellationToken cancellationToken = default)
    {
      await EnsureConnectedAsync(timeoutMs, cancellationToken);
      cancellationToken.ThrowIfCancellationRequested();
      return await _device.ContinuityManager.CheckContinuityAsync(param, rangeFrom, rangeTo);
    }

    public async Task<double> CheckDiodeAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, int timeoutMs = MeasurementTimeoutMs, CancellationToken cancellationToken = default)
    {
      await EnsureConnectedAsync(timeoutMs, cancellationToken);
      cancellationToken.ThrowIfCancellationRequested();
      return await _device.DiodeManager.CheckDiodeAsync(param, rangeFrom, rangeTo);
    }

    public async Task<KeysightCommandResult> QueryAsync(
      string command,
      double responseDelayMs = 0,
      int timeoutMs = DefaultTimeoutMs,
      int delayBeforeCallMs = 0,
      CancellationToken cancellationToken = default)
    {
      if (string.IsNullOrWhiteSpace(command))
      {
        throw new ArgumentException("Command is empty.", nameof(command));
      }

      await EnsureConnectedAsync(timeoutMs, cancellationToken);

      return await RunTimedAsync(
        command.Trim(),
        timeoutMs,
        token => _device.DeviceProtocol.QueryAsync(
          command.Trim(),
          responseDelay: responseDelayMs,
          timeout: timeoutMs,
          delayBeforeCall: delayBeforeCallMs,
          cancellationToken: token),
        cancellationToken);
    }

    private async Task EnsureConnectedAsync(int timeoutMs, CancellationToken cancellationToken)
    {
      if (_device.ConnectionInfo.IsConnected)
      {
        return;
      }

      var connection = await ConnectAsync(timeoutMs, cancellationToken);
      if (!connection.Success)
      {
        throw connection.Error ?? new InvalidOperationException(connection.Response);
      }
    }

    private static string FormatRange(double range)
    {
      return range <= 0 ? "AUTO" : range.ToString("G", CultureInfo.InvariantCulture);
    }

    private async Task<KeysightCommandResult> RunTimedAsync(
      string operation,
      int timeoutMs,
      Func<CancellationToken, Task<string>> action,
      CancellationToken cancellationToken)
    {
      var stopwatch = Stopwatch.StartNew();
      _log($"[Keysight] TX {operation}");

      using var timeoutCts = timeoutMs > 0
        ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
        : null;
      timeoutCts?.CancelAfter(timeoutMs);

      CancellationToken effectiveToken = timeoutCts?.Token ?? cancellationToken;

      try
      {
        string response = await action(effectiveToken);
        stopwatch.Stop();
        _log($"[Keysight] RX {operation}: {response} ({stopwatch.ElapsedMilliseconds} ms)");
        return new KeysightCommandResult(operation, response, stopwatch.Elapsed, true, false);
      }
      catch (OperationCanceledException ex) when (timeoutCts?.IsCancellationRequested == true && !cancellationToken.IsCancellationRequested)
      {
        stopwatch.Stop();
        _log($"[Keysight] TIMEOUT {operation} after {stopwatch.ElapsedMilliseconds} ms");
        return new KeysightCommandResult(operation, string.Empty, stopwatch.Elapsed, false, true, ex);
      }
      catch (TimeoutException ex)
      {
        stopwatch.Stop();
        _log($"[Keysight] TIMEOUT {operation} after {stopwatch.ElapsedMilliseconds} ms: {ex.Message}");
        return new KeysightCommandResult(operation, string.Empty, stopwatch.Elapsed, false, true, ex);
      }
      catch (Exception ex)
      {
        stopwatch.Stop();
        bool timedOut = timeoutMs > 0 && stopwatch.ElapsedMilliseconds >= timeoutMs;
        string state = timedOut ? "TIMEOUT" : "ERROR";
        _log($"[Keysight] {state} {operation} after {stopwatch.ElapsedMilliseconds} ms: {ex.Message}");
        return new KeysightCommandResult(operation, string.Empty, stopwatch.Elapsed, false, timedOut, ex);
      }
    }
  }
}
