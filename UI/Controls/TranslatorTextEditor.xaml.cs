using Ask.Core.Shared.Metadata.View.EditorHost.TextEditor;
using Ask.UI.Shared.Contracts.Ask.UI.Shared.Contracts;
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
using UI.Controls.TextEditor;

namespace UI.Controls
{
  /// <summary>
  /// Логика взаимодействия для TranslatorTextEditor.xaml
  /// </summary>
  public partial class TranslatorTextEditor : UserControl
  {
    private TextEditorUI? _activeEditor;

    public TranslatorTextEditor()
    {
      InitializeComponent();
    }

    /// <summary>
    /// Raised when the back action is requested.
    /// </summary>
    public event EventHandler? BackRequested;

    /// <summary>
    /// Raised when the save action is requested.
    /// </summary>
    public event EventHandler? SaveRequested;

    /// <summary>
    /// Raised when open-folder action is requested.
    /// </summary>
    public event EventHandler? OpenFolderRequested;

    /// <summary>
    /// Raised when print action is requested.
    /// </summary>
    public event EventHandler? PrintRequested;

    /// <summary>
    /// Gets the underlying text editor control.
    /// </summary>
    public TextEditorUI Editor => _activeEditor ?? TextEditor;

    /// <summary>
    /// Gets or sets editor text.
    /// </summary>
    public string Text
    {
      get => Editor.Text;
      set => Editor.Text = value;
    }

    /// <summary>
    /// Gets or sets read-only mode for the editor.
    /// </summary>
    public bool IsReadOnly
    {
      get => Editor.IsReadOnly;
      set => Editor.IsReadOnly = value;
    }
    public TextEditorUI GetTextEditor() => Editor;

    public void SetEditor(ITextEditorView editor)
    {
      if (editor is not IUiViewAdapter adapter || adapter.NativeView is not UIElement element)
      {
        return;
      }

      DetachFromParent(element);
      EditorHost.Content = element;
      _activeEditor = element as TextEditorUI;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e) =>
      SaveRequested?.Invoke(this, EventArgs.Empty);

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e) =>
      OpenFolderRequested?.Invoke(this, EventArgs.Empty);

    private void PrintButton_Click(object sender, RoutedEventArgs e) =>
      PrintRequested?.Invoke(this, EventArgs.Empty);

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

  }
}
