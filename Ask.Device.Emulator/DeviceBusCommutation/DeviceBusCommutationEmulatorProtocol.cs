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
    private readonly Func<bool> measurementErrorProvider;
    private readonly Func<int> simulatedChainResultProvider;

    public DeviceBusCommutationEmulatorProtocol(
      Func<int> deviceNumberProvider,
      Func<int> chassisNumberProvider)
      : this(
        deviceNumberProvider,
        chassisNumberProvider,
        IdleHardwareErrorSimulator.ShouldSimulateHardwareError,
        ExecutionConfig.GetIsErrorSimulationEnabled,
        () => Random.Shared.Next(4) == 0 ? Random.Shared.Next(1, 256) : 0)
    {
    }

    internal DeviceBusCommutationEmulatorProtocol(
      Func<int> deviceNumberProvider,
      Func<int> chassisNumberProvider,
      Func<bool> hardwareErrorProvider,
      Func<bool> measurementErrorProvider,
      Func<int> simulatedChainResultProvider)
    {
      this.deviceNumberProvider = deviceNumberProvider;
      this.chassisNumberProvider = chassisNumberProvider;
      this.hardwareErrorProvider = hardwareErrorProvider;
      this.measurementErrorProvider = measurementErrorProvider;
      this.simulatedChainResultProvider = simulatedChainResultProvider;
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
        return Task.FromResult(measurementErrorProvider()
          ? simulatedChainResultProvider().ToString()
          : "0");
      }

      if (commandNumber == 8)
      {
        return Task.FromResult(parts[1]);
      }

      if (commandNumber == 41)
      {
        int relaySelector = int.Parse(parts[1]);
        if (relaySelector % 10 != 0)
        {
          return Task.FromResult("1");
        }

        int relayCount = (relaySelector / 10) switch
        {
          1 or 7 => 2,
          2 => 1,
          _ => 0
        };
        return Task.FromResult(relayCount.ToString());
      }

      string? answer = commandNumber switch
      {
        1 => null,
        2 => "2.0.1",
        4 or 5 or 9 => normalizedCommand,
        7 => $"7.{parts[1]}",
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
