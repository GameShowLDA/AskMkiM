using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;
using Ask.Device.Communication.Common;
using System.Text.Json;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Communication.Usb
{
  /// <summary>
  /// Общая USB-заглушка протокола обмена для бесперебойников.
  /// </summary>
  public class UninterruptiblePowerSupplyUsbProtocol : IDeviceProtocol
  {
    private const string ConnectCommand = "UPS:CONNECT";
    private const string StartPowerCommand = "UPS:POWER:START";
    private const string StopPowerCommand = "UPS:POWER:STOP";
    private const string VerifyPowerCommand = "UPS:POWER:VERIFY";
    private const string ControlDelayMinutes = "0.2";
    private static readonly TimeSpan StartStateConfirmationTimeout = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan StopStateConfirmationTimeout = TimeSpan.FromSeconds(5);
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

    private readonly DeviceWithUSB _device;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="UninterruptiblePowerSupplyUsbProtocol"/>.
    /// </summary>
    /// <param name="device">USB-устройство.</param>
    public UninterruptiblePowerSupplyUsbProtocol(DeviceWithUSB device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      OperationLock = new SemaphoreSlim(1, 1);
    }

    /// <inheritdoc />
    public SemaphoreSlim OperationLock { get; set; }

    /// <inheritdoc />
    public async Task<string> QueryAsync(
      string command,
      double responseDelay = 0,
      int timeout = 0,
      int port = 0,
      int delayBeforeCall = 0,
      CancellationToken cancellationToken = default)
    {
      using (await OperationLock.LockAsync(cancellationToken))
      {
        if (delayBeforeCall > 0)
        {
          await Task.Delay(delayBeforeCall, cancellationToken);
        }

        string pattern = string.IsNullOrWhiteSpace(_device.ConnectionDetails)
          ? _device.Name
          : _device.ConnectionDetails;

        bool found = UsbDeviceLocator.TryFindByName(pattern, out var descriptor);

        if (_device is IUninterruptiblePowerSupply ups)
        {
          ups.LastResolvedDevicePath = found ? descriptor.DeviceId : string.Empty;
        }

        UpsProtocolResponse payload = await ExecuteCommandAsync(command, found, descriptor, cancellationToken);

        if (responseDelay > 0)
        {
          await Task.Delay((int)Math.Ceiling(responseDelay), cancellationToken);
        }

        payload.Timeout = timeout;
        payload.Port = port;

        string response = JsonSerializer.Serialize(payload);
        LogInformation($"[{_device.Name}] UPS Query: {response}", isDeviceLog: true);
        return response;
      }
    }

    private async Task<UpsProtocolResponse> ExecuteCommandAsync(
      string command,
      bool found,
      UsbDeviceDescriptor descriptor,
      CancellationToken cancellationToken)
    {
      var response = CreateBaseResponse(command, found, descriptor);

      if (!found)
      {
        response.Success = false;
        response.Error = $"UPS \"{_device.ConnectionDetails}\" was not found in the system USB devices.";
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

    private static bool IsPowerEnabled(ViewPowerMonitorSnapshot snapshot)
    {
      if (snapshot.OutputOn)
      {
        return true;
      }

      return ActiveWorkModes.Any(mode => string.Equals(mode, snapshot.WorkMode, StringComparison.OrdinalIgnoreCase));
    }

    private UpsProtocolResponse CreateBaseResponse(string command, bool found, UsbDeviceDescriptor descriptor)
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

    private sealed class UpsProtocolResponse
    {
      public string Transport { get; set; } = string.Empty;

      public string DeviceType { get; set; } = string.Empty;

      public string Command { get; set; } = string.Empty;

      public bool Found { get; set; }

      public bool Success { get; set; }

      public bool ViewPowerAvailable { get; set; }

      public bool OutputOn { get; set; }

      public string DeviceName { get; set; } = string.Empty;

      public string DeviceId { get; set; } = string.Empty;

      public string PnpDeviceId { get; set; } = string.Empty;

      public string Service { get; set; } = string.Empty;

      public string PortName { get; set; } = string.Empty;

      public string ProtocolType { get; set; } = string.Empty;

      public string ViewPowerDeviceId { get; set; } = string.Empty;

      public string WorkMode { get; set; } = string.Empty;

      public string Message { get; set; } = string.Empty;

      public string Error { get; set; } = string.Empty;

      public string RawResponse { get; set; } = string.Empty;

      public int Timeout { get; set; }

      public int Port { get; set; }
    }
  }
}
