using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Engine.ControlCommandAnalyser.Model;
using System.Windows.Controls;
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
      var leftEditor = GetLeftEditor();
      leftEditor.GoToLine(item.SourceLineNumber);

      var rightEditor = GetRightEditor();
      rightEditor.GoToLine(item.FormattedLineNumber);
    }

    private void ErrorClear()
    {
      ErrorCount = 0;
      WarningCount = 0;
    }

    public void SetLeftEditor(TextEditorUI textEditorUI)
    {
      if (textEditorUI == null)
        return;

      if (textEditorUI.Parent is Panel oldParent)
      {
        oldParent.Children.Remove(textEditorUI);
      }
      else if (textEditorUI.Parent is ContentControl oldContent)
      {
        oldContent.Content = null;
      }
      else if (textEditorUI.Parent is Decorator decorator)
      {
        decorator.Child = null;
      }

      LeftBox.Children.Clear(); // Можно просто очистить всё, если нужно заменить
      LeftBox.Children.Add(textEditorUI);
    }

    public void SetRightEditor(TextEditorUI textEditorUI)
    {
      if (RightBox == null || textEditorUI == null)
      {
        return;
      }

      RightBox.Children.Clear();
      RightBox.Children.Add(textEditorUI);
    }

    public TextEditorUI GetRightEditor()
    {
      if (RightBox == null)
      {
        return null;
      }

      return RightBox.Children[0] as TextEditorUI;
    }

    public TextEditorUI GetLeftEditor()
    {
      if (LeftBox == null)
      {
        return null;
      }

      return LeftBox.Children[0] as TextEditorUI;
    }

    public string GetLeftEditorName()
    {
      return FirstFileName.Text;
    }

    public string GetRightEditorName()
    {
      return SecondFileName.Text;
    }

    public void SetRightEditorName(string newText)
    {
      SecondFileName.Text = newText;
    }

    public void SetLeftEditorName(string newText)
    {
      FirstFileName.Text = newText;
    }

    private void BreakpointSet(BreakpointEvents.BreakpointSet obj)
    {
      var model = GetCommandByNumber(obj.CommandNumber);
      if (model == null)
        return;

      model.HasBreakpoint = true;
    }

    private void BreakpointRemoved(BreakpointEvents.BreakpointRemoved obj)
    {
      var model = GetCommandByNumber(obj.CommandNumber);
      if (model == null)
        return;

      model.HasBreakpoint = false;
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
