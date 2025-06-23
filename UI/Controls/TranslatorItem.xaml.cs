using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Serialization;
using ICSharpCode.AvalonEdit;
using UI.Controls.ErrorList;
using UI.Controls.TextEditor;
using Utilities.Models;
using System;

namespace UI.Controls
{
  /// <summary>
  /// Логика взаимодействия для TranslatorContainer.xaml
  /// </summary>
  public partial class TranslatorItem : UserControl
  {
    public string FirstFilePath { get; set; }
    public string SecondFilePath { get; set; }

    public int errorCount = 0;

    public TranslatorItem()
    {
      InitializeComponent();
      ErrorListBoxVertical.ErrorItemDoubleClicked += ErrorListBoxVertical_ErrorItemDoubleClicked;
    }

    private void ErrorListBoxVertical_ErrorItemDoubleClicked(ErrorItem error)
    {
      var lineNumber = error.LineNumber;
      var textEditor = GetLeftEditor();

      if (lineNumber <= 0 || lineNumber > textEditor.Document.LineCount)
        return;

      var line = textEditor.Document.GetLineByNumber(lineNumber);
      textEditor.ScrollToLine(lineNumber);
      textEditor.Select(line.Offset, line.Length); // можно убрать, если не нужно выделение
      textEditor.Focus();
    }

    public void AddError(ErrorItem errorItem)
    {
      ErrorListBoxVertical.Errors.Add(errorItem);
    }

    public void ErrorClear()
    {
      ErrorListBoxVertical.Errors.Clear();
      errorCount = 0;
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

    public void SetError(List<ErrorItem> errorItems)
    {
      foreach (ErrorItem errorItem in errorItems)
      {
        ErrorListBoxVertical.Errors.Add(errorItem);
        errorCount++;
      }

      if (errorCount > 0)
      {
        AppConfiguration.Base.EventAggregator.RaiseInfoMessage($"Общее кол-во ошибок: {errorCount}");
      }

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
  }
}
