using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    public TranslatorItem()
    {
      InitializeComponent();
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
  }
}
