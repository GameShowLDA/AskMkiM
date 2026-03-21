using System.Management;
using System.Runtime.InteropServices;

namespace Ask.Device.Communication.Com
{
  public static class ComPortResetNative
  {
    private const int CR_SUCCESS = 0x00000000;

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    private static extern int CM_Locate_DevNode(out uint pdnDevInst, string pDeviceID, int ulFlags);

    [DllImport("cfgmgr32.dll")]
    private static extern int CM_Disable_DevNode(uint dnDevInst, int ulFlags);

    [DllImport("cfgmgr32.dll")]
    private static extern int CM_Enable_DevNode(uint dnDevInst, int ulFlags);

    public static string? GetPnpInstanceId(string comPort)
    {
      using var s = new ManagementObjectSearcher(
        $"SELECT PNPDeviceID FROM Win32_SerialPort WHERE DeviceID = '{comPort}'");
      foreach (ManagementObject o in s.Get())
        return o["PNPDeviceID"]?.ToString();
      return null;
    }

    public static bool Restart(string comPort, int waitMs = 800)
    {
      var id = GetPnpInstanceId(comPort);
      if (string.IsNullOrWhiteSpace(id))
        return false;

      if (CM_Locate_DevNode(out uint devInst, id!, 0) != CR_SUCCESS)
        return false;

      var rc1 = CM_Disable_DevNode(devInst, 0);
      Thread.Sleep(waitMs);
      var rc2 = CM_Enable_DevNode(devInst, 0);

      return rc1 == CR_SUCCESS && rc2 == CR_SUCCESS;
    }
  }
}
