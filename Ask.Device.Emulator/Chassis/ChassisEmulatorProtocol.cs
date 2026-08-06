using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;

namespace Ask.Device.Emulator.Chassis
{
  /// <summary>
  /// Эмулирует строковые ответы контроллера шасси.
  /// </summary>
  internal sealed class ChassisEmulatorProtocol : IDeviceProtocol
  {
    private readonly Func<bool> _hardwareErrorProvider;
    private bool _powerEnabled;

    public ChassisEmulatorProtocol()
      : this(IdleHardwareErrorSimulator.ShouldSimulateHardwareError)
    {
    }

    internal ChassisEmulatorProtocol(Func<bool> hardwareErrorProvider)
    {
      _hardwareErrorProvider = hardwareErrorProvider
        ?? throw new ArgumentNullException(nameof(hardwareErrorProvider));
    }

    public SemaphoreSlim OperationLock { get; set; } = new(1, 1);

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

      if (parts.SequenceEqual(new[] { 1, 0, 0, 0 }))
      {
        return "1.0.1";
      }

      if (parts.SequenceEqual(new[] { 2, 1, 0, 0 }))
      {
        _powerEnabled = false;
        return "2.0.1";
      }

      if (parts.SequenceEqual(new[] { 2, 1, 1, 0 }))
      {
        _powerEnabled = true;
        return "1";
      }

      if (parts.SequenceEqual(new[] { 2, 2, 1, 0 }))
      {
        _powerEnabled = false;
        return "1";
      }

      if (parts.SequenceEqual(new[] { 7, 0, 0, 0 }))
      {
        return _powerEnabled ? "1" : "0";
      }

      return string.Empty;
    }

    private static bool TryParse(string command, out int[] parts)
    {
      parts = [];
      string[] values = command?.Trim().TrimEnd('.').Split('.') ?? [];
      if (values.Length != 4)
      {
        return false;
      }

      parts = new int[4];
      for (int index = 0; index < values.Length; index++)
      {
        if (!int.TryParse(values[index], out parts[index]) || parts[index] < 0)
        {
          parts = [];
          return false;
        }
      }

      return true;
    }
  }
}