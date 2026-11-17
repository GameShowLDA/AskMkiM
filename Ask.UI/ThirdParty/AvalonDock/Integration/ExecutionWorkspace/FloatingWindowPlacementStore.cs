using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Xml.Serialization;
using AvalonDock.Controls;
using AvalonDock.Layout;

namespace Ask.UI.ThirdParty.Docking.Integration.ExecutionWorkspace;

internal static class FloatingWindowPlacementStore
{
    private const double MinimumWidth = 240;
    private const double MinimumHeight = 160;

    internal static string GetCompanionPath(string layoutPath) => layoutPath + ".display.xml";

    internal static void Save(string path, IEnumerable<LayoutFloatingWindowControl> windows)
    {
        var placements = windows
            .Select(Capture)
            .Where(item => item is not null)
            .Cast<FloatingPlacement>()
            .ToArray();

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var temporaryPath = path + ".tmp";
        try
        {
            using (var stream = File.Create(temporaryPath))
                new XmlSerializer(typeof(FloatingPlacementSnapshot)).Serialize(
                    stream, new FloatingPlacementSnapshot { Placements = placements });
            File.Move(temporaryPath, path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    internal static void Restore(
        string path,
        LayoutRoot layout,
        IEnumerable<LayoutFloatingWindowControl> windows,
        nint ownerHandle)
    {
        if (!File.Exists(path))
            return;

        FloatingPlacementSnapshot? snapshot;
        try
        {
            using var stream = File.OpenRead(path);
            snapshot = new XmlSerializer(typeof(FloatingPlacementSnapshot)).Deserialize(stream)
                as FloatingPlacementSnapshot;
        }
        catch (InvalidOperationException)
        {
            return;
        }

        if (snapshot?.Placements is not { Length: > 0 })
            return;

        var monitors = GetMonitors();
        var fallbackMonitor = FindMonitor(ownerHandle, monitors)
            ?? monitors.FirstOrDefault(item => item.IsPrimary)
            ?? monitors.FirstOrDefault();
        var windowsById = windows
            .Select(window => (Window: window, Id: GetContentId(window.Model)))
            .Where(item => item.Id is not null)
            .ToDictionary(item => item.Id!, item => item.Window, StringComparer.Ordinal);

        foreach (var placement in snapshot.Placements)
        {
            if (!IsValid(placement))
                continue;

            var monitor = monitors.FirstOrDefault(item =>
                string.Equals(item.DeviceName, placement.Monitor, StringComparison.OrdinalIgnoreCase))
                ?? fallbackMonitor;
            if (monitor is null)
                continue;

            var bounds = RestoreBounds(placement, monitor);
            var content = layout.Descendents().OfType<LayoutContent>()
                .FirstOrDefault(item => string.Equals(item.ContentId, placement.ContentId, StringComparison.Ordinal));
            if (content is not null)
                Apply(content, bounds);

            if (windowsById.TryGetValue(placement.ContentId, out var window))
            {
                window.Left = bounds.X;
                window.Top = bounds.Y;
                window.Width = bounds.Width;
                window.Height = bounds.Height;
            }
        }
    }

    internal static Rect RestoreBounds(FloatingPlacement placement, DisplayMonitor monitor)
    {
        var work = monitor.WorkArea;
        var widthPixels = Math.Clamp(placement.Width * work.Width, MinimumWidth * monitor.ScaleX, work.Width);
        var heightPixels = Math.Clamp(placement.Height * work.Height, MinimumHeight * monitor.ScaleY, work.Height);
        var leftPixels = Math.Clamp(
            work.Left + placement.Left * work.Width,
            work.Left,
            work.Right - widthPixels);
        var topPixels = Math.Clamp(
            work.Top + placement.Top * work.Height,
            work.Top,
            work.Bottom - heightPixels);
        return new Rect(
            leftPixels / monitor.ScaleX,
            topPixels / monitor.ScaleY,
            widthPixels / monitor.ScaleX,
            heightPixels / monitor.ScaleY);
    }

    private static FloatingPlacement? Capture(LayoutFloatingWindowControl window)
    {
        var contentId = GetContentId(window.Model);
        var handle = new WindowInteropHelper(window).Handle;
        if (contentId is null || handle == nint.Zero || !GetWindowRect(handle, out var bounds))
            return null;

        var monitor = FindMonitor(handle, GetMonitors());
        if (monitor is null)
            return null;
        var work = monitor.WorkArea;
        if (work.Width <= 0 || work.Height <= 0)
            return null;

        return new FloatingPlacement
        {
            ContentId = contentId,
            Monitor = monitor.DeviceName,
            Left = (double)(bounds.Left - work.Left) / work.Width,
            Top = (double)(bounds.Top - work.Top) / work.Height,
            Width = (double)(bounds.Right - bounds.Left) / work.Width,
            Height = (double)(bounds.Bottom - bounds.Top) / work.Height,
        };
    }

    private static string? GetContentId(ILayoutElement model) => model
        .Descendents()
        .OfType<LayoutContent>()
        .Select(item => item.ContentId)
        .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));

    private static void Apply(LayoutContent content, Rect bounds)
    {
        content.FloatingLeft = bounds.X;
        content.FloatingTop = bounds.Y;
        content.FloatingWidth = bounds.Width;
        content.FloatingHeight = bounds.Height;
    }

    private static bool IsValid(FloatingPlacement placement) =>
        !string.IsNullOrWhiteSpace(placement.ContentId)
        && double.IsFinite(placement.Left)
        && double.IsFinite(placement.Top)
        && double.IsFinite(placement.Width)
        && double.IsFinite(placement.Height)
        && placement.Left is >= -1 and <= 2
        && placement.Top is >= -1 and <= 2
        && placement.Width is > 0 and <= 2
        && placement.Height is > 0 and <= 2;

    private static IReadOnlyList<DisplayMonitor> GetMonitors()
    {
        var monitors = new List<DisplayMonitor>();
        EnumDisplayMonitors(nint.Zero, nint.Zero, (monitor, _, _, _) =>
        {
            var info = new NativeMonitorInfo { Size = Marshal.SizeOf<NativeMonitorInfo>() };
            if (!GetMonitorInfo(monitor, ref info))
                return true;
            var scaleX = 1d;
            var scaleY = 1d;
            if (GetDpiForMonitor(monitor, 0, out var dpiX, out var dpiY) == 0)
            {
                scaleX = dpiX / 96d;
                scaleY = dpiY / 96d;
            }
            monitors.Add(new DisplayMonitor(
                monitor,
                info.DeviceName,
                new Rect(info.WorkArea.Left, info.WorkArea.Top,
                    info.WorkArea.Right - info.WorkArea.Left, info.WorkArea.Bottom - info.WorkArea.Top),
                scaleX,
                scaleY,
                (info.Flags & 1) != 0));
            return true;
        }, nint.Zero);
        return monitors;
    }

    private static DisplayMonitor? FindMonitor(nint windowHandle, IReadOnlyList<DisplayMonitor> monitors)
    {
        if (windowHandle == nint.Zero)
            return null;
        var handle = MonitorFromWindow(windowHandle, 2);
        return monitors.FirstOrDefault(item => item.Handle == handle);
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint window, out NativeRect bounds);

    [DllImport("user32.dll")]
    private static extern nint MonitorFromWindow(nint window, uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(
        nint deviceContext,
        nint clipRectangle,
        MonitorEnumeration callback,
        nint data);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint monitor, ref NativeMonitorInfo info);

    [DllImport("Shcore.dll")]
    private static extern int GetDpiForMonitor(nint monitor, int dpiType, out uint dpiX, out uint dpiY);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct NativeMonitorInfo
    {
        internal int Size;
        internal NativeRect MonitorArea;
        internal NativeRect WorkArea;
        internal int Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        internal string DeviceName;
    }

    private delegate bool MonitorEnumeration(nint monitor, nint deviceContext, nint bounds, nint data);
}

internal sealed record DisplayMonitor(
    nint Handle,
    string DeviceName,
    Rect WorkArea,
    double ScaleX,
    double ScaleY,
    bool IsPrimary);

public sealed class FloatingPlacementSnapshot
{
    public FloatingPlacement[] Placements { get; set; } = [];
}

public sealed class FloatingPlacement
{
    public string ContentId { get; set; } = string.Empty;
    public string Monitor { get; set; } = string.Empty;
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}
