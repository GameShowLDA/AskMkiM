using Ask.Device.Runtime.Device;
using System.Diagnostics;
using System.Globalization;

namespace TestConsole.B7783
{
  public sealed class B7783MultimeterController
  {
    private const int DefaultTimeoutMs = 5000;
    private const int MeasurementTimeoutMs = 10000;
    private readonly MultimeterB7783 _device;
    private readonly Action<string> _log;

    public B7783MultimeterController(MultimeterB7783? device = null, Action<string>? log = null)
    {
      _device = device ?? new MultimeterB7783();
      _log = log ?? Console.WriteLine;
    }

    public string Name => _device.Name;

    public string ConnectionDetails
    {
      get => _device.ConnectionDetails;
      set => _device.ConnectionDetails = value;
    }

    public bool IsConnected => _device.IsConnected;

    public string LastResolvedDevicePath => _device.LastResolvedDevicePath;

    public string ConnectionStatus => _device.ConnectableManager.GetConnectionStatus();

    public async Task<B7783CommandResult> InitializeAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
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

    public async Task<B7783CommandResult> ConnectAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
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
      _log("[B7783] DISCONNECT");

      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        bool result = await _device.ConnectableManager.DisconnectAsync();
        _log($"[B7783] DISCONNECT -> {result} ({stopwatch.ElapsedMilliseconds} ms)");
        return result;
      }
      catch (Exception ex)
      {
        _log($"[B7783] DISCONNECT ERROR ({stopwatch.ElapsedMilliseconds} ms): {ex.Message}");
        throw;
      }
    }

    public Task<B7783CommandResult> IdentifyAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return QueryAsync("*IDN?", timeoutMs: timeoutMs, cancellationToken: cancellationToken);
    }

    public Task<B7783CommandResult> ResetAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return QueryAsync("*RST", timeoutMs: timeoutMs, cancellationToken: cancellationToken);
    }

    public Task<B7783CommandResult> ClearStatusAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return QueryAsync("*CLS", timeoutMs: timeoutMs, cancellationToken: cancellationToken);
    }

    public Task<B7783CommandResult> ReadAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return QueryAsync("READ?", timeoutMs: timeoutMs, cancellationToken: cancellationToken);
    }

    public async Task<B7783CommandResult> SetResistanceModeAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      if (!_device.IsConnected)
      {
        var connection = await ConnectAsync(timeoutMs, cancellationToken);
        if (!connection.Success)
        {
          return connection;
        }
      }

      return await RunTimedAsync(
        "SET RESISTANCE MODE",
        timeoutMs,
        async token =>
        {
          bool result = await _device.ResistanceManager.SetResistanceModeAsync();
          token.ThrowIfCancellationRequested();
          return result ? _device.ConnectableManager.GetConnectionStatus() : "Resistance mode was not confirmed.";
        },
        cancellationToken);
    }

    public async Task<double> MeasureResistanceAsync(int timeoutMs = MeasurementTimeoutMs, CancellationToken cancellationToken = default)
    {
      var mode = await SetResistanceModeAsync(timeoutMs, cancellationToken);
      if (!mode.Success)
      {
        throw mode.Error ?? new InvalidOperationException("Failed to configure resistance mode.");
      }

      return await _device.ResistanceManager.MeasureResistanceAsync();
    }

    public async Task<double> MeasureDcVoltageAsync(int timeoutMs = MeasurementTimeoutMs, CancellationToken cancellationToken = default)
    {
      var configure = await QueryAsync("CONFIGURE:VOLTAGE:DC AUTO", timeoutMs: timeoutMs, cancellationToken: cancellationToken);
      if (!configure.Success)
      {
        throw configure.Error ?? new InvalidOperationException("Failed to configure DC voltage mode.");
      }

      return await MeasureDoubleAsync("READ?", timeoutMs, cancellationToken);
    }

    public async Task<double> MeasureAcVoltageAsync(int timeoutMs = MeasurementTimeoutMs, CancellationToken cancellationToken = default)
    {
      var configure = await QueryAsync("CONFIGURE:VOLTAGE:AC AUTO", timeoutMs: timeoutMs, cancellationToken: cancellationToken);
      if (!configure.Success)
      {
        throw configure.Error ?? new InvalidOperationException("Failed to configure AC voltage mode.");
      }

      return await MeasureDoubleAsync("READ?", timeoutMs, cancellationToken);
    }

    public async Task<B7783CommandResult> QueryAsync(
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

      if (!_device.IsConnected)
      {
        var connection = await ConnectAsync(timeoutMs, cancellationToken);
        if (!connection.Success)
        {
          return connection;
        }
      }

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

    private async Task<double> MeasureDoubleAsync(string command, int timeoutMs, CancellationToken cancellationToken)
    {
      var result = await QueryAsync(command, timeoutMs: timeoutMs, cancellationToken: cancellationToken);
      if (!result.Success)
      {
        throw result.Error ?? new InvalidOperationException($"Command {command} failed.");
      }

      string response = result.Response.Trim().Replace("+", string.Empty, StringComparison.Ordinal);
      if (double.TryParse(response, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
      {
        return value;
      }

      throw new FormatException($"Invalid B7-78 response for {command}: '{result.Response}'.");
    }

    private async Task<B7783CommandResult> RunTimedAsync(
      string operation,
      int timeoutMs,
      Func<CancellationToken, Task<string>> action,
      CancellationToken cancellationToken)
    {
      var stopwatch = Stopwatch.StartNew();
      _log($"[B7783] TX {operation}");

      using var timeoutCts = timeoutMs > 0
        ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
        : null;
      timeoutCts?.CancelAfter(timeoutMs);

      CancellationToken effectiveToken = timeoutCts?.Token ?? cancellationToken;

      try
      {
        string response = await action(effectiveToken);
        stopwatch.Stop();
        _log($"[B7783] RX {operation}: {response} ({stopwatch.ElapsedMilliseconds} ms)");
        return new B7783CommandResult(operation, response, stopwatch.Elapsed, true, false);
      }
      catch (OperationCanceledException ex) when (timeoutCts?.IsCancellationRequested == true && !cancellationToken.IsCancellationRequested)
      {
        stopwatch.Stop();
        _log($"[B7783] TIMEOUT {operation} after {stopwatch.ElapsedMilliseconds} ms");
        return new B7783CommandResult(operation, string.Empty, stopwatch.Elapsed, false, true, ex);
      }
      catch (TimeoutException ex)
      {
        stopwatch.Stop();
        _log($"[B7783] TIMEOUT {operation} after {stopwatch.ElapsedMilliseconds} ms: {ex.Message}");
        return new B7783CommandResult(operation, string.Empty, stopwatch.Elapsed, false, true, ex);
      }
      catch (Exception ex)
      {
        stopwatch.Stop();
        bool timedOut = timeoutMs > 0 && stopwatch.ElapsedMilliseconds >= timeoutMs;
        string state = timedOut ? "TIMEOUT" : "ERROR";
        _log($"[B7783] {state} {operation} after {stopwatch.ElapsedMilliseconds} ms: {ex.Message}");
        return new B7783CommandResult(operation, string.Empty, stopwatch.Elapsed, false, timedOut, ex);
      }
    }
  }
}
