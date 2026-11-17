using Ask.Core.Shared.DTO.TextEditor;
using System.IO;
using System.Text;
using UI.Components.MultiEditorMethods;
using UI.Controls.TextEditorControl;
using UI.Services.FileManager;
using UI.Windows.WpfDocking.Windows.Docking;

namespace Ask.UI.UnitTests.Services.FileManager;

public sealed class DocumentSaveStateTests
{
  [Fact]
  public void OpenedDocumentWithEquivalentBomAndLineEndingsIsNotModified() => RunInSta(() =>
  {
    var (editor, item) = CreateDocument("\uFEFFпервая\r\nвторая", "первая\nвторая");
    var service = new FileCompareService(null!);

    Assert.False(service.HasFileChanged(item));
  });

  [Fact]
  public void EditUndoAndRedoFollowSavedContent() => RunInSta(() =>
  {
    var (editor, item) = CreateDocument("исходный", "исходный");
    var service = new FileCompareService(null!);
    editor.TextEditor.Document.UndoStack.ClearAll();

    editor.TextEditor.Document.Insert(editor.Text.Length, " текст");
    Assert.True(service.HasFileChanged(item));

    editor.TextEditor.Document.UndoStack.Undo();
    Assert.False(service.HasFileChanged(item));

    editor.TextEditor.Document.UndoStack.Redo();
    Assert.True(service.HasFileChanged(item));
  });

  [Fact]
  public void SuccessfulSaveSnapshotClearsModifiedState() => RunInSta(() =>
  {
    var (editor, item) = CreateDocument("исходный", "изменённый");
    var service = new FileCompareService(null!);
    Assert.True(service.HasFileChanged(item));

    DocumentTextState.MarkSaved(editor.TextEditorModel, editor.Text);

    Assert.False(service.HasFileChanged(item));
  });

  [Fact]
  public void EachOpenDocumentIsConfirmedOnceAndCancellationStopsClosing() => RunInSta(() =>
  {
    var (firstEditor, first) = CreateDocument("один", "один+");
    var duplicate = new DockItem { Title = "duplicate.pkw", Content = firstEditor };
    var (_, second) = CreateDocument("два", "два+");
    int calls = 0;

    bool result = SaveFileManager.ConfirmCloseAll([first, duplicate, second], _ => ++calls < 2);

    Assert.False(result);
    Assert.Equal(2, calls);
    Assert.Equal("один+", firstEditor.Text);
  });

  [Fact]
  public void AtomicWritePreservesEncodingPreamble() =>
    WithTemporaryDirectory(directory =>
    {
      var pathWithBom = Path.Combine(directory, "with-bom.pkw");
      var pathWithoutBom = Path.Combine(directory, "without-bom.pkw");

      SaveFileManager.WriteTextFileAtomically(pathWithBom, "текст", Encoding.UTF8, encrypt: false);
      SaveFileManager.WriteTextFileAtomically(
        pathWithoutBom,
        "текст",
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        encrypt: false);

      Assert.True(File.ReadAllBytes(pathWithBom).AsSpan().StartsWith(Encoding.UTF8.Preamble));
      Assert.False(File.ReadAllBytes(pathWithoutBom).AsSpan().StartsWith(Encoding.UTF8.Preamble));
    });

  [Fact]
  public void FailedAtomicWriteKeepsPreviousFile() =>
    WithTemporaryDirectory(directory =>
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
      var path = Path.Combine(directory, "document.pk");
      File.WriteAllText(path, "исходный", Encoding.UTF8);
      var strictCp866 = Encoding.GetEncoding(
        866,
        EncoderFallback.ExceptionFallback,
        DecoderFallback.ExceptionFallback);

      Assert.Throws<EncoderFallbackException>((Action)(() =>
        SaveFileManager.WriteTextFileAtomically(path, "emoji: 😀", strictCp866, encrypt: false)));
      Assert.Equal("исходный", File.ReadAllText(path, Encoding.UTF8));
      Assert.Empty(Directory.EnumerateFiles(directory, ".*.tmp"));
    });

  private static (TextEditorUI Editor, DockItem Item) CreateDocument(string savedText, string currentText)
  {
    var model = new TextEditorModel("document.pkw", "document.pkw", Encoding.UTF8);
    DocumentTextState.MarkSaved(model, savedText);
    var editor = new TextEditorUI(textEditorModel: model) { Text = currentText };
    var item = new DockItem { Title = model.FileName, Content = editor };
    return (editor, item);
  }

  private static void WithTemporaryDirectory(Action<string> action)
  {
    var directory = Path.Combine(Path.GetTempPath(), $"AskMkiM-{Guid.NewGuid():N}");
    Directory.CreateDirectory(directory);
    try
    {
      action(directory);
    }
    finally
    {
      Directory.Delete(directory, recursive: true);
    }
  }

  private static void RunInSta(Action action)
  {
    Exception? exception = null;
    var thread = new Thread(() =>
    {
      try
      {
        action();
      }
      catch (Exception error)
      {
        exception = error;
      }
    }) { IsBackground = true };
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    Assert.True(thread.Join(TimeSpan.FromSeconds(15)));
    if (exception != null)
    {
      throw new InvalidOperationException("Проверка состояния документа завершилась ошибкой.", exception);
    }
  }
}
