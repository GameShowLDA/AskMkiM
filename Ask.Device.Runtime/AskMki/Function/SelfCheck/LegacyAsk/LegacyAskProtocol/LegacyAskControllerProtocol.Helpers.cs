using Ask.Core.Services.Config.LegacyMki;
using System.Globalization;
using System.IO.Ports;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.Tests.SelfControl.LegacyAskProtocol;

/// <summary>
/// Выполняет прямой обмен с контроллером старого тестера АСК по бинарному протоколу MKI.
/// </summary>

public sealed partial class LegacyAskControllerProtocol
{
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
  private void ReadExactly(byte[] buffer, CancellationToken cancellationToken)
  {
    var serialPort = GetSerialPort();
    int offset = 0;
    while (offset < buffer.Length)
    {
      cancellationToken.ThrowIfCancellationRequested();
      int read = serialPort.Read(buffer, offset, buffer.Length - offset);
      if (read == 0)
      {
        throw new TimeoutException("Контроллер АСК не вернул ответ в заданное время.");
      }

      offset += read;
    }
  }

  /// <summary>
  /// Записывает в лог кадр обмена с контроллером АСК.
  /// </summary>
  private static void LogFrame(string direction, byte[] frame, string details)
  {
    string bytes = string.Join(" ", frame.Select(value => value.ToString("X2", CultureInfo.InvariantCulture)));
    LogInformation($"АСК {direction}: {bytes}; {details}", isDeviceLog: true);
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
