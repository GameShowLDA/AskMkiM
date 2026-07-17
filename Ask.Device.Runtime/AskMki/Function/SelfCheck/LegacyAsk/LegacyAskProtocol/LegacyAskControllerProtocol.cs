using Ask.Core.Services.Config.LegacyMki;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.Tests.SelfControl.LegacyAskProtocol;

/// <summary>
/// Выполняет прямой обмен с контроллером старого тестера АСК по бинарному протоколу MKI.
/// </summary>
public sealed partial class LegacyAskControllerProtocol : IDisposable
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
  private Task SendStatusFrameAsync(byte[] request, CancellationToken cancellationToken)
  {
    if (_options.IsIdleMode)
    {
      cancellationToken.ThrowIfCancellationRequested();
      ValidateStatus(0);
      return Task.CompletedTask;
    }

    try
    {
      cancellationToken.ThrowIfCancellationRequested();
      Open();
      var serialPort = GetSerialPort();
      byte[] response = new byte[_options.UseNetworkProtocol ? 5 : 1];

      LogFrame("TX", request, "кадр");
      serialPort.Write(request, 0, request.Length);
      ReadExactly(response, cancellationToken);
      LogFrame("RX", response, "кадр");

      var parsed = ParseResponse(response, expectWord: false);
      ValidateStatus(parsed.Status);
      return Task.CompletedTask;
    }

    catch (TimeoutException ex)
    {
      LogError($"АСК timeout: кадр не получил ответ за {_options.TimeoutMs} мс. {ex.Message}", isDeviceLog: true);
      throw new TimeoutException($"Контроллер АСК не вернул ответ за {_options.TimeoutMs} мс.", ex);
    }
    catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException)
    {
      LogError($"АСК ошибка обмена кадром: {ex}", isDeviceLog: true);
      throw;
    }
  }

  /// <summary>
  /// Выполняет один цикл записи кадра и чтения ответа контроллера.
  /// </summary>
  private Task<LegacyAskControllerResponse> ExchangeAsync(
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
      LogInformation($"АСК idle: command=0x{command:X2}, value=0x{value:X4}, status=0x{simulatedResponse.Status:X2}, data=0x{simulatedResponse.Data:X4}", isDeviceLog: true);
      return Task.FromResult(simulatedResponse);
    }

    try
    {
      cancellationToken.ThrowIfCancellationRequested();
      Open();
      var serialPort = GetSerialPort();
      byte[] request = BuildRequest(command, value, hasArgument);
      byte[] response = new byte[_options.UseNetworkProtocol ? 5 : expectWord ? 3 : 1];

      LogFrame("TX", request, $"command=0x{command:X2}, value=0x{value:X4}, word={expectWord}");
      serialPort.Write(request, 0, request.Length);
      ReadExactly(response, cancellationToken);
      LogFrame("RX", response, $"command=0x{command:X2}");

      var parsed = ParseResponse(response, expectWord);
      ValidateStatus(parsed.Status);
      LogInformation($"АСК ответ: command=0x{command:X2}, status=0x{parsed.Status:X2}, data=0x{parsed.Data:X4}", isDeviceLog: true);
      return Task.FromResult(parsed);
    }
    catch (TimeoutException ex)
    {
      LogError($"АСК timeout: command=0x{command:X2}, value=0x{value:X4}, word={expectWord}, timeout={_options.TimeoutMs} мс. {ex.Message}", isDeviceLog: true);
      throw new TimeoutException($"Контроллер АСК не вернул ответ за {_options.TimeoutMs} мс на команду 0x{command:X2}.", ex);
    }
    catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException or LegacyAskProtocolException)
    {
      LogError($"АСК ошибка обмена: command=0x{command:X2}, value=0x{value:X4}, word={expectWord}, error={ex}", isDeviceLog: true);
      throw;
    }
  }

  /// <summary>
  /// Формирует кадр запроса в формате RS-232 или RS-485 старой MKI.
  /// </summary>
}
