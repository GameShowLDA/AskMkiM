using Ask.Core.Services.Config.LegacyMki;
using System.Globalization;
using System.IO.Ports;

namespace Ask.Engine.Tests.SelfControl.LegacyAskProtocol;

/// <summary>
/// Выполняет прямой обмен с контроллером старого тестера АСК по бинарному протоколу MKI.
/// </summary>
public sealed class LegacyAskControllerProtocol : IDisposable
{
  private const byte ReadRegisterMask = 0x20;
  private const byte FunctionMask = 0x80;
  private const byte SaSku = 0x01;

  private readonly LegacyAskControllerProtocolOptions _options;
  private readonly SerialPort? _serialPort;

  /// <summary>
  /// Создает протокол обмена с контроллером АСК.
  /// </summary>
  /// <param name="options">Параметры обмена и режима выполнения.</param>
  public LegacyAskControllerProtocol(LegacyAskControllerProtocolOptions options)
  {
    _options = options ?? throw new ArgumentNullException(nameof(options));
    _serialPort = options.IsIdleMode ? null : CreateSerialPort(options);
  }

  /// <summary>
  /// Открывает последовательный порт и очищает буферы обмена.
  /// </summary>
  public void Open()
  {
    if (_options.IsIdleMode)
    {
      return;
    }

    var serialPort = GetSerialPort();
    if (serialPort.IsOpen)
    {
      return;
    }

    serialPort.Open();
    serialPort.DiscardInBuffer();
    serialPort.DiscardOutBuffer();
  }

  /// <summary>
  /// Читает слово из регистра контроллера АСК.
  /// </summary>
  /// <param name="register">Регистр контроллера.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  public Task<ushort> ReadRegisterAsync(LegacyAskRegister register, CancellationToken cancellationToken)
  {
    byte command = ToReadRegisterCommand(register);
    return SendWordCommandAsync(command, 0, hasArgument: false, cancellationToken);
  }

  /// <summary>
  /// Записывает слово в регистр контроллера АСК.
  /// </summary>
  /// <param name="register">Регистр контроллера.</param>
  /// <param name="value">Записываемое значение.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  public Task WriteRegisterAsync(LegacyAskRegister register, ushort value, CancellationToken cancellationToken)
  {
    byte command = ToWriteRegisterCommand(register);
    return SendStatusCommandAsync(command, value, hasArgument: true, cancellationToken);
  }

  /// <summary>
  /// Записывает слово в подрегистр старого составного регистра MKI.
  /// </summary>
  /// <param name="register">Базовый регистр контроллера.</param>
  /// <param name="subRegister">Номер подрегистра, который старый код передавал через ADWR.</param>
  /// <param name="value">Значение без битов подадреса.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  public Task WriteSubRegisterAsync(LegacyAskRegister register, byte subRegister, ushort value, CancellationToken cancellationToken)
  {
    if (subRegister is 0 or > 7)
    {
      throw new LegacyAskProtocolException("Некорректный номер подрегистра MKI.");
    }

    if ((value & 0xE000) != 0)
    {
      throw new LegacyAskProtocolException("Значение подрегистра MKI содержит занятые биты подадреса.");
    }

    ushort addressedValue = (ushort)(value | (subRegister << 13));
    return WriteRegisterAsync(register, addressedValue, cancellationToken);
  }

  /// <summary>
  /// Читает результат АЦП командой старого контроллера <c>funRDADC</c>.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  public Task<ushort> ReadAdcAsync(CancellationToken cancellationToken)
  {
    return SendWordCommandAsync(ToFunctionCommand(LegacyAskFunction.ReadAdc), 0, hasArgument: false, cancellationToken);
  }

  /// <summary>
  /// Проверяет подключение электронной точки командой старого контроллера <c>funPESOB</c>.
  /// </summary>
  /// <param name="pointAddress">Адрес точки в формате регистра <c>rgADDR</c>: СК, БК и точка.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  public Task CheckElectronicConnectionAsync(ushort pointAddress, CancellationToken cancellationToken)
  {
    return SendStatusCommandAsync(ToFunctionCommand(LegacyAskFunction.CheckElectronicConnection), pointAddress, hasArgument: true, cancellationToken);
  }

  /// <summary>
  /// Проверяет размыкание электронной точки командой старого контроллера <c>funPERZB</c>.
  /// </summary>
  /// <param name="pointAddress">Адрес точки в формате регистра <c>rgADDR</c>: СК, БК и точка.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  public Task CheckElectronicDisconnectionAsync(ushort pointAddress, CancellationToken cancellationToken)
  {
    return SendStatusCommandAsync(ToFunctionCommand(LegacyAskFunction.CheckElectronicDisconnection), pointAddress, hasArgument: true, cancellationToken);
  }

  /// <summary>
  /// Проверяет отсутствие лишнего подключения электронной точки командой старого контроллера <c>funPENOSOB</c>.
  /// </summary>
  /// <param name="pointAddress">Адрес точки в формате регистра <c>rgADDR</c>: СК, БК и точка.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  public Task CheckNoElectronicConnectionAsync(ushort pointAddress, CancellationToken cancellationToken)
  {
    return SendStatusCommandAsync(ToFunctionCommand(LegacyAskFunction.CheckNoElectronicConnection), pointAddress, hasArgument: true, cancellationToken);
  }

  /// <summary>
  /// Записывает регистр команд контроллера АСК.
  /// </summary>
  /// <param name="value">Слово команды.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  public Task WriteCommandRegisterAsync(ushort value, CancellationToken cancellationToken)
  {
    return SendStatusCommandAsync(ToFunctionCommand(LegacyAskFunction.WriteCommandRegister), value, hasArgument: true, cancellationToken);
  }

  /// <summary>
  /// Записывает команду подключения к шинам в контроллер АСК.
  /// </summary>
  /// <param name="value">Слово команды шин.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  public Task WriteBusCommandAsync(ushort value, CancellationToken cancellationToken)
  {
    return SendStatusCommandAsync(ToFunctionCommand(LegacyAskFunction.WriteBusCommand), value, hasArgument: true, cancellationToken);
  }

  /// <summary>
  /// Записывает порог остановки таймера АЦП.
  /// </summary>
  /// <param name="value">Порог остановки.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  public Task SetTimerStopAsync(ushort value, CancellationToken cancellationToken)
  {
    return SendStatusCommandAsync(ToFunctionCommand(LegacyAskFunction.SetTimerStop), value, hasArgument: true, cancellationToken);
  }

  /// <summary>
  /// Запускает таймер АЦП с заданным порогом старта.
  /// </summary>
  /// <param name="value">Порог старта.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  public Task StartTimerAsync(ushort value, CancellationToken cancellationToken)
  {
    return SendStatusCommandAsync(ToFunctionCommand(LegacyAskFunction.StartTimer), value, hasArgument: true, cancellationToken);
  }

  /// <summary>
  /// Читает состояние готовности таймера АЦП.
  /// </summary>
  /// <param name="stopFlag">Флаг ожидания остановки.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  public Task<ushort> ReadTimerReadyAsync(ushort stopFlag, CancellationToken cancellationToken)
  {
    return SendWordCommandAsync(ToFunctionCommand(LegacyAskFunction.ReadTimerReady), stopFlag, hasArgument: true, cancellationToken);
  }

  /// <summary>
  /// Читает слово счетчика таймера АЦП.
  /// </summary>
  /// <param name="offset">Смещение слова счетчика.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  public Task<ushort> ReadTimerWordAsync(ushort offset, CancellationToken cancellationToken)
  {
    return SendWordCommandAsync(ToFunctionCommand(LegacyAskFunction.ReadTimerWord), offset, hasArgument: true, cancellationToken);
  }

  /// <summary>
  /// Записывает количество стробов в контроллер командой <c>funWRQSTROB</c>.
  /// </summary>
  /// <param name="count">Количество импульсов строба.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  public Task WriteStrobeCountAsync(ushort count, CancellationToken cancellationToken)
  {
    return SendStatusCommandAsync(ToFunctionCommand(LegacyAskFunction.WriteStrobeCount), count, hasArgument: true, cancellationToken);
  }

  /// <summary>
  /// Настраивает параметры строба командой <c>funSETSTROB</c>.
  /// </summary>
  /// <param name="pointAddress">Адрес точки в формате регистра <c>rgADDR</c>.</param>
  /// <param name="parameter">Параметр строба, который старый контроллер принимает отдельным байтом.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  public Task SetStrobeAsync(ushort pointAddress, byte parameter, CancellationToken cancellationToken)
  {
    return SendStatusFrameAsync(BuildSetStrobeRequest(pointAddress, parameter), cancellationToken);
  }

  /// <summary>
  /// Освобождает последовательный порт.
  /// </summary>
  public void Dispose()
  {
    _serialPort?.Dispose();
  }

  /// <summary>
  /// Создает настройки протокола из аппаратной legacy-конфигурации.
  /// </summary>
  /// <param name="profile">Аппаратная конфигурация АСК.</param>
  /// <param name="isIdleMode">Признак холостого режима с эмуляцией ответов.</param>
  public static LegacyAskControllerProtocolOptions CreateOptions(
    LegacyMkiHardwareProfile profile,
    bool isIdleMode = false,
    byte networkAddress = SaSku)
  {
    ArgumentNullException.ThrowIfNull(profile);

    var port = profile.HardwareAux.PortSku;
    byte comNumber = port.Com1;
    bool isComx4Channel = comNumber > 8;

    if (comNumber == 0 && !isIdleMode)
    {
      throw new LegacyAskProtocolException("Для контроллера АСК выбран параллельный порт. Боевой режим поддерживает обмен только через COM.");
    }

    if (comNumber == 0 && isIdleMode)
    {
      comNumber = 1;
    }

    if (isComx4Channel)
    {
      if (profile.HardwareConfig.Comx4Com1 is 0 or > 8)
      {
        if (!isIdleMode)
        {
          throw new LegacyAskProtocolException("Для COMx4 не указан физический COM-порт разветвителя.");
        }

        comNumber = 1;
      }
      else
      {
        comNumber = profile.HardwareConfig.Comx4Com1;
      }
    }

    int channel = isComx4Channel ? port.Com1 - 9 : 3;
    return new LegacyAskControllerProtocolOptions(
      PortName: $"COM{comNumber.ToString(CultureInfo.InvariantCulture)}",
      BaudRate: GetBaudRate(port.Baud),
      Parity: GetParity(port.Parity),
      DataBits: port.Len == 0 ? 8 : port.Len,
      StopBits: GetStopBits(port.QStopBit),
      TimeoutMs: port.MsTmo == 0 ? 1000 : port.MsTmo,
      UseNetworkProtocol: profile.HardwareAux.Net != 0,
      NetworkAddress: networkAddress,
      RtsEnable: (channel & 0x01) != 0,
      DtrEnable: (channel & 0x02) != 0,
      IsIdleMode: isIdleMode);
  }

  /// <summary>
  /// Отправляет команду, которая возвращает только статусный байт.
  /// </summary>
  private async Task SendStatusCommandAsync(byte command, ushort value, bool hasArgument, CancellationToken cancellationToken)
  {
    await ExchangeAsync(command, value, hasArgument, expectWord: false, cancellationToken).ConfigureAwait(false);
  }

  /// <summary>
  /// Отправляет команду, которая возвращает статусный байт и слово данных.
  /// </summary>
  private async Task<ushort> SendWordCommandAsync(byte command, ushort value, bool hasArgument, CancellationToken cancellationToken)
  {
    var response = await ExchangeAsync(command, value, hasArgument, expectWord: true, cancellationToken).ConfigureAwait(false);
    return response.Data;
  }

  /// <summary>
  /// Отправляет заранее сформированный кадр команды, которая возвращает только статус.
  /// </summary>
  private async Task SendStatusFrameAsync(byte[] request, CancellationToken cancellationToken)
  {
    if (_options.IsIdleMode)
    {
      cancellationToken.ThrowIfCancellationRequested();
      ValidateStatus(0);
      return;
    }

    using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    timeoutSource.CancelAfter(_options.TimeoutMs);

    try
    {
      Open();
      var serialPort = GetSerialPort();
      byte[] response = new byte[_options.UseNetworkProtocol ? 5 : 1];

      await serialPort.BaseStream.WriteAsync(request, timeoutSource.Token).ConfigureAwait(false);
      await serialPort.BaseStream.FlushAsync(timeoutSource.Token).ConfigureAwait(false);
      await ReadExactlyAsync(response, timeoutSource.Token).ConfigureAwait(false);

      var parsed = ParseResponse(response, expectWord: false);
      ValidateStatus(parsed.Status);
    }
    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
    {
      throw new TimeoutException($"Контроллер АСК не вернул ответ за {_options.TimeoutMs} мс.");
    }
  }

  /// <summary>
  /// Выполняет один цикл записи кадра и чтения ответа контроллера.
  /// </summary>
  private async Task<LegacyAskControllerResponse> ExchangeAsync(
    byte command,
    ushort value,
    bool hasArgument,
    bool expectWord,
    CancellationToken cancellationToken)
  {
    if (_options.IsIdleMode)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var simulatedResponse = SimulateExchange(command, value, expectWord);
      ValidateStatus(simulatedResponse.Status);
      return simulatedResponse;
    }

    using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    timeoutSource.CancelAfter(_options.TimeoutMs);

    try
    {
      Open();
      var serialPort = GetSerialPort();
      byte[] request = BuildRequest(command, value, hasArgument);
      byte[] response = new byte[_options.UseNetworkProtocol ? 5 : expectWord ? 3 : 1];

      await serialPort.BaseStream.WriteAsync(request, timeoutSource.Token).ConfigureAwait(false);
      await serialPort.BaseStream.FlushAsync(timeoutSource.Token).ConfigureAwait(false);
      await ReadExactlyAsync(response, timeoutSource.Token).ConfigureAwait(false);

      var parsed = ParseResponse(response, expectWord);
      ValidateStatus(parsed.Status);
      return parsed;
    }
    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
    {
      throw new TimeoutException($"Контроллер АСК не вернул ответ за {_options.TimeoutMs} мс.");
    }
  }

  /// <summary>
  /// Формирует кадр запроса в формате RS-232 или RS-485 старой MKI.
  /// </summary>
  private byte[] BuildRequest(byte command, ushort value, bool hasArgument)
  {
    byte hi = (byte)(value >> 8);
    byte lo = (byte)value;

    if (_options.UseNetworkProtocol)
    {
      byte address = _options.NetworkAddress;
      byte checksum = (byte)(address + command + hi + lo);
      return [address, command, hi, lo, checksum];
    }

    return hasArgument ? [command, hi, lo] : [command];
  }

  /// <summary>
  /// Формирует кадр настройки строба в том виде, как его ожидает старый контроллер MKI.
  /// </summary>
  private byte[] BuildSetStrobeRequest(ushort pointAddress, byte parameter)
  {
    if (_options.UseNetworkProtocol)
    {
      throw new LegacyAskProtocolException("Команда настройки строба в старом контроллере поддерживается только в режиме RS-232.");
    }

    byte command = ToFunctionCommand(LegacyAskFunction.SetStrobe);
    byte hi = (byte)(pointAddress >> 8);
    byte lo = (byte)pointAddress;
    return [command, hi, lo, parameter];
  }

  /// <summary>
  /// Разбирает ответ контроллера и проверяет сетевой адрес и CRC для RS-485.
  /// </summary>
  private LegacyAskControllerResponse ParseResponse(byte[] response, bool expectWord)
  {
    if (_options.UseNetworkProtocol)
    {
      byte expectedAddress = (byte)(_options.NetworkAddress | 0x80);
      byte actualAddress = response[0];
      if ((actualAddress & 0x7F) != _options.NetworkAddress)
      {
        throw new LegacyAskProtocolException($"Неверный сетевой адрес ответа АСК: 0x{actualAddress:X2}, ожидается 0x{expectedAddress:X2}.");
      }

      byte checksum = (byte)(response[0] + response[1] + response[2] + response[3]);
      if (checksum != response[4])
      {
        throw new LegacyAskProtocolException("Ошибка контрольной суммы ответа АСК.");
      }

      return new LegacyAskControllerResponse(response[1], ToUInt16(response[2], response[3]));
    }

    ushort data = expectWord ? ToUInt16(response[1], response[2]) : (ushort)0;
    return new LegacyAskControllerResponse(response[0], data);
  }

  /// <summary>
  /// Читает из COM-порта ровно заданное количество байт.
  /// </summary>
  private async Task ReadExactlyAsync(byte[] buffer, CancellationToken cancellationToken)
  {
    var serialPort = GetSerialPort();
    int offset = 0;
    while (offset < buffer.Length)
    {
      int read = await serialPort.BaseStream.ReadAsync(buffer.AsMemory(offset), cancellationToken).ConfigureAwait(false);
      if (read == 0)
      {
        throw new TimeoutException("Контроллер АСК не вернул ответ в заданное время.");
      }

      offset += read;
    }
  }

  /// <summary>
  /// Возвращает COM-порт, который используется в боевом режиме.
  /// </summary>
  private SerialPort GetSerialPort()
  {
    return _serialPort ?? throw new InvalidOperationException("COM-порт не создается в холостом режиме.");
  }

  /// <summary>
  /// Эмулирует ответ контроллера АСК тем же протокольным путем, что и боевой обмен.
  /// </summary>
  /// <summary>
  /// Возвращает команду чтения регистра старого контроллера.
  /// </summary>
  private static byte ToReadRegisterCommand(LegacyAskRegister register)
  {
    return (byte)(ToWriteRegisterCommand(register) | ReadRegisterMask);
  }

  /// <summary>
  /// Возвращает команду записи регистра старого контроллера.
  /// </summary>
  private static byte ToWriteRegisterCommand(LegacyAskRegister register)
  {
    return (byte)((((ushort)register) & 0x3F) >> 1);
  }

  private static LegacyAskControllerResponse SimulateExchange(byte command, ushort value, bool expectWord)
  {
    ushort data = expectWord ? SimulateWord(command, value) : (ushort)0;
    return new LegacyAskControllerResponse(0, data);
  }

  /// <summary>
  /// Возвращает слово данных для эмулированного ответа контроллера.
  /// </summary>
  private static ushort SimulateWord(byte command, ushort value)
  {
    if (command == ToFunctionCommand(LegacyAskFunction.ReadTimerReady))
    {
      return 0x0008;
    }

    if (command == ToReadRegisterCommand(LegacyAskRegisters.PpuNetCommand))
    {
      return (ushort)(LegacyAskPpuNetBits.PpuReady | LegacyAskPpuNetBits.PkiReady);
    }

    if (command == ToReadRegisterCommand(LegacyAskRegisters.PpuMkiCommand))
    {
      return LegacyAskPpuMkiBits.Good;
    }

    if (command == ToFunctionCommand(LegacyAskFunction.ReadTimerWord) ||
        command == ToFunctionCommand(LegacyAskFunction.ReadAdc))
    {
      return 0;
    }

    return 0;
  }

  /// <summary>
  /// Проверяет статусный байт контроллера и преобразует его в понятную ошибку.
  /// </summary>
  private static void ValidateStatus(byte status)
  {
    if ((status & 0x10) != 0)
    {
      throw new LegacyAskProtocolException("Контроллер АСК сообщил сбой по питанию.");
    }

    if ((status & 0x20) != 0)
    {
      throw new LegacyAskProtocolException("Контроллер АСК сообщил ошибку паритета обмена.");
    }

    if ((status & 0x40) != 0)
    {
      throw new LegacyAskProtocolException("Контроллер АСК сообщил тайм-аут приема команды.");
    }

    if ((status & 0x80) != 0)
    {
      throw new LegacyAskProtocolException("Контроллер АСК не поддерживает отправленную команду.");
    }

    byte lowStatus = (byte)(status & 0x0F);
    if (lowStatus != 0)
    {
      throw new LegacyAskProtocolException(GetControllerErrorMessage(lowStatus));
    }
  }

  /// <summary>
  /// Возвращает текст ошибки по младшему полубайту статуса контроллера.
  /// </summary>
  private static string GetControllerErrorMessage(byte status)
  {
    return status switch
    {
      1 => "Контроллер АСК сообщил ошибку записи регистра.",
      2 => "Контроллер АСК сообщил ошибку исполнения команды.",
      3 => "Контроллер АСК сообщил отсутствие соединения.",
      4 => "Контроллер АСК сообщил лишнюю связь.",
      _ => $"Контроллер АСК вернул код ошибки {status}."
    };
  }

  /// <summary>
  /// Преобразует функцию старого контроллера в командный байт.
  /// </summary>
  private static byte ToFunctionCommand(LegacyAskFunction function)
  {
    return (byte)((byte)function | FunctionMask);
  }

  /// <summary>
  /// Собирает слово из старшего и младшего байта в порядке старой MKI.
  /// </summary>
  private static ushort ToUInt16(byte hi, byte lo)
  {
    return (ushort)((hi << 8) | lo);
  }

  /// <summary>
  /// Создает и настраивает последовательный порт.
  /// </summary>
  private static SerialPort CreateSerialPort(LegacyAskControllerProtocolOptions options)
  {
    return new SerialPort(options.PortName, options.BaudRate, options.Parity, options.DataBits, options.StopBits)
    {
      Handshake = Handshake.None,
      ReadTimeout = options.TimeoutMs,
      WriteTimeout = options.TimeoutMs,
      RtsEnable = options.RtsEnable,
      DtrEnable = options.DtrEnable
    };
  }

  /// <summary>
  /// Преобразует код скорости из старого mki_hrd.cfg в бод.
  /// </summary>
  private static int GetBaudRate(byte baudCode)
  {
    int[] baudRates = [1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200];
    return baudCode < baudRates.Length ? baudRates[baudCode] : 9600;
  }

  /// <summary>
  /// Преобразует код паритета старой MKI в значение <see cref="Parity"/>.
  /// </summary>
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
  /// Преобразует количество стоп-бит старой MKI в значение <see cref="StopBits"/>.
  /// </summary>
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
/// Настройки прямого обмена с контроллером старого тестера АСК.
/// </summary>
public sealed record LegacyAskControllerProtocolOptions(
  string PortName,
  int BaudRate,
  Parity Parity,
  int DataBits,
  StopBits StopBits,
  int TimeoutMs,
  bool UseNetworkProtocol,
  byte NetworkAddress,
  bool RtsEnable,
  bool DtrEnable,
  bool IsIdleMode);

/// <summary>
/// Ответ контроллера АСК на один бинарный кадр.
/// </summary>
public sealed record LegacyAskControllerResponse(byte Status, ushort Data);

/// <summary>
/// Исключение ошибки обмена по протоколу старого контроллера АСК.
/// </summary>
public sealed class LegacyAskProtocolException : Exception
{
  /// <summary>
  /// Создает исключение протокола обмена с АСК.
  /// </summary>
  /// <param name="message">Текст ошибки.</param>
  public LegacyAskProtocolException(string message)
    : base(message)
  {
  }
}
