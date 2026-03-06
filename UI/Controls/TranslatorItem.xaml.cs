using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.View.EditorHost.TextEditor;
using Ask.UI.Shared.Contracts.Ask.UI.Shared.Contracts;
using System.Windows;
using System.Windows.Controls;
using UI.Controls.ErrorList;
using UI.Controls.TextEditor;

namespace UI.Controls
{
  /// <summary>
  /// Логика взаимодействия для TranslatorContainer.xaml
  /// </summary>
  public partial class TranslatorItem : UserControl
  {
    public string FirstFilePath { get; set; }
    public string SecondFilePath { get; set; }

    public int ErrorCount { get; private set; } = 0;
    public int WarningCount { get; private set; } = 0;
    public int GeneralCount => ErrorCount + WarningCount;

    private List<BaseCommandModel> translationModels = new List<BaseCommandModel>();
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
      var leftEditor = GetLeftEditor();
      leftEditor?.GoToLine(item.SourceLineNumber);

      var rightEditor = GetRightEditor();
      rightEditor?.GoToLine(item.FormattedLineNumber);
    }

    private void ErrorClear()
    {
      ErrorCount = 0;
      WarningCount = 0;
    }

    public void SetLeftEditor(ITextEditorView editor)
    {
      if (editor is not IUiViewAdapter adapter)
        return;

      if (adapter.NativeView is not UIElement element)
        return;

      DetachFromParent(element);

      LeftBox.Children.Clear();
      LeftBox.Children.Add(element);
    }

    public void SetRightEditor(ITextEditorView textEditorUI)
    {
      if (RightBox == null || textEditorUI == null)
        return;

      RightBox.Children.Clear();
      RightBox.Children.Add(textEditorUI.View);
    }

    public TextEditorUI GetRightEditor()
    {
      if (RightBox == null) return null;
      return RightBox.Children[0] as TextEditorUI;
    }

    public TextEditorUI GetLeftEditor()
    {
      if (LeftBox == null) return null;
      return LeftBox.Children[0] as TextEditorUI;
    }

    public string GetLeftEditorName() => FirstFileName.Text;
    public string GetRightEditorName() => SecondFileName.Text;

    public void SetRightEditorName(string newText) => SecondFileName.Text = newText;
    public void SetLeftEditorName(string newText) => FirstFileName.Text = newText;

    private void ErrorListBoxVertical_BreakpointItemDoubleClicked(BreakpointListItem bp)
    {
      var model = GetCommandByNumber(bp.CommandNumber);
      if (model == null) return;

      var left = GetLeftEditor();
      var right = GetRightEditor();
      if (left == null || right == null) return;

      int leftLine = model.StartLineNumber + 1;
      int rightLine = model.FormattedStartLineNumber + 1;

      left.GoToLine(leftLine);
      right.GoToLine(rightLine);
    }

    private void ErrorListBoxVertical_BreakpointEnabledChanged(BreakpointListItem bp, bool enabled)
    {
      var left = GetLeftEditor();
      var right = GetRightEditor();
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

      var left = GetLeftEditor();
      var right = GetRightEditor();
      if (left == null || right == null) return;

      int leftLine = model.StartLineNumber + 1;
      int rightLine = model.FormattedStartLineNumber + 1;

      left.EnsureBreakpoint(leftLine, obj.CommandNumber, isSet: true, raiseEvents: false);
      right.EnsureBreakpoint(rightLine, obj.CommandNumber, isSet: true, raiseEvents: false);

      ErrorListBoxVertical.UpsertBreakpoint(obj.CommandNumber, rightLine, isEnabled: true);

      if (obj.LineNumber == leftLine - 1)
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

      var left = GetLeftEditor();
      var right = GetRightEditor();
      if (left == null || right == null) return;

      int leftLine = model.StartLineNumber + 1;
      int rightLine = model.FormattedStartLineNumber + 1;

      left.EnsureBreakpoint(leftLine, obj.CommandNumber, isSet: false, raiseEvents: false);
      right.EnsureBreakpoint(rightLine, obj.CommandNumber, isSet: false, raiseEvents: false);

      ErrorListBoxVertical.RemoveBreakpoint(obj.CommandNumber);

      if (obj.LineNumber == leftLine - 1)
        right.ScrollToLine(rightLine);
    }

    private void BreakpointOn(BreakpointEvents.BreakpointOn obj)
    {
      var model = GetCommandByNumber(obj.CommandNumber);
      if (model == null) return;

      var left = GetLeftEditor();
      var right = GetRightEditor();
      if (left == null || right == null) return;

      int leftLine = model.StartLineNumber + 1;
      int rightLine = model.FormattedStartLineNumber + 1;

      left.EnableBreakpoint(obj.CommandNumber, raiseEvents: false);
      right.EnableBreakpoint(obj.CommandNumber, raiseEvents: false);

      ErrorListBoxVertical.UpsertBreakpoint(obj.CommandNumber, rightLine, isEnabled: true);
    }

    private void BreakpointOff(BreakpointEvents.BreakpointOff obj)
    {
      var model = GetCommandByNumber(obj.CommandNumber);
      if (model == null) return;

      var left = GetLeftEditor();
      var right = GetRightEditor();
      if (left == null || right == null) return;

      int leftLine = model.StartLineNumber + 1;
      int rightLine = model.FormattedStartLineNumber + 1;

      left.DisableBreakpoint(obj.CommandNumber, raiseEvents: false);
      right.DisableBreakpoint(obj.CommandNumber, raiseEvents: false);

      ErrorListBoxVertical.UpsertBreakpoint(obj.CommandNumber, rightLine, isEnabled: false);
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