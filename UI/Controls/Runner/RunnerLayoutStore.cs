using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Ask.Core.Services.Config.AppSettings;
using UI.Windows.WpfDocking.Windows;
using UI.Windows.WpfDocking.Windows.Docking;

namespace UI.Controls.Runner;

internal static class RunnerLayoutStore
{
  private const int CurrentVersion = 2;

  internal static string? GetCurrentPath() => RoleAuthorizationConfig.CurrentRole is { } role
    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
      "AskMkiM", "runner-layout", $"{role}.json")
    : null;

  internal static void Save(string path, DockLayout layout, string[] keys)
  {
    var data = new Snapshot(keys, layout.ShowActions.Select(ActionData.FromAction).ToArray(),
      new[] { layout.LeftDockTreeWidth, layout.RightDockTreeWidth, layout.TopDockTreeHeight,
        layout.BottomDockTreeHeight }.Select(Format).ToArray(),
      TypeDescriptor.GetConverter(typeof(DockTreeZOrder)).ConvertToInvariantString(layout.DockTreeZOrder)!, CurrentVersion);
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
    try
    {
      File.WriteAllText(temporary, JsonSerializer.Serialize(data));
      File.Move(temporary, path, overwrite: true);
    }
    finally
    {
      File.Delete(temporary);
    }
  }

  internal static DockLayout? Load(string path, IReadOnlyDictionary<string, DockItem> items, DpiScale dpi)
  {
    if (!File.Exists(path))
      return null;

    var data = JsonSerializer.Deserialize<Snapshot>(File.ReadAllText(path));
    if (data?.Keys == null || data.Actions == null || data.Edges?.Length != 4
      || data.Version is < 0 or > CurrentVersion
      || data.Keys.Length > items.Count || data.Keys.Distinct().Count() != data.Keys.Length
      || data.Keys.Any(key => key == null || !items.ContainsKey(key))
      || !new[] { "protocol", "errors", "equipment", "translation" }.All(data.Keys.Contains)
      || data.Actions.Length > data.Keys.Length * 2)
      return null;

    var layout = new DockLayout
    {
      LeftDockTreeWidth = Parse(data.Edges[0]),
      RightDockTreeWidth = Parse(data.Edges[1]),
      TopDockTreeHeight = Parse(data.Edges[2]),
      BottomDockTreeHeight = Parse(data.Edges[3]),
      DockTreeZOrder = (DockTreeZOrder)TypeDescriptor.GetConverter(typeof(DockTreeZOrder))
        .ConvertFromInvariantString(data.Order)!,
    };
    foreach (var key in data.Keys)
      layout.DockItems.Add(new DockItemReference { Target = items[key] });
    foreach (var entry in data.Actions)
      layout.ShowActions.Add(entry.ToAction(data.Keys.Length, dpi, data.Version < CurrentVersion));
    return layout;
  }

  private static string Format(SplitterDistance value) =>
    TypeDescriptor.GetConverter(typeof(SplitterDistance)).ConvertToInvariantString(value)!;

  private static SplitterDistance Parse(string value) =>
    (SplitterDistance)TypeDescriptor.GetConverter(typeof(SplitterDistance)).ConvertFromInvariantString(value)!;

  private static Rect FitToMonitor(double[] bounds, DpiScale dpi)
  {
    if (bounds.Length != 4 || bounds.Any(value => !double.IsFinite(value) || Math.Abs(value) > 100000)
      || bounds[2] <= 0 || bounds[3] <= 0)
      throw new FormatException("Недопустимые размеры плавающей панели.");

    var pixels = new System.Drawing.Rectangle((int)(bounds[0] * dpi.DpiScaleX),
      (int)(bounds[1] * dpi.DpiScaleY), (int)(bounds[2] * dpi.DpiScaleX),
      (int)(bounds[3] * dpi.DpiScaleY));
    var work = System.Windows.Forms.Screen.FromRectangle(pixels).WorkingArea;
    var width = Math.Min(bounds[2], work.Width / dpi.DpiScaleX);
    var height = Math.Min(bounds[3], work.Height / dpi.DpiScaleY);
    return new Rect(
      Math.Clamp(bounds[0], work.Left / dpi.DpiScaleX, work.Right / dpi.DpiScaleX - width),
      Math.Clamp(bounds[1], work.Top / dpi.DpiScaleY, work.Bottom / dpi.DpiScaleY - height),
      width, height);
  }

  private sealed record Snapshot(string[] Keys, ActionData[] Actions, string[] Edges, string Order, int Version = 0);

  private sealed record ActionData(
    string Kind, int Source, DockItemShowMethod Method, int Target = -1,
    bool Floating = false, int Index = -1, DockPosition Position = DockPosition.Unknown,
    Dock Side = Dock.Left, string? Size = null, bool AutoHide = false,
    bool SizeForTarget = false, int Ancestor = 0, double[]? Bounds = null)
  {
    internal static ActionData FromAction(ShowAction action) => action switch
    {
      ShowAsTabbedAction tab => new("tab", tab.Source, tab.ShowMethod, tab.Target, tab.IsFloating, tab.Index),
      ShowAsSidePaneAction split => new("split", split.Source, split.ShowMethod, split.Target, split.IsFloating,
        Side: split.Side, Size: Format(split.Size), AutoHide: split.IsAutoHide,
        SizeForTarget: split.IsSizeForTarget, Ancestor: split.AncestorLevel),
      ShowAsDockPositionAction dock => new("dock", dock.Source, dock.ShowMethod, Position: dock.DockPosition),
      ShowAsFloatingAction floating => new("float", floating.Source, floating.ShowMethod,
        Bounds: new[] { floating.Left, floating.Top, floating.Width, floating.Height }),
      _ => throw new NotSupportedException(action.GetType().Name),
    };

    internal ShowAction ToAction(int count, DpiScale dpi, bool legacy)
    {
      if (Source < 0 || Source >= count || !Enum.IsDefined(Method)
        || ((Kind == "tab" || Kind == "split") && (Target < 0 || Target >= count || Target == Source))
        || Ancestor < 0 || Ancestor >= count || Index < -1 || Index > count)
        throw new FormatException("Недопустимое действие раскладки исполнителя.");

      if (legacy && Kind == "split" && Enum.IsDefined(Side))
      {
        // Старый формат записывал сторону измеряемой панели и позицию цели.
        bool sizedFirst = Side is Dock.Left or Dock.Top;
        bool horizontal = Side is Dock.Left or Dock.Right;
        var sourceSide = SizeForTarget
          ? (horizontal ? Dock.Right : Dock.Bottom)
          : (horizontal ? Dock.Left : Dock.Top);
        return (this with { Side = sourceSide, SizeForTarget = sizedFirst == SizeForTarget })
          .ToAction(count, dpi, legacy: false);
      }

      return Kind switch
      {
        "tab" => new ShowAsTabbedAction(Source, Target, Floating, Index, Method),
        "split" when Enum.IsDefined(Side) && Size != null => new ShowAsSidePaneAction(
          Source, Target, Floating, Ancestor, AutoHide, Side, Parse(Size), SizeForTarget, Method),
        "dock" when Enum.IsDefined(Position) => new ShowAsDockPositionAction(Source, Position, Method),
        "float" when Bounds != null => new ShowAsFloatingAction(Source, FitToMonitor(Bounds, dpi), Method),
        _ => throw new FormatException("Неизвестное действие раскладки исполнителя."),
      };
    }
  }
}
