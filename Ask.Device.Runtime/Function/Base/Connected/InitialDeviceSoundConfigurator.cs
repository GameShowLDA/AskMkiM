using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Device.Emulator;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.Function.Base.Connected;

/// <summary>
/// Отключает звуковую сигнализацию устройства после первой успешной инициализации.
/// </summary>
internal sealed class InitialDeviceSoundConfigurator
{
  private readonly IDevice _device;
  private readonly IReadOnlyList<string> _commands;
  private readonly SemaphoreSlim _sync = new(1, 1);
  private bool _configurationAttempted;

  /// <summary>
  /// Создаёт компонент первоначальной настройки звуковой сигнализации.
  /// </summary>
  /// <param name="device">Настраиваемое устройство.</param>
  /// <param name="commands">Команды отключения звуковой сигнализации.</param>
  internal InitialDeviceSoundConfigurator(IDevice device, IReadOnlyList<string> commands)
  {
    _device = device ?? throw new ArgumentNullException(nameof(device));
    _commands = commands ?? throw new ArgumentNullException(nameof(commands));
  }

  /// <summary>
  /// Однократно отправляет команды отключения звуковой сигнализации.
  /// </summary>
  /// <returns>Задача, представляющая выполнение настройки.</returns>
  internal async Task ApplyOnceAsync()
  {
    if (_configurationAttempted || _commands.Count == 0)
    {
      return;
    }

    await _sync.WaitAsync();
    try
    {
      if (_configurationAttempted)
      {
        return;
      }

      _configurationAttempted = true;

      foreach (string command in _commands)
      {
        await SendCommandAsync(command);
      }

      LogInformation(
        $"[{_device.Name}] Звуковая сигнализация отключена после первичной инициализации.",
        isDeviceLog: true);
    }
    catch (Exception ex)
    {
      LogWarning(
        $"[{_device.Name}] Не удалось отключить звуковую сигнализацию после первичной инициализации: {ex.Message}",
        isDeviceLog: true);
    }
    finally
    {
      _sync.Release();
    }
  }

  /// <summary>
  /// Передаёт команду устройству с учётом Real/Idle-шлюза мультиметра.
  /// </summary>
  /// <param name="command">Команда отключения звуковой сигнализации.</param>
  /// <returns>Ответ устройства или эмулятора.</returns>
  private Task<string> SendCommandAsync(string command)
  {
    return _device is IMultimeter multimeter
      ? DeviceProtocolEmulator.QueryMultimeterAsync(multimeter, command, string.Empty)
      : _device.DeviceProtocol.QueryAsync(command);
  }
}
