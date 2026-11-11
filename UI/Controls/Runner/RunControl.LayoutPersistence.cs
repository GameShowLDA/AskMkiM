using Ask.UI.ThirdParty.Docking.Integration.ExecutionWorkspace;
using System.IO;
using System.ComponentModel;
using System.Windows;
using UI.Windows.WpfDocking.Windows.Docking;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.Metadata.Enums.RoleEnums;

namespace UI.Controls.Runner;

public partial class RunControl
{
  private readonly string? _runnerLayoutPath = BuildAvalonLayoutPath();
  private readonly RoleType? _runnerLayoutRole = RoleAuthorizationConfig.CurrentRole;
  private bool _restoringRunnerLayout;
  private bool _runnerDocumentsReady;
  private Window? _layoutHostWindow;
  private DockItem? _equipmentLayoutItem;
  private DockItem? _translationLayoutItem;

  internal void SuspendRunnerFloatingWindows() => RunnerWorkspace.SetFloatingWindowsVisible(false);

  internal void ResumeRunnerFloatingWindows()
  {
    if (!_closingDockLayout && RunnerWorkspace.IsLoaded)
      RunnerWorkspace.SetFloatingWindowsVisible(true);
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

    _restoringRunnerLayout = true;
    try
    {
      RunnerWorkspace.TryRestoreLayout(_runnerLayoutPath);
      RebindWorkspaceContents();
      SelectRunnerDocument(translation);
    }
    catch (Exception exception)
    {
      Ask.LogLib.LoggerUtility.LogException("Не удалось восстановить раскладку AvalonDock исполнителя.", exception);
      RunnerWorkspace.ResetLayout();
      RebindWorkspaceContents();
    }
    finally
    {
      _restoringRunnerLayout = false;
    }
  }

  private void RebindWorkspaceContents()
  {
    RunnerWorkspace.SetContent(ExecutionWorkspacePane.Protocol, ProtocolPane);
    RunnerWorkspace.SetContent(ExecutionWorkspacePane.Errors, ErrorsPane);
    if (_equipmentLayoutItem != null)
      ShowRunnerDocument(_equipmentLayoutItem, DockItemShowMethod.Deselect);
    if (_translationLayoutItem != null)
      ShowRunnerDocument(_translationLayoutItem, DockItemShowMethod.Deselect);
    if (SourceDocument != null)
      ShowRunnerDocument(SourceDocument, DockItemShowMethod.Deselect);
    else
      RunnerWorkspace.HidePane(ExecutionWorkspacePane.Source);
    if (_inspectionProtocolDockItem != null && _inspectionProtocolEditor != null)
      ShowRunnerDocument(_inspectionProtocolDockItem, DockItemShowMethod.Deselect);
    else
      RunnerWorkspace.HidePane(ExecutionWorkspacePane.Result);
  }

  internal void SaveRunnerLayout(bool allowClosing = false)
  {
    if (_runnerLayoutPath == null || !_runnerDocumentsReady || _restoringRunnerLayout
      || (_closingDockLayout && !allowClosing))
      return;

    try
    {
      RunnerWorkspace.SaveLayout(_runnerLayoutPath);
    }
    catch (Exception exception)
    {
      Ask.LogLib.LoggerUtility.LogException("Не удалось сохранить раскладку AvalonDock исполнителя.", exception);
    }
  }

  private void LayoutHost_Closing(object? sender, CancelEventArgs e) => SaveRunnerLayout();

  private void DetachLayoutHost()
  {
    if (_layoutHostWindow != null)
      _layoutHostWindow.Closing -= LayoutHost_Closing;
    _layoutHostWindow = null;
  }

  private static string? BuildAvalonLayoutPath()
  {
    var legacyPath = RunnerLayoutStore.GetCurrentPath();
    return legacyPath == null ? null : Path.ChangeExtension(legacyPath, ".avalondock.json");
  }

  private void OnRunnerLayoutResetRequested(ExecutionEvents.RunnerLayoutResetRequested request)
  {
    if (_closingDockLayout || request.Role != _runnerLayoutRole)
      return;

    Dispatcher.Invoke(() =>
    {
      RunnerWorkspace.ResetLayout();
      RebindWorkspaceContents();
      if (_translationLayoutItem != null)
        SelectRunnerDocument(_translationLayoutItem);
      else
        RunnerWorkspace.ActivatePane(ExecutionWorkspacePane.Protocol);
      SaveRunnerLayout();
    });
  }
}
