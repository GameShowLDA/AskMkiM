using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Input;
using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Core.Shared.DTO.TextEditor;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Metadata.Static;
using Ask.UI.Features.ProtocolNew.Execution;
using UI.Components;
using UI.Controls.Runner;
using UI.Controls.TextEditorControl;
using UI.Windows.WpfDocking.Windows;
using UI.Windows.WpfDocking.Windows.Docking;

internal static partial class Program
{
  private static async Task VerifyRunnerSourceAsync(Application application, string directory)
  {
    foreach (var resource in new[] { "/UI;component/Style.xaml", "/UI;component/Windows/WpfDocking/Themes/Generic.xaml" })
      application.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(resource, UriKind.Relative) });

    var settings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AskMkiM", "error-list-layout.json");
    var previousSettings = File.Exists(settings) ? File.ReadAllBytes(settings) : null;
    var workspace = new MultiEditorControl();
    var files = new UI.Components.MultiEditorMethods.FileManager(workspace);
    var owner = new Window { Content = workspace, Width = 1200, Height = 800, Opacity = 0, ShowActivated = false, ShowInTaskbar = false };
    try
    {
      var path = Path.Combine(directory, "source.pkw");
      const string saved = "10 ОК ИСХОДНИК\n20 КЦ";
      File.WriteAllText(path, saved, Encoding.UTF8);
      var source = new TextEditorUI(FileType.PKW, new TextEditorModel(path, "source.pkw", Encoding.UTF8)
      { SavedTextSnapshot = saved, SourceArchiveName = "archive" }) { Text = saved };
      var translated = new TextEditorUI(FileType.OPKW, new TextEditorModel(path, "source.opkw")) { Text = "Translated", IsReadOnly = true };
      var runner = new RunControl { FileName = "source.asktrace", OpkFilePath = path, TranslatedSourceText = saved };
      runner.SetLeftEditor(translated);
      runner.SetSourceEditor(source);
      var selectDocument = typeof(RunControl).GetMethod("SelectRunnerDocument", BindingFlags.Instance | BindingFlags.NonPublic)!;
      var pendingDock = (DockControl)runner.FindName("RunnerDockManager");
      var pendingProgram = pendingDock.DockItems.Single(item => item.Content is UI.Controls.TranslatorEditor);
      Require(pendingProgram.DockPosition == DockPosition.Unknown, "Regression must begin before document layout");
      selectDocument.Invoke(runner, new object[] { pendingProgram });
      Require(pendingProgram.DockPosition == DockPosition.Unknown, "Selection before Loaded must wait for document layout");
      runner.ShowInspectionProtocol("Early result");
      runner.ClearInspectionProtocol();
      runner.ShowInspectionProtocol("Latest result");
      var protocolPane = (DockItem)runner.FindName("ProtocolPane");
      var selectionDuringLayout = false;
      protocolPane.StateChanging += (_, _) =>
      {
        if (!selectionDuringLayout)
        {
          selectionDuringLayout = true;
          selectDocument.Invoke(runner, new object[] { pendingProgram });
        }
      };
      owner.Show();
      await files.RunControlService.AddRunItem(runner, EditorType.Run);
      await application.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
      var initialDock = (DockControl)runner.FindName("RunnerDockManager");
      Require(selectionDuringLayout, "Selection must also be exercised during initial pane creation");
      var earlyResult = initialDock.DockItems.Single(item => item.TabText == "Итоговый протокол");
      Require(((Ask.UI.Controls.TextEditorControl.TextEditorUI)earlyResult.Content).Text == "Latest result",
        "Only the latest pending protocol must appear after loading");
      Require(initialDock.DockItems.Single(item => item.Content is UI.Controls.TranslatorEditor).IsSelected
        && !runner.SourceDocument!.IsSelected, "The translated program must be selected on startup");
      runner.SourceDocument!.Show(DockItemShowMethod.Select);
      selectDocument.Invoke(runner, new object[] { pendingProgram });
      Require(pendingProgram.IsSelected && !runner.SourceDocument.IsSelected,
        "A subsequent run must select the existing translated document");
      runner.ShowInspectionProtocol("Inspection protocol");
      var dock = (DockControl)runner.FindName("RunnerDockManager");
      var final = dock.DockItems.Single(item => item.TabText == "Итоговый протокол");
      var sourceItem = runner.SourceDocument!;
      foreach (var side in new[] { Dock.Top, Dock.Right, Dock.Bottom, Dock.Left })
      {
        sourceItem.Show(final.FirstPane, side, new SplitterDistance(1, SplitterUnitType.Star));
        await application.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        Require(sourceItem.FirstPane != final.FirstPane && dock.DockItems.Count == 6,
          $"Docking to {side} must preserve all executor documents");
        sourceItem.Show(final.FirstPane, final.FirstPane.Items.Count);
        Require(sourceItem.FirstPane == final.FirstPane, $"Documents must regroup after docking to {side}");
      }
      sourceItem.Show(final.FirstPane, Dock.Left, new SplitterDistance(1, SplitterUnitType.Star));
      await application.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
      Require(sourceItem.FirstPane != final.FirstPane && !source.IsReadOnly && translated.IsReadOnly,
        "Source must be editable beside the read-only protocol and translation");
      Require(dock.DockItems.Count == 6, "All six tabs must share one docking manager");

      foreach (var theme in new[] { "light", "dark" })
      {
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri($"/UI;component/Resources/Theme/{theme}.xaml", UriKind.Relative) });
        await application.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        sourceItem.ApplyTemplate();
        Require(sourceItem.Template.FindName("SaveSourceButton", sourceItem) is Button,
          "Source toolbar must survive theme changes");
      }
      source.Text += "\n30 ИЗМЕНЕНИЕ";
      Require(sourceItem.TabText.EndsWith(" *"), "Unsaved changes must mark the source tab");
      var save = (Button)sourceItem.Template.FindName("SaveSourceButton", sourceItem);
      save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
      Require(File.ReadAllText(path) == source.Text && source.TextEditorModel.SavedTextSnapshot == source.Text,
        "Save must write the source and update its snapshot");
      Require(!sourceItem.TabText.EndsWith(" *"), "Saving must clear the modified indicator");
      Require(runner.IsSourceTranslationOutdated, "Saving must not mark an outdated translation as current");
      source.Text += "\n40 НЕ СОХРАНЕНО";
      var inlineCalls = 0;
      runner.TranslateSourceAsync = editor =>
      {
        inlineCalls++;
        var updated = new TextEditorUI(FileType.OPKW, new TextEditorModel(path, "source.opkw"))
          { Text = "Updated translation", IsReadOnly = true };
        runner.ApplySourceTranslation(updated, new List<BaseCommandModel>(), editor.Text);
        return Task.FromResult(true);
      };
      AnswerSourceQuestion(application, "NoButton");
      Require(await runner.PrepareSourceForRunAsync() && inlineCalls == 0 && runner.IsSourceTranslationOutdated,
        "No must allow execution with the old translation");
      AnswerSourceQuestion(application, "YesButton");
      Require(await runner.PrepareSourceForRunAsync() && inlineCalls == 1 && !runner.IsSourceTranslationOutdated,
        "Yes must update the translation and allow execution");
      Require(dock.DockItems.Contains(final) && sourceItem.FirstPane != final.FirstPane
        && runner.GetTranslatedEditor()!.Text == "Updated translation",
        "Inline translation must preserve panels and the final protocol");
      source.Text += "\n50 ОШИБКА";
      runner.TranslateSourceAsync = _ => Task.FromResult(false);
      AnswerSourceQuestion(application, "YesButton");
      Require(!await runner.PrepareSourceForRunAsync() && runner.IsSourceTranslationOutdated,
        "A failed translation must block execution and retain the previous snapshot");
      var pending = source.Text;
      var calls = 0;
      var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      runner.TranslateSourceAsync = async editor =>
      {
        calls++;
        Require(editor.Text == pending && runner.IsTransferringSource, "Translate must receive the live unsaved buffer");
        var copy = new TextEditorUI(FileType.PKW, new TextEditorModel(path, "source.pkw", Encoding.UTF8)
        { SavedTextSnapshot = editor.TextEditorModel.SavedTextSnapshot }) { Text = editor.Text };
        var result = files.TranslationService.CreateTranslationFile(path);
        var translator = await files.TranslationService.AddTranslatorItem(copy, result, EditorType.Translator);
        Require(translator != null && translator.GetLeftBox().GetTextEditor().Text == pending,
          "Translator must retain the unsaved text");
        completion.SetResult();
        return false;
      };
      var translate = (Button)sourceItem.Template.FindName("TranslateSourceButton", sourceItem);
      var guard = new ExecutionRunGuard();
      var guardOwner = new object();
      Require(guard.TryAcquire("Source regression", guardOwner, out _), "Execution guard must be free");
      try
      {
        translate.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(calls == 0, "Translate must not close an active executor");
      }
      finally { guard.Release(guardOwner); }
      translate.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
      await completion.Task.WaitAsync(TimeSpan.FromSeconds(15));
      await application.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
      Require(calls == 1 && dock.DockItems.Count == 0, "Successful handoff must close all executor panes");
      runner.ApplySourceTranslation(translated, new List<BaseCommandModel>(), saved);
      Require(!await runner.TranslateCurrentSourceAsync() && !await runner.PrepareSourceForRunAsync()
        && dock.DockItems.Count == 0, "A closed executor must ignore late translation results and reject starts");
      Require(SystemStateManager.GetIsControlProgramActive(), "Executor cleanup must not deactivate the new translator");
      Require(!File.ReadAllText(path).Contains("НЕ СОХРАНЕНО"), "Translation must not implicitly save the source");
      Log("Source editor regression passed: docking, themes, save, dirty state, execution guard and unsaved handoff");
    }
    finally
    {
      owner.Close();
      if (previousSettings != null) File.WriteAllBytes(settings, previousSettings);
      else if (File.Exists(settings)) File.Delete(settings);
    }
  }

  private static void AnswerSourceQuestion(Application application, string buttonName)
  {
    application.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
    {
      var dialog = application.Windows.OfType<Message.MessageBoxCustom>().Single();
      dialog.Opacity = 0;
      var button = (Button)dialog.FindName(buttonName);
      button.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
        { RoutedEvent = Mouse.PreviewMouseDownEvent });
    }));
  }
}
