using Ask.Core.Services.EventCore.Adapters;
using Message;
using System.IO;
using System.Windows;
using UI.Controls;
using UI.Controls.TextEditorControl;

namespace UI.Services
{
  /// <summary>
  /// Provides navigation from translator UI to the original source file.
  /// </summary>
  public static class TranslatorNavigationService
  {
    public static bool TryOpenSourceFileFromTranslator(TextEditorContainer? textEditorContainer, Action? onSourceOpened = null)
    {
      if (textEditorContainer == null)
      {
        ShowSourceFileError();
        return false;
      }

      var translatorDockItem = textEditorContainer.DockManager.DockItems
        .FirstOrDefault(item => item.Content is TranslatorEditor);

      if (translatorDockItem?.Content is not TranslatorEditor translatorEditor)
      {
        ShowSourceFileError();
        return false;
      }

      return TryOpenSourceFileFromTranslator(translatorEditor, onSourceOpened);
    }

    public static bool TryOpenSourceFileFromTranslator(TranslatorEditor? translatorEditor, Action? onSourceOpened = null)
    {
      if (translatorEditor == null)
      {
        ShowSourceFileError();
        return false;
      }

      var textEditor = translatorEditor.GetTextEditor();
      if (textEditor?.TextEditorModel == null)
      {
        ShowEditorNotFoundError();
        return false;
      }

      var filePath = textEditor.TextEditorModel.FilePath;
      if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
      {
        ShowSourceFileError();
        return false;
      }

      FileInteractionEventAdapter.RaiseOpenFileInEditorAgain(filePath);
      onSourceOpened?.Invoke();
      return true;
    }

    private static void ShowSourceFileError()
    {
      MessageBoxCustom.Show(
        "Source file was not found",
        "Open File Error",
        MessageBoxButton.OK,
        MessageBoxImage.Warning);
    }

    private static void ShowEditorNotFoundError()
    {
      MessageBoxCustom.Show(
        "Text editor was not found",
        "Open File Error",
        MessageBoxButton.OK,
        MessageBoxImage.Warning);
    }
  }
}
