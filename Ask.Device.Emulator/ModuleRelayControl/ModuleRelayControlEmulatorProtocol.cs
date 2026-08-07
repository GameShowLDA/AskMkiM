using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using System.Text.Json;

namespace Ask.Device.Emulator.ModuleRelayControl
{
  /// <summary>
  /// Эмулирует строковые ответы модуля коммутации реле.
  /// </summary>
  internal sealed class ModuleRelayControlEmulatorProtocol : IDeviceProtocol
  {
    private readonly Func<int> _moduleNumberProvider;
    private readonly Func<int> _chassisNumberProvider;
    private readonly Func<bool> _hardwareErrorProvider;
    private readonly Func<bool> _measurementErrorProvider;
    private bool _notDefaultState;
    private bool _meterEnabled;

    public ModuleRelayControlEmulatorProtocol(
      Func<int> moduleNumberProvider,
      Func<int> chassisNumberProvider)
      : this(
        moduleNumberProvider,
        chassisNumberProvider,
        IdleHardwareErrorSimulator.ShouldSimulateHardwareError,
        ExecutionConfig.GetIsErrorSimulationEnabled)
    {
    }

    internal ModuleRelayControlEmulatorProtocol(
      Func<int> moduleNumberProvider,
      Func<int> chassisNumberProvider,
      Func<bool> hardwareErrorProvider)
      : this(moduleNumberProvider, chassisNumberProvider, hardwareErrorProvider, () => false)
    {
    }

    internal ModuleRelayControlEmulatorProtocol(
      Func<int> moduleNumberProvider,
      Func<int> chassisNumberProvider,
      Func<bool> hardwareErrorProvider,
      Func<bool> measurementErrorProvider)
    {
      _moduleNumberProvider = moduleNumberProvider
        ?? throw new ArgumentNullException(nameof(moduleNumberProvider));
      _chassisNumberProvider = chassisNumberProvider
        ?? throw new ArgumentNullException(nameof(chassisNumberProvider));
      _hardwareErrorProvider = hardwareErrorProvider
        ?? throw new ArgumentNullException(nameof(hardwareErrorProvider));
      _measurementErrorProvider = measurementErrorProvider
        ?? throw new ArgumentNullException(nameof(measurementErrorProvider));
    }

    /// <inheritdoc />
    public SemaphoreSlim OperationLock { get; set; } = new(1, 1);

    /// <inheritdoc />
    public async Task<string> QueryAsync(
      string command,
      double responseDelay = 0,
      int timeout = 0,
      int port = 0,
      int delayBeforeCall = 0,
      CancellationToken cancellationToken = default)
    {
      if (delayBeforeCall > 0)
      {
        await Task.Delay(TimeSpan.FromMilliseconds(delayBeforeCall), cancellationToken);
      }

      if (_hardwareErrorProvider() || !TryParse(command, out int[] parts))
      {
        return string.Empty;
      }

      object? payload = CreatePayload(parts);
      return payload is null ? string.Empty : JsonSerializer.Serialize(payload);
    }

    private object? CreatePayload(int[] parts)
    {
      int command = parts[0];
      switch (command)
      {
        case 1 when parts.Length == 4 && parts[3] is 0 or 1 or 2:
          _notDefaultState = parts[3] == 1;
          return Envelope(new { NotDefaultState = _notDefaultState });

        case 2 when parts.Length >= 2:
          _notDefaultState = false;
          _meterEnabled = false;
          return Envelope(new { Answer = "2.0.1", NotDefaultState = false });

        case 4 when IsCommand(parts, 4) && parts[1] is >= 1 and <= 3
          && parts[2] is >= 1 and <= 4 && parts[3] is 1 or 2:
        case 8 when IsCommand(parts, 4) && parts[1] > 0
          && parts[2] is >= 1 and <= 3 && parts[3] is 1 or 2:
        case 9 when Matches(parts, 3) && parts[1] is >= 1 and <= 3 && parts[2] is 1 or 2:
        case 11 when IsCommand(parts, 4) && parts[1] > 0
          && parts[2] >= parts[1] && parts[3] is 11 or 12 or 21 or 22:
        case 81 when (IsCommand(parts, 3) || (IsCommand(parts, 4) && parts[3] == 0))
          && parts[1] > 0 && parts[2] is 1 or 2:
          _notDefaultState = true;
          return command == 9 ? CommandEnvelope(parts[..3]) : CommandEnvelope(parts);

        case 5 when Matches(parts, 2) && parts[1] is 1 or 2:
          _meterEnabled = parts[1] == 1;
          _notDefaultState = true;
          return CommandEnvelope(parts[..2]);

        case 7 when Matches(parts, 1):
          return Envelope(new { Answer = $"7.{(_meterEnabled ? 1 : 2)}", NotDefaultState = _notDefaultState });

        case 82 when IsCommand(parts, 4) && parts[1] > 0
          && parts[2] is 1 or 2 && parts[3] is 1 or 2:
          _notDefaultState = true;
          return Envelope(new
          {
            Answer = Join(parts),
            NotDefaultState = true,
            Checked = true
          });

        case 6 when Matches(parts, 2) && parts[1] > 0:
          {
            bool simulateMeasurementError = _measurementErrorProvider();
            int failedStage = parts[1] % 3;
            bool connectPoint = !simulateMeasurementError || failedStage != 0;
            bool disconnectBusA = !simulateMeasurementError || failedStage != 1;
            bool disconnectBusB = !simulateMeasurementError || failedStage != 2;
            return Envelope(new
            {
              Status = "sucsess",
              NumberPoint = parts[1],
              ConnectPoint = connectPoint,
              DisconnectBusA = disconnectBusA,
              DisconnectBusB = disconnectBusB,
              SelfControl = connectPoint && disconnectBusA && disconnectBusB
            });
          }

        case 10 when Matches(parts, 2) && parts[1] is >= 1 and <= 4:
          return Envelope(new
          {
            NumberBus = parts[1],
            ProtectReleBusA = 100 + (parts[1] * 2) - 1,
            ProtectReleBusB = 108 + (parts[1] * 2) - 1,
            ConnectProtect = true,
            MainReleBusA = 100 + (parts[1] * 2),
            MainReleBusB = 108 + (parts[1] * 2),
            ConnectMain = true,
            Error = 0
          });

        default:
          return null;
      }
    }

    private object CommandEnvelope(int[] parts)
      => Envelope(new { Answer = Join(parts), NotDefaultState = _notDefaultState });

    private object Envelope(object payload)
    {
      var result = new Dictionary<string, object?>
      {
        ["ModuleName"] = "MKR",
        ["NumberDevice"] = _moduleNumberProvider(),
        ["NumberChassis"] = _chassisNumberProvider()
      };

      foreach (var property in payload.GetType().GetProperties())
      {
        result[property.Name] = property.GetValue(payload);
      }

      return result;
    }

    private static bool TryParse(string command, out int[] parts)
    {
      parts = [];
      if (string.IsNullOrWhiteSpace(command))
      {
        return false;
      }

      string[] values = command.Trim().TrimEnd('.').Split('.');
      parts = new int[values.Length];
      for (int index = 0; index < values.Length; index++)
      {
        if (!int.TryParse(values[index], out parts[index]))
        {
          parts = [];
          return false;
        }
      }

      return parts.Length > 0;
    }

    private static bool IsCommand(int[] parts, int length) => parts.Length == length;

    private static bool Matches(int[] parts, int meaningfulLength)
    {
      if (parts.Length == meaningfulLength)
      {
        return true;
      }

      return parts.Length == 4
        && parts.Skip(meaningfulLength).All(parameter => parameter == 0);
    }

    private static string Join(int[] parts) => string.Join('.', parts);
  }
}
