using Ask.Core.Shared.Metadata.View.EditorHost.TextEditor;
using Ask.UI.Shared.Contracts.Ask.UI.Shared.Contracts;
using System.Windows;
using System.Windows.Controls;
using UI.Controls.TextEditorControl;

namespace UI.Controls
{
  /// <summary>
  /// Interaction logic for TranslatorEditor.xaml.
  /// </summary>
  public partial class TranslatorEditor : UserControl
  {
    private TextEditorUI? _activeEditor;

    public TranslatorEditor()
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
    /// Raised when saving the translated file to disk is requested.
    /// </summary>
    public event EventHandler? SaveToDiskRequested;

    /// <summary>
    /// Gets the underlying text editor control.
    /// </summary>
    public TextEditorUI Editor => _activeEditor ?? TranslatorTextEditor;

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

    public void SetArchiveButtonVisibility(bool isVisible)
    {
      if (SaveButton == null)
      {
        return;
      }

      SaveButton.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BackButton_Click(object sender, RoutedEventArgs e) =>
      BackRequested?.Invoke(this, EventArgs.Empty);

    private void SaveButton_Click(object sender, RoutedEventArgs e) =>
      SaveRequested?.Invoke(this, EventArgs.Empty);

    private void SaveToDiskButton_Click(object sender, RoutedEventArgs e) =>
      SaveToDiskRequested?.Invoke(this, EventArgs.Empty);

    public TextEditorUI GetTextEditor() => Editor;

    public void SetSaveToDiskVisible(bool isVisible)
    {
      SaveToDiskButton.Visibility = isVisible
        ? Visibility.Visible
        : Visibility.Collapsed;
    }

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
