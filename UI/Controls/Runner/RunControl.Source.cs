using Ask.UI.Features.ProtocolNew.Execution;
using Ask.Core.Shared.DTO.Executor;
using Message;
using System.Windows;
using System.Windows.Controls;
using UI.Controls.TextEditorControl;
using UI.Windows.WpfDocking.Windows.Docking;
using UI.Windows.WpfDocking.Windows.Docking.Primitives;

namespace UI.Controls.Runner
{
  public partial class RunControl
  {
    public DockItem? SourceDocument { get; private set; }
    public TextEditorUI? SourceEditor => SourceDocument?.Content as TextEditorUI;
    public Func<DockItem, bool>? SaveSourceFile { get; set; }
    public Func<TextEditorUI, Task<bool>>? TranslateSourceAsync { get; set; }
    public bool IsTransferringSource { get; set; }
    public string? TranslatedSourceText { get; set; }
    public bool IsSourceTranslationOutdated => SourceEditor != null
      && TranslatedSourceText != null && !string.Equals(SourceEditor.Text, TranslatedSourceText, StringComparison.Ordinal);

    public void ApplySourceTranslation(TextEditorUI editor, List<BaseCommandModel> models, string sourceText)
    {
      if (_closingDockLayout)
        return;

      var item = RunnerDockManager.DockItems.First(document => document.Content is UI.Controls.TranslatorEditor);
      ((UI.Controls.TranslatorEditor)item.Content).SetEditor(editor);
      TranslationModels = models;
      ControlProgram = models;
      SourceEditor!.Text = sourceText;
      TranslatedSourceText = sourceText;
      SyncBreakpointsFromEditor(editor);
      SelectRunnerDocument(item);
    }

    public async Task<bool> PrepareSourceForRunAsync()
    {
      if (_closingDockLayout)
        return false;

      if (!IsSourceTranslationOutdated)
        return !IsTransferringSource;

      var result = MessageBoxCustom.Show(
        "Исходный файл изменён после последней трансляции. Транслировать его сейчас?",
        "Программа контроля изменена", MessageBoxButton.YesNo, MessageBoxImage.Warning);
      return result == MessageBoxResult.No || (result == MessageBoxResult.Yes && await TranslateCurrentSourceAsync());
    }

    public TextEditorUI? GetTranslatedEditor() => GetTranslatorEditor()?.GetTextEditor();

    public void SetSourceEditor(TextEditorUI editor)
    {
      if (editor.Parent is Panel panel)
        panel.Children.Remove(editor);
      else if (editor.Parent is ContentControl content)
        content.Content = null;
      else if (editor.Parent is Decorator decorator)
        decorator.Child = null;

      SourceDocument = new DockItem
      {
        Title = editor.TextEditorModel.FileName,
        TabText = editor.TextEditorModel.FileName,
        Content = editor,
        Style = (Style)FindResource("RunnerSourceStyle"),
        HideOnPerformClose = false,
        Tag = !ExecutionRunGuard.GetActivity().IsBusy,
      };
      DocumentTab.SetHideCloseButton(SourceDocument, true);
      SourceDocument.Closing += RunnerPane_Closing;
      RunnerDockManager.DockItems.Add(SourceDocument);
      editor.ConfigureBreakpoints(interactive: false, visible: false);
      if (RunnerDockManager.IsLoaded)
        ShowRunnerDocument(SourceDocument, DockItemShowMethod.Deselect);
    }

    private void SaveSourceButton_Click(object sender, RoutedEventArgs e)
    {
      if (SourceDocument != null)
        SaveSourceFile?.Invoke(SourceDocument);
    }

    private async void TranslateSourceButton_Click(object sender, RoutedEventArgs e)
      => await TranslateCurrentSourceAsync();

    public async Task<bool> TranslateCurrentSourceAsync()
    {
      if (_closingDockLayout || SourceEditor == null || TranslateSourceAsync == null || IsTransferringSource
        || ExecutionRunGuard.GetActivity().IsBusy)
        return false;

      IsTransferringSource = true;
      IsEnabled = false;
      try
      {
        var success = await TranslateSourceAsync(SourceEditor);
        return success && !_closingDockLayout;
      }
      finally
      {
        IsEnabled = true;
        IsTransferringSource = false;
      }
    }
  }
}
