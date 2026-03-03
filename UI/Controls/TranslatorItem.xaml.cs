using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.View.EditorHost.TextEditor;
using Ask.UI.Shared.Contracts.Ask.UI.Shared.Contracts;
using ICSharpCode.AvalonEdit;
using System;
using System.Windows;
using System.Windows.Controls;
using UI.Controls.TextEditor;
using UI.Services;

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
      get
      {
        return translationModels;
      }
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
        MessageEventAdapter.RaiseInfoMessage(
               $"Общее кол-во ошибок и предупреждений: {GeneralCount}");
      }
    }

    public TranslatorItem()
    {
      InitializeComponent();
      ErrorListBoxVertical.ItemDoubleClicked += ErrorListBoxVertical_ErrorItemDoubleClicked;

      EventAggregator.Subscribe<BreakpointEvents.BreakpointSet>(e => BreakpointSet(e));
      EventAggregator.Subscribe<BreakpointEvents.BreakpointRemoved>(e => BreakpointRemoved(e));
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
    }

    public void SetRightEditor(ITextEditorView textEditorUI)
    {
      if (RightBox == null || textEditorUI == null)
      {
        return;
      }

      var rightEditor = GetRightBox();
      if (rightEditor == null)
      {
        RightBox.Children.Clear();
        rightEditor = new TranslatorEditor();
        RightBox.Children.Add(rightEditor);
      }

      rightEditor.SetEditor(textEditorUI);
      rightEditor.BackRequested -= RightEditor_BackRequested;
      rightEditor.BackRequested += RightEditor_BackRequested;
    }

    private void RightEditor_BackRequested(object? sender, EventArgs e)
    {
      TranslatorNavigationService.TryOpenSourceFileFromTranslator(GetRightBox());
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

    public string GetLeftEditorName()
    {
      return GetLeftBox().FileName.Text;
    }

    public string GetRightEditorName()
    {
      return GetRightBox().TranslationFileName.Text;
    }

    public void SetRightEditorName(string newText)
    {
      GetRightBox().TranslationFileName.Text = newText;
    }

    public void SetLeftEditorName(string newText)
    {
      GetLeftBox().FileName.Text = newText;
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

      var left = GetLeftBox().GetTextEditor();
      var right = GetRightBox().GetTextEditor();

      int leftLine = model.StartLineNumber + 1;
      int rightLine = model.FormattedStartLineNumber + 1;

      left.EnsureBreakpoint(leftLine, obj.CommandNumber, isSet: true, raiseEvents: false);

      right.EnsureBreakpoint(rightLine, obj.CommandNumber, isSet: true, raiseEvents: false);

      if (obj.LineNumber == leftLine - 1)
        right.GoToLine(rightLine);
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

      var left = GetLeftBox().GetTextEditor();
      var right = GetRightBox().GetTextEditor();

      int leftLine = model.StartLineNumber + 1;
      int rightLine = model.FormattedStartLineNumber + 1;

      left.EnsureBreakpoint(leftLine, obj.CommandNumber, isSet: false, raiseEvents: false);
      right.EnsureBreakpoint(rightLine, obj.CommandNumber, isSet: false, raiseEvents: false);

      if (obj.LineNumber == leftLine - 1)
        right.GoToLine(rightLine);
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
