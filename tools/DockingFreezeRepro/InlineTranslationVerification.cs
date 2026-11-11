using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Ask.Core.Shared.DTO.TextEditor;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Metadata.Static;
using UI.Components;
using UI.Controls;
using UI.Controls.Runner;
using UI.Controls.TextEditorControl;
using UI.Windows.WpfDocking.Windows.Docking;
using Ask.DataBase.Provider.Context;
using Microsoft.EntityFrameworkCore;

internal static partial class Program
{
  private static async Task VerifyInlineTranslationAsync(Application application, string directory, string assemblyPath)
  {
    // Проверяем фактический orchestration из отдельно собранного приложения без запуска его startup и оборудования.
    var assembly = Assembly.LoadFrom(Path.GetFullPath(assemblyPath));
    var databasePath = Path.Combine(AppContext.BaseDirectory, "Resources", "app.db");
    Require(!File.Exists(databasePath), "Run inline-translation from a fresh isolated output directory without Resources/app.db");
    Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
    await using (var database = new AppDbContext())
      await database.Database.EnsureCreatedAsync();
    foreach (var resource in new[] { "/UI;component/Style.xaml", "/UI;component/Windows/WpfDocking/Themes/Generic.xaml" })
      application.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(resource, UriKind.Relative) });
    var settings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AskMkiM", "error-list-layout.json");
    var oldSettings = File.Exists(settings) ? File.ReadAllBytes(settings) : null;
    var multi = new MultiWindowControl();
    var owner = new Window { Content = multi, Width = 1200, Height = 800, Opacity = 0, ShowActivated = false, ShowInTaskbar = false };
    application.MainWindow = owner;
    try
    {
      owner.Show();
      var multiService = Activator.CreateInstance(assembly.GetType("MainWindowProgram.Services.MultiWindowService")!,
        multi, multi.RunService, multi.EditorDocumentService, multi.ProtocolViewerService, multi.WorkspaceService, multi.TranslationService)!;
      var files = Activator.CreateInstance(assembly.GetType("MainWindowProgram.Services.FileService")!,
        null, multiService, (Func<bool>)(() => false))!;
      var serviceType = assembly.GetType("MainWindowProgram.Services.TranslationServices")!;
      var translation = Activator.CreateInstance(serviceType, multiService, files, null)!;
      var translateMethod = serviceType.GetMethod("TranslateRunnerSourceAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
      var path = Path.Combine(directory, "source.pkw");
      const string text = "10 ОК ПРОВЕРКА\n20 РМ Х1/1=1.2.1\n30 КЦ";
      File.WriteAllText(path, text, Encoding.UTF8);
      var source = new TextEditorUI(FileType.PKW, new TextEditorModel(path, "source.pkw") { SavedTextSnapshot = text }) { Text = text };
      var result = new TextEditorUI(FileType.OPKW, new TextEditorModel(path, "source.opkw")) { Text = "Old translation", IsReadOnly = true };
      var runner = new RunControl { FileName = "source.asktrace", OpkFilePath = path, TranslatedSourceText = "old source" };
      runner.SetLeftEditor(result);
      runner.SetSourceEditor(source);
      runner.TranslateSourceAsync = editor => (Task<bool>)translateMethod.Invoke(translation, new object[] { runner, editor })!;
      await multi.RunService.AddRunItem(runner, EditorType.Run);
      await application.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
      runner.ShowInspectionProtocol("Existing final protocol");
      var dock = (DockControl)runner.FindName("RunnerDockManager");
      var oldPanels = dock.DockItems.ToArray();
      var runMethod = serviceType.GetMethod("RunAsync")!;
      var executionGuard = new Ask.UI.Features.ProtocolNew.Execution.ExecutionRunGuard();
      var executionOwner = new object();
      Require(executionGuard.TryAcquire("Runner audit", executionOwner, out _), "Execution guard must be available");
      try
      {
        await (Task)runMethod.Invoke(translation, new object[] { false, true })!;
        Require(oldPanels.All(item => dock.DockItems.Contains(item)),
          "A repeat request during execution must preserve the current executor");
      }
      finally
      {
        executionGuard.Release(executionOwner);
      }
      runner.IsTransferringSource = true;
      try
      {
        await (Task)runMethod.Invoke(translation, new object[] { false, true })!;
        Require(!await runner.PrepareSourceForRunAsync(),
          "The executor start button must reject a request during translation before prompting");
      }
      finally
      {
        runner.IsTransferringSource = false;
      }
      Require(oldPanels.All(item => dock.DockItems.Contains(item)),
        "A run request during translation must preserve the current executor");
      var updated = await runner.TranslateCurrentSourceAsync();
      if (!updated)
      {
        var errorContainer = multi.GetActiveTextEditorContainer(EditorType.Translator);
        foreach (var command in errorContainer.GetDockControl().DockItems.Select(item => item.Content)
          .OfType<TranslatorItem>().SelectMany(item => item.TranslationModels))
          foreach (var error in command.Errors)
            Log(error.Description);
      }
      Require(updated, "Valid source must translate inside the executor");
      Require(oldPanels.Length == dock.DockItems.Count && oldPanels.All(item => dock.DockItems.Contains(item))
        && runner.TranslationModels.Any(command => command.Mnemonic == "РМ")
        && runner.TranslationModels.Any(command => command.Mnemonic == "КЦ"),
        "Successful translation must update command models without replacing panels");
      Require(!runner.IsSourceTranslationOutdated && runner.GetTranslatedEditor()!.Text != "Old translation",
        "The editor and translated source snapshot must be updated together");
      var breakpoint = runner.TranslationModels.Single(command => command.Mnemonic == "ВШ");
      breakpoint.HasBreakpoint = true;
      breakpoint.IsBreakpointEnabled = false;
      var commandNumber = int.Parse(breakpoint.CommandNumber);
      runner.GetTranslatedEditor()!.EnsureBreakpoint(breakpoint.FormattedStartLineNumber + 1, commandNumber, true);
      runner.GetTranslatedEditor()!.DisableBreakpoint(commandNumber);
      source.Text += "\n{Новый комментарий}";
      Require(await runner.TranslateCurrentSourceAsync(), "A second valid translation must update in place");
      var restored = runner.TranslationModels.Single(command => command.CommandNumber == breakpoint.CommandNumber);
      Require(restored.HasBreakpoint && !restored.IsBreakpointEnabled
        && runner.GetTranslatedEditor()!.HasBreakpointCommand(commandNumber)
        && !runner.GetTranslatedEditor()!.IsBreakpointEnabled(commandNumber),
        "Disabled breakpoints must survive recompilation in the models and the new editor");
      source.Text += "\n40 НЕИЗВЕСТНАЯ_КОМАНДА";
      Require(!await runner.TranslateCurrentSourceAsync(), "Translation errors must reject execution");
      await application.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
      Require(dock.DockItems.Count == 0, "Translation errors must close the executor");
      var editorContainer = multi.GetActiveTextEditorContainer(EditorType.Translator);
      var failedTranslation = editorContainer.GetDockControl().DockItems.Select(item => item.Content).OfType<TranslatorItem>().Single();
      Require(failedTranslation.ErrorCount > 0
        && failedTranslation.GetLeftBox().GetTextEditor().Text.Contains("40 НЕИЗВЕСТНАЯ_КОМАНДА")
        && failedTranslation.TranslatedSourceText == failedTranslation.GetLeftBox().GetTextEditor().Text,
        "The translator must show diagnostics for the current unsaved source");
      Require(File.ReadAllText(path) == text, "Translation must not write unsaved source to disk");
      Require(CountEventSubscriptions(failedTranslation) == 4, "Loaded translator must have one subscription per breakpoint event");
      owner.Content = null;
      await application.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
      Require(CountEventSubscriptions(failedTranslation) == 0, "Detached translator must release its subscriptions");
      owner.Content = multi;
      await application.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
      Require(CountEventSubscriptions(failedTranslation) == 4, "Reloading the translator must restore subscriptions without duplicates");
      Log("Inline translation regression passed: production translator, in-place result, error handoff");
    }
    finally
    {
      owner.Close();
      if (oldSettings != null) File.WriteAllBytes(settings, oldSettings);
      else if (File.Exists(settings)) File.Delete(settings);
    }
  }
}
