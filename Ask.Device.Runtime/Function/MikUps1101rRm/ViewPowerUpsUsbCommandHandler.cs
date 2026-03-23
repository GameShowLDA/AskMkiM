using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;
using Ask.Device.Communication.Usb;
using NewCore.Function.MikUps1101rRm.ViewPower;
using System.Text.Json;
using static Ask.LogLib.LoggerUtility;

namespace NewCore.Function.MikUps1101rRm
{
  /// <summary>
  /// Обрабатывает UPS-команды поверх общего <see cref="UsbProtocol"/> через локальный интерфейс ViewPower.
  /// </summary>
  public class ViewPowerUpsUsbCommandHandler : IUsbCommandHandler
  {
    /// <summary>
    /// Команда проверки доступности UPS.
    /// </summary>
    private const string ConnectCommand = "UPS:CONNECT";

    /// <summary>
    /// Команда включения выходного питания UPS.
    /// </summary>
    private const string StartPowerCommand = "UPS:POWER:START";

    /// <summary>
    /// Команда отключения выходного питания UPS.
    /// </summary>
    private const string StopPowerCommand = "UPS:POWER:STOP";

    /// <summary>
    /// Команда проверки состояния выходного питания UPS.
    /// </summary>
    private const string VerifyPowerCommand = "UPS:POWER:VERIFY";

    /// <summary>
    /// Задержка выполнения команды ViewPower в минутах.
    /// </summary>
    private const string ControlDelayMinutes = "0.2";

    /// <summary>
    /// Таймаут ожидания подтверждения включения питания UPS.
    /// </summary>
    private static readonly TimeSpan StartStateConfirmationTimeout = TimeSpan.FromSeconds(8);

    /// <summary>
    /// Таймаут ожидания подтверждения отключения питания UPS.
    /// </summary>
    private static readonly TimeSpan StopStateConfirmationTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Набор режимов работы, означающих включённое выходное питание.
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

    /// <summary>
    /// Выполняет UPS-команду через ViewPower и возвращает сериализованный JSON-ответ.
    /// </summary>
    /// <param name="device">Устройство UPS.</param>
    /// <param name="command">Команда транспорта.</param>
    /// <param name="responseDelay">Задержка перед возвратом ответа в миллисекундах.</param>
    /// <param name="timeout">Пользовательский таймаут операции.</param>
    /// <param name="port">Пользовательский порт операции.</param>
    /// <param name="delayBeforeCall">Задержка перед выполнением команды в миллисекундах.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>JSON-строка с результатом выполнения команды.</returns>
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

      if (device is not IUninterruptiblePowerSupply ups)
      {
        throw new InvalidOperationException("ViewPowerUpsUsbCommandHandler поддерживает только IUninterruptiblePowerSupply.");
      }

      if (delayBeforeCall > 0)
      {
        await Task.Delay(delayBeforeCall, cancellationToken);
      }

      string pattern = string.IsNullOrWhiteSpace(device.ConnectionDetails)
        ? device.Name
        : device.ConnectionDetails;

      bool found = UsbDeviceLocator.TryFindByName(pattern, out var descriptor);
      ups.LastResolvedDevicePath = found ? descriptor.DeviceId : string.Empty;

      UpsProtocolResponse payload = await ExecuteCommandAsync(device, command, found, descriptor, cancellationToken);

      if (responseDelay > 0)
      {
        await Task.Delay((int)Math.Ceiling(responseDelay), cancellationToken);
      }

      payload.Timeout = timeout;
      payload.Port = port;

      string response = JsonSerializer.Serialize(payload);
      LogInformation($"[{device.Name}] UPS Query: {response}", isDeviceLog: true);
      return response;
    }

    /// <summary>
    /// Выполняет бизнес-логику конкретной UPS-команды.
    /// </summary>
    /// <param name="device">Устройство UPS.</param>
    /// <param name="command">Команда транспорта.</param>
    /// <param name="found">Признак обнаружения USB-устройства.</param>
    /// <param name="descriptor">Дескриптор найденного устройства.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Результат выполнения команды.</returns>
    private async Task<UpsProtocolResponse> ExecuteCommandAsync(
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
          case ConnectCommand:
            response.Success = true;
            response.Message = $"USB found. ViewPower port: {response.PortName}. Work mode: {response.WorkMode}.";
            return response;

          case VerifyPowerCommand:
            response.Success = true;
            response.Message = response.OutputOn ? "UPS output power is enabled." : "UPS output power is disabled.";
            return response;

          case StartPowerCommand:
            return await ExecuteRealtimeControlAsync(
              client,
              response,
              snapshot,
              expectedState: true,
              "powerCtrlON",
              StartStateConfirmationTimeout,
              cancellationToken).ConfigureAwait(false);

          case StopPowerCommand:
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
    /// Выполняет команду управления питанием UPS через ViewPower и ждёт подтверждения состояния.
    /// </summary>
    /// <param name="client">Клиент ViewPower.</param>
    /// <param name="response">Объект ответа, который требуется дополнить.</param>
    /// <param name="snapshot">Исходный снимок состояния UPS.</param>
    /// <param name="expectedState">Ожидаемое конечное состояние питания.</param>
    /// <param name="controlType">Тип команды управления в ViewPower.</param>
    /// <param name="confirmationTimeout">Таймаут подтверждения нового состояния.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Обновлённый результат выполнения команды.</returns>
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
    /// Определяет, включено ли выходное питание UPS по данным ViewPower.
    /// </summary>
    /// <param name="snapshot">Снимок состояния UPS.</param>
    /// <returns><see langword="true"/>, если питание включено.</returns>
    private static bool IsPowerEnabled(ViewPowerMonitorSnapshot snapshot)
    {
      if (snapshot.OutputOn)
      {
        return true;
      }

      return ActiveWorkModes.Any(mode => string.Equals(mode, snapshot.WorkMode, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Создаёт базовый объект ответа UPS до выполнения конкретной команды.
    /// </summary>
    /// <param name="command">Команда транспорта.</param>
    /// <param name="found">Признак обнаружения устройства.</param>
    /// <param name="descriptor">Дескриптор найденного устройства.</param>
    /// <returns>Базовый объект ответа UPS.</returns>
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
    /// Описывает сериализуемый ответ UPS-протокола.
    /// </summary>
    private sealed class UpsProtocolResponse
    {
      /// <summary>
      /// Получает или задаёт имя транспорта, обработавшего команду.
      /// </summary>
      public string Transport { get; set; } = string.Empty;

      /// <summary>
      /// Получает или задаёт строковый тип устройства.
      /// </summary>
      public string DeviceType { get; set; } = string.Empty;

      /// <summary>
      /// Получает или задаёт исходную команду транспорта.
      /// </summary>
      public string Command { get; set; } = string.Empty;

      /// <summary>
      /// Получает или задаёт признак обнаружения устройства.
      /// </summary>
      public bool Found { get; set; }

      /// <summary>
      /// Получает или задаёт признак успешного выполнения команды.
      /// </summary>
      public bool Success { get; set; }

      /// <summary>
      /// Получает или задаёт признак доступности ViewPower.
      /// </summary>
      public bool ViewPowerAvailable { get; set; }

      /// <summary>
      /// Получает или задаёт признак включённого выходного питания.
      /// </summary>
      public bool OutputOn { get; set; }

      /// <summary>
      /// Получает или задаёт имя найденного USB-устройства.
      /// </summary>
      public string DeviceName { get; set; } = string.Empty;

      /// <summary>
      /// Получает или задаёт системный DeviceID USB-устройства.
      /// </summary>
      public string DeviceId { get; set; } = string.Empty;

      /// <summary>
      /// Получает или задаёт системный PnP DeviceID USB-устройства.
      /// </summary>
      public string PnpDeviceId { get; set; } = string.Empty;

      /// <summary>
      /// Получает или задаёт имя драйверного сервиса USB-устройства.
      /// </summary>
      public string Service { get; set; } = string.Empty;

      /// <summary>
      /// Получает или задаёт имя порта UPS в ViewPower.
      /// </summary>
      public string PortName { get; set; } = string.Empty;

      /// <summary>
      /// Получает или задаёт тип протокола UPS в ViewPower.
      /// </summary>
      public string ProtocolType { get; set; } = string.Empty;

      /// <summary>
      /// Получает или задаёт идентификатор UPS в ViewPower.
      /// </summary>
      public string ViewPowerDeviceId { get; set; } = string.Empty;

      /// <summary>
      /// Получает или задаёт текущий режим работы UPS.
      /// </summary>
      public string WorkMode { get; set; } = string.Empty;

      /// <summary>
      /// Получает или задаёт служебное сообщение об успешном результате.
      /// </summary>
      public string Message { get; set; } = string.Empty;

      /// <summary>
      /// Получает или задаёт текст ошибки.
      /// </summary>
      public string Error { get; set; } = string.Empty;

      /// <summary>
      /// Получает или задаёт сырой ответ ViewPower.
      /// </summary>
      public string RawResponse { get; set; } = string.Empty;

      /// <summary>
      /// Получает или задаёт пользовательский таймаут операции.
      /// </summary>
      public int Timeout { get; set; }

      /// <summary>
      /// Получает или задаёт пользовательский порт операции.
      /// </summary>
      public int Port { get; set; }
    }
  }
}
