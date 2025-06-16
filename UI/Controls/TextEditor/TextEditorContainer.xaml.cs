using AppConfiguration.Interface;
using DevZest.Windows.Docking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UI.Controls.TextEditor
{
  /// <summary>
  /// Логика взаимодействия для TextEditorContainer.xaml
  /// </summary>
  public partial class TextEditorContainer : UserControl, ITextAdapter
  {
    public TextEditorContainer()
    {
      InitializeComponent();
    }

    public string GetText()
    {
      var foundDockItem = this.DockManager.DockItems.FirstOrDefault(item => item.IsActiveItem == true);
      if (foundDockItem != null)
      {
        if(foundDockItem.Content is TextEditorUI)
        {
          var textEditor = (TextEditorUI)foundDockItem.Content;
          return textEditor.Text;
        }
      }
      return string.Empty;
    }

    public TextEditorUI GetTextEditor()
    {
      var foundDockItem = this.DockManager.DockItems.FirstOrDefault(item => item.IsActiveItem == true);
      if (foundDockItem != null)
      {
        if (foundDockItem.Content is TextEditorUI)
        {
          var textEditor = (TextEditorUI)foundDockItem.Content;
          return textEditor;
        }
      }
      return null;
    }
  }
}
