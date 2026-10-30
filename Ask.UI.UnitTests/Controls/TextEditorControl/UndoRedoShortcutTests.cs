using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;

namespace Ask.UI.UnitTests.Controls.TextEditorControl;

public sealed class UndoRedoShortcutTests
{
  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public void AltShortcutsShareHistoryWithStandardCommands(bool modern) => RunInSta(() =>
  {
    var editor = CreateEditor(modern);
    var undo = GetShortcut(editor, ModifierKeys.Alt, ApplicationCommands.Undo);
    var redo = GetShortcut(editor, ModifierKeys.Alt | ModifierKeys.Shift, ApplicationCommands.Redo);
    editor.Text = "A";
    editor.Document.UndoStack.ClearAll();
    editor.Document.Insert(1, "B");
    editor.Document.Insert(2, "C");

    Execute(undo, editor);
    Assert.Equal("AB", editor.Text);
    Execute(undo, editor);
    Assert.Equal("A", editor.Text);
    Assert.False(ApplicationCommands.Undo.CanExecute(null, editor.TextArea));
    Execute(redo, editor);
    Assert.Equal("AB", editor.Text);
    Execute(redo, editor);
    Assert.Equal("ABC", editor.Text);
    Assert.False(ApplicationCommands.Redo.CanExecute(null, editor.TextArea));

    ApplicationCommands.Undo.Execute(null, editor.TextArea);
    Assert.Equal("AB", editor.Text);
    Execute(redo, editor);
    Assert.Equal("ABC", editor.Text);
    Execute(undo, editor);
    ApplicationCommands.Redo.Execute(null, editor.TextArea);
    Assert.Equal("ABC", editor.Text);
  });

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public void NewEditAfterUndoDiscardsRedoHistory(bool modern) => RunInSta(() =>
  {
    var editor = CreateEditor(modern);
    var undo = GetShortcut(editor, ModifierKeys.Alt, ApplicationCommands.Undo);
    GetShortcut(editor, ModifierKeys.Alt | ModifierKeys.Shift, ApplicationCommands.Redo);
    editor.Text = "A";
    editor.Document.UndoStack.ClearAll();
    editor.Document.Insert(1, "B");
    Execute(undo, editor);
    editor.Document.Insert(1, "C");
    Assert.Equal("AC", editor.Text);
    Assert.False(ApplicationCommands.Redo.CanExecute(null, editor.TextArea));
    Execute(undo, editor);
    Assert.Equal("A", editor.Text);
  });

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public void ShortcutsPreserveGroupedEdits(bool modern) => RunInSta(() =>
  {
    var editor = CreateEditor(modern);
    var undo = GetShortcut(editor, ModifierKeys.Alt, ApplicationCommands.Undo);
    var redo = GetShortcut(editor, ModifierKeys.Alt | ModifierKeys.Shift, ApplicationCommands.Redo);
    editor.Document.UndoStack.ClearAll();
    using (editor.Document.RunUpdate())
    {
      editor.Document.Insert(0, "A");
      editor.Document.Insert(1, "B");
    }
    Execute(undo, editor);
    Assert.Equal(string.Empty, editor.Text);
    Assert.False(ApplicationCommands.Undo.CanExecute(null, editor.TextArea));
    Execute(redo, editor);
    Assert.Equal("AB", editor.Text);
  });

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public void ReadOnlyEditorPreservesStandardUndoRedoBehavior(bool modern) => RunInSta(() =>
  {
    var editor = CreateEditor(modern);
    var baseline = new TextEditor();
    var undo = GetShortcut(editor, ModifierKeys.Alt, ApplicationCommands.Undo);
    var redo = GetShortcut(editor, ModifierKeys.Alt | ModifierKeys.Shift, ApplicationCommands.Redo);
    foreach (var target in new[] { editor, baseline })
    {
      target.Document.Insert(0, "A");
      target.Document.Insert(1, "B");
      ApplicationCommands.Undo.Execute(null, target.TextArea);
      Assert.True(target.Document.UndoStack.CanUndo);
      Assert.True(target.Document.UndoStack.CanRedo);
      target.IsReadOnly = true;
    }
    foreach (var binding in new[] { undo, redo })
    {
      var command = Assert.IsAssignableFrom<RoutedCommand>(binding.Command);
      Assert.Equal(command.CanExecute(null, baseline.TextArea), command.CanExecute(null, editor.TextArea));
      command.Execute(null, baseline.TextArea);
      command.Execute(null, editor.TextArea);
      Assert.Equal(baseline.Text, editor.Text);
      Assert.Equal(baseline.Document.UndoStack.CanUndo, editor.Document.UndoStack.CanUndo);
      Assert.Equal(baseline.Document.UndoStack.CanRedo, editor.Document.UndoStack.CanRedo);
    }
    Assert.Equal("A", editor.Text);
  });

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public void EmptyHistoryDisablesBothCommands(bool modern) => RunInSta(() =>
  {
    var editor = CreateEditor(modern);
    GetShortcut(editor, ModifierKeys.Alt, ApplicationCommands.Undo);
    GetShortcut(editor, ModifierKeys.Alt | ModifierKeys.Shift, ApplicationCommands.Redo);
    editor.Document.UndoStack.ClearAll();
    Assert.False(ApplicationCommands.Undo.CanExecute(null, editor.TextArea));
    Assert.False(ApplicationCommands.Redo.CanExecute(null, editor.TextArea));
  });

  private static TextEditor CreateEditor(bool modern)
  {
    UserControl control = modern
      ? new Ask.UI.Controls.TextEditorControl.TextEditorUI()
      : new global::UI.Controls.TextEditorControl.TextEditorUI();
    return Assert.IsType<TextEditor>(control.FindName("textEditor"));
  }

  private static KeyBinding GetShortcut(TextEditor editor, ModifierKeys modifiers, RoutedCommand command)
  {
    var binding = Assert.Single(editor.TextArea.InputBindings.OfType<KeyBinding>(),
      binding => binding.Key == Key.Back && binding.Modifiers == modifiers);
    Assert.Same(command, binding.Command);
    return binding;
  }

  private static void Execute(KeyBinding binding, TextEditor editor)
  {
    var command = Assert.IsAssignableFrom<RoutedCommand>(binding.Command);
    Assert.True(command.CanExecute(null, editor.TextArea));
    command.Execute(null, editor.TextArea);
  }

  private static void RunInSta(Action action)
  {
    Exception? exception = null;
    var thread = new Thread(() => { try { action(); } catch (Exception error) { exception = error; } }) { IsBackground = true };
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    Assert.True(thread.Join(TimeSpan.FromSeconds(15)));
    if (exception != null) throw new InvalidOperationException("Проверка Undo/Redo завершилась ошибкой.", exception);
  }
}
