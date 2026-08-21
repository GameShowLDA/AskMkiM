using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Device.Runtime.Device;
using System.Diagnostics;
using Ask.Device.Runtime.Device.Multimeters;

namespace TestConsole.B7783
{
  /// <summary>
  /// Предоставляет высокоуровневый API для управления мультиметром В7-783.
  /// </summary>
  public sealed class B7783MultimeterController
  {
    /// <summary>
    /// Время ожидания операций по умолчанию, в миллисекундах.
    /// </summary>
    private const int DefaultTimeoutMs = 5000;

    /// <summary>
    /// Время ожидания операций измерения, в миллисекундах.
    /// </summary>
    private const int MeasurementTimeoutMs = 10000;

    /// <summary>
    /// Экземпляр мультиметра В7-783.
    /// </summary>
    private readonly MultimeterB7783 _device;

    /// <summary>
    /// Делегат вывода диагностических сообщений.
    /// </summary>
    private readonly Action<string> _log;

    /// <summary>
    /// Инициализирует контроллер мультиметра В7-783.
    /// </summary>
    /// <param name="device">
    /// Экземпляр мультиметра. Если не указан, создаётся новый.
    /// </param>
    /// <param name="log">
    /// Делегат журналирования. Если не указан, используется вывод в консоль.
    /// </param>
    public B7783MultimeterController(MultimeterB7783? device = null, Action<string>? log = null)
    {
      _device = device ?? new MultimeterB7783();
      _log = log ?? Console.WriteLine;
    }

    /// <summary>
    /// Наименование мультиметра.
    /// </summary>
    public string Name => _device.Name;

    /// <summary>
    /// Строка с параметрами подключения мультиметра.
    /// </summary>
    public string ConnectionDetails
    {
      get => _device.ConnectionDetails;
      set => _device.ConnectionDetails = value;
    }

    /// <summary>
    /// Путь к последнему успешно найденному USB-устройству.
    /// </summary>
    public string LastResolvedDevicePath => _device.LastResolvedDevicePath;

    /// <summary>
    /// Текущее состояние подключения мультиметра.
    /// </summary>
    public string ConnectionStatus => _device.ConnectionInfo.GetConnectionStatus();

    /// <summary>
    /// Выполняет инициализацию мультиметра.
    /// </summary>
    /// <param name="timeoutMs">Максимальное время ожидания операции, в миллисекундах.</param>
    /// <param name="cancellationToken">Маркер отмены операции.</param>
    /// <returns>Результат выполнения команды.</returns>
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

    /// <summary>
    /// Выполняет подключение к мультиметру.
    /// </summary>
    /// <param name="timeoutMs">Максимальное время ожидания операции, в миллисекундах.</param>
    /// <param name="cancellationToken">Маркер отмены операции.</param>
    /// <returns>Результат выполнения команды.</returns>
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

    /// <summary>
    /// Выполняет отключение от мультиметра.
    /// </summary>
    /// <param name="cancellationToken">Маркер отмены операции.</param>
    /// <returns>
    /// <see langword="true"/>, если отключение выполнено успешно;
    /// иначе <see langword="false"/>.
    /// </returns>
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

    /// <summary>
    /// Возвращает идентификационную информацию о мультиметре.
    /// </summary>
    /// <param name="timeoutMs">Максимальное время ожидания операции, в миллисекундах.</param>
    /// <param name="cancellationToken">Маркер отмены операции.</param>
    /// <returns>Результат выполнения команды.</returns>
    public Task<B7783CommandResult> IdentifyAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return QueryAsync("*IDN?", timeoutMs: timeoutMs, cancellationToken: cancellationToken);
    }


    /// <summary>
    /// Выполняет сброс мультиметра к заводским настройкам.
    /// </summary>
    /// <param name="timeoutMs">Максимальное время ожидания операции, в миллисекундах.</param>
    /// <param name="cancellationToken">Маркер отмены операции.</param>
    /// <returns>Результат выполнения команды.</returns>
    public Task<B7783CommandResult> ResetAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return QueryAsync("*RST", timeoutMs: timeoutMs, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Очищает регистры состояния и ошибок мультиметра.
    /// </summary>
    /// <param name="timeoutMs">Максимальное время ожидания операции, в миллисекундах.</param>
    /// <param name="cancellationToken">Маркер отмены операции.</param>
    /// <returns>Результат выполнения команды.</returns>
    public Task<B7783CommandResult> ClearStatusAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return QueryAsync("*CLS", timeoutMs: timeoutMs, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Выполняет считывание текущего результата измерения.
    /// </summary>
    /// <param name="timeoutMs">Максимальное время ожидания операции, в миллисекундах.</param>
    /// <param name="cancellationToken">Маркер отмены операции.</param>
    /// <returns>Результат выполнения команды.</returns>
    public Task<B7783CommandResult> ReadAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      return QueryAsync("READ?", timeoutMs: timeoutMs, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Переводит мультиметр в режим измерения сопротивления.
    /// </summary>
    /// <param name="timeoutMs">Максимальное время ожидания операции, в миллисекундах.</param>
    /// <param name="cancellationToken">Маркер отмены операции.</param>
    /// <returns>Результат выполнения команды.</returns>
    public async Task<B7783CommandResult> SetResistanceModeAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      if (!_device.ConnectionInfo.IsConnected)
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
          return result ? _device.ConnectionInfo.GetConnectionStatus() : "Resistance mode was not confirmed.";
        },
        cancellationToken);
    }

    /// <summary>
    /// Переводит мультиметр в режим прозвонки.
    /// </summary>
    /// <param name="timeoutMs">Максимальное время ожидания операции, в миллисекундах.</param>
    /// <param name="cancellationToken">Маркер отмены операции.</param>
    /// <returns>Результат выполнения команды.</returns>
    public async Task<B7783CommandResult> SetContinuityModeAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      if (!_device.ConnectionInfo.IsConnected)
      {
        var connection = await ConnectAsync(timeoutMs, cancellationToken);
        if (!connection.Success)
        {
          return connection;
        }
      }

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

    /// <summary>
    /// Переводит мультиметр в режим проверки диодов.
    /// </summary>
    /// <param name="timeoutMs">Максимальное время ожидания операции, в миллисекундах.</param>
    /// <param name="cancellationToken">Маркер отмены операции.</param>
    /// <returns>Результат выполнения команды.</returns>
    public async Task<B7783CommandResult> SetDiodeModeAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      if (!_device.ConnectionInfo.IsConnected)
      {
        var connection = await ConnectAsync(timeoutMs, cancellationToken);
        if (!connection.Success)
        {
          return connection;
        }
      }

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

    /// <summary>
    /// Выполняет проверку цепи в режиме прозвонки.
    /// </summary>
    /// <param name="expectedOutcome">
    /// Ожидаемый результат проверки цепи.
    /// </param>
    /// <param name="timeoutMs">Максимальное время ожидания операции, в миллисекундах.</param>
    /// <param name="cancellationToken">Маркер отмены операции.</param>
    /// <returns>Результат выполнения команды.</returns>
    public async Task<B7783CommandResult> CheckContinuityAsync(bool expectedOutcome, int timeoutMs = MeasurementTimeoutMs, CancellationToken cancellationToken = default)
    {
      if (!_device.ConnectionInfo.IsConnected)
      {
        var connection = await ConnectAsync(timeoutMs, cancellationToken);
        if (!connection.Success)
        {
          return connection;
        }
      }

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

    /// <summary>
    /// Измеряет сопротивление в режиме прозвонки.
    /// </summary>
    /// <param name="param">
    /// Ожидаемое значение сопротивления. Используется в режиме имитации.
    /// </param>
    /// <param name="rangeFrom">Нижняя граница допустимого диапазона.</param>
    /// <param name="rangeTo">Верхняя граница допустимого диапазона.</param>
    /// <param name="timeoutMs">Максимальное время ожидания операции, в миллисекундах.</param>
    /// <param name="cancellationToken">Маркер отмены операции.</param>
    /// <returns>Результат выполнения команды.</returns>
    public async Task<B7783CommandResult> MeasureContinuityResistanceAsync(
      MeasurementRange measurementRange,
      int timeoutMs = MeasurementTimeoutMs,
      CancellationToken cancellationToken = default)
    {
      if (!_device.ConnectionInfo.IsConnected)
      {
        var connection = await ConnectAsync(timeoutMs, cancellationToken);
        if (!connection.Success)
        {
          return connection;
        }
      }

      return await RunTimedAsync(
        "MEASURE CONTINUITY RESISTANCE",
        timeoutMs,
        async token =>
        {
          double result = await _device.ContinuityManager.CheckContinuityAsync(measurementRange);
          token.ThrowIfCancellationRequested();
          return result.ToString("G17", System.Globalization.CultureInfo.InvariantCulture);
        },
        cancellationToken);
    }

    /// <summary>
    /// Выполняет измерение прямого напряжения на диоде.
    /// </summary>
    /// <param name="param">
    /// Ожидаемое значение напряжения. Используется в режиме имитации.
    /// </param>
    /// <param name="rangeFrom">Нижняя граница допустимого диапазона.</param>
    /// <param name="rangeTo">Верхняя граница допустимого диапазона.</param>
    /// <param name="timeoutMs">Максимальное время ожидания операции, в миллисекундах.</param>
    /// <param name="cancellationToken">Маркер отмены операции.</param>
    /// <returns>Результат выполнения команды.</returns>
    public async Task<B7783CommandResult> MeasureDiodeAsync(
    double param = 0,
    double rangeFrom = -1,
    double rangeTo = -1,
    int timeoutMs = MeasurementTimeoutMs,
    CancellationToken cancellationToken = default)
    {
      if (!_device.ConnectionInfo.IsConnected)
      {
        var connection = await ConnectAsync(timeoutMs, cancellationToken);
        if (!connection.Success)
        {
          return connection;
        }
      }

      return await RunTimedAsync(
        "MEASURE DIODE",
        timeoutMs,
        async token =>
        {
          MeasurementRange measurementRange = new MeasurementRange(param, rangeFrom, rangeTo);
          double result = await _device.DiodeManager.CheckDiodeAsync(measurementRange);

          token.ThrowIfCancellationRequested();
          return result.ToString("G17", System.Globalization.CultureInfo.InvariantCulture);
        },
        cancellationToken);
    }

    /// <summary>
    /// Переводит мультиметр в режим измерения постоянного напряжения.
    /// </summary>
    /// <param name="timeoutMs">Максимальное время ожидания операции, в миллисекундах.</param>
    /// <param name="cancellationToken">Маркер отмены операции.</param>
    /// <returns>Результат выполнения команды.</returns>
    public async Task<B7783CommandResult> SetDcVoltageModeAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      if (!_device.ConnectionInfo.IsConnected)
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
          return result ? _device.ConnectionInfo.GetConnectionStatus() : "DC voltage mode was not confirmed.";
        },
        cancellationToken);
    }

    public async Task<B7783CommandResult> SetResistanceRangeAsync(double range, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      if (!_device.ConnectionInfo.IsConnected)
      {
        var connection = await ConnectAsync(timeoutMs, cancellationToken);
        if (!connection.Success)
        {
          return connection;
        }
      }

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

    public async Task<B7783CommandResult> SetDcVoltageRangeAsync(double range, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      if (!_device.ConnectionInfo.IsConnected)
      {
        var connection = await ConnectAsync(timeoutMs, cancellationToken);
        if (!connection.Success)
        {
          return connection;
        }
      }

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

    /// <summary>
    /// Переводит мультиметр в режим измерения переменного напряжения.
    /// </summary>
    /// <param name="timeoutMs">Максимальное время ожидания операции, в миллисекундах.</param>
    /// <param name="cancellationToken">Маркер отмены операции.</param>
    /// <returns>Результат выполнения команды.</returns>
    public async Task<B7783CommandResult> SetAcVoltageModeAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      if (!_device.ConnectionInfo.IsConnected)
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
          return result ? _device.ConnectionInfo.GetConnectionStatus() : "AC voltage mode was not confirmed.";
        },
        cancellationToken);
    }

    public async Task<B7783CommandResult> SetAcVoltageRangeAsync(double range, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      if (!_device.ConnectionInfo.IsConnected)
      {
        var connection = await ConnectAsync(timeoutMs, cancellationToken);
        if (!connection.Success)
        {
          return connection;
        }
      }

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

    /// <summary>
    /// Переводит мультиметр в режим измерения ёмкости.
    /// </summary>
    /// <param name="timeoutMs">Максимальное время ожидания операции, в миллисекундах.</param>
    /// <param name="cancellationToken">Маркер отмены операции.</param>
    /// <returns>Результат выполнения команды.</returns>
    public async Task<B7783CommandResult> SetCapacitanceModeAsync(int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      if (!_device.ConnectionInfo.IsConnected)
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
          return result ? _device.ConnectionInfo.GetConnectionStatus() : "Capacitance mode was not confirmed.";
        },
        cancellationToken);
    }

    public async Task<B7783CommandResult> SetCapacitanceRangeAsync(double range, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
      if (!_device.ConnectionInfo.IsConnected)
      {
        var connection = await ConnectAsync(timeoutMs, cancellationToken);
        if (!connection.Success)
        {
          return connection;
        }
      }

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

    /// <summary>
    /// Выполняет измерение электрического сопротивления.
    /// </summary>
    /// <param name="timeoutMs">Максимальное время ожидания операции, в миллисекундах.</param>
    /// <param name="cancellationToken">Маркер отмены операции.</param>
    /// <returns>Измеренное значение сопротивления.</returns>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если не удалось перевести мультиметр в режим измерения сопротивления.
    /// </exception>
    public async Task<double> MeasureResistanceAsync(int timeoutMs = MeasurementTimeoutMs, CancellationToken cancellationToken = default)
    {
      var mode = await SetResistanceModeAsync(timeoutMs, cancellationToken);
      if (!mode.Success)
      {
        throw mode.Error ?? new InvalidOperationException("Failed to configure resistance mode.");
      }

      return await _device.ResistanceManager.MeasureResistanceAsync(new MeasurementRange(0, 0, 0));
    }

    /// <summary>
    /// Выполняет измерение постоянного напряжения.
    /// </summary>
    /// <param name="param">
    /// Ожидаемое значение напряжения. Используется в режиме имитации.
    /// </param>
    /// <param name="rangeFrom">Нижняя граница допустимого диапазона.</param>
    /// <param name="rangeTo">Верхняя граница допустимого диапазона.</param>
    /// <param name="timeoutMs">Максимальное время ожидания операции, в миллисекундах.</param>
    /// <param name="cancellationToken">Маркер отмены операции.</param>
    /// <returns>Измеренное значение постоянного напряжения.</returns>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если не удалось подключиться к мультиметру.
    /// </exception>
    public async Task<double> MeasureDcVoltageAsync(
      double param = 0,
      double rangeFrom = -1,
      double rangeTo = -1,
      int timeoutMs = MeasurementTimeoutMs,
      CancellationToken cancellationToken = default)
    {
      if (!_device.ConnectionInfo.IsConnected)
      {
        var connection = await ConnectAsync(timeoutMs, cancellationToken);
        if (!connection.Success)
        {
          throw connection.Error ?? new InvalidOperationException(connection.Response);
        }
      }

      cancellationToken.ThrowIfCancellationRequested();

      MeasurementRange measurementRange = new MeasurementRange(param, rangeFrom, rangeTo);
      return await _device.DcVoltageManager.MeasureDCVoltageAsync(measurementRange);
    }

    /// <summary>
    /// Выполняет измерение постоянного напряжения.
    /// </summary>
    /// <param name="param">
    /// Ожидаемое значение напряжения. Используется в режиме имитации.
    /// </param>
    /// <param name="rangeFrom">Нижняя граница допустимого диапазона.</param>
    /// <param name="rangeTo">Верхняя граница допустимого диапазона.</param>
    /// <param name="timeoutMs">Максимальное время ожидания операции, в миллисекундах.</param>
    /// <param name="cancellationToken">Маркер отмены операции.</param>
    /// <returns>Измеренное значение переменного напряжения.</returns>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если не удалось подключиться к мультиметру.
    /// </exception>
    public async Task<double> MeasureAcVoltageAsync(
      double param = 0,
      double rangeFrom = -1,
      double rangeTo = -1,
      int timeoutMs = MeasurementTimeoutMs,
      CancellationToken cancellationToken = default)
    {
      if (!_device.ConnectionInfo.IsConnected)
      {
        var connection = await ConnectAsync(timeoutMs, cancellationToken);
        if (!connection.Success)
        {
          throw connection.Error ?? new InvalidOperationException(connection.Response);
        }
      }

      cancellationToken.ThrowIfCancellationRequested();

      MeasurementRange measurementRangeAc = new MeasurementRange(param, rangeFrom, rangeTo);
      return await _device.AcVoltageManager.MeasureACVoltageAsync(measurementRangeAc);
    }

    /// <summary>
    /// Выполняет измерение ёмкости.
    /// </summary>
    /// <param name="param">
    /// Ожидаемое значение ёмкости. Используется в режиме имитации.
    /// </param>
    /// <param name="rangeFrom">Нижняя граница допустимого диапазона.</param>
    /// <param name="rangeTo">Верхняя граница допустимого диапазона.</param>
    /// <param name="timeoutMs">Максимальное время ожидания операции, в миллисекундах.</param>
    /// <param name="cancellationToken">Маркер отмены операции.</param>
    /// <returns>Измеренное значение ёмкости.</returns>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если не удалось подключиться к мультиметру.
    /// </exception>
    public async Task<double> MeasureCapacitanceAsync(
      double param = 0,
      double rangeFrom = -1,
      double rangeTo = -1,
      int timeoutMs = MeasurementTimeoutMs,
      CancellationToken cancellationToken = default)
    {
      if (!_device.ConnectionInfo.IsConnected)
      {
        var connection = await ConnectAsync(timeoutMs, cancellationToken);
        if (!connection.Success)
        {
          throw connection.Error ?? new InvalidOperationException(connection.Response);
        }
      }

      cancellationToken.ThrowIfCancellationRequested();

      MeasurementRange measurementRange = new MeasurementRange(param, rangeFrom, rangeTo);
      return await _device.CapacitanceManager.MeasureCapacitanceAsync(measurementRange);
    }

    /// <summary>
    /// Отправляет произвольную SCPI-команду мультиметру и возвращает результат её выполнения.
    /// </summary>
    /// <param name="command">SCPI-команда.</param>
    /// <param name="responseDelayMs">
    /// Дополнительная задержка перед чтением ответа, в миллисекундах.
    /// </param>
    /// <param name="timeoutMs">Максимальное время ожидания операции, в миллисекундах.</param>
    /// <param name="delayBeforeCallMs">
    /// Задержка перед отправкой команды, в миллисекундах.
    /// </param>
    /// <param name="cancellationToken">Маркер отмены операции.</param>
    /// <returns>Результат выполнения команды.</returns>
    /// <exception cref="ArgumentException">
    /// Выбрасывается, если команда не задана.
    /// </exception>
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

      if (!_device.ConnectionInfo.IsConnected)
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

    private static string FormatRange(double range)
    {
      return range <= 0 ? "AUTO" : range.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Выполняет операцию с контролем времени выполнения, журналированием
    /// и обработкой ошибок.
    /// </summary>
    /// <param name="operation">Наименование выполняемой операции.</param>
    /// <param name="timeoutMs">Максимальное время ожидания операции, в миллисекундах.</param>
    /// <param name="action">Асинхронная операция для выполнения.</param>
    /// <param name="cancellationToken">Маркер отмены операции.</param>
    /// <returns>Результат выполнения операции.</returns>
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
