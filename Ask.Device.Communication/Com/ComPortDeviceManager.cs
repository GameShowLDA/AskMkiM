using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Device.Communication.Com
{
  public static class DeviceManager
  {
    public static void DisableDevice(string comPortName)
    {
      try
      {
        var psi = new ProcessStartInfo
        {
          FileName = "powershell",
          Arguments = $"-Command \"Get-PnpDevice | Where-Object {{ $_.Name -like '*({comPortName})*' }} | Disable-PnpDevice -Confirm:$false\"",
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          UseShellExecute = false,
          CreateNoWindow = true,
          Verb = "runas" // важно — админ!
        };
        using var process = Process.Start(psi);
        process.WaitForExit();
      }
      catch
      {

      }
    }

    public static void EnableDevice(string comPortName)
    {
      try
      {

        var psi = new ProcessStartInfo
        {
          FileName = "powershell",
          Arguments = $"-Command \"Get-PnpDevice | Where-Object {{ $_.Name -like '*({comPortName})*' }} | Enable-PnpDevice -Confirm:$false\"",
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          UseShellExecute = false,
          CreateNoWindow = true,
          Verb = "runas"
        };
        using var process = Process.Start(psi);
        process.WaitForExit();
      }
      catch
      {
      }
    }
  }
}
