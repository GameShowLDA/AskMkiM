using System.Diagnostics;

namespace NewCore.Communication
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
