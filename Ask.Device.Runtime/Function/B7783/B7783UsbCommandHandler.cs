using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Device.Communication.Usb;
using Ask.Device.Communication.Usb.Discovery;
using Ask.Device.Runtime.Device;
using Ivi.Visa;
using NationalInstruments.Visa;
using System.Text;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.Function.B7783
{
  public sealed class B7783UsbCommandHandler : IUsbCommandHandler
  {
    private const int DefaultTimeout = 5000;
    private const string UsbTmcResourcePattern = "USB?*INSTR";

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

      if (device is not MultimeterB7783 multimeter)
      {
        throw new InvalidOperationException("B7783UsbCommandHandler поддерживает только MultimeterB7783.");
      }

      if (delayBeforeCall > 0)
      {
        await Task.Delay(delayBeforeCall, cancellationToken);
      }

      string pattern = string.IsNullOrWhiteSpace(device.ConnectionDetails)
        ? device.Name
        : device.ConnectionDetails;

      if (UsbDeviceLocator.TryFindByName(pattern, out var descriptor))
      {
        multimeter.LastResolvedDevicePath = descriptor.DeviceId;
      }

      string response = await Task.Run(
        () => ExecuteVisaCommand(
          command,
          pattern,
          timeout <= 0 ? DefaultTimeout : timeout,
          responseDelay),
        cancellationToken);

      LogInformation($"[{device.Name}] USB Query: {command} -> {response}", isDeviceLog: true);
      return response;
    }

    private static string ExecuteVisaCommand(string command, string pattern, int timeout, double responseDelay)
    {
      if (string.IsNullOrWhiteSpace(command))
      {
        throw new ArgumentException("Команда USB-SCPI не задана.", nameof(command));
      }

      using var resourceManager = new ResourceManager();
      string resourceName = FindInstrumentResource(resourceManager, pattern);

      using IVisaSession session = resourceManager.Open(resourceName);
      if (session is not MessageBasedSession messageSession)
      {
        throw new InvalidOperationException($"VISA-ресурс \"{resourceName}\" не поддерживает message-based обмен.");
      }

      messageSession.TimeoutMilliseconds = timeout;
      messageSession.SendEndEnabled = true;
      messageSession.TerminationCharacter = (byte)'\n';
      messageSession.TerminationCharacterEnabled = true;

      try
      {
        messageSession.RawIO.Write(EnsureLineEnding(command));

        bool expectsResponse = command.Contains('?', StringComparison.Ordinal);
        if (!expectsResponse)
        {
          return string.Empty;
        }

        if (responseDelay > 0)
        {
          Thread.Sleep((int)Math.Ceiling(responseDelay));
        }

        return ReadResponse(messageSession, command);
      }
      catch (IOTimeoutException ex)
      {
        throw new TimeoutException(
          $"VISA timeout при выполнении \"{command}\" через \"{resourceName}\" за {timeout} мс.",
          ex);
      }
      catch (VisaException ex)
      {
        throw new InvalidOperationException($"VISA.NET ошибка при выполнении \"{command}\" через \"{resourceName}\": {ex.Message}", ex);
      }
    }

    private static string ReadResponse(MessageBasedSession session, string command)
    {
      byte[] buffer = new byte[4096];
      session.RawIO.Read(buffer, 0, buffer.Length, out long readCount, out ReadStatus readStatus);

      if (readCount <= 0)
      {
        throw new InvalidOperationException($"viRead({command}) не вернул данных. ReadStatus: {readStatus}.");
      }

      return DecodeResponse(buffer, readCount);
    }

    private static string DecodeResponse(byte[] buffer, long readCount)
    {
      return Encoding.ASCII.GetString(buffer, 0, (int)readCount).Trim('\0', '\r', '\n', ' ');
    }

    private static string FindInstrumentResource(ResourceManager resourceManager, string pattern)
    {
      var resources = resourceManager.Find(UsbTmcResourcePattern).ToList();

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
        ? "нет"
        : string.Join(", ", resources);

      throw new InvalidOperationException(
        $"VISA-ресурс В7-78/3 не найден по шаблону \"{pattern}\". Найденные USBTMC ресурсы: {foundResources}");
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
  }
}
