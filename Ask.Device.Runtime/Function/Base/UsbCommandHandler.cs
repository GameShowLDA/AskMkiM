using System.Text;
using System.Text.Json;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;
using Ask.Core.Shared.Metadata.Commands.MultimeterCommands.Connected;
using Ask.Device.Communication.Usb;
using Ask.Device.Communication.Usb.Discovery;
using Ask.Device.Runtime.Base.Device;
using Ask.Device.Runtime.Function.MikUps1101rRm.ViewPower;
using Ivi.Visa;
using NationalInstruments.Visa;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.Function.Base
{
  public sealed class UsbCommandHandler : IUsbCommandHandler
  {
    private const int DefaultTimeout = 5000;
    private const string UpsConnectCommand = "UPS:CONNECT";
    private const string UpsStartPowerCommand = "UPS:POWER:START";
    private const string UpsStopPowerCommand = "UPS:POWER:STOP";
    private const string UpsVerifyPowerCommand = "UPS:POWER:VERIFY";
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

    private static bool IsRetryableVisaOpenException(Exception exception)
    {
      return exception is VisaException || exception is NativeVisaException;
    }

    private static string FindInstrumentResource(
      ResourceManager resourceManager,
      string pattern,
      UsbConnectedProfile profile)
    {
      string resourcePattern = string.IsNullOrWhiteSpace(profile.VisaResourcePattern)
        ? "USB?*INSTR"
        : profile.VisaResourcePattern;

      var resources = resourceManager.Find(resourcePattern).ToList();

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

    private static string EnsureLineEnding(string command)
    {
      return command.EndsWith("\n", StringComparison.Ordinal)
        ? command
        : command + "\n";
    }

    private static string GetUsbSearchPattern(IDevice device)
    {
      return string.IsNullOrWhiteSpace(device.ConnectionDetails)
        ? device.Name
        : device.ConnectionDetails;
    }

    private static int GetProfileTimeout(UsbConnectedProfile profile)
    {
      return profile.Timeout <= 0 ? DefaultTimeout : profile.Timeout;
    }

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

    private static void SetCompatibleLastResolvedDevicePath(IDevice device, string path)
    {
      var property = device.GetType().GetProperty("LastResolvedDevicePath");
      if (property?.CanWrite == true && property.PropertyType == typeof(string))
      {
        property.SetValue(device, path);
      }
    }

    private static bool IsPowerEnabled(ViewPowerMonitorSnapshot snapshot)
    {
      if (snapshot.OutputOn)
      {
        return true;
      }

      return ActiveWorkModes.Any(mode => string.Equals(mode, snapshot.WorkMode, StringComparison.OrdinalIgnoreCase));
    }

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
