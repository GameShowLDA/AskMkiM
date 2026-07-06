using Ask.Device.Runtime.Device;
using System.Diagnostics;

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

    public async Task<B7783CommandResult> SetDcVoltageModeAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
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
        "SET DC VOLTAGE MODE",
        timeoutMs,
        async token =>
        {
          bool result = await _device.DcVoltageManager.SetDCVoltageModeAsync();
          token.ThrowIfCancellationRequested();
          return result ? _device.ConnectableManager.GetConnectionStatus() : "DC voltage mode was not confirmed.";
        },
        cancellationToken);
    }

    public async Task<B7783CommandResult> SetAcVoltageModeAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
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
        "SET AC VOLTAGE MODE",
        timeoutMs,
        async token =>
        {
          bool result = await _device.AcVoltageManager.SetACVoltageModeAsync();
          token.ThrowIfCancellationRequested();
          return result ? _device.ConnectableManager.GetConnectionStatus() : "AC voltage mode was not confirmed.";
        },
        cancellationToken);
    }

    public async Task<B7783CommandResult> SetCapacitanceModeAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
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
        "SET CAPACITANCE MODE",
        timeoutMs,
        async token =>
        {
          bool result = await _device.CapacitanceManager.SetCapacitanceModeAsync();
          token.ThrowIfCancellationRequested();
          return result ? _device.ConnectableManager.GetConnectionStatus() : "Capacitance mode was not confirmed.";
        },
        cancellationToken);
    }

    public async Task<B7783CommandResult> SetCapacitanceRangeAsync(
      double rangeNanofarads,
      int timeoutMs = DefaultTimeoutMs,
      CancellationToken cancellationToken = default)
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
        $"SET CAPACITANCE RANGE {rangeNanofarads:G17} nF",
        timeoutMs,
        async token =>
        {
          if (_device.CapacitanceManager is not Ask.Device.Runtime.Function.B7783.CapacitanceMeasurement measurement)
          {
            throw new InvalidOperationException("B7-78/3 capacitance range control is not available.");
          }

          bool result = await measurement.SetCapacitanceRangeAsync(rangeNanofarads);
          token.ThrowIfCancellationRequested();
          return result ? _device.ConnectableManager.GetConnectionStatus() : "Capacitance range was not confirmed.";
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

    public async Task<double> MeasureDcVoltageAsync(
      double param = 0,
      double rangeFrom = -1,
      double rangeTo = -1,
      int timeoutMs = MeasurementTimeoutMs,
      CancellationToken cancellationToken = default)
    {
      if (!_device.IsConnected)
      {
        var connection = await ConnectAsync(timeoutMs, cancellationToken);
        if (!connection.Success)
        {
          throw connection.Error ?? new InvalidOperationException(connection.Response);
        }
      }

      cancellationToken.ThrowIfCancellationRequested();
      return await _device.DcVoltageManager.MeasureDCVoltageAsync(param, rangeFrom, rangeTo);
    }

    public async Task<double> MeasureAcVoltageAsync(
      double param = 0,
      double rangeFrom = -1,
      double rangeTo = -1,
      int timeoutMs = MeasurementTimeoutMs,
      CancellationToken cancellationToken = default)
    {
      if (!_device.IsConnected)
      {
        var connection = await ConnectAsync(timeoutMs, cancellationToken);
        if (!connection.Success)
        {
          throw connection.Error ?? new InvalidOperationException(connection.Response);
        }
      }

      cancellationToken.ThrowIfCancellationRequested();
      return await _device.AcVoltageManager.MeasureACVoltageAsync(param, rangeFrom, rangeTo);
    }

    public async Task<double> MeasureCapacitanceAsync(
      double param = 0,
      double rangeFrom = -1,
      double rangeTo = -1,
      int timeoutMs = MeasurementTimeoutMs,
      CancellationToken cancellationToken = default)
    {
      if (!_device.IsConnected)
      {
        var connection = await ConnectAsync(timeoutMs, cancellationToken);
        if (!connection.Success)
        {
          throw connection.Error ?? new InvalidOperationException(connection.Response);
        }
      }

      cancellationToken.ThrowIfCancellationRequested();
      return await _device.CapacitanceManager.MeasureCapacitanceAsync(param, rangeFrom, rangeTo);
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
