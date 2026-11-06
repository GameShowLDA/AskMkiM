using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UI.Windows.WpfDocking.Windows;
using UI.Windows.WpfDocking.Windows.Docking;

namespace UI.Controls.Runner;

public partial class RunControl
{
  private readonly string? _runnerLayoutPath = RunnerLayoutStore.GetCurrentPath();
  private bool _restoringRunnerLayout;
  private bool _runnerDocumentsReady;
  private Window? _layoutHostWindow;
  private DockItem? _equipmentLayoutItem;
  private DockItem? _translationLayoutItem;
  private DockItem[] _suspendedFloatingItems = Array.Empty<DockItem>();
  private readonly Dictionary<DockPaneSplit, SplitterDistance> _hiddenResultSplits = new();

  internal void SuspendRunnerFloatingWindows()
  {
    if (_suspendedFloatingItems.Length != 0)
      return;

    _suspendedFloatingItems = RunnerDockManager.DockItems
      .Where(item => item.DockPosition == DockPosition.Floating).ToArray();
    foreach (var item in _suspendedFloatingItems)
      item.Hide();
  }

  internal void ResumeRunnerFloatingWindows()
  {
    if (_closingDockLayout || !RunnerDockManager.IsLoaded || PresentationSource.FromVisual(this) == null)
      return;

    var items = _suspendedFloatingItems;
    _suspendedFloatingItems = Array.Empty<DockItem>();
    foreach (var item in items)
      if (item.DockControl == RunnerDockManager && item.IsHidden)
        item.Show(DockItemShowMethod.Select);
  }

  private Dictionary<string, DockItem> GetLayoutItems()
  {
    var items = new Dictionary<string, DockItem>
    {
      ["protocol"] = ProtocolPane,
      ["errors"] = ErrorsPane,
      ["equipment"] = _equipmentLayoutItem!,
      ["translation"] = _translationLayoutItem!,
    };
    if (SourceDocument != null)
      items.Add("source", SourceDocument);
    if (_inspectionProtocolDockItem != null)
      items.Add("result", _inspectionProtocolDockItem);
    return items;
  }

  private void RestoreRunnerLayout(DockItem equipment, DockItem translation)
  {
    _equipmentLayoutItem = equipment;
    _translationLayoutItem = translation;
    if (_runnerDocumentsReady)
      return;
    _runnerDocumentsReady = true;
    if (_runnerLayoutPath == null)
      return;

    var items = GetLayoutItems();
    if (!items.ContainsKey("result"))
      items.Add("result", CreateInspectionProtocolDockItem());

    try
    {
      var saved = RunnerLayoutStore.Load(_runnerLayoutPath, items, VisualTreeHelper.GetDpi(this));
      if (saved == null)
        return;

      var previousItems = RunnerDockManager.DockItems.ToArray();
      var fallback = RunnerDockManager.Save();
      for (var i = 0; i < previousItems.Length; i++)
        fallback.DockItems[i].Target = previousItems[i];

      _restoringRunnerLayout = true;
      try
      {
        ReplaceRunnerLayout(saved);
        if (ProtocolPane.FirstPane == null || ErrorsPane.FirstPane == null
          || equipment.FirstPane == null || translation.FirstPane == null)
          throw new InvalidOperationException("В сохранённой раскладке отсутствует обязательная панель.");

        if (items["result"].FirstPane != null)
        {
          if (_inspectionProtocolEditor == null)
            items["result"].Hide();
          else if (items["result"].IsHidden)
            items["result"].Show(DockItemShowMethod.Select);
        }
        if (SourceDocument is { FirstPane: null } source)
          ShowRunnerDocument(source, DockItemShowMethod.Deselect);
        SelectRunnerDocument(translation);
        if (items["result"].FirstPane != null)
          _inspectionProtocolDockItem = items["result"];
        _errorHeightCustomized = true;
      }
      catch
      {
        ReplaceRunnerLayout(fallback);
        throw;
      }
      finally
      {
        _restoringRunnerLayout = false;
        UpdateErrorPaneSplit();
      }
    }
    catch (Exception exception)
    {
      Ask.LogLib.LoggerUtility.LogException("Не удалось восстановить раскладку исполнителя.", exception);
    }
  }

  private void ReplaceRunnerLayout(DockLayout layout)
  {
    foreach (var item in RunnerDockManager.DockItems.ToArray())
      item.Close();
    RunnerDockManager.Load(layout, target => (DockItem)target);
  }

  internal void SaveRunnerLayout()
  {
    if (_runnerLayoutPath == null || !_runnerDocumentsReady || _closingDockLayout
      || _restoringRunnerLayout || _suspendedFloatingItems.Length != 0)
      return;

    var documents = GetLayoutItems();
    var keys = RunnerDockManager.DockItems.Select(item =>
      documents.FirstOrDefault(entry => entry.Value == item).Key).ToArray();
    if (keys.Any(key => key == null) || !new[] { "protocol", "errors", "equipment", "translation" }.All(keys.Contains))
      return;

    try
    {
      Dictionary<DockPaneSplit, SplitterDistance>? sizes = null;
      var layout = DockLayoutAdapter.Save(RunnerDockManager,
        split =>
        {
          // Сохраняем заданную пропорцию, даже если минимальный размер панели временно её ограничил.
          if (split.SplitterDistance.IsStar)
            return split.SplitterDistance;
          sizes ??= MeasureRunnerSplits();
          return sizes.TryGetValue(split, out var size) || _hiddenResultSplits.TryGetValue(split, out size)
            ? size : split.SplitterDistance;
        });
      RunnerLayoutStore.Save(_runnerLayoutPath, layout, keys);
    }
    catch (Exception exception)
    {
      Ask.LogLib.LoggerUtility.LogException("Не удалось сохранить раскладку исполнителя.", exception);
    }
  }

  private Dictionary<DockPaneSplit, SplitterDistance> MeasureRunnerSplits()
  {
    var sizes = new Dictionary<DockPaneSplit, SplitterDistance>();
    MeasureLayoutSplits(RunnerDockManager, sizes);
    foreach (Window window in Application.Current.Windows)
      if (window is UI.Windows.WpfDocking.Windows.Docking.Primitives.NativeFloatingWindow
        { DataContext: FloatingWindow floating } && floating.DockControl == RunnerDockManager)
        MeasureLayoutSplits(window, sizes);
    return sizes;
  }

  private void RememberResultSplitSizes(DockPane pane)
  {
    if (_runnerLayoutPath == null)
      return;

    _hiddenResultSplits.Clear();
    var sizes = MeasureRunnerSplits();
    for (var split = pane.Parent; split != null; split = split.Parent)
      if (sizes.TryGetValue(split, out var size))
        _hiddenResultSplits.Add(split, size);
  }

  private static void MeasureLayoutSplits(DependencyObject visual, IDictionary<DockPaneSplit, SplitterDistance> sizes)
  {
    if (visual is SplitContainer { DataContext: DockPaneSplit split, Child1: FrameworkElement first,
      Child2: FrameworkElement second })
    {
      var firstLength = split.Orientation == Orientation.Horizontal ? first.ActualWidth : first.ActualHeight;
      var secondLength = split.Orientation == Orientation.Horizontal ? second.ActualWidth : second.ActualHeight;
      if (firstLength > 0 && secondLength > 0)
      {
        var ratio = split.IsSplitterTopLeft ? firstLength / secondLength : secondLength / firstLength;
        sizes[split] = new SplitterDistance(ratio, SplitterUnitType.Star);
      }
    }
    for (var i = 0; i < VisualTreeHelper.GetChildrenCount(visual); i++)
      MeasureLayoutSplits(VisualTreeHelper.GetChild(visual, i), sizes);
  }

  private void LayoutHost_Closing(object? sender, CancelEventArgs e) => SaveRunnerLayout();

  private void DetachLayoutHost()
  {
    if (_layoutHostWindow != null)
      _layoutHostWindow.Closing -= LayoutHost_Closing;
    _layoutHostWindow = null;
  }
}
