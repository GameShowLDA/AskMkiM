using System.Text;
using System.Text.Json;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;
using Ask.Core.Shared.Metadata.Commands.MultimeterCommands.Connected;
using Ask.Device.Communication.Usb;
using Ask.Device.Communication.Usb.Discovery;
using Ask.Device.Runtime.AskMkiM.Function.MikUps1101rRm.ViewPower;
using Ask.Device.Runtime.Base.DeviceProtocol;
using Ivi.Visa;
using NationalInstruments.Visa;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.AskMkiM.Function.Base
{
  /// <summary>
  /// Обрабатывает выполнение USB-команд для устройств, поддерживающих обмен данными
  /// через USBTMC (VISA) и ViewPower.
  /// </summary>
  public sealed class UsbCommandHandler : IUsbCommandHandler
  {
    /// <summary>
    /// Время ожидания ответа устройства по умолчанию, в миллисекундах.
    /// </summary>
    private const int DefaultTimeout = 5000;

    /// <summary>
    /// Команда проверки доступности ИБП через USB.
    /// </summary>
    private const string UpsConnectCommand = "UPS:CONNECT";

    /// <summary>
    /// Команда включения выходного питания ИБП.
    /// </summary>
    private const string UpsStartPowerCommand = "UPS:POWER:START";

    /// <summary>
    /// Команда отключения выходного питания ИБП.
    /// </summary>
    private const string UpsStopPowerCommand = "UPS:POWER:STOP";

    /// <summary>
    /// Команда проверки состояния выходного питания ИБП.
    /// </summary>
    private const string UpsVerifyPowerCommand = "UPS:POWER:VERIFY";

    /// <summary>
    /// Задержка выполнения команды управления ИБП в минутах,
    /// передаваемая протоколу ViewPower.
    /// </summary>
    private const string ControlDelayMinutes = "0.2";

    /// <summary>
    /// Максимальное время ожидания подтверждения включения выходного питания ИБП.
    /// </summary>
    private static readonly TimeSpan StartStateConfirmationTimeout = TimeSpan.FromSeconds(8);

    /// <summary>
    /// Максимальное время ожидания подтверждения отключения выходного питания ИБП.
    /// </summary>
    private static readonly TimeSpan StopStateConfirmationTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Список режимов работы, при которых выходное питание ИБП считается включённым.
    /// </summary>
    private static readonly string[] ActiveWorkModes =
    {
      "Line mode",
      "Battery mode",
      "Battery test mode",
      "Fault mode",
      "ECO mode",
      "Converter mode",
      "AVR mode",
      "Power on mode",
    };

    /// <inheritdoc />
    public async Task<string> ExecuteAsync(
      IDevice device,
      string command,
      double responseDelay = 0,
      int timeout = 0,
      int port = 0,
      int delayBeforeCall = 0,
      CancellationToken cancellationToken = default)
    {
      ArgumentNullException.ThrowIfNull(device);

      if (device is not DeviceWithUSB usbDevice)
      {
        throw new InvalidOperationException("UsbCommandHandler supports only USB devices.");
      }

      if (delayBeforeCall > 0)
      {
        await Task.Delay(delayBeforeCall, cancellationToken);
      }

      string pattern = GetUsbSearchPattern(device);
      bool found = ResolveUsbDevice(device, usbDevice, pattern, out var descriptor);
      UsbConnectedProfile profile = usbDevice.ConnectedProfile;
      int effectiveTimeout = timeout <= 0 ? GetProfileTimeout(profile) : timeout;

      string response = profile.UseViewPower
        ? await ExecuteViewPowerCommandAsync(device, command, found, descriptor, responseDelay, effectiveTimeout, port, cancellationToken)
          .ConfigureAwait(false)
        : await Task.Run(
          () => ExecuteVisaCommand(command, pattern, profile, effectiveTimeout, responseDelay),
          cancellationToken).ConfigureAwait(false);

      LogInformation($"[{device.Name}] USB Query: {command} -> {response}", isDeviceLog: true);
      return response;
    }

    /// <summary>
    /// Выполняет SCPI-команду через интерфейс VISA.
    /// </summary>
    /// <param name="command">SCPI-команда для отправки устройству.</param>
    /// <param name="pattern">Шаблон поиска USB-ресурса VISA.</param>
    /// <param name="profile">Профиль параметров USB-подключения.</param>
    /// <param name="timeout">Время ожидания ответа устройства, в миллисекундах.</param>
    /// <param name="responseDelay">Дополнительная задержка перед чтением ответа, в миллисекундах.</param>
    /// <returns>Ответ устройства либо пустая строка, если команда не предполагает ответа.</returns>
    /// <exception cref="ArgumentException">
    /// Выбрасывается, если команда не указана.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если ресурс VISA не поддерживает обмен сообщениями
    /// либо произошла ошибка библиотеки VISA.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// Выбрасывается при превышении времени ожидания ответа устройства.
    /// </exception>
    private static string ExecuteVisaCommand(
      string command,
      string pattern,
      UsbConnectedProfile profile,
      int timeout,
      double responseDelay)
    {
      if (string.IsNullOrWhiteSpace(command))
      {
        throw new ArgumentException("USB-SCPI command is not specified.", nameof(command));
      }

      using var resourceManager = new ResourceManager();
      string resourceName = FindInstrumentResource(resourceManager, pattern, profile);

      using IVisaSession session = OpenSessionWithRetry(resourceManager, resourceName, profile);
      if (session is not MessageBasedSession messageSession)
      {
        throw new InvalidOperationException($"VISA resource \"{resourceName}\" does not support message-based exchange.");
      }

      messageSession.TimeoutMilliseconds = timeout;
      messageSession.SendEndEnabled = profile.SendEndEnabled;
      messageSession.TerminationCharacter = profile.TerminationCharacter;
      messageSession.TerminationCharacterEnabled = profile.TerminationCharacterEnabled;

      try
      {
        messageSession.RawIO.Write(profile.AppendLineEnding ? EnsureLineEnding(command) : command);

        if (!command.Contains('?', StringComparison.Ordinal))
        {
          return string.Empty;
        }

        if (responseDelay > 0)
        {
          Thread.Sleep((int)Math.Ceiling(responseDelay));
        }

        return ReadResponse(messageSession, command, profile.ReadBufferSize);
      }
      catch (IOTimeoutException ex)
      {
        throw new TimeoutException(
          $"VISA timeout while executing \"{command}\" through \"{resourceName}\" for {timeout} ms.",
          ex);
      }
      catch (VisaException ex)
      {
        throw new InvalidOperationException($"VISA.NET error while executing \"{command}\" through \"{resourceName}\": {ex.Message}", ex);
      }
    }

    /// <summary>
    /// Выполняет команду управления ИБП через ViewPower и возвращает
    /// сериализованный ответ протокола.
    /// </summary>
    /// <param name="device">Устройство, для которого выполняется команда.</param>
    /// <param name="command">Команда управления ИБП.</param>
    /// <param name="found">Признак успешного обнаружения USB-устройства.</param>
    /// <param name="descriptor">Описание обнаруженного USB-устройства.</param>
    /// <param name="responseDelay">Дополнительная задержка перед возвратом результата, в миллисекундах.</param>
    /// <param name="timeout">Время ожидания выполнения команды, в миллисекундах.</param>
    /// <param name="port">Номер порта, используемый при взаимодействии.</param>
    /// <param name="cancellationToken">Маркер отмены операции.</param>
    /// <returns>JSON-строка с результатом выполнения команды.</returns>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если устройство не поддерживает работу через ViewPower.
    /// </exception>
    private static async Task<string> ExecuteViewPowerCommandAsync(
      IDevice device,
      string command,
      bool found,
      UsbDeviceDescriptor descriptor,
      double responseDelay,
      int timeout,
      int port,
      CancellationToken cancellationToken)
    {
      if (device is not IUninterruptiblePowerSupply)
      {
        throw new InvalidOperationException("ViewPower USB mode supports only UPS devices.");
      }

      UpsProtocolResponse payload = await ExecuteUpsCommandAsync(device, command, found, descriptor, cancellationToken)
        .ConfigureAwait(false);

      if (responseDelay > 0)
      {
        await Task.Delay((int)Math.Ceiling(responseDelay), cancellationToken).ConfigureAwait(false);
      }

      payload.Timeout = timeout;
      payload.Port = port;
      return JsonSerializer.Serialize(payload);
    }

    /// <summary>
    /// Выполняет команду управления ИБП через ViewPower.
    /// </summary>
    /// <param name="device">Устройство, для которого выполняется команда.</param>
    /// <param name="command">Команда управления ИБП.</param>
    /// <param name="found">Признак успешного обнаружения USB-устройства.</param>
    /// <param name="descriptor">Описание обнаруженного USB-устройства.</param>
    /// <param name="cancellationToken">Маркер отмены операции.</param>
    /// <returns>Объект с результатом выполнения команды.</returns>
    private static async Task<UpsProtocolResponse> ExecuteUpsCommandAsync(
      IDevice device,
      string command,
      bool found,
      UsbDeviceDescriptor descriptor,
      CancellationToken cancellationToken)
    {
      var response = CreateBaseResponse(command, found, descriptor);

      if (!found)
      {
        response.Success = false;
        response.Error = $"UPS \"{device.ConnectionDetails}\" was not found in the system USB devices.";
        return response;
      }

      try
      {
        using var client = new ViewPowerClient();
        ViewPowerSessionContext session = await client.OpenSessionAsync(cancellationToken).ConfigureAwait(false);
        ViewPowerMonitorSnapshot snapshot = await client.GetMonitorDataAsync(session.PortName, cancellationToken).ConfigureAwait(false);

        response.Transport = "VIEWPOWER-HTTP";
        response.ViewPowerAvailable = true;
        response.PortName = session.PortName;
        response.ProtocolType = string.IsNullOrWhiteSpace(snapshot.ProtocolType) ? session.ProtocolType : snapshot.ProtocolType;
        response.OutputOn = IsPowerEnabled(snapshot);
        response.WorkMode = snapshot.WorkMode;
        response.ViewPowerDeviceId = snapshot.DeviceId;

        switch (command)
        {
          case UpsConnectCommand:
            response.Success = true;
            response.Message = $"USB found. ViewPower port: {response.PortName}. Work mode: {response.WorkMode}.";
            return response;

          case UpsVerifyPowerCommand:
            response.Success = true;
            response.Message = response.OutputOn ? "UPS output power is enabled." : "UPS output power is disabled.";
            return response;

          case UpsStartPowerCommand:
            return await ExecuteRealtimeControlAsync(
              client,
              response,
              snapshot,
              expectedState: true,
              "powerCtrlON",
              StartStateConfirmationTimeout,
              cancellationToken).ConfigureAwait(false);

          case UpsStopPowerCommand:
            return await ExecuteRealtimeControlAsync(
              client,
              response,
              snapshot,
              expectedState: false,
              "powerCtrlOFF",
              StopStateConfirmationTimeout,
              cancellationToken).ConfigureAwait(false);

          default:
            response.Success = true;
            response.Message = "USB device resolved.";
            return response;
        }
      }
      catch (Exception ex)
      {
        response.Transport = "VIEWPOWER-HTTP";
        response.Success = false;
        response.Error = ex.Message;
        return response;
      }
    }

    /// <summary>
    /// Выполняет команду управления выходным питанием ИБП через ViewPower
    /// и ожидает подтверждения изменения состояния.
    /// </summary>
    /// <param name="client">Клиент взаимодействия с ViewPower.</param>
    /// <param name="response">Объект, в который записывается результат выполнения команды.</param>
    /// <param name="snapshot">Текущее состояние ИБП.</param>
    /// <param name="expectedState">
    /// Ожидаемое состояние выходного питания после выполнения команды.
    /// </param>
    /// <param name="controlType">Тип команды управления ViewPower.</param>
    /// <param name="confirmationTimeout">
    /// Максимальное время ожидания подтверждения изменения состояния.
    /// </param>
    /// <param name="cancellationToken">Маркер отмены операции.</param>
    /// <returns>Объект с результатом выполнения команды управления.</returns>
    private static async Task<UpsProtocolResponse> ExecuteRealtimeControlAsync(
      ViewPowerClient client,
      UpsProtocolResponse response,
      ViewPowerMonitorSnapshot snapshot,
      bool expectedState,
      string controlType,
      TimeSpan confirmationTimeout,
      CancellationToken cancellationToken)
    {
      bool currentState = IsPowerEnabled(snapshot);
      if (currentState == expectedState)
      {
        response.Success = true;
        response.OutputOn = currentState;
        response.WorkMode = snapshot.WorkMode;
        response.Message = expectedState
          ? "UPS output power is already enabled."
          : "UPS output power is already disabled.";
        return response;
      }

      await client.InitializeRealTimeControlAsync(
        snapshot.PortName,
        snapshot.ProtocolType,
        cancellationToken).ConfigureAwait(false);

      ViewPowerCommandResult commandResult = await client.SendRealTimeControlAsync(
        snapshot.PortName,
        controlType,
        ControlDelayMinutes,
        cancellationToken).ConfigureAwait(false);

      ViewPowerMonitorSnapshot confirmedSnapshot = await client.WaitForMonitorStateAsync(
        snapshot.PortName,
        nextSnapshot => IsPowerEnabled(nextSnapshot) == expectedState,
        confirmationTimeout,
        cancellationToken).ConfigureAwait(false);

      response.RawResponse = commandResult.ResponseText;
      response.OutputOn = IsPowerEnabled(confirmedSnapshot);
      response.WorkMode = confirmedSnapshot.WorkMode;
      response.ViewPowerDeviceId = confirmedSnapshot.DeviceId;
      response.Success = response.OutputOn == expectedState;

      if (response.Success)
      {
        response.Message = expectedState
          ? "UPS output power was enabled."
          : "UPS output power was disabled.";
      }
      else
      {
        response.Error = commandResult.Accepted
          ? "ViewPower accepted the command, but UPS state did not change in time."
          : $"ViewPower command was rejected: {commandResult.ResponseText}";
      }

      return response;
    }

    /// <summary>
    /// Считывает ответ устройства из сеанса VISA.
    /// </summary>
    /// <param name="session">Сеанс обмена сообщениями VISA.</param>
    /// <param name="command">Команда, для которой ожидается ответ.</param>
    /// <param name="readBufferSize">Размер буфера чтения в байтах.</param>
    /// <returns>Ответ устройства в виде строки.</returns>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если устройство не вернуло данных.
    /// </exception>
    private static string ReadResponse(MessageBasedSession session, string command, int readBufferSize)
    {
      int bufferSize = readBufferSize <= 0 ? 4096 : readBufferSize;
      byte[] buffer = new byte[bufferSize];
      session.RawIO.Read(buffer, 0, buffer.Length, out long readCount, out ReadStatus readStatus);

      if (readCount <= 0)
      {
        throw new InvalidOperationException($"viRead({command}) returned no data. ReadStatus: {readStatus}.");
      }

      return Encoding.ASCII.GetString(buffer, 0, (int)readCount).Trim('\0', '\r', '\n', ' ');
    }

    /// <summary>
    /// Открывает сеанс VISA с повторными попытками при возникновении
    /// временных ошибок подключения.
    /// </summary>
    /// <param name="resourceManager">Менеджер ресурсов VISA.</param>
    /// <param name="resourceName">Имя ресурса VISA.</param>
    /// <param name="profile">Профиль параметров USB-подключения.</param>
    /// <returns>Открытый сеанс VISA.</returns>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если не удалось открыть сеанс после всех попыток.
    /// </exception>
    private static IVisaSession OpenSessionWithRetry(
      ResourceManager resourceManager,
      string resourceName,
      UsbConnectedProfile profile)
    {
      Exception? lastError = null;
      int retryCount = profile.OpenRetryCount <= 0 ? 1 : profile.OpenRetryCount;
      int retryDelayMs = Math.Max(0, profile.OpenRetryDelayMs);

      for (int attempt = 1; attempt <= retryCount; attempt++)
      {
        try
        {
          return resourceManager.Open(resourceName);
        }
        catch (Exception ex) when (IsRetryableVisaOpenException(ex) && attempt < retryCount)
        {
          lastError = ex;
          Thread.Sleep(retryDelayMs * attempt);
        }
      }

      throw new InvalidOperationException(
        $"Unable to open USB VISA session for resource \"{resourceName}\" after {retryCount} attempts. Check that the instrument is not opened by another process.",
        lastError);
    }

    /// <summary>
    /// Определяет, является ли ошибка открытия сеанса VISA временной
    /// и допускает повторную попытку подключения.
    /// </summary>
    /// <param name="exception">Проверяемое исключение.</param>
    /// <returns>
    /// <see langword="true"/>, если исключение допускает повторную попытку открытия сеанса;
    /// иначе <see langword="false"/>.
    /// </returns>
    private static bool IsRetryableVisaOpenException(Exception exception)
    {
      return exception is VisaException || exception is NativeVisaException;
    }

    /// <summary>
    /// Находит ресурс USBTMC VISA, соответствующий заданному шаблону поиска.
    /// </summary>
    /// <param name="resourceManager">Менеджер ресурсов VISA.</param>
    /// <param name="pattern">Шаблон поиска USB-устройства.</param>
    /// <param name="profile">Профиль параметров USB-подключения.</param>
    /// <returns>Имя найденного ресурса VISA.</returns>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если подходящий ресурс VISA не найден.
    /// </exception>
    private static string FindInstrumentResource(
      ResourceManager resourceManager,
      string pattern,
      UsbConnectedProfile profile)
    {
      string resourcePattern = string.IsNullOrWhiteSpace(profile.VisaResourcePattern)
        ? "USB?*INSTR"
        : profile.VisaResourcePattern;

      List<string> resources;
      try
      {
        resources = resourceManager.Find(resourcePattern).ToList();
      }
      catch (Exception ex) when (ex is VisaException or NativeVisaException)
      {
        throw new InvalidOperationException(
          $"USBTMC VISA resources are not available by pattern \"{resourcePattern}\". Device pattern: \"{pattern}\". {ex.Message}",
          ex);
      }

      string? matched = resources.FirstOrDefault(resource => IsResourceMatch(resource, pattern));
      if (!string.IsNullOrWhiteSpace(matched))
      {
        return matched;
      }

      if (resources.Count == 1)
      {
        return resources[0];
      }

      string foundResources = resources.Count == 0
        ? "none"
        : string.Join(", ", resources);

      throw new InvalidOperationException(
        $"USBTMC VISA resource was not found by pattern \"{pattern}\". Found USBTMC resources: {foundResources}");
    }

    /// <summary>
    /// Проверяет соответствие ресурса VISA заданному шаблону поиска.
    /// </summary>
    /// <param name="resource">Имя ресурса VISA.</param>
    /// <param name="pattern">Шаблон поиска устройства.</param>
    /// <returns>
    /// <see langword="true"/>, если ресурс соответствует шаблону;
    /// иначе <see langword="false"/>.
    /// </returns>
    private static bool IsResourceMatch(string resource, string pattern)
    {
      if (string.IsNullOrWhiteSpace(pattern))
      {
        return true;
      }

      if (resource.Contains(pattern, StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }

      var match = System.Text.RegularExpressions.Regex.Match(
        pattern,
        @"VID_([0-9A-F]{4})&PID_([0-9A-F]{4})",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase);

      if (!match.Success)
      {
        return false;
      }

      string vid = $"0x{match.Groups[1].Value}";
      string pid = $"0x{match.Groups[2].Value}";
      return resource.Contains(vid, StringComparison.OrdinalIgnoreCase) &&
             resource.Contains(pid, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Добавляет символ окончания строки к команде, если он отсутствует.
    /// </summary>
    /// <param name="command">Команда для отправки устройству.</param>
    /// <returns>Команда, оканчивающаяся символом новой строки.</returns>
    private static string EnsureLineEnding(string command)
    {
      return command.EndsWith("\n", StringComparison.Ordinal)
        ? command
        : command + "\n";
    }

    /// <summary>
    /// Возвращает шаблон поиска USB-устройства.
    /// </summary>
    /// <param name="device">Устройство, для которого выполняется поиск.</param>
    /// <returns>
    /// Строка поиска, основанная на сведениях о подключении устройства
    /// или его имени.
    /// </returns>
    private static string GetUsbSearchPattern(IDevice device)
    {
      return string.IsNullOrWhiteSpace(device.ConnectionDetails)
        ? device.Name
        : device.ConnectionDetails;
    }

    /// <summary>
    /// Возвращает эффективное время ожидания ответа устройства.
    /// </summary>
    /// <param name="profile">Профиль параметров USB-подключения.</param>
    /// <returns>
    /// Время ожидания в миллисекундах.
    /// Если в профиле указано недопустимое значение, возвращается значение по умолчанию.
    /// </returns>
    private static int GetProfileTimeout(UsbConnectedProfile profile)
    {
      return profile.Timeout <= 0 ? DefaultTimeout : profile.Timeout;
    }

    /// <summary>
    /// Выполняет поиск USB-устройства и сохраняет сведения
    /// о последнем успешно найденном устройстве.
    /// </summary>
    /// <param name="device">Устройство, для которого выполняется поиск.</param>
    /// <param name="usbDevice">USB-устройство с профилем подключения.</param>
    /// <param name="pattern">Шаблон поиска USB-устройства.</param>
    /// <param name="descriptor">
    /// При успешном поиске содержит описание найденного USB-устройства.
    /// </param>
    /// <returns>
    /// <see langword="true"/>, если устройство найдено;
    /// иначе <see langword="false"/>.
    /// </returns>
    private static bool ResolveUsbDevice(
      IDevice device,
      DeviceWithUSB usbDevice,
      string pattern,
      out UsbDeviceDescriptor descriptor)
    {
      bool found = UsbDeviceLocator.TryFindByName(pattern, out descriptor);
      string resolvedPath = found ? descriptor.DeviceId : string.Empty;

      usbDevice.ConnectedProfile.LastResolvedDevicePath = resolvedPath;
      SetCompatibleLastResolvedDevicePath(device, resolvedPath);
      return found;
    }

    /// <summary>
    /// Сохраняет путь к последнему найденному USB-устройству
    /// в совместимом свойстве устройства, если оно существует.
    /// </summary>
    /// <param name="device">Устройство, для которого обновляется путь.</param>
    /// <param name="path">Путь к последнему найденному USB-устройству.</param>
    private static void SetCompatibleLastResolvedDevicePath(IDevice device, string path)
    {
      var property = device.GetType().GetProperty("LastResolvedDevicePath");
      if (property?.CanWrite == true && property.PropertyType == typeof(string))
      {
        property.SetValue(device, path);
      }
    }

    /// <summary>
    /// Определяет, включено ли выходное питание ИБП.
    /// </summary>
    /// <param name="snapshot">Текущее состояние ИБП.</param>
    /// <returns>
    /// <see langword="true"/>, если выходное питание включено;
    /// иначе <see langword="false"/>.
    /// </returns>
    private static bool IsPowerEnabled(ViewPowerMonitorSnapshot snapshot)
    {
      if (snapshot.OutputOn)
      {
        return true;
      }

      return ActiveWorkModes.Any(mode => string.Equals(mode, snapshot.WorkMode, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Создаёт базовый объект ответа для выполнения команды управления ИБП.
    /// </summary>
    /// <param name="command">Выполняемая команда.</param>
    /// <param name="found">Признак успешного обнаружения USB-устройства.</param>
    /// <param name="descriptor">Описание обнаруженного USB-устройства.</param>
    /// <returns>Инициализированный объект ответа.</returns>
    private static UpsProtocolResponse CreateBaseResponse(string command, bool found, UsbDeviceDescriptor descriptor)
    {
      return new UpsProtocolResponse
      {
        Transport = "USB-HID",
        DeviceType = "UninterruptiblePowerSupply",
        Command = command,
        Found = found,
        DeviceName = found ? descriptor.Name : string.Empty,
        DeviceId = found ? descriptor.DeviceId : string.Empty,
        PnpDeviceId = found ? descriptor.PnpDeviceId : string.Empty,
        Service = found ? descriptor.Service : string.Empty,
      };
    }

    /// <summary>
    /// Представляет результат выполнения команды управления ИБП,
    /// полученный через USB или ViewPower.
    /// </summary>
    private sealed class UpsProtocolResponse
    {
      /// <summary>
      /// Наименование используемого транспорта.
      /// </summary>
      public string Transport { get; set; } = string.Empty;

      /// <summary>
      /// Тип устройства.
      /// </summary>
      public string DeviceType { get; set; } = string.Empty;

      /// <summary>
      /// Выполненная команда.
      /// </summary>
      public string Command { get; set; } = string.Empty;

      /// <summary>
      /// Признак успешного обнаружения устройства.
      /// </summary>
      public bool Found { get; set; }

      /// <summary>
      /// Признак успешного выполнения команды.
      /// </summary>
      public bool Success { get; set; }

      /// <summary>
      /// Признак доступности сервиса ViewPower.
      /// </summary>
      public bool ViewPowerAvailable { get; set; }

      /// <summary>
      /// Признак включённого выходного питания ИБП.
      /// </summary>
      public bool OutputOn { get; set; }

      /// <summary>
      /// Имя обнаруженного устройства.
      /// </summary>
      public string DeviceName { get; set; } = string.Empty;

      /// <summary>
      /// Идентификатор устройства.
      /// </summary>
      public string DeviceId { get; set; } = string.Empty;

      /// <summary>
      /// Идентификатор устройства Plug and Play.
      /// </summary>
      public string PnpDeviceId { get; set; } = string.Empty;

      /// <summary>
      /// Имя системной службы, обслуживающей устройство.
      /// </summary>
      public string Service { get; set; } = string.Empty;

      /// <summary>
      /// Имя COM-порта, используемого ViewPower.
      /// </summary>
      public string PortName { get; set; } = string.Empty;

      /// <summary>
      /// Тип протокола, используемого ViewPower.
      /// </summary>
      public string ProtocolType { get; set; } = string.Empty;

      /// <summary>
      /// Идентификатор устройства в ViewPower.
      /// </summary>
      public string ViewPowerDeviceId { get; set; } = string.Empty;

      /// <summary>
      /// Текущий режим работы ИБП.
      /// </summary>
      public string WorkMode { get; set; } = string.Empty;

      /// <summary>
      /// Информационное сообщение о результате выполнения команды.
      /// </summary>
      public string Message { get; set; } = string.Empty;

      /// <summary>
      /// Сообщение об ошибке, если выполнение завершилось неуспешно.
      /// </summary>
      public string Error { get; set; } = string.Empty;

      /// <summary>
      /// Необработанный ответ, полученный от ViewPower.
      /// </summary>
      public string RawResponse { get; set; } = string.Empty;

      /// <summary>
      /// Время ожидания выполнения команды, в миллисекундах.
      /// </summary>
      public int Timeout { get; set; }

      /// <summary>
      /// Номер порта, используемого при выполнении команды.
      /// </summary>
      public int Port { get; set; }
    }
  }
}

