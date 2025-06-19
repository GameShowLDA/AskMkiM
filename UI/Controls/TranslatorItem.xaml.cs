using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Serialization;
using UI.Controls.ErrorList;
using UI.Controls.TextEditor;
using Utilities.Models;

namespace UI.Controls
{
  /// <summary>
  /// Логика взаимодействия для TranslatorContainer.xaml
  /// </summary>
  public partial class TranslatorItem : UserControl
  {
    public string FirstFilePath { get; set; }
    public string SecondFilePath { get; set; }

    public TranslatorItem()
    {
      InitializeComponent();
    }

    public void AddError(ErrorItem errorItem)
    {
      ErrorListBoxVertical.Errors.Add(errorItem);
    }

    public void ErrorClear()
    {
      ErrorListBoxVertical.Errors.Clear();
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


    public void SetRighttEditor(TextEditorUI textEditorUI)
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
      ErrorListBoxVertical.Errors.Clear();
      foreach (ErrorItem errorItem in errorItems)
      {
        ErrorListBoxVertical.Errors.Add(errorItem);
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
  }
}
