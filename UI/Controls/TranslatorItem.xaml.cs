using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.View.EditorHost.TextEditor;
using Ask.UI.Features.Notifications.Models;
using Ask.UI.Infrastructure.UI.Overlay.Notifications.Runtime;
using Ask.UI.Shared.Contracts.Ask.UI.Shared.Contracts;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UI.Components;
using UI.Controls.TextEditorControl;
using UI.Services;
using UI.Services.Archive;

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
      get
      {
        return translationModels;
      }
      set
      {
        ApplyTranslationModels(value, BuildIssuesSnapshot(value));
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
      rightEditor.BackRequested -= RightEditor_BackRequestedAsync;
      rightEditor.BackRequested += RightEditor_BackRequestedAsync;
      rightEditor.SaveRequested -= RightEditor_SaveRequestedAsync;
      rightEditor.SaveRequested += RightEditor_SaveRequestedAsync;
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
        right.GoToLineWithoutSelection(rightLine);
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
        right.GoToLineWithoutSelection(rightLine);
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
