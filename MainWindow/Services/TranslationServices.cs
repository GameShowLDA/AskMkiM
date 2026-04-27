using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.FileFormats;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Static;
using Ask.Core.Shared.Metadata.View.EditorHost.TextEditor;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.Model;
using Message;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using UI.Components;
using UI.Components.SearchControls;
using UI.Controls;
using UI.Controls.Runner;
using UI.Controls.TextEditorControl;
using UI.Windows.WpfDocking.Windows.Docking;
using static Ask.LogLib.LoggerUtility;

namespace MainWindowProgram.Services
{
  /// <summary>
  /// Сервис трансляции команд из текстового редактора.
  /// Обеспечивает распознавание команд, отображение результатов трансляции и работу с двумя редакторами: исходным и переводом.
  /// </summary>
  public class TranslationServices
  {
    private static readonly Regex LatinLettersRegex = new("[A-Za-z]", RegexOptions.Compiled);
    private readonly LookalikeLatinToCyrillicNormalizer _lookalikeNormalizer = new(Encoding.UTF8);

    /// <summary>
    /// Сервис для управления многооконным интерфейсом.
    /// </summary>
    private readonly MultiWindowService _multiWindow;

    /// <summary>
    /// Сервис для работы с файлами.
    /// </summary>
    private readonly FileService _fileService;

    private static readonly HashSet<string> SupportedExecutionSourceExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
      ".pk",
      ".pkw",
      ".acs",
    };

    private TextEditorUI _actualTextEditor;

    /// <summary>
    /// Проверяет, разрешено ли ставить точку остановки на указанной команде.
    /// </summary>
    /// <param name="m">Название команды.</param>
    /// <returns>
    /// <see langword="true"/>, если команда поддерживает точки остановки; иначе <see langword="false"/>.
    /// </returns>
    private static bool IsBreakpointAllowed(BaseCommandModel m) =>
        m.Mnemonic != "СП"
        && m.Mnemonic != "ЦУ"
        && m.Mnemonic != "КЦ"
        && m.Mnemonic != "РМ"
        && m.Mnemonic != "УП"
        && m.Mnemonic != "ОК";

    /// <summary>
    /// Строит словарь соответствия между номерами команд и их наименованиями (мнемониками) для быстрого доступа при работе с точками остановки.
    /// </summary>
    /// <param name="models">Команды, на которые разрешены точки.</param>
    /// <returns>Словарь </returns>
    private static Dictionary<int, string> BuildNumCommandWithMnemonic(IEnumerable<BaseCommandModel> models)
    {
      var result = new Dictionary<int, string>();
      int commandNumber;

      foreach (var model in models)
      {
        commandNumber = int.Parse(model.CommandNumber);
        if (result.ContainsKey(commandNumber))
        {
          continue;
        }

        result.Add(commandNumber, model.Mnemonic);
      }

      return result;
    }

    /// <summary>
    /// Восстанавливает точки останова после пересборки:
    /// обновляет флаги моделей и синхронизирует два редактора.
    /// </summary>
    private static void RestoreBreakpoints(
        List<BaseCommandModel> models,
        ITextEditorView leftEditor,
        ITextEditorView rightEditor,
        Dictionary<int, bool> preserved)
    {
      var requiredCommands = new HashSet<int>();

      var leftLineByCommand = new Dictionary<int, int>();
      var rightLineByCommand = new Dictionary<int, int>();
      var enabledByCommand = new Dictionary<int, bool>();

      for (int i = 0; i < models.Count; i++)
      {
        var model = models[i];

        if (!int.TryParse(model.CommandNumber, out var cmd))
        {
          model.HasBreakpoint = false;
          model.IsBreakpointEnabled = true;
          continue;
        }

        bool isAllowed = IsBreakpointAllowed(model);
        bool hasPreserved = preserved.TryGetValue(cmd, out bool preservedEnabled);

        bool hasBreakpoint = isAllowed && hasPreserved;
        model.HasBreakpoint = hasBreakpoint;
        model.IsBreakpointEnabled = !hasBreakpoint || preservedEnabled;

        if (!isAllowed)
        {
          continue;
        }

        leftLineByCommand[cmd] = model.StartLineNumber;
        rightLineByCommand[cmd] = model.FormattedStartLineNumber + 1;

        if (hasBreakpoint)
        {
          requiredCommands.Add(cmd);
          enabledByCommand[cmd] = preservedEnabled;
        }
      }

      SyncEditorBreakpoints(leftEditor, requiredCommands, leftLineByCommand, enabledByCommand);
      SyncEditorBreakpoints(rightEditor, requiredCommands, rightLineByCommand, enabledByCommand);
    }

    /// <summary>
    /// Синхронизирует набор точек останова в редакторе:
    /// снимает лишние и добавляет отсутствующие.
    /// </summary>
    private static void SyncEditorBreakpoints(
        ITextEditorView editor,
        HashSet<int> requiredCommands,
        Dictionary<int, int> lineByCommand,
        Dictionary<int, bool> enabledByCommand)
    {
      var existing = editor.BreakpointCommandsNumbers.ToList();

      for (int i = 0; i < existing.Count; i++)
      {
        int cmd = existing[i];
        if (!requiredCommands.Contains(cmd))
        {
          editor.EnsureBreakpoint(1, cmd, isSet: false, raiseEvents: false);
        }
      }

      foreach (var cmd in requiredCommands)
      {
        if (lineByCommand.TryGetValue(cmd, out int line1Based))
        {
          editor.EnsureBreakpoint(line1Based, cmd, isSet: true, raiseEvents: false);
        }

        if (enabledByCommand.TryGetValue(cmd, out bool enabled))
        {
          if (enabled)
          {
            editor.EnableBreakpoint(cmd, raiseEvents: false);
          }
          else
          {
            editor.DisableBreakpoint(cmd, raiseEvents: false);
          }
        }
      }
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="AdminServices"/>.
    /// </summary>
    /// <param name="multiWindow">Сервис управления многооконным интерфейсом.</param>
    /// <param name="fileService">Сервис  для работы с файлами.</param>
    public TranslationServices(MultiWindowService multiWindow, FileService fileService)
    {
      _multiWindow = multiWindow;
      _fileService = fileService;
    }

    private async Task RunWithTranslationProgressAsync(Func<ProgressWindow, Task> action)
    {
      var owner = System.Windows.Application.Current?.MainWindow;
      var previousEffect = owner?.Effect;
      ProgressWindow? progressWindow = null;

      try
      {
        progressWindow = new ProgressWindow
        {
          Owner = owner,
          WindowStartupLocation = owner == null
                ? WindowStartupLocation.CenterScreen
                : WindowStartupLocation.CenterOwner,
        };

        progressWindow.Configure(
            "Трансляция программы контроля",
            "Подготовка окна",
            "Готовим отображение этапов и интерфейс транслятора.");

        if (owner != null)
        {
          owner.Effect = new BlurEffect { Radius = 8 };
        }

        progressWindow.Show();

        await WaitForUiReadyAsync(progressWindow);
        await action(progressWindow);
      }
      finally
      {
        progressWindow?.Close();

        if (owner != null)
        {
          owner.Effect = previousEffect;
        }
      }
    }

    private async Task<TranslationBuildResult> BuildTranslationAsync(string text, ProgressWindow progressWindow)
    {
      SetTranslationStage(
          progressWindow,
          "Подготовка фоновой трансляции",
          "Запускаем разбор программы контроля вне UI-потока.",
          8d);

      if (System.Windows.Application.Current != null)
      {
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(
            () => { },
            DispatcherPriority.ContextIdle);
      }

      bool buildStageUpdatesEnabled = true;

      var progress = new Progress<string>(stage =>
      {
        if (!buildStageUpdatesEnabled)
        {
          return;
        }

        var (status, hint, progressValue) = MapBuildStage(stage);
        SetTranslationStage(progressWindow, status, hint, progressValue);
      });

      var result = await Task.Run(() =>
      {
        var manager = new CommandTranslationManager();
        var result = manager.BuildTranslation(text, progress);
        manager.SetSourseLines(result.Models);
        return result;
      });

      buildStageUpdatesEnabled = false;
      return result;
    }

    private static (string Status, string Hint, double Progress) MapBuildStage(string stage) => stage switch
    {
      "Начало трансляции" => (
          "Подготовка трансляции",
          "Инициализируем парсеры, форматтеры и служебные структуры.",
          18d),
      "Формирование данных" => (
          "Формирование текста трансляции",
          "Строим строки результата и связываем их с исходными командами.",
          42d),
      "Проверка взаимосвязей" => (
          "Проверка взаимосвязей",
          "Проверяем обязательные команды, переходы и связи между точками.",
          62d),
      "Готово" => (
          "Фоновая сборка завершена",
          "Результат готов. Подключаем его к интерфейсу.",
          70d),
      _ => (stage, stage, 0d),
    };

    private static void SetTranslationStage(
        ProgressWindow progressWindow,
        string status,
        string hint,
        double? progress = null)
    {
      if (progress.HasValue)
      {
        progressWindow.SetProgress(progress.Value);
      }

      progressWindow.SetStage(status, hint);
    }

    private static async Task SetTranslationStageAsync(
        ProgressWindow progressWindow,
        string status,
        string hint,
        double? progress = null)
    {
      SetTranslationStage(progressWindow, status, hint, progress);
      await FlushProgressWindowAsync(progressWindow);
    }

    private static async Task FlushProgressWindowAsync(ProgressWindow progressWindow)
    {
      var dispatcher = progressWindow.Dispatcher;
      if (dispatcher == null)
      {
        return;
      }

      await dispatcher.InvokeAsync(
          progressWindow.UpdateLayout,
          DispatcherPriority.Background);

      await dispatcher.InvokeAsync(
          progressWindow.UpdateLayout,
          DispatcherPriority.Render);

      await dispatcher.InvokeAsync(
          () => { },
          DispatcherPriority.ContextIdle);
    }

    private static void SetDeferredVisibility(FrameworkElement? element, bool isVisible)
    {
      if (element == null)
      {
        return;
      }

      element.Opacity = isVisible ? 1d : 0d;
      element.IsHitTestVisible = isVisible;
    }

    private static async Task RevealDeferredElementsAsync(params FrameworkElement?[] elements)
    {
      var visibleElements = elements
          .Where(element => element != null)
          .Cast<FrameworkElement>()
          .Distinct()
          .ToArray();

      if (visibleElements.Length == 0)
      {
        return;
      }

      foreach (var element in visibleElements)
      {
        SetDeferredVisibility(element, true);
      }

      await WaitForUiReadyAsync(visibleElements);
    }

    private static async Task WaitForUiReadyAsync(params FrameworkElement?[] elements)
    {
      var readyElements = elements
          .Where(element => element != null)
          .Cast<FrameworkElement>()
          .Distinct()
          .ToArray();

      foreach (var element in readyElements)
      {
        await WaitForLoadedAsync(element);
      }

      var dispatcher = readyElements.FirstOrDefault()?.Dispatcher ?? System.Windows.Application.Current?.Dispatcher;
      if (dispatcher == null)
      {
        return;
      }

      await dispatcher.InvokeAsync(() =>
      {
        foreach (var element in readyElements)
        {
          element.UpdateLayout();
        }
      }, DispatcherPriority.Loaded);

      await dispatcher.InvokeAsync(() =>
      {
        foreach (var element in readyElements)
        {
          element.UpdateLayout();
        }
      }, DispatcherPriority.Render);

      await dispatcher.InvokeAsync(() => { }, DispatcherPriority.ContextIdle);
    }

    private static Task WaitForLoadedAsync(FrameworkElement element)
    {
      if (element.IsLoaded)
      {
        return Task.CompletedTask;
      }

      var source = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
      RoutedEventHandler? handler = null;
      handler = (_, _) =>
      {
        element.Loaded -= handler;
        source.TrySetResult(null);
      };

      element.Loaded += handler;
      return source.Task;
    }

    /// <summary>
    /// Запускает процесс трансляции текущего открытого текста из редактора.
    /// Выполняет распознавание команд, логирует результат и применяет подсветку
    /// в соответствии с успешностью распознавания.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию трансляции.</returns>
    public async Task BuildAsync()
    {
      var editor = _multiWindow.GetActiveTextEditor(EditorType.TextEditor);
      var translationContainer = _multiWindow.GetActiveTextEditorContainer(EditorType.Translator);

      if (editor != null && !EnsureSupportedExecutionSource(editor))
      {
        return;
      }

      if (editor == null && translationContainer != null)
      {
        await TryUpdateExistingTranslator(translationContainer);
      }
      else if (editor != null)
      {
        await TryCreateNewTranslator(editor);
      }
      else
      {
        ShowEditorNotFoundError();
      }
    }

    /// <summary>
    /// Запускает процесс трансляции текущего открытого текста из редактора.
    /// Выполняет распознавание команд, логирует результат и применяет подсветку
    /// в соответствии с успешностью распознавания.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию трансляции.</returns>
    public async Task RunAsync()
    {
      var editor = _multiWindow.GetActiveTextEditor(EditorType.TextEditor);
      var container = _multiWindow.GetActiveTextEditorContainer(EditorType.Translator);
      var runContainer = _multiWindow.GetActiveTextEditorContainer(EditorType.Run);

      if (editor != null && !EnsureSupportedExecutionSource(editor))
      {
        return;
      }

      if (runContainer != null)
      {
        var runControl = runContainer.GetDockControl().DockItems[0].Content as RunControl;
        if (runControl != null)
        {
          await runControl.Start(runControl.TranslationModels);
          return;
        }
      }

      if (container == null && runContainer == null && editor == null)
      {
        MessageBoxCustom.Show(
            $"Не удалось запустить исполнитель программы контроля.",
            "Ошибка запуска программы контроля",
            image: MessageBoxImage.Error);
        return;
      }

      await BuildAsync();

      if (container == null && editor != null)
      {
        container = _multiWindow.GetActiveTextEditorContainer(EditorType.Translator);
      }

      if (container == null && runContainer != null)
      {
        container = runContainer;
      }

      var dockManager = container.GetDockControl();
      if (dockManager == null)
      {
        return;
      }

      DockItem? foundDockItem = null;

      for (int i = 0; i < 500; i++)
      {
        if (dockManager.DockItems.Count > 0)
        {
          foundDockItem = dockManager.DockItems.FirstOrDefault(item => item.IsActiveItem == true);
          if (foundDockItem != null)
          {
            break;
          }
        }

        await Task.Delay(10);
      }

      if (foundDockItem?.Content is not TranslatorItem translator)
      {
        if (foundDockItem?.Content is RunControl run)
        {
          await PrepareRun(runContainer, _actualTextEditor, run);
          // TODO: закрыть вкладку со старым транслятором
          // await _multiWindow.CloseRunItem(run, EditorType.Run);
        }
        else
        {
          return;
        }
      }
      else
      {
        var sourceEditor = translator.GetLeftBox().GetTextEditor();
        if (sourceEditor == null)
        {
          ShowEditorNotFoundError();
          return;
        }

        if (!EnsureSupportedExecutionSource(sourceEditor))
        {
          return;
        }

        _actualTextEditor = translator.GetRightBox().GetTextEditor();
        if (_actualTextEditor == null)
        {
          ShowEditorNotFoundError();
          return;
        }

        if (translator.ErrorCount > 0)
        {
          MessageBoxCustom.Show(
              $"Возникли ошибки сборки ({translator.ErrorCount} ошибок). Устраните ошибки и повторите попытку.",
              "Ошибка запуска программы контроля",
              image: MessageBoxImage.Error);
          return;
        }

        await _multiWindow.DeleteTranslatorItem(translator, EditorType.Translator);

        RunControl runControl = new RunControl();
        runControl.TranslationModels = translator.TranslationModels;
        await PrepareRun(runContainer, _actualTextEditor, runControl);
      }
    }

    /// <summary>
    /// Подготавливает вкладку исполнения и запускает выполнение.
    /// </summary>
    /// <param name="runContainer">Контейнер исполнения.</param>
    /// <param name="editor">Редактор, содержащий путь к файлу и исходные данные.</param>
    /// <param name="runControl">Контрол исполнения.</param>
    private async Task PrepareRun(TextEditorContainer runContainer, TextEditorUI editor, RunControl runControl)
    {
      runControl.OpkFilePath = editor.TextEditorModel.FilePath;
      runControl.FileName = BuildDerivedFileName(editor.TextEditorModel.FilePath, editor.TextEditorModel.FileName, ".lst", "protocol.lst");
      runControl.SetLeftEditor(editor);

      if (runContainer == null)
      {
        await _multiWindow.RunService.AddRunItem(runControl, EditorType.Run);
      }
      else
      {
        var dockItem = runContainer.GetDockControl().DockItems.FirstOrDefault(item => item.Content == runControl);
        if (dockItem != null)
        {
          dockItem.PerformClose();
        }

        await _multiWindow.RunService.AddRunItem(runControl, EditorType.Run);
      }

      await runControl.Start(runControl.TranslationModels);
    }

    private static string GetDisplayFileName(string? filePath, string? fileName)
    {
      if (!string.IsNullOrWhiteSpace(filePath))
      {
        return Path.GetFileName(filePath);
      }

      return fileName ?? string.Empty;
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

    /// <summary>
    /// Пытается создать новый транслятор, используя текст из указанного редактора.
    /// </summary>
    /// <param name="editor">Редактор с исходным текстом.</param>
    private async Task TryCreateNewTranslator(TextEditorUI editor)
    {
      if (!TryPrepareTextForTranslation(editor, out var text))
      {
        return;
      }

      if (_multiWindow.RemoveActiveTextEditor(true))
      {
        EditorEventAdapter.RaiseTextEditorContainerClosing(true, editor.TextEditorModel.FileName);
        await CreateNewTranslator(editor, text);
      }
    }

    /// <summary>
    /// Пытается обновить существующий транслятор, если активен соответствующий элемент интерфейса.
    /// </summary>
    /// <param name="container">Контейнер, содержащий редактор трансляции.</param>
    private async Task TryUpdateExistingTranslator(TextEditorContainer container)
    {
      var dockManager = container.GetDockControl();
      if (dockManager == null)
      {
        return;
      }

      var foundDockItem = dockManager.DockItems.FirstOrDefault(item => item.IsActiveItem == true);
      if (foundDockItem?.Content is not TranslatorItem translator)
      {
        return;
      }

      var editor = translator.GetLeftBox().GetTextEditor();
      if (editor == null)
      {
        ShowEditorNotFoundError();
        return;
      }

      if (!EnsureSupportedExecutionSource(editor))
      {
        return;
      }

      await EditExistingTranslator(editor, foundDockItem);
    }

    private static bool EnsureSupportedExecutionSource(TextEditorUI editor)
    {
      if (IsSupportedExecutionSource(editor))
      {
        return true;
      }

      MessageBoxCustom.Show(
          "Неподдерживаемый тип файла для исполнителя и трансляции. Поддерживаются файлы .pk, .pkw и .acs.",
          "Неподдерживаемый тип файла",
          MessageBoxButton.OK,
          image: MessageBoxImage.Warning);

      return false;
    }

    private static bool IsSupportedExecutionSource(TextEditorUI editor)
    {
      var filePath = editor.TextEditorModel?.FilePath;
      if (!string.IsNullOrWhiteSpace(filePath)
          && SupportedExecutionSourceExtensions.Contains(Path.GetExtension(filePath)))
      {
        return true;
      }

      var fileName = editor.TextEditorModel?.FileName;
      return !string.IsNullOrWhiteSpace(fileName)
          && SupportedExecutionSourceExtensions.Contains(Path.GetExtension(fileName));
    }

    /// <summary>
    /// Выводит сообщение об ошибке, если редактор не найден.
    /// </summary>
    private void ShowEditorNotFoundError()
    {
      MessageBoxCustom.Show("Редактор не найден", "Ошибка", MessageBoxButton.OK, image: MessageBoxImage.Error);
    }

    private void ShowTranslationError(Exception ex)
    {
      MessageBoxCustom.Show(
          "Не удалось выполнить трансляцию программы контроля.",
          "Ошибка запуска программы контроля",
          image: MessageBoxImage.Error);

      LogError($"Не удалось выполнить трансляцию программы контроля: {ex}.");
    }

    /// <summary>
    /// Обновляет существующий компонент транслятора на основе текста из заданного редактора.
    /// Выполняет трансляцию команд и выводит результат во второй (правый) редактор.
    /// </summary>
    /// <param name="editor">Редактор с исходным текстом.</param>
    /// <param name="foundDockItem">Док-элемент, содержащий компонент транслятора.</param>
    private async Task EditExistingTranslator(TextEditorUI editor, DockItem foundDockItem)
    {
      if (!TryPrepareTextForTranslation(editor, out var text))
      {
        return;
      }
      var translateEditor = _fileService.CreateTranslationFileAsync(editor.TextEditorModel.FilePath);
      if (translateEditor == null)
      {
        return;
      }

      if (editor.TextEditorModel != null && translateEditor.TextEditorModel != null)
      {
        translateEditor.TextEditorModel.FilePath = editor.TextEditorModel.FilePath;
      }

      var currentItem = foundDockItem.Content as TranslatorItem;
      Dictionary<int, bool> preservedBreakpoints = currentItem != null
          ? currentItem.TranslationModels
              .Where(x => x.HasBreakpoint)
              .ToDictionary(x => int.Parse(x.CommandNumber), x => x.IsBreakpointEnabled)
          : new Dictionary<int, bool>();

      SetDeferredVisibility(currentItem, false);
      SetDeferredVisibility(translateEditor.View, false);

      try
      {
        await RunWithTranslationProgressAsync(async progressWindow =>
        {
          await SetTranslationStageAsync(
              progressWindow,
              "Подготовка редактора результата",
              "Создаём новый экземпляр редактора для обновлённого текста трансляции.",
              8d);

          var translationResult = await BuildTranslationAsync(text, progressWindow);
          var models = translationResult.Models;
          var allowed = models.Where(IsBreakpointAllowed).ToList();

          await SetTranslationStageAsync(
              progressWindow,
              "Подготовка диагностики",
              "Собираем ошибки и предупреждения в пакет до обновления интерфейса.",
              76d);

          var issuesSnapshot = await Task.Run(() => TranslatorItem.BuildIssuesSnapshot(models));

          await SetTranslationStageAsync(
              progressWindow,
              "Передача текста результата",
              "Загружаем сформированный текст в правый редактор.",
              82d);

          translateEditor.Text = translationResult.FormattedText;

          // ЛЕВЫЙ редактор работает по исходным строкам
          editor.ConfigureBreakpoints(interactive: true, visible: false);
          editor.RightBreakpoint = allowed
              .Select(m => m.StartLineNumber)
              .ToList();
          editor.NumCommandWithMnemonic = BuildNumCommandWithMnemonic(allowed);

          if (currentItem != null)
          {
            await SetTranslationStageAsync(
                progressWindow,
                "Подключение редактора",
                "Меняем экземпляр правого редактора и подготавливаем его к отображению.",
                86d);

            currentItem.SetRightEditor(translateEditor);
            currentItem.SetRightEditorName(translateEditor.TextEditorModel?.FileName ?? string.Empty);

            await SetTranslationStageAsync(
                progressWindow,
                "Обновление диагностики",
                "Применяем подготовленный список ошибок и предупреждений к таблице.",
                90d);

            currentItem.ApplyTranslationModels(models, issuesSnapshot);

            await SetTranslationStageAsync(
                progressWindow,
                "Синхронизация точек останова",
                "Обновляем разрешённые строки и восстанавливаем сохранённые точки останова.",
                94d);

            // ПРАВЫЙ редактор работает по форматированным строкам
            translateEditor.ConfigureBreakpoints(interactive: true, visible: true);
            translateEditor.RightBreakpoint = allowed
                .Select(m => m.FormattedStartLineNumber + 1)
                .ToList();
            translateEditor.NumCommandWithMnemonic = BuildNumCommandWithMnemonic(allowed);

            RestoreBreakpoints(models, editor, translateEditor, preservedBreakpoints);

            await SetTranslationStageAsync(
                progressWindow,
                "Загрузка интерфейса",
                "Подготавливаем обновлённую вкладку к показу без промежуточного мерцания.",
                98d);

            await WaitForUiReadyAsync(currentItem, translateEditor.View);
          }
        });
      }
      catch (Exception ex)
      {
        ShowTranslationError(ex);
      }
      finally
      {
        await RevealDeferredElementsAsync(currentItem, translateEditor.View);
      }
    }

    /// <summary>
    /// Создаёт новый компонент транслятора, содержащий редактор исходного текста и редактор результата трансляции.
    /// Выполняет разбор команд и отображает результат трансляции.
    /// </summary>
    /// <param name="editor">Редактор с исходным текстом.</param>
    /// <param name="text">Текст, подлежащий трансляции.</param>
    /// <returns>Асинхронная задача создания компонента транслятора.</returns>
    private async Task CreateNewTranslator(TextEditorUI editor, string text)
    {
      TranslatorItem? createdItem = null;
      ITextEditorView? translateEditor = null;

      try
      {
        translateEditor = _fileService.CreateTranslationFileAsync(editor.TextEditorModel.FilePath);
        if (translateEditor == null)
        {
          return;
        }

        editor.TextArea.Document.Text = text;

        if (!editor.TextArea.TextView.LineTransformers.OfType<BracesCommentColorizer>().Any())
        {
          editor.TextArea.TextView.LineTransformers.Add(new BracesCommentColorizer());
        }

        CancellationTokenSource redrawToken = null;

        editor.TextChanged += async (_, __) =>
        {
          redrawToken?.Cancel();
          redrawToken = new CancellationTokenSource();
          var token = redrawToken.Token;

          try
          {
            await Task.Delay(80, token);
            if (!token.IsCancellationRequested)
            {
              System.Windows.Application.Current.Dispatcher.Invoke(() =>
              {
                editor.TextArea.TextView.Redraw();
              });
            }
          }
          catch (TaskCanceledException)
          {
          }
        };

        if (editor.TextEditorModel != null && translateEditor.TextEditorModel != null)
        {
          translateEditor.TextEditorModel.FilePath = editor.TextEditorModel.FilePath;
        }

        SetDeferredVisibility(translateEditor.View, false);

        await RunWithTranslationProgressAsync(async progressWindow =>
        {
          await SetTranslationStageAsync(
              progressWindow,
              "Подготовка редактора результата",
              "Создаём редактор для новой вкладки транслятора.",
              8d);

          var translationResult = await BuildTranslationAsync(text, progressWindow);
          var models = translationResult.Models;
          var allowed = models.Where(IsBreakpointAllowed).ToList();

          await SetTranslationStageAsync(
              progressWindow,
              "Подготовка диагностики",
              "Собираем ошибки и предупреждения до открытия новой вкладки.",
              76d);

          var issuesSnapshot = await Task.Run(() => TranslatorItem.BuildIssuesSnapshot(models));

          await SetTranslationStageAsync(
              progressWindow,
              "Передача текста результата",
              "Передаём итоговый текст в редактор результата.",
              82d);

          translateEditor.Text = translationResult.FormattedText;

          // ЛЕВЫЙ редактор — исходные строки
          editor.ConfigureBreakpoints(interactive: true, visible: false);
          editor.RightBreakpoint = allowed
              .Select(m => m.StartLineNumber)
              .ToList();
          editor.NumCommandWithMnemonic = BuildNumCommandWithMnemonic(allowed);

          EditorEventAdapter.RaiseCloseRunItem(editor);

          await SetTranslationStageAsync(
              progressWindow,
              "Открытие вкладки трансляции",
              "Создаём контейнер и подключаем исходный и результирующий редакторы.",
              86d);

          createdItem = await _multiWindow.AddTranslatorItem(editor, translateEditor, EditorType.Translator);
          if (createdItem == null)
          {
            return;
          }

          SetDeferredVisibility(createdItem, false);

          await SetTranslationStageAsync(
              progressWindow,
              "Обновление диагностики",
              "Применяем подготовленный список ошибок и предупреждений к новой вкладке.",
              90d);

          createdItem.ApplyTranslationModels(models, issuesSnapshot);

          await SetTranslationStageAsync(
              progressWindow,
              "Синхронизация точек останова",
              "Готовим правый редактор к навигации и установке точек останова.",
              94d);

          // ПРАВЫЙ редактор — форматированные строки
          translateEditor.ConfigureBreakpoints(interactive: true, visible: true);
          translateEditor.RightBreakpoint = allowed
              .Select(m => m.FormattedStartLineNumber + 1)
              .ToList();
          translateEditor.NumCommandWithMnemonic = BuildNumCommandWithMnemonic(allowed);

          await SetTranslationStageAsync(
              progressWindow,
              "Загрузка интерфейса",
              "Подготавливаем новую вкладку к показу без преждевременного отображения.",
              98d);

          await WaitForUiReadyAsync(createdItem, translateEditor.View);
        });
      }
      catch (Exception ex)
      {
        ShowTranslationError(ex);

        EditorEventAdapter.RaiseTextEditorActivated(editor);
        _multiWindow.EditorDocumentService.OpenFile(editor.TextEditorModel.FilePath);
      }
      finally
      {
        await RevealDeferredElementsAsync(createdItem, translateEditor?.View);
      }
    }

    private bool TryPrepareTextForTranslation(TextEditorUI editor, out string text)
    {
      text = editor.Text ?? string.Empty;
      if (!LatinLettersRegex.IsMatch(text))
      {
        return true;
      }

      var replaceDecision = MessageBoxCustom.Show(
        "В тексте найдены английские буквы.\nЗаменить их на русские аналоги перед трансляцией?\nЕсли не заменить, возможны ошибки локализации, и часть параметров может быть не распознана.",
        "Проверка текста перед трансляцией",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning);

      if (replaceDecision == MessageBoxResult.Yes)
      {
        var normalizedText = _lookalikeNormalizer.Normalize(text);
        if (!string.Equals(normalizedText, text, StringComparison.Ordinal))
        {
          editor.Text = normalizedText;
          text = normalizedText;
        }

        return true;
      }

      MessageBoxCustom.Show(
        "Трансляция будет выполнена без замены английских букв. Возможны ошибки локализации, и часть параметров может быть не распознана.",
        "Предупреждение",
        MessageBoxButton.OK,
        MessageBoxImage.Warning);

      return true;
    }
  }
}




