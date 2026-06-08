using Ask.Diagnostics.Abstractions;
using Ask.Diagnostics.Infrastructure;
using Ask.Diagnostics.Models;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Ask.Diagnostics.Collectors
{
  public sealed class SystemInfoCollector : ICrashDataCollector
  {
    public string Name => "SystemInfo";

    public int Order => 700;

    public async Task CollectAsync(CrashContext context, CancellationToken cancellationToken = default)
    {
      var entryAssembly = Assembly.GetEntryAssembly();
      var version = entryAssembly?
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion
        ?? entryAssembly?.GetName().Version?.ToString()
        ?? "unknown";

      var payload = new
      {
        appVersion = version,
        os = RuntimeInformation.OSDescription,
        dotnet = RuntimeInformation.FrameworkDescription,
        machineName = Environment.MachineName,
        userName = Environment.UserName,
        processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
        osArchitecture = RuntimeInformation.OSArchitecture.ToString(),
        is64BitProcess = Environment.Is64BitProcess,
        processorCount = Environment.ProcessorCount,
        memoryGb = GetTotalMemoryGb(),
        workingSetMb = Math.Round(Environment.WorkingSet / 1024d / 1024d, 2),
        baseDirectory = AppContext.BaseDirectory,
      };

      await JsonFileWriter.WriteAsync(
        Path.Combine(context.PackageDirectory, "system-info.json"),
        payload,
        cancellationToken).ConfigureAwait(false);
    }

    private static double? GetTotalMemoryGb()
    {
      var status = new MemoryStatusEx();
      status.Length = (uint)Marshal.SizeOf<MemoryStatusEx>();

      if (!GlobalMemoryStatusEx(ref status))
      {
        return null;
      }

      return Math.Round(status.TotalPhys / 1024d / 1024d / 1024d, 2);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
      public uint Length;
      public uint MemoryLoad;
      public ulong TotalPhys;
      public ulong AvailPhys;
      public ulong TotalPageFile;
      public ulong AvailPageFile;
      public ulong TotalVirtual;
      public ulong AvailVirtual;
      public ulong AvailExtendedVirtual;
    }
  }
}
