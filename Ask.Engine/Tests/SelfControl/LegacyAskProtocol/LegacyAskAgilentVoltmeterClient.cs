using Ask.Core.Services.Config.LegacyMki;
using System.Globalization;
using System.IO.Ports;

namespace Ask.Engine.Tests.SelfControl.LegacyAskProtocol;

/// <summary>
/// Выполняет обмен с цифровым вольтметром Agilent по правилам старой MKI.
/// </summary>
internal sealed class LegacyAskAgilentVoltmeterClient : IDisposable
{
  private const int AgilentComCode = 6;
  private const int AgilentUsbCode = 7;
  private readonly LegacyMkiHardwareProfile _profile;
  private readonly bool _isIdleMode;
  private readonly SerialPort? _serialPort;
  private double _currentRange;

  /// <summary>
  /// Создаёт клиент вольтметра Agilent для холостого или боевого режима.
  /// </summary>
  /// <param name="profile">Legacy-конфигурация аппаратуры.</param>
  /// <param name="isIdleMode">Признак холостого режима.</param>
  public LegacyAskAgilentVoltmeterClient(LegacyMkiHardwareProfile profile, bool isIdleMode)
  {
    _profile = profile ?? throw new ArgumentNullException(nameof(profile));
    _isIdleMode = isIdleMode;

    if (_isIdleMode)
    {
      return;
    }

    ValidateRealModeSupport(profile);
    _serialPort = CreateSerialPort(profile.HardwareAux.PortVm);
    _serialPort.Open();
  }

  /// <summary>
  /// Устанавливает режим измерения постоянного напряжения.
  /// </summary>
  /// <param name="range">Диапазон измерения, В.</param>
  /// <param name="cancellationToken">Токен отмены.</param>
  public Task SetDcVoltageModeAsync(double range, CancellationToken cancellationToken)
  {
    return SetModeAsync("VOLT:DC", range, cancellationToken);
  }

  /// <summary>
  /// Устанавливает режим измерения сопротивления.
  /// </summary>
  /// <param name="range">Диапазон измерения, Ом.</param>
  /// <param name="cancellationToken">Токен отмены.</param>
  public Task SetResistanceModeAsync(double range, CancellationToken cancellationToken)
  {
    return SetModeAsync("RES", range, cancellationToken);
  }

  /// <summary>
  /// Измеряет значение или возвращает эмулированный ответ в холостом режиме.
  /// </summary>
  /// <param name="expected">Ожидаемое значение для холостого режима.</param>
  /// <param name="cancellationToken">Токен отмены.</param>
  /// <returns>Результат измерения.</returns>
  public Task<LegacyAskVoltmeterMeasurement> MeasureAsync(double expected, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();

    if (_isIdleMode)
    {
      bool idleIsOverload = expected > _currentRange * 1.5;
      return Task.FromResult(new LegacyAskVoltmeterMeasurement(idleIsOverload ? _currentRange : expected, idleIsOverload));
    }

    string response = Query("READ?", cancellationToken);
    double value = ParseMeasurement(response);
    bool isOverload = value > _currentRange * 1.5;
    return Task.FromResult(new LegacyAskVoltmeterMeasurement(isOverload ? _currentRange : value, isOverload));
  }

  /// <summary>
  /// Закрывает порт вольтметра.
  /// </summary>
  public void Dispose()
  {
    _serialPort?.Dispose();
  }

  /// <summary>
  /// Устанавливает режим Agilent в формате CONF старой MKI.
  /// </summary>
  /// <param name="function">SCPI-функция измерения.</param>
  /// <param name="range">Диапазон измерения.</param>
  /// <param name="cancellationToken">Токен отмены.</param>
  private Task SetModeAsync(string function, double range, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    _currentRange = range;

    if (_isIdleMode)
    {
      return Task.CompletedTask;
    }

    if ((_profile.HardwareAux.PortVm.Base & 0xFF) != 0)
    {
      Write("SYST:RWL", cancellationToken);
    }

    Write(string.Create(CultureInfo.InvariantCulture, $"CONF:{function} {range:0.00000E+00},DEF"), cancellationToken);

    if (_profile.HardwareAux.BeepOff != 0)
    {
      Write("SYST:BEEP:STAT OFF", cancellationToken);
    }

    Write("TRIG:SOUR IMM", cancellationToken);
    ValidateMode(function, range, cancellationToken);
    return Task.CompletedTask;
  }

  /// <summary>
  /// Проверяет, что Agilent принял режим, диапазон и источник запуска.
  /// </summary>
  /// <param name="function">Ожидаемая SCPI-функция.</param>
  /// <param name="range">Ожидаемый диапазон.</param>
  /// <param name="cancellationToken">Токен отмены.</param>
  private void ValidateMode(string function, double range, CancellationToken cancellationToken)
  {
    string actualFunction = NormalizeResponse(Query("FUNC?", cancellationToken));
    if (!actualFunction.StartsWith(function, StringComparison.OrdinalIgnoreCase))
    {
      throw new LegacyAskProtocolException($"Agilent вернул неверный режим измерения: {actualFunction}.");
    }

    string rangeResponse = Query($"{function}:RANG?", cancellationToken);
    double actualRange = ParseMeasurement(rangeResponse);
    if (Math.Abs(actualRange - range) > Math.Max(range * 0.001, 0.000001) && Math.Abs(actualRange / 2.0 - range) > Math.Max(range * 0.001, 0.000001))
    {
      throw new LegacyAskProtocolException($"Agilent вернул неверный диапазон измерения: {rangeResponse.Trim()}.");
    }

    string trigger = NormalizeResponse(Query("TRIG:SOUR?", cancellationToken));
    if (!trigger.StartsWith("IMM", StringComparison.OrdinalIgnoreCase))
    {
      throw new LegacyAskProtocolException($"Agilent вернул неверный источник запуска: {trigger}.");
    }
  }

  /// <summary>
  /// Отправляет команду без чтения ответа.
  /// </summary>
  /// <param name="command">SCPI-команда.</param>
  /// <param name="cancellationToken">Токен отмены.</param>
  private void Write(string command, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    var serialPort = GetSerialPort();
    serialPort.WriteLine(command);
  }

  /// <summary>
  /// Отправляет запрос и читает ответ.
  /// </summary>
  /// <param name="command">SCPI-запрос.</param>
  /// <param name="cancellationToken">Токен отмены.</param>
  /// <returns>Ответ прибора.</returns>
  private string Query(string command, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    var serialPort = GetSerialPort();
    serialPort.DiscardInBuffer();
    serialPort.WriteLine(command);
    return serialPort.ReadLine();
  }

  /// <summary>
  /// Возвращает открытый порт или выбрасывает ошибку настройки.
  /// </summary>
  /// <returns>Открытый COM-порт.</returns>
  private SerialPort GetSerialPort()
  {
    return _serialPort ?? throw new LegacyAskProtocolException("COM-порт Agilent не открыт.");
  }

  /// <summary>
  /// Проверяет, поддерживается ли текущая конфигурация в боевом режиме.
  /// </summary>
  /// <param name="profile">Legacy-конфигурация аппаратуры.</param>
  private static void ValidateRealModeSupport(LegacyMkiHardwareProfile profile)
  {
    if (profile.HardwareConfig.DvV7 == AgilentUsbCode)
    {
      throw new LegacyAskProtocolException("Agilent USB в новом нативном самоконтроле пока не поддержан. Укажите Agilent (COM).");
    }

    if (profile.HardwareConfig.DvV7 != AgilentComCode)
    {
      throw new LegacyAskProtocolException("Боевой режим нативного самоконтроля цифрового вольтметра сейчас поддерживает только Agilent (COM).");
    }

    if (profile.HardwareAux.PortVm.Com1 == 0)
    {
      throw new LegacyAskProtocolException("Для Agilent не задан COM-порт в legacy-конфигурации.");
    }

    if (profile.HardwareAux.PortVm.Com1 > 8)
    {
      throw new LegacyAskProtocolException("Agilent через канал COMx4 пока не поддержан в нативном самоконтроле. Укажите прямой COM-порт.");
    }
  }

  /// <summary>
  /// Создаёт последовательный порт по legacy-настройкам.
  /// </summary>
  /// <param name="port">Настройки порта из legacy-конфигурации.</param>
  /// <returns>Настроенный последовательный порт.</returns>
  private static SerialPort CreateSerialPort(LegacyMkiPortSettings port)
  {
    int timeout = port.MsTmo == 0 ? 12000 : port.MsTmo;
    return new SerialPort($"COM{port.Com1}", GetBaudRate(port.Baud), GetParity(port.Parity), port.Len == 0 ? 8 : port.Len, GetStopBits(port.QStopBit))
    {
      Handshake = Handshake.None,
      NewLine = "\r\n",
      ReadTimeout = timeout,
      WriteTimeout = timeout,
      RtsEnable = (port.RtsDtr & 0x01) != 0,
      DtrEnable = (port.RtsDtr & 0x02) != 0
    };
  }

  /// <summary>
  /// Преобразует ответ прибора в число.
  /// </summary>
  /// <param name="response">Строка ответа Agilent.</param>
  /// <returns>Числовое значение ответа.</returns>
  private static double ParseMeasurement(string response)
  {
    string token = response.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];
    if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
    {
      throw new LegacyAskProtocolException($"Agilent вернул некорректный результат измерения: {response.Trim()}.");
    }

    return value;
  }

  /// <summary>
  /// Нормализует текстовый ответ прибора.
  /// </summary>
  /// <param name="response">Ответ прибора.</param>
  /// <returns>Ответ без кавычек, пробелов и переводов строк.</returns>
  private static string NormalizeResponse(string response)
  {
    return response.Trim().Trim('"').Replace(" ", string.Empty, StringComparison.Ordinal);
  }

  /// <summary>
  /// Преобразует код скорости старой MKI в бод.
  /// </summary>
  /// <param name="baudCode">Код скорости из legacy-конфигурации.</param>
  /// <returns>Скорость обмена.</returns>
  private static int GetBaudRate(byte baudCode)
  {
    int[] baudRates = [1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200];
    return baudCode < baudRates.Length ? baudRates[baudCode] : 9600;
  }

  /// <summary>
  /// Преобразует код паритета старой MKI.
  /// </summary>
  /// <param name="parityCode">Код паритета из legacy-конфигурации.</param>
  /// <returns>Паритет последовательного порта.</returns>
  private static Parity GetParity(byte parityCode)
  {
    return parityCode switch
    {
      1 => Parity.Odd,
      2 => Parity.Even,
      3 => Parity.Space,
      4 => Parity.Mark,
      _ => Parity.None
    };
  }

  /// <summary>
  /// Преобразует количество стоп-бит старой MKI.
  /// </summary>
  /// <param name="stopBits">Количество стоп-бит из legacy-конфигурации.</param>
  /// <returns>Стоп-биты последовательного порта.</returns>
  private static StopBits GetStopBits(byte stopBits)
  {
    return stopBits switch
    {
      2 => StopBits.Two,
      _ => StopBits.One
    };
  }
}

/// <summary>
/// Результат измерения цифрового вольтметра.
/// </summary>
/// <param name="Value">Измеренное значение или граница диапазона при перегрузке.</param>
/// <param name="IsOverload">Признак перегрузки диапазона.</param>
internal sealed record LegacyAskVoltmeterMeasurement(double Value, bool IsOverload);
