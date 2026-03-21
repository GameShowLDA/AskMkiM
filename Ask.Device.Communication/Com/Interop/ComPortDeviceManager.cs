using System.Diagnostics;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Communication.Com
{
  /// <summary>
  /// Управляет включением и отключением COM-устройств на уровне операционной системы.
  /// </summary>
  public static class ComPortDeviceManager
  {
    /// <summary>
    /// Отключает устройство, связанное с указанным COM-портом.
    /// </summary>
    /// <param name="comPortName">Имя COM-порта, например <c>COM3</c>.</param>
    public static void DisableDevice(string comPortName)
    {
      SetDeviceState(comPortName, isEnabled: false);
    }

    /// <summary>
    /// Включает устройство, связанное с указанным COM-портом.
    /// </summary>
    /// <param name="comPortName">Имя COM-порта, например <c>COM3</c>.</param>
    public static void EnableDevice(string comPortName)
    {
      SetDeviceState(comPortName, isEnabled: true);
    }

    /// <summary>
    /// Изменяет состояние PnP-устройства, найденного по имени COM-порта.
    /// </summary>
    /// <param name="comPortName">Имя COM-порта, по которому выполняется поиск устройства.</param>
    /// <param name="isEnabled"><see langword="true"/>, если устройство нужно включить; иначе <see langword="false"/>.</param>
    private static void SetDeviceState(string comPortName, bool isEnabled)
    {
      ArgumentException.ThrowIfNullOrWhiteSpace(comPortName);

      try
      {
        var processStartInfo = new ProcessStartInfo
        {
          FileName = "powershell",
          Arguments = isEnabled
            ? $"-Command \"Get-PnpDevice | Where-Object {{ $_.Name -like '*({comPortName})*' }} | Enable-PnpDevice -Confirm:$false\""
            : $"-Command \"Get-PnpDevice | Where-Object {{ $_.Name -like '*({comPortName})*' }} | Disable-PnpDevice -Confirm:$false\"",
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          UseShellExecute = false,
          CreateNoWindow = true,
          Verb = "runas",
        };

        using var process = Process.Start(processStartInfo);
        process?.WaitForExit();
      }
      catch (Exception ex)
      {
        LogException(ex, $"Не удалось {(isEnabled ? "включить" : "отключить")} COM-устройство {comPortName}.", isDeviceLog: true);
      }
    }
  }
}
