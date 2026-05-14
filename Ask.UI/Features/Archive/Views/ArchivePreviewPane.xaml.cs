using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.UI.Controls.TextEditorControl;
using Ask.UI.Shared.TextEditor;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Ask.UI.Features.Archive.Views
{
  public partial class ArchivePreviewPane : UserControl
  {
    public static readonly DependencyProperty TextProperty =
      DependencyProperty.Register(nameof(Text), typeof(string), typeof(ArchivePreviewPane), new PropertyMetadata(string.Empty, OnPreviewChanged));

    public static readonly DependencyProperty FileTypeProperty =
      DependencyProperty.Register(nameof(FileType), typeof(FileType), typeof(ArchivePreviewPane), new PropertyMetadata(FileType.None, OnPreviewChanged));

    public ArchivePreviewPane()
    {
      InitializeComponent();
    }

    public string Text
    {
      get => (string)GetValue(TextProperty);
      set => SetValue(TextProperty, value);
    }

    public FileType FileType
    {
      get => (FileType)GetValue(FileTypeProperty);
      set => SetValue(FileTypeProperty, value);
    }

    private static void OnPreviewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      ((ArchivePreviewPane)d).RenderPreview();
    }

    private void RenderPreview()
    {
      if (string.IsNullOrWhiteSpace(Text))
      {
        EditorHost.Content = null;
        return;
      }

      var editor = new TextEditorUI(FileType)
      {
        Text = Text,
        IsReadOnly = true,
      };

      if (FileTypeResolver.SupportsBraceCommentColorizer(FileType) &&
          !editor.TextArea.TextView.LineTransformers.OfType<BracesCommentColorizer>().Any())
      {
        editor.TextArea.TextView.LineTransformers.Add(new BracesCommentColorizer());
      }

      EditorHost.Content = editor;
      editor.TextArea.TextView.Redraw();
    }
  }
}
