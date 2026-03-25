using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.View.EditorHost.TextEditor;
using Ask.UI.Features.Notifications.Models;
using Ask.UI.Infrastructure.UI.Overlay.Notifications.Runtime;
using Ask.UI.Shared.Contracts.Ask.UI.Shared.Contracts;
using System.Windows;
using System.Windows.Controls;
using Ask.UI.Controls.ErrorList;
using System.Windows.Media;
using UI.Components;
using UI.Controls.TextEditor;
using UI.Services;
using UI.Services.Archive;
using Ask.Core.Services.EventCore.Adapters;

namespace UI.Controls
{
  /// <summary>
  /// Логика взаимодействия для TranslatorContainer.xaml
  /// </summary>
  public partial class TranslatorItem : UserControl
  {
    public sealed class TranslationIssuesSnapshot
    {
      public TranslationIssuesSnapshot(List<IDisplayIssue> issues, int errorCount, int warningCount)
      {
        Issues = issues;
        ErrorCount = errorCount;
        WarningCount = warningCount;
      }

      public List<IDisplayIssue> Issues { get; }

      public int ErrorCount { get; }

      public int WarningCount { get; }
    }

    public string FirstFilePath { get; set; }
    public string SecondFilePath { get; set; }

    public int ErrorCount { get; private set; } = 0;
    public int WarningCount { get; private set; } = 0;
    public int GeneralCount => ErrorCount + WarningCount;

    private List<BaseCommandModel> translationModels = new List<BaseCommandModel>();
    private readonly ArchiveSaveService _archiveSaveService = new ArchiveSaveService();

    public List<BaseCommandModel> TranslationModels
    {
      get => translationModels;
      set
      {
        translationModels = value;

        ErrorClear();
        ErrorListBoxVertical.ClearAll();

        foreach (var model in value)
        {
          if (model.Errors.Count > 0)
          {
            ErrorListBoxVertical.AddErrors(model.Errors);
            ErrorCount += model.Errors.Count;
          }

          if (model.Warnings.Count > 0)
          {
            ErrorListBoxVertical.AddWarnings(model.Warnings);
            WarningCount += model.Warnings.Count;
          }
        }

        Ask.Core.Services.EventCore.Adapters.MessageEventAdapter.RaiseInfoMessage(
          $"Общее кол-во ошибок и предупреждений: {GeneralCount}");
		  
        ApplyTranslationModels(value, BuildIssuesSnapshot(value));
      }
    }

    public TranslatorItem()
    {
      InitializeComponent();

      ErrorListBoxVertical.ItemDoubleClicked += ErrorListBoxVertical_ErrorItemDoubleClicked;

      // Новое: вкладка "Точки остановки"
      ErrorListBoxVertical.BreakpointItemDoubleClicked += ErrorListBoxVertical_BreakpointItemDoubleClicked;
      ErrorListBoxVertical.BreakpointEnabledChanged += ErrorListBoxVertical_BreakpointEnabledChanged;

      // События брейкпоинтов
      EventAggregator.Subscribe<BreakpointEvents.BreakpointSet>(e => BreakpointSet(e));
      EventAggregator.Subscribe<BreakpointEvents.BreakpointRemoved>(e => BreakpointRemoved(e));
      EventAggregator.Subscribe<BreakpointEvents.BreakpointOn>(e => BreakpointOn(e));
      EventAggregator.Subscribe<BreakpointEvents.BreakpointOff>(e => BreakpointOff(e));
    }

    private void ErrorListBoxVertical_ErrorItemDoubleClicked(IDisplayIssue item)
    {
      var leftEditor = GetLeftBox().GetTextEditor();
      leftEditor.GoToLine(item.SourceLineNumber);

      var rightEditor = GetRightBox().GetTextEditor();
      rightEditor.GoToLine(item.FormattedLineNumber);
    }

    private void ErrorClear()
    {
      ErrorCount = 0;
      WarningCount = 0;
    }

    public static TranslationIssuesSnapshot BuildIssuesSnapshot(IEnumerable<BaseCommandModel> models)
    {
      var issues = new List<IDisplayIssue>();
      int errorCount = 0;
      int warningCount = 0;

      foreach (var model in models)
      {
        if (model.Errors.Count > 0)
        {
          issues.AddRange(model.Errors);
          errorCount += model.Errors.Count;
        }

        if (model.Warnings.Count > 0)
        {
          issues.AddRange(model.Warnings);
          warningCount += model.Warnings.Count;
        }
      }

      return new TranslationIssuesSnapshot(issues, errorCount, warningCount);
    }

    public void ApplyTranslationModels(List<BaseCommandModel> models, TranslationIssuesSnapshot issuesSnapshot)
    {
      translationModels = models;
      ErrorClear();
      ErrorCount = issuesSnapshot.ErrorCount;
      WarningCount = issuesSnapshot.WarningCount;

      ErrorListBoxVertical.SetIssues(issuesSnapshot.Issues);
      UpdateArchiveButtonVisibility();

      MessageEventAdapter.RaiseInfoMessage(
             $"Общее кол-во ошибок и предупреждений: {GeneralCount}");
    }

    public void SetLeftEditor(ITextEditorView editor)
    {
      if (editor is not IUiViewAdapter adapter)
        return;

      if (adapter.NativeView is not UIElement element)
        return;

      DetachFromParent(element);

      if (LeftBox == null || element == null)
      {
        return;
      }

      var leftEditor = GetLeftBox();
      if (leftEditor == null)
      {
        LeftBox.Children.Clear();
        leftEditor = new TranslatorTextEditor();
        LeftBox.Children.Add(leftEditor);
      }

      leftEditor.SetEditor(editor);
      leftEditor.SaveRequested -= LeftEditor_SaveRequested;
      leftEditor.SaveRequested += LeftEditor_SaveRequested;
      leftEditor.OpenFolderRequested -= LeftEditor_OpenFolderRequested;
      leftEditor.OpenFolderRequested += LeftEditor_OpenFolderRequested;
      leftEditor.PrintRequested -= LeftEditor_PrintRequested;
      leftEditor.PrintRequested += LeftEditor_PrintRequested;
    }

    public void SetRightEditor(ITextEditorView textEditorUI)
    {
      if (RightBox == null || textEditorUI == null)
        return;

      var rightEditor = GetRightBox();
      if (rightEditor == null)
      {
        RightBox.Children.Clear();
        rightEditor = new TranslatorEditor();
        RightBox.Children.Add(rightEditor);
      }

      rightEditor.SetEditor(textEditorUI);
      rightEditor.BackRequested -= RightEditor_BackRequestedAsync;
      rightEditor.BackRequested += RightEditor_BackRequestedAsync;
      rightEditor.SaveRequested -= RightEditor_SaveRequestedAsync;
      rightEditor.SaveRequested += RightEditor_SaveRequestedAsync;
      rightEditor.SetArchiveButtonVisibility(ErrorCount == 0);
    }

    private void RightEditor_BackRequestedAsync(object? sender, EventArgs e)
    {
      if (TranslatorNavigationService.TryOpenSourceFileFromTranslator(GetRightBox()))
      {
        CloseTranslatorTab();
      }
    }

    private void RightEditor_SaveRequestedAsync(object? sender, EventArgs e)
    {
      bool flowControl = SaveFileToArchive();
      if (!flowControl)
      {
        return;
      }
    }

    private bool SaveFileToArchive()
    {
      var rightBox = GetRightBox();
      var rightTextEditor = rightBox?.GetTextEditor();
      if (rightTextEditor?.TextEditorModel == null)
      {
        NotificationHostService.Instance.Show(
          "Сохранение в архив",
          "Редактор не готов к сохранению в архив.",
          NotificationType.Error);
        return false;
      }

      return _archiveSaveService.SaveFileToArchive(this, TranslationModels, rightTextEditor.TextEditorModel.FilePath);
    }

    private void LeftEditor_SaveRequested(object? sender, EventArgs e)
    {
      FindMultiEditorControl()?.EditorDocumentService.SaveFile();
    }

    private void LeftEditor_OpenFolderRequested(object? sender, EventArgs e)
    {
      FindMultiEditorControl()?.EditorDocumentService.OpenFolder();
    }

    private void LeftEditor_PrintRequested(object? sender, EventArgs e)
    {
      FindMultiEditorControl()?.EditorDocumentService.PrintFile();
    }

    private void CloseTranslatorTab()
    {
      var textEditorContainer = FindTextEditorContainer();
      if (textEditorContainer == null)
      {
        return;
      }

      var translatorDockItem = textEditorContainer.DockManager.DockItems
        .FirstOrDefault(item => item.Content == this);

      translatorDockItem?.PerformClose();
    }

    private TextEditorContainer? FindTextEditorContainer()
    {
      DependencyObject? current = this;

      while (current != null)
      {
        if (current is TextEditorContainer textEditorContainer)
        {
          return textEditorContainer;
        }

        current = current is FrameworkElement frameworkElement && frameworkElement.Parent != null
          ? frameworkElement.Parent
          : VisualTreeHelper.GetParent(current);
      }

      return null;
    }

    private MultiEditorControl? FindMultiEditorControl()
    {
      DependencyObject? current = this;

      while (current != null)
      {
        if (current is MultiEditorControl multiEditorControl)
        {
          return multiEditorControl;
        }

        current = current is FrameworkElement frameworkElement && frameworkElement.Parent != null
          ? frameworkElement.Parent
          : VisualTreeHelper.GetParent(current);
      }

      return null;
    }

    public TranslatorEditor GetRightBox()
    {
      if (RightBox == null)
      {
        return null;
      }

      return RightBox.Children[0] as TranslatorEditor;
    }

    public TranslatorTextEditor GetLeftBox()
    {
      if (LeftBox == null)
      {
        return null;
      }

      return LeftBox.Children[0] as TranslatorTextEditor;
    }

    public string GetLeftEditorName() => GetLeftBox().FileName.Text;
    public string GetRightEditorName() => GetRightBox().TranslationFileName.Text;

    public void SetRightEditorName(string newText) => GetRightBox().TranslationFileName.Text = newText;
    public void SetLeftEditorName(string newText) => GetLeftBox().FileName.Text = newText;

    private void UpdateArchiveButtonVisibility()
    {
      var rightEditor = GetRightBox();
      rightEditor?.SetArchiveButtonVisibility(ErrorCount == 0);
    }

    private void ErrorListBoxVertical_BreakpointItemDoubleClicked(BreakpointListItem bp)
	  {
      var model = GetCommandByNumber(bp.CommandNumber);
      if (model == null) return;

      var left = GetLeftBox().GetTextEditor();
      var right = GetRightBox().GetTextEditor();
	    if (left == null || right == null) return;

	    int leftLine = model.StartLineNumber;
	    int rightLine = model.FormattedStartLineNumber + 1;

	    left.GoToLine(leftLine);
	    right.GoToLine(rightLine);
	  }

    private void ErrorListBoxVertical_BreakpointEnabledChanged(BreakpointListItem bp, bool enabled)
	  {
	    var left = GetLeftBox().GetTextEditor();
	    var right = GetRightBox().GetTextEditor();
	    if (left == null || right == null) return;

	    if (!left.HasBreakpointCommand(bp.CommandNumber) && !right.HasBreakpointCommand(bp.CommandNumber))
		    return;

	    bool current = left.HasBreakpointCommand(bp.CommandNumber)
		    ? left.IsBreakpointEnabled(bp.CommandNumber)
		    : right.IsBreakpointEnabled(bp.CommandNumber);

	    if (current == enabled)
		    return;

	    if (enabled)
	    {
		    left.EnableBreakpoint(bp.CommandNumber, raiseEvents: true);
		    right.EnableBreakpoint(bp.CommandNumber, raiseEvents: false);
	    }
	    else
	    {
		    left.DisableBreakpoint(bp.CommandNumber, raiseEvents: true);
		    right.DisableBreakpoint(bp.CommandNumber, raiseEvents: false);
	    }
	  }

    private static void DetachFromParent(UIElement element)
    {
      switch (element)
      {
        case FrameworkElement fe when fe.Parent is Panel panel:
          panel.Children.Remove(element);
          break;

        case FrameworkElement fe when fe.Parent is ContentControl content:
          content.Content = null;
          break;

        case FrameworkElement fe when fe.Parent is Decorator decorator:
          decorator.Child = null;
          break;
      }
    }

    /// <summary>
    /// Обработчик события установки точки.
    /// Обновляет модель команды и синхронизирует точки в обоих редакторах
    /// без повторной генерации событий.
    /// </summary>
    /// <param name="obj">Событие установки точки (содержит номер команды).</param>
    private void BreakpointSet(BreakpointEvents.BreakpointSet obj)
    {
      var model = GetCommandByNumber(obj.CommandNumber);
      if (model == null) return;

      model.HasBreakpoint = true;
      model.IsBreakpointEnabled = true;

      var left = GetLeftBox().GetTextEditor();
      var right = GetRightBox().GetTextEditor();
	    if (left == null || right == null) return;

      int leftLine = model.StartLineNumber;
      int rightLine = model.FormattedStartLineNumber + 1;

      left.EnsureBreakpoint(leftLine, obj.CommandNumber, isSet: true, raiseEvents: false);
      right.EnsureBreakpoint(rightLine, obj.CommandNumber, isSet: true, raiseEvents: false);

      ErrorListBoxVertical.UpsertBreakpoint(obj.CommandNumber, rightLine, model.Mnemonic, isEnabled: true);

      if (obj.LineNumber == leftLine)
        right.ScrollToLine(rightLine);
    }

    /// <summary>
    /// Обработчик события снятия точки.
    /// Обновляет модель команды и синхронизирует снятие точки в обоих редакторах
    /// без повторной генерации событий.
    /// </summary>
    /// <param name="obj">Событие снятия точки (содержит номер команды).</param>
    private void BreakpointRemoved(BreakpointEvents.BreakpointRemoved obj)
    {
      var model = GetCommandByNumber(obj.CommandNumber);
      if (model == null) return;

      model.HasBreakpoint = false;
      model.IsBreakpointEnabled = true;

      var left = GetLeftBox().GetTextEditor();
      var right = GetRightBox().GetTextEditor();
	    if (left == null || right == null) return;

      int leftLine = model.StartLineNumber;
      int rightLine = model.FormattedStartLineNumber + 1;

      left.EnsureBreakpoint(leftLine, obj.CommandNumber, isSet: false, raiseEvents: false);
      right.EnsureBreakpoint(rightLine, obj.CommandNumber, isSet: false, raiseEvents: false);

      ErrorListBoxVertical.RemoveBreakpoint(obj.CommandNumber);

      if (obj.LineNumber == leftLine)
		    right.ScrollToLine(rightLine);
    }

    private void BreakpointOn(BreakpointEvents.BreakpointOn obj)
    {
      var model = GetCommandByNumber(obj.CommandNumber);
      if (model == null) return;

      model.HasBreakpoint = true;
      model.IsBreakpointEnabled = true;

      var left = GetLeftBox().GetTextEditor();
      var right = GetRightBox().GetTextEditor();
	    if (left == null || right == null) return;

      int leftLine = model.StartLineNumber;
      int rightLine = model.FormattedStartLineNumber + 1;

      left.EnableBreakpoint(obj.CommandNumber, raiseEvents: false);
      right.EnableBreakpoint(obj.CommandNumber, raiseEvents: false);

      ErrorListBoxVertical.UpsertBreakpoint(obj.CommandNumber, rightLine, model.Mnemonic, isEnabled: true);

      right.ScrollToLine(rightLine);
    }

    private void BreakpointOff(BreakpointEvents.BreakpointOff obj)
    {
      var model = GetCommandByNumber(obj.CommandNumber);
      if (model == null) return;

      model.HasBreakpoint = true;
      model.IsBreakpointEnabled = false;

      var left = GetLeftBox().GetTextEditor();
      var right = GetRightBox().GetTextEditor();
	    if (left == null || right == null) return;

      int leftLine = model.StartLineNumber;
      int rightLine = model.FormattedStartLineNumber + 1;

      left.DisableBreakpoint(obj.CommandNumber, raiseEvents: false);
      right.DisableBreakpoint(obj.CommandNumber, raiseEvents: false);

      ErrorListBoxVertical.UpsertBreakpoint(obj.CommandNumber, rightLine, model.Mnemonic, isEnabled: false);

      right.ScrollToLine(rightLine);
    }

    private BaseCommandModel? GetCommandByNumber(int commandNumber)
    {
      return translationModels
        .FirstOrDefault(x =>
          int.TryParse(x.CommandNumber, out var num) &&
          num == commandNumber);
    }
  }
}
