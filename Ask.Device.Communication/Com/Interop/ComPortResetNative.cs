using System.Management;
using System.Runtime.InteropServices;

namespace Ask.Device.Communication.Com
{
  /// <summary>
  /// Предоставляет низкоуровневые операции для перезапуска COM-устройства через Windows Configuration Manager.
  /// </summary>
  public static class ComPortResetNative
  {
    /// <summary>
    /// Код успешного выполнения вызова WinAPI.
    /// </summary>
    private const int CR_SUCCESS = 0x00000000;

    /// <summary>
    /// Ищет узел устройства по его PnP-идентификатору.
    /// </summary>
    /// <param name="pdnDevInst">Идентификатор найденного узла устройства.</param>
    /// <param name="pDeviceID">PnP-идентификатор устройства.</param>
    /// <param name="ulFlags">Флаги поиска узла устройства.</param>
    /// <returns>Код результата WinAPI.</returns>
    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    private static extern int CM_Locate_DevNode(out uint pdnDevInst, string pDeviceID, int ulFlags);

    /// <summary>
    /// Отключает узел устройства по идентификатору.
    /// </summary>
    /// <param name="dnDevInst">Идентификатор узла устройства.</param>
    /// <param name="ulFlags">Флаги отключения.</param>
    /// <returns>Код результата WinAPI.</returns>
    [DllImport("cfgmgr32.dll")]
    private static extern int CM_Disable_DevNode(uint dnDevInst, int ulFlags);

    /// <summary>
    /// Включает узел устройства по идентификатору.
    /// </summary>
    /// <param name="dnDevInst">Идентификатор узла устройства.</param>
    /// <param name="ulFlags">Флаги включения.</param>
    /// <returns>Код результата WinAPI.</returns>
    [DllImport("cfgmgr32.dll")]
    private static extern int CM_Enable_DevNode(uint dnDevInst, int ulFlags);

    /// <summary>
    /// Получает PnP-идентификатор устройства, соответствующего указанному COM-порту.
    /// </summary>
    /// <param name="comPort">Имя COM-порта.</param>
    /// <returns>PnP-идентификатор устройства или <see langword="null"/>, если устройство не найдено.</returns>
    public static string? GetPnpInstanceId(string comPort)
    {
      using var searcher = new ManagementObjectSearcher(
        $"SELECT PNPDeviceID FROM Win32_SerialPort WHERE DeviceID = '{comPort}'");

      foreach (ManagementObject portObject in searcher.Get())
      {
        return portObject["PNPDeviceID"]?.ToString();
      }

      return null;
    }

    /// <summary>
    /// Перезапускает устройство, связанное с указанным COM-портом.
    /// </summary>
    /// <param name="comPort">Имя COM-порта.</param>
    /// <param name="waitMs">Пауза между отключением и включением устройства в миллисекундах.</param>
    /// <returns><see langword="true"/>, если устройство удалось отключить и повторно включить; иначе <see langword="false"/>.</returns>
    public static bool RestartDevice(string comPort, int waitMs = 800)
    {
      var pnpInstanceId = GetPnpInstanceId(comPort);
      if (string.IsNullOrWhiteSpace(pnpInstanceId))
      {
        return false;
      }

      if (CM_Locate_DevNode(out uint deviceInstance, pnpInstanceId, 0) != CR_SUCCESS)
      {
        return false;
      }

      var disableResult = CM_Disable_DevNode(deviceInstance, 0);
      Thread.Sleep(waitMs);
      var enableResult = CM_Enable_DevNode(deviceInstance, 0);

      return disableResult == CR_SUCCESS && enableResult == CR_SUCCESS;
    }
  }
}
