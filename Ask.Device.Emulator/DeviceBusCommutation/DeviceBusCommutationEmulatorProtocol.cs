using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using System.Text.Json;

namespace Ask.Device.Emulator.DeviceBusCommutation
{
  /// <summary>
  /// Эмулирует протокольные ответы устройства коммутации шин.
  /// </summary>
  internal sealed class DeviceBusCommutationEmulatorProtocol : IDeviceProtocol
  {
    private readonly Func<int> deviceNumberProvider;
    private readonly Func<int> chassisNumberProvider;
    private readonly Func<bool> hardwareErrorProvider;

    public DeviceBusCommutationEmulatorProtocol(
      Func<int> deviceNumberProvider,
      Func<int> chassisNumberProvider,
      Func<bool>? hardwareErrorProvider = null)
    {
      this.deviceNumberProvider = deviceNumberProvider;
      this.chassisNumberProvider = chassisNumberProvider;
      this.hardwareErrorProvider = hardwareErrorProvider ?? (() => false);
    }

    public SemaphoreSlim OperationLock { get; set; } = new(1, 1);

    public Task<string> QueryAsync(
      string command,
      double responseDelay = 0,
      int timeout = 0,
      int port = 0,
      int delayBeforeCall = 0,
      CancellationToken cancellationToken = default)
    {
      if (hardwareErrorProvider())
      {
        return Task.FromResult(string.Empty);
      }

      string normalizedCommand = command.Trim().TrimEnd('.');
      string[] parts = normalizedCommand.Split('.');
      if (parts.Length != 4 || parts.Any(part => !int.TryParse(part, out _)))
      {
        return Task.FromResult(string.Empty);
      }

      int commandNumber = int.Parse(parts[0]);
      if (commandNumber == 6)
      {
        return Task.FromResult("0");
      }

      if (commandNumber == 8)
      {
        return Task.FromResult(parts[1]);
      }

      if (commandNumber == 41)
      {
        return Task.FromResult("1");
      }

      string? answer = commandNumber switch
      {
        1 => null,
        2 => "2.0.1",
        4 or 5 or 7 or 9 => $"{normalizedCommand}.",
        _ => null
      };

      if (commandNumber is not (1 or 2 or 4 or 5 or 7 or 9))
      {
        return Task.FromResult(string.Empty);
      }

      var response = new Dictionary<string, object?>
      {
        ["ModuleName"] = "DeviceBusCommutation",
        ["NumberDevice"] = deviceNumberProvider(),
        ["NumberChassis"] = chassisNumberProvider()
      };

      if (answer is not null)
      {
        response["Answer"] = answer;
      }

      return Task.FromResult(JsonSerializer.Serialize(response));
    }
  }
}
