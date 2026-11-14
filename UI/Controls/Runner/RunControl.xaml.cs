using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Metadata.Static;
using Ask.Core.Shared.Metadata.View.EditorHost;
using Ask.Engine.ControlCommandExecutor.Execution;
using Ask.UI.Controls.ErrorList;
using Ask.UI.Controls.ProtocolNew;
using Ask.UI.Services.Notifications;
using ResultProtocolEditor = Ask.UI.Controls.TextEditorControl.TextEditorUI;
using Ask.UI.Features.Archive.Services;
using Ask.UI.Shared.Formatting;
using Ask.UI.ThirdParty.Docking.Integration.ExecutionWorkspace;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using UI.Controls.TextEditorControl;
using UI.Services;
using UI.Services.ProtocolManager;
using UI.Windows.WpfDocking.Windows.Docking;
using UI.Windows.WpfDocking.Windows.Docking.Primitives;
using static Ask.LogLib.LoggerUtility;
using InspectionProtocolHost = Ask.UI.Controls.ProtocolNew.IInspectionProtocolHost;

namespace UI.Controls.Runner
{
  /// <summary>
  /// Логика взаимодействия для RunControl.xaml
  /// </summary>
  public partial class RunControl : UserControl, IRunView, InspectionProtocolHost
  {
    /// <summary>
    /// Флаг, указывающий, находится ли интерфейс в заблокированном состоянии.
    /// Используется для предотвращения повторного применения изменений.
    /// </summary>
    private static bool isLocked = false;

    private List<BaseCommandModel> ControlProgram = null;

    public int ErrorCount { get; private set; } = 0;
    public int TranslationErrorCount { get; private set; } = 0;

    private ProtocolUI ProtocolUI { get; set; }

    public string FileName { get; set; }

    public string OpkFilePath { get; set; }
    public string? SourceArchiveName { get; set; }

    private List<BaseCommandModel> translationModels = new List<BaseCommandModel>();

    private TextEditorContainer _leftEditor;
    private readonly ArchiveSaveService _archiveSaveService = new ArchiveSaveService();
    private readonly TranslatedFileSaveService _translatedFileSaveService = new TranslatedFileSaveService();
    private DockItem? _inspectionProtocolDockItem;
    private ResultProtocolEditor? _inspectionProtocolEditor;
    private readonly List<DockItem> _runnerDocuments = [];
    private DockItem ProtocolPane = null!;
    private DockItem ErrorsPane = null!;
    private ContentControl MainContent = null!;
    private ErrorListControl ErrorListBoxVertical = null!;

    public List<BaseCommandModel> TranslationModels
    {
      get
      {
        return translationModels;
      }
      set
      {
        translationModels = value ?? new List<BaseCommandModel>();
        ErrorClear();
        TranslationErrorCount = 0;

        foreach (var model in translationModels)
        {
          if (model.Errors.Count > 0)
          {
            SetTranslationError(model.Errors);
          }
        }

        UpdateTranslatorEditorActions();
        UpdateArchiveButtonVisibility();
      }
    }

    public UserControl View => this;

    private bool task = false;

    private DevicesStatus devicesStatus;
    public RunControl()
    {
      InitializeComponent();
      InitializeWorkspaceDocuments();
      ProtocolUI = new ProtocolUI(true);
      ProtocolUI.SetRuntimeExecutionSettingsEnabled(true);
      ProtocolUI.InspectionProtocolHost = this;
      ProtocolUI.BeforeStartAsync = PrepareSourceForRunAsync;
      ProtocolUI.ErrorListBoxVerticalVisibility = Visibility.Collapsed;
      MainContent.Content = ProtocolUI;
      ErrorListBoxVertical.ItemDoubleClicked += ErrorItemDoubleClicked;
      devicesStatus = new DevicesStatus();

      EventAggregator.Subscribe<SystemStateEvents.LockedChanged>(OnRunnerLockedChanged);
      EventAggregator.Subscribe<ExecutionEvents.ActiveDeviceChanged>(OnRunnerActiveDeviceChanged);
      EventAggregator.Subscribe<ExecutionEvents.RunnerLayoutResetRequested>(OnRunnerLayoutResetRequested);
      EventAggregator.Subscribe<FileInteractionEvents.ViewProtocol>(OnViewProtocol);

      InitializeDocking();
      Loaded += RunControl_Loaded;

      // Вкладка "Точки остановки"
      ErrorListBoxVertical.BreakpointItemDoubleClicked += BreakpointItemDoubleClicked;
      ErrorListBoxVertical.BreakpointEnabledChanged += BreakpointEnabledChanged;
      ErrorListBoxVertical.BreakpointToggleRequested += BreakpointToggleRequested;

      // События брейкпоинтов, чтобы вкладка обновлялась в Run
      EventAggregator.Subscribe<BreakpointEvents.BreakpointSet>(OnBreakpointSet);
      EventAggregator.Subscribe<BreakpointEvents.BreakpointRemoved>(OnBreakpointRemoved);
      EventAggregator.Subscribe<BreakpointEvents.BreakpointOn>(OnBreakpointOn);
      EventAggregator.Subscribe<BreakpointEvents.BreakpointOff>(OnBreakpointOff);
    }

    private void InitializeWorkspaceDocuments()
    {
      MainContent = new ContentControl();
      ErrorListBoxVertical = new ErrorListControl
      {
        MinHeight = 60,
        StringsNumberVisible = Visibility.Visible,
        MeasureResultVisible = Visibility.Visible,
      };
      ProtocolPane = new DockItem
      {
        Title = "Протокол исполнения",
        TabText = "Протокол исполнения",
        Content = MainContent,
        HideOnPerformClose = false,
      };
      ErrorsPane = new DockItem
      {
        Title = "Ошибки и точки остановки",
        TabText = "Ошибки и точки остановки",
        Content = ErrorListBoxVertical,
        HideOnPerformClose = false,
      };
      DocumentTab.SetHideCloseButton(ProtocolPane, true);
      DocumentTab.SetHideCloseButton(ErrorsPane, true);
      _runnerDocuments.Add(ProtocolPane);
      _runnerDocuments.Add(ErrorsPane);
      RunnerWorkspace.SetContent(ExecutionWorkspacePane.Protocol, ProtocolPane);
      RunnerWorkspace.SetContent(ExecutionWorkspacePane.Errors, ErrorsPane);
      RunnerWorkspace.HidePane(ExecutionWorkspacePane.Source);
      RunnerWorkspace.HidePane(ExecutionWorkspacePane.Result);
    }

    private void OnRunnerLockedChanged(SystemStateEvents.LockedChanged e) => OnLockedChanged(e.IsLocked);

    private void OnRunnerActiveDeviceChanged(ExecutionEvents.ActiveDeviceChanged e) => devicesStatus.LoadDevices(e.Devices);

    private void UnsubscribeRunnerEvents()
    {
      EventAggregator.Unsubscribe<SystemStateEvents.LockedChanged>(OnRunnerLockedChanged);
      EventAggregator.Unsubscribe<ExecutionEvents.ActiveDeviceChanged>(OnRunnerActiveDeviceChanged);
      EventAggregator.Unsubscribe<ExecutionEvents.RunnerLayoutResetRequested>(OnRunnerLayoutResetRequested);
      EventAggregator.Unsubscribe<FileInteractionEvents.ViewProtocol>(OnViewProtocol);
      EventAggregator.Unsubscribe<BreakpointEvents.BreakpointSet>(OnBreakpointSet);
      EventAggregator.Unsubscribe<BreakpointEvents.BreakpointRemoved>(OnBreakpointRemoved);
      EventAggregator.Unsubscribe<BreakpointEvents.BreakpointOn>(OnBreakpointOn);
      EventAggregator.Unsubscribe<BreakpointEvents.BreakpointOff>(OnBreakpointOff);
    }
    /// <summary>
    /// Обрабатывает событие изменения состояния блокировки интерфейса.
    /// Скрывает или отображает верхнюю панель окна в зависимости от нового значения.
    /// </summary>
    /// <param name="newValue">Новое состояние блокировки: <c>true</c> — интерфейс заблокирован; <c>false</c> — разблокирован.</param>
    private void OnLockedChanged(bool newValue)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        isLocked = newValue;
        if (SourceDocument != null)
          SourceDocument.Tag = !newValue;
        var translatorItem = _runnerDocuments.FirstOrDefault(item => item.Content is TranslatorEditor);
        if (translatorItem != null)
        {
          var translatorEditor = translatorItem.Content as TranslatorEditor;
          var canReturnToSource = CanReturnToSourceFile();
          if (newValue)
          {
            translatorEditor.BackButton.Visibility = Visibility.Collapsed;
            isLocked = true;
          }

          else
          {
            translatorEditor.BackButton.Visibility = canReturnToSource ? Visibility.Visible : Visibility.Collapsed;
            isLocked = false;
          }
        }
      });
    }

    private async void ErrorItemDoubleClicked(IDisplayIssue obj)
    {
      var protocolUI = MainContent.Content as ProtocolUI;
      if (protocolUI != null)
      {
        if (obj.SourceLineNumber >= 0)
        {
          RunnerWorkspace.ActivatePane(ExecutionWorkspacePane.Protocol);
          await protocolUI.MoveToLineAsync(obj.SourceLineNumber);
        }

        if (obj.FormattedLineNumber >= 0)
        {
          var dockItemPk = _runnerDocuments.FirstOrDefault(item => item.Content is TranslatorEditor);
          if (dockItemPk?.Content is TranslatorEditor translator && translator.GetTextEditor() is TextEditorUI textEditor)
          {
            SelectRunnerDocument(dockItemPk);
            textEditor.GoToIssue(obj, useFormattedLineNumber: true);
          }
        }
      }
    }

    private void RunControl_Loaded(object sender, RoutedEventArgs e)
    {
      InitializeDockLayout();
      ErrorListBoxVertical.RefreshLayoutFromHost();
      FocusMainContent();
    }

    private DockItem? GetActionDocument(object sender) =>
      ResolveActionDocument((sender as FrameworkElement)?.Tag as DockItem);

    private DockItem? ResolveActionDocument(DockItem? document)
    {
      if (document != SourceDocument)
        return document;

      return _runnerDocuments.FirstOrDefault(item => item.Content is TranslatorEditor);
    }

    private void OpenActiveDocumentButton_Click(object sender, RoutedEventArgs e)
    {
      var activeDocument = GetActionDocument(sender);
      if (activeDocument == _inspectionProtocolDockItem)
      {
        ProtocolUI.OpenInspectionProtocolInEditor();
        return;
      }

      if (activeDocument?.Content is TranslatorEditor translatorEditor)
      {
        var filePath = translatorEditor.GetTextEditor().TextEditorModel?.FilePath;
        if (!string.IsNullOrWhiteSpace(filePath))
        {
          FileInteractionEventAdapter.RaiseOpenFileInEditorAgain(filePath);
        }
      }
    }

    private void OpenActiveDocumentFolderButton_Click(object sender, RoutedEventArgs e)
    {
      var activeDocument = GetActionDocument(sender);
      if (activeDocument == _inspectionProtocolDockItem)
      {
        ProtocolUI.OpenInspectionProtocolFolder();
        return;
      }

      if (activeDocument?.Content is TranslatorEditor translatorEditor)
      {
        var filePath = translatorEditor.GetTextEditor().TextEditorModel?.FilePath;
        if (!string.IsNullOrWhiteSpace(filePath))
        {
          FolderService.OpenFileFolder(filePath);
        }
      }
    }

    private void ExportActiveDocumentPdfButton_Click(object sender, RoutedEventArgs e)
    {
      if (GetActionDocument(sender) == _inspectionProtocolDockItem &&
          !string.IsNullOrWhiteSpace(_inspectionProtocolEditor?.Text))
      {
        new UI.Services.ProtocolManager.PdfProtocolGenerator().ExportProtocolToPdf(
          ProtocolUI.InspectionProtocolFilePath ?? _inspectionProtocolDockItem.TabText ?? "Протокол",
          _inspectionProtocolEditor.Text);
      }
    }

    private async void PrintActiveDocumentButton_Click(object sender, RoutedEventArgs e)
    {
      var activeDocument = GetActionDocument(sender);
      string? text = activeDocument switch
      {
        _ when activeDocument == _inspectionProtocolDockItem => _inspectionProtocolEditor?.Text,
        { Content: TranslatorEditor translatorEditor } => translatorEditor.Text,
        _ => null,
      };

      if (!string.IsNullOrWhiteSpace(text))
      {
        await PrintOperationNotificationService.PrintTextAsync(
          text,
          activeDocument?.TabText ?? "Печать документа");
      }
    }
    private void FocusMainContent()
    {
      if (MainContent.Content is IInputElement focusable && focusable.Focusable)
      {
        Keyboard.Focus(focusable);
      }
      else if (MainContent.Content is FrameworkElement fe)
      {
        fe.Loaded += (_, _) =>
        {
          fe.Focus();
          Keyboard.Focus(fe);
        };
      }
    }

    private void SetTranslationError(List<ErrorItem> errorItems)
    {
      if (errorItems == null || errorItems.Count == 0)
        return;

      ErrorListBoxVertical.AddErrors(errorItems);
      ErrorCount += errorItems.Count;
      TranslationErrorCount += errorItems.Count;

      if (ErrorCount > 0)
      {
        MessageEventAdapter.RaiseInfoMessage($"Общее кол-во ошибок: {CountDisplayFormatter.Format(ErrorCount)}");
      }
    }

    public void SetLeftEditor(TextEditorUI textEditorUI)
    {
      LogInformation("SetLeftEditor вызван: " + this.GetHashCode());
      if (textEditorUI == null)
        return;

      if (textEditorUI.Parent is Panel oldParent)
      {
        oldParent.Children.Remove(textEditorUI);
      }
      else if (textEditorUI.Parent is ContentControl oldContent)
      {
        oldContent.Content = null;
      }
      else if (textEditorUI.Parent is Decorator decorator)
      {
        decorator.Child = null;
      }

      ClearInspectionProtocolCore();
      foreach (var dockItem in _runnerDocuments.Where(item => item != ProtocolPane && item != ErrorsPane && item != SourceDocument).ToArray())
      {
        StopTrackingWorkspaceTitle(dockItem);
        dockItem.Close();
        _runnerDocuments.Remove(dockItem);
      }

      var rightEditor = new TranslatorEditor();
      rightEditor.SetEditor(textEditorUI);
      rightEditor.BackRequested += TranslatorEditor_BackRequested;
      rightEditor.SaveRequested -= RightEditor_SaveRequestedAsync;
      rightEditor.SaveRequested += RightEditor_SaveRequestedAsync;
      rightEditor.SaveToDiskRequested -= RightEditor_SaveToDiskRequestedAsync;
      rightEditor.SaveToDiskRequested += RightEditor_SaveToDiskRequestedAsync;
      rightEditor.SetArchiveButtonVisibility(ErrorCount == 0);
      rightEditor.BackButton.Visibility = CanReturnToSourceFile() && !isLocked
        ? Visibility.Visible
        : Visibility.Collapsed;
      var fileName = GetDisplayFileName(textEditorUI.TextEditorModel.FilePath, textEditorUI.TextEditorModel.OriginalFileName);
      var filePath = textEditorUI.TextEditorModel.FilePath;
      rightEditor.TranslationFileName.Text = fileName;
      rightEditor.SetSaveToDiskVisible(translationModels.Count > 0 && ErrorCount == 0);
      var dockItemPk = new DockItem
      {
        Title = fileName,
        TabText = fileName,
        Content = rightEditor,
        Style = (Style)FindResource("RunnerTranslationStyle"),
      };

      var dockItemDeviceState = new DockItem
      {
        Title = filePath,
        TabText = "Состояние оборудования",
        Content = devicesStatus,
      };

      DocumentTab.SetHideCloseButton(dockItemPk, true);
      DocumentTab.SetHideCloseButton(dockItemDeviceState, true);

      _runnerDocuments.Add(dockItemPk);
      _runnerDocuments.Add(dockItemDeviceState);

      void ShowDocuments()
      {
        SystemStateManager.SetIsControlProgramActive(true);
        ShowRunnerDocument(dockItemDeviceState);
        ShowRunnerDocument(dockItemPk);
        if (SourceDocument != null)
          ShowRunnerDocument(SourceDocument, DockItemShowMethod.Deselect);
        RestoreRunnerLayout(dockItemDeviceState, dockItemPk);
        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
          SyncBreakpointsFromEditor(textEditorUI);
        }), DispatcherPriority.Loaded);
      }

      if (!RunnerWorkspace.IsLoaded)
      {
        RoutedEventHandler? loadedHandler = null;
        loadedHandler = (s, e) =>
        {
          RunnerWorkspace.Loaded -= loadedHandler;
          if (!_runnerDocuments.Contains(dockItemPk) || !_runnerDocuments.Contains(dockItemDeviceState))
            return;

          try
          {
            ShowDocuments();
          }
          catch (Exception ex)
          {
            LogException("Ошибка при отображении документов исполнителя после загрузки:", ex);
          }
        };
        RunnerWorkspace.Loaded += loadedHandler;
      }
      else
      {
        ShowDocuments();
      }
    }

    public async Task Start(
      List<BaseCommandModel> models,
      bool startStepByStep = false,
      bool isStepInto = true)
    {
      ProtocolUI.MenuButtonVisibility(false);
      ControlProgram = models;

      if (models.Count == 0 || models[0].Mnemonic != "ОК")
      {
        return;
      }

      ProtocolUI.Header = BuildDerivedFileName(
        OpkFilePath,
        FileName,
        ProtocolFileExtensions.Trace,
        $"protocol{ProtocolFileExtensions.Trace}");

      ActionSettings settings = new ActionSettings()
      {
        StartDelegate = StartTest,
        CheckType = CheckType.ControlProgram,
      };

      ProtocolUI.SetSettings(settings);
      this.FileName = ProtocolUI.Header;

      await ProtocolUI.StartAsync(startStepByStep, isStepInto);
    }

    private void OnViewProtocol(FileInteractionEvents.ViewProtocol e)
    {
      if (!IsProtocolForCurrentRun(e.Protocol.ProgramPath))
      {
        return;
      }

      ProtocolUI.ShowInspectionProtocol(ProtocolService.BuildProtocolText(e.Protocol));
    }

    private bool IsProtocolForCurrentRun(string protocolProgramPath)
    {
      if (string.IsNullOrWhiteSpace(protocolProgramPath) || string.IsNullOrWhiteSpace(OpkFilePath))
      {
        return false;
      }

      return string.Equals(
        Path.GetFullPath(protocolProgramPath),
        Path.GetFullPath(OpkFilePath),
        StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public void ShowInspectionProtocol(string protocolText)
    {
      Dispatcher.Invoke(() =>
      {
        if (_closingDockLayout)
          return;

        ClearInspectionProtocolCore();

        _inspectionProtocolEditor = new ResultProtocolEditor
        {
          IsReadOnly = true,
        };
        _inspectionProtocolEditor.SetFileType(FileType.InspectionProtocol);
        _inspectionProtocolEditor.WordWrap = true;
        _inspectionProtocolEditor.Text = protocolText ?? string.Empty;

        _inspectionProtocolDockItem ??= CreateInspectionProtocolDockItem();
        _inspectionProtocolDockItem.Content = _inspectionProtocolEditor;
        ShowPendingInspectionProtocol();
      });
    }

    private void ShowPendingInspectionProtocol()
    {
      if (!_closingDockLayout && _dockLayoutInitialized && RunnerWorkspace.IsLoaded
        && _inspectionProtocolEditor != null && _inspectionProtocolDockItem is { } document)
      {
        ShowRunnerDocument(document);
      }
    }

    private DockItem CreateInspectionProtocolDockItem()
    {
      var document = new DockItem
      {
        Title = "Итоговый протокол",
        TabText = "Итоговый протокол",
        Style = (Style)FindResource("RunnerDocumentStyle"),
        Tag = FileType.InspectionProtocol,
      };
      DocumentTab.SetHideCloseButton(document, true);
      _runnerDocuments.Add(document);
      return document;
    }

    /// <inheritdoc />
    public void ClearInspectionProtocol()
    {
      Dispatcher.Invoke(ClearInspectionProtocolCore);
    }

    private void ClearInspectionProtocolCore()
    {
      if (_inspectionProtocolEditor != null)
      {
        _inspectionProtocolEditor.Text = string.Empty;
      }

      if (_inspectionProtocolDockItem is { } document)
      {
        RunnerWorkspace.HidePane(ExecutionWorkspacePane.Result);
        document.Content = null;
        RunnerWorkspace.SetContent(ExecutionWorkspacePane.Result, null);
      }

      _inspectionProtocolEditor = null;
    }

    private async Task StartTest(ActionSettings settings, IUserInteractionService _messageService, IInputFieldProvider inputFieldProvider, IInputHighlightService inputHighlightService, CancellationToken cancellationToken)
    {
      TextEditorUI? editor = null;

      Application.Current.Dispatcher.Invoke(() =>
      {
        var dockItem = _runnerDocuments.FirstOrDefault(di => di.Content is TranslatorEditor);
        if (dockItem != null)
        {
          SelectRunnerDocument(dockItem);
          var translatorEditor = dockItem.Content as TranslatorEditor;
          editor = translatorEditor?.GetTextEditor();
        }
      });

      var manager = new CommandExecutionManager(ProtocolUI, editor, ControlProgram, OpkFilePath, SourceArchiveName);
      manager.ClearError += ErrorClear;
      manager.AddError += AddError;
      try
      {
        await manager.ExecuteAllAsync();
      }
      finally
      {
        settings.AfterEquipmentResetDelegate = manager.AfterEquipmentResetDelegate;
      }
    }

    private bool CanReturnToSourceFile()
    {
      if (string.IsNullOrWhiteSpace(OpkFilePath))
      {
        return true;
      }

      var extension = Path.GetExtension(OpkFilePath);
      return !string.Equals(extension, ".opk", StringComparison.OrdinalIgnoreCase);
    }

    private void AddError(ErrorItem errorItem)
    {
      Application.Current.Dispatcher?.Invoke(() =>
      {
        ErrorListBoxVertical.AddError(errorItem);
        ErrorCount++;

        if (ErrorCount > 0)
        {
          MessageEventAdapter.RaiseInfoMessage($"Общее кол-во ошибок: {CountDisplayFormatter.Format(ErrorCount)}");
        }
      });
    }

    private void ErrorClear()
    {
      Application.Current.Dispatcher?.Invoke(() =>
      {
        ErrorListBoxVertical.ClearAll();
        ErrorCount = 0;
        UpdateArchiveButtonVisibility();
      });
    }

    private void UpdateArchiveButtonVisibility()
    {
      Application.Current.Dispatcher?.Invoke(() =>
      {
        var translatorEditor = _runnerDocuments
          .FirstOrDefault(item => item.Content is TranslatorEditor)?
          .Content as TranslatorEditor;
        if (translatorEditor != null)
        {
          translatorEditor.SetArchiveButtonVisibility(ErrorCount == 0);
          translatorEditor.SetSaveToDiskVisible(CanSaveTranslatedFileToDisk());
        }
      });
    }

    private void TranslatorEditor_BackRequested(object? sender, EventArgs e)
    {
      var translatorEditor = GetTranslatorEditor();
      TranslatorNavigationService.TryOpenSourceFileFromTranslator(
        translatorEditor,
        onSourceOpened: () => EditorEventAdapter.RaiseCloseRunItem(this));
    }

    private void RightEditor_SaveRequestedAsync(object? sender, EventArgs e)
    {
      var rightEditor = sender as TranslatorEditor;
      var sourceFilePath = rightEditor?.GetTextEditor()?.TextEditorModel?.FilePath;
      if (string.IsNullOrWhiteSpace(sourceFilePath))
      {
        sourceFilePath = OpkFilePath;
      }

      var sourceText = TryReadSourceTextForArchive(sourceFilePath)
        ?? rightEditor?.GetTextEditor()?.Text
        ?? string.Empty;

      _archiveSaveService.SaveFileToArchive(this, sourceText, sourceFilePath);
    }

    private static string? TryReadSourceTextForArchive(string? sourceFilePath)
    {
      if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
      {
        return null;
      }

      var ext = Path.GetExtension(sourceFilePath);
      var encoding = string.Equals(ext, ".pkw", StringComparison.OrdinalIgnoreCase)
        || string.Equals(ext, ".opkw", StringComparison.OrdinalIgnoreCase)
          ? new UTF8Encoding(false)
          : Encoding.GetEncoding(866);

      return File.ReadAllText(sourceFilePath, encoding);
    }

    private void RightEditor_SaveToDiskRequestedAsync(object? sender, EventArgs e)
    {
      var translatedText = SourceEditor?.Text;
      var sourceFilePath = SourceEditor?.TextEditorModel?.FilePath;
      if (string.IsNullOrWhiteSpace(sourceFilePath))
      {
        sourceFilePath = OpkFilePath;
      }

      _translatedFileSaveService.SaveToDisk(this, translatedText ?? string.Empty, sourceFilePath);
    }

    private bool CanSaveTranslatedFileToDisk()
    {
      return translationModels.Count > 0 && TranslationErrorCount == 0;
    }

    private static string GetDisplayFileName(string? filePath, string? fileName)
    {
      if (!string.IsNullOrWhiteSpace(fileName))
      {
        return fileName;
      }
      var uniqueNameWithoutExtention = Path.GetFileNameWithoutExtension(filePath);
      var index = uniqueNameWithoutExtention.LastIndexOf('_');
      if (index != -1)
      {
        uniqueNameWithoutExtention = uniqueNameWithoutExtention[..index];
      }
      return string.IsNullOrWhiteSpace(uniqueNameWithoutExtention)
         ? string.Empty
        : uniqueNameWithoutExtention + Path.GetExtension(filePath);

      /*return string.IsNullOrWhiteSpace(filePath)
        ? string.Empty
        : Path.GetFileName(filePath);*/
    }

    private static string BuildDerivedFileName(string? sourceFilePath, string? sourceFileName, string extension, string fallbackFileName)
    {
      string baseName = Path.GetFileNameWithoutExtension(sourceFilePath);
      if (string.IsNullOrWhiteSpace(baseName))
      {
        baseName = Path.GetFileNameWithoutExtension(sourceFileName);
      }

      return string.IsNullOrWhiteSpace(baseName)
        ? fallbackFileName
        : $"{baseName}{extension}";
    }

    private TranslatorEditor? GetTranslatorEditor()
    {
      return _runnerDocuments
        .FirstOrDefault(item => item.Content is TranslatorEditor)?
        .Content as TranslatorEditor;
    }

    private void UpdateTranslatorEditorActions()
    {
      var translatorEditor = GetTranslatorEditor();
      if (translatorEditor == null)
      {
        return;
      }

      translatorEditor.SetSaveToDiskVisible(CanSaveTranslatedFileToDisk());
    }

    /// <summary>
    /// Синхронизирует список точек остановки во вкладке
    /// с текущим состоянием редактора выполнения.
    /// </summary>
    /// <param name="editor">Экземпляр <see cref="TextEditorUI"/>, содержащий активные точки остановки.</param>
    /// <remarks>
    /// Метод полностью очищает текущий список точек остановки во вкладке,
    /// затем заново формирует его на основе данных редактора.
    /// </remarks>
    private void SyncBreakpointsFromEditor(TextEditorUI editor)
    {
      if (editor == null)
        return;

      ErrorListBoxVertical.ClearBreakpoints();

      var doc = editor.Document;
      var cmdNumbers = editor.BreakpointCommandsNumbers;
      var anchors = editor.BreakPointLines;

      int count = Math.Min(cmdNumbers.Count, anchors.Count);

      for (int i = 0; i < count; i++)
      {
        int cmd = cmdNumbers[i];
        int line1 = doc.GetLineByOffset(anchors[i].Offset).LineNumber;
        bool enabled = editor.IsBreakpointEnabled(cmd);

        ErrorListBoxVertical.UpsertBreakpoint(cmd, line1, editor.NumCommandWithMnemonic[cmd], enabled);
        SetModelBreakpoint(cmd, has: true, enabled: enabled);
      }
    }

    /// <summary>
    /// Обрабатывает двойной щелчок по элементу списка точек остановки.
    /// </summary>
    /// <param name="bp">Элемент списка точек остановки.</param>
    /// <remarks>
    /// При наличии корректного номера строки (1-based)
    /// выполняется переход редактора к соответствующей строке.
    /// </remarks>
    private void BreakpointItemDoubleClicked(BreakpointListItem bp)
    {
      var editor = GetTranslatorEditor()?.GetTextEditor();
      if (editor == null)
        return;

      if (bp.RightLine.HasValue && bp.RightLine.Value > 0)
      {
        var translationDocument = _runnerDocuments.FirstOrDefault(item => item.Content is TranslatorEditor);
        if (translationDocument != null)
          SelectRunnerDocument(translationDocument);
        editor.ScrollToLine(bp.RightLine.Value);
      }
    }

    /// <summary>
    /// Обрабатывает изменение состояния (включена/выключена)
    /// точки остановки из списка.
    /// </summary>
    /// <param name="bp">Элемент точки остановки.</param>
    /// <param name="enabled">Состояние точки остановки.</param>
    /// <remarks><see langword="true"/> для включения точки остановки.</remarks>
    private void BreakpointEnabledChanged(BreakpointListItem bp, bool enabled)
    {
      var editor = GetTranslatorEditor()?.GetTextEditor();
      if (editor == null)
        return;

      if (!editor.HasBreakpointCommand(bp.CommandNumber))
        return;

      if (editor.IsBreakpointEnabled(bp.CommandNumber) == enabled)
        return;

      if (enabled)
        editor.EnableBreakpoint(bp.CommandNumber, raiseEvents: true);
      else
        editor.DisableBreakpoint(bp.CommandNumber, raiseEvents: true);
    }

    private void BreakpointToggleRequested(IDisplayIssue? issue)
    {
      var editor = GetTranslatorEditor()?.GetTextEditor();
      if (editor == null)
        return;

      if (editor.ToggleBreakpointFromMousePosition())
        return;

      int lineNumber = issue?.FormattedLineNumber > 0
        ? issue.FormattedLineNumber
        : issue?.SourceLineNumber > 0
          ? issue.SourceLineNumber
          : -1;

      if (lineNumber > 0)
      {
        editor.ToggleBreakpointFromKeyboardAtLine(lineNumber);
        return;
      }

      editor.ToggleBreakpointFromKeyboardAtCaret();
    }

    /// <summary>
    /// Обрабатывает событие установки новой точки остановки.
    /// </summary>
    /// <param name="e">Аргументы события установки точки остановки.</param>
    private void OnBreakpointSet(BreakpointEvents.BreakpointSet e)
    {
      var editor = GetTranslatorEditor()?.GetTextEditor();
      if (editor == null)
        return;

      int line1 = GetLine1BasedByCommand(editor, e.CommandNumber);
      ErrorListBoxVertical.UpsertBreakpoint(e.CommandNumber, line1, editor.NumCommandWithMnemonic[e.CommandNumber], isEnabled: true);

      SetModelBreakpoint(e.CommandNumber, has: true, enabled: true);
    }

    /// <summary>
    /// Обрабатывает событие удаления точки остановки.
    /// </summary>
    /// <param name="e">Аргументы события удаления точки остановки.</param>
    private void OnBreakpointRemoved(BreakpointEvents.BreakpointRemoved e)
    {
      ErrorListBoxVertical.RemoveBreakpoint(e.CommandNumber);
      SetModelBreakpoint(e.CommandNumber, has: false, enabled: true);
    }

    /// <summary>
    /// Обрабатывает событие включения точки остановки.
    /// </summary>
    /// <param name="e">Аргументы события выключения точки остановки.</param>
    private void OnBreakpointOn(BreakpointEvents.BreakpointOn e)
    {
      var editor = GetTranslatorEditor()?.GetTextEditor();
      if (editor == null)
        return;

      int line1 = GetLine1BasedByCommand(editor, e.CommandNumber);
      ErrorListBoxVertical.UpsertBreakpoint(e.CommandNumber, line1, editor.NumCommandWithMnemonic[e.CommandNumber], isEnabled: true);

      SetModelBreakpoint(e.CommandNumber, has: true, enabled: true);
    }

    /// <summary>
    /// Обрабатывает событие выключения точки остановки.
    /// </summary>
    /// <param name="e">Аргументы события выключения точки остановки.</param>
    private void OnBreakpointOff(BreakpointEvents.BreakpointOff e)
    {
      var editor = GetTranslatorEditor()?.GetTextEditor();
      if (editor == null)
        return;

      int line1 = GetLine1BasedByCommand(editor, e.CommandNumber);
      ErrorListBoxVertical.UpsertBreakpoint(e.CommandNumber, line1, editor.NumCommandWithMnemonic[e.CommandNumber], isEnabled: false);

      SetModelBreakpoint(e.CommandNumber, has: true, enabled: false);
    }

    /// <summary>
    /// Возвращает 1-based номер строки документа,
    /// соответствующий указанной команде.
    /// </summary>
    /// <param name="editor">Редактор, содержащий точки остановки.</param>
    /// <param name="commandNumber">Номер команды.</param>
    /// <returns>
    /// Номер строки документа (1-based),
    /// на которой расположена точка остановки.
    /// </returns>
    private static int GetLine1BasedByCommand(TextEditorUI editor, int commandNumber)
    {
      var doc = editor.Document;
      int idx = editor.BreakpointCommandsNumbers.IndexOf(commandNumber);
      return doc.GetLineByOffset(editor.BreakPointLines[idx].Offset).LineNumber; // 1-based
    }

    /// <summary>
    /// Обновляет состояние точки остановки
    /// в модели команды трансляции.
    /// </summary>
    /// <param name="commandNumber">Номер команды.</param>
    /// <param name="has">
    /// Признак наличия точки остановки.
    /// </param>
    /// <param name="enabled">
    /// Признак включённого состояния точки остановки.
    /// </param>
    private void SetModelBreakpoint(int commandNumber, bool has, bool enabled)
    {
      var model = translationModels.FirstOrDefault(m =>
        int.TryParse(m.CommandNumber, out var num) && num == commandNumber);

      if (model == null) return;

      model.HasBreakpoint = has;
      model.IsBreakpointEnabled = enabled;
    }
  }
}
