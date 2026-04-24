using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Services.FileFormats.Apk;
using Ask.Core.Services.FileFormats.Opk;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.Core.Shared.Metadata.View.EditorHost.TextEditor;
using Ask.UI.Controls.ProtocolNew;
using MainWindowProgram.Services.Conversion;
using MainWindowProgram.Windows;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using UI.Components;
using UI.Controls.Archive;
using UI.Controls.FileCompare;
using UI.Controls.Search;
using UI.Controls.TextEditorControl;
using UI.Services.Archive;


namespace MainWindowProgram.Services
{
  /// <summary>
  /// Реализация сервиса для работы с файлами.
  /// Содержит команды и методы для открытия, создания, сохранения, печати, поиска и других операций с файлами.
  /// </summary>
  public class FileService
  {
    /// <summary>
    /// Сервис управления многооконным интерфейсом.
    /// </summary>
    private readonly MultiWindowService _multiWindow;

    private readonly MainWindow _mainWindow;

    /// <summary>
    /// Делегат, предоставляющий актуальное значение состояния блокировки приложения.
    /// </summary>
    private readonly Func<bool> _isLockedProvider;
    private readonly IOpkToPkConverter _opkToPkConverter;
    private readonly OpkToOpkwConverter _opkToOpkwConverter;
    private readonly PkToOpkwConverter _pkToOpkwConverter;
    private readonly ApkToApkwConverter _apkToApkwConverter;

    private bool _isSearchWindowOpen;
    private bool _selectFileHandlerAttached;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="FileService"/>.
    /// </summary>
    /// <param name="multiWindow">Сервис для работы с окнами редакторов.</param>
    /// <param name="isLockedProvider">Функция, возвращающая признак блокировки интерфейса.</param>
    public FileService(MainWindow mainWindow, MultiWindowService multiWindow, Func<bool> isLockedProvider)
    {
      _multiWindow = multiWindow;
      _mainWindow = mainWindow;
      _mainWindow.SearchWindow = new SearchWindow();
      _isLockedProvider = isLockedProvider;
      _opkToPkConverter = new OpkToPkConverter();
      _pkToOpkwConverter = new PkToOpkwConverter();
      _opkToOpkwConverter = new OpkToOpkwConverter(
        _opkToPkConverter,
        (inputPath, outputDirectory) =>
        {
          var result = _pkToOpkwConverter.Convert(inputPath, outputDirectory);
          return new OpkToOpkwTranslationResult
          {
            OutputPath = result.OutputPath,
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            ErrorCount = result.ErrorCount,
          };
        });
      _apkToApkwConverter = new ApkToApkwConverter(
        _opkToPkConverter,
        (inputPath, outputDirectory) =>
        {
          var result = _pkToOpkwConverter.Convert(inputPath, outputDirectory);
          return new ApkToApkwPkConversionResult
          {
            OutputPath = result.OutputPath,
            Success = result.Success,
            ErrorCount = result.ErrorCount,
          };
        },
        () => new ApkwArchiveWriter(),
        ArchiveDirectoryService.ResolveReviewArchivesRootPath);

      EventAggregator.Subscribe<SearchEvents.SearchWindowClosing>(e => OnSearchWindowClosing(e.IsClosing));

      EventAggregator.Unsubscribe<FileInteractionEvents.ViewProtocol>(e => ViewProtocol(e.Protocol));
      EventAggregator.Subscribe<FileInteractionEvents.ViewProtocol>(e => ViewProtocol(e.Protocol));

      EventAggregator.Unsubscribe<FileInteractionEvents.GetProtocolInfo>(e => OnGetProtocolInfo(e.Protocol));
      EventAggregator.Subscribe<FileInteractionEvents.GetProtocolInfo>(e => OnGetProtocolInfo(e.Protocol));
    }

    private void OnGetProtocolInfo(ProtocolModel protocolModel)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        ProtocolInfoWindow protocolInfoWindow = new ProtocolInfoWindow(protocolModel);
        Application.Current.MainWindow.Effect = new System.Windows.Media.Effects.BlurEffect();
        bool? dialogResult = protocolInfoWindow.ShowDialog();
        Application.Current.MainWindow.Effect = null;
      });
    }

    private void OnSearchWindowClosing(bool closing)
    {
      _isSearchWindowOpen = false;
      MessageEventAdapter.RaiseInfoMessage(string.Empty);
    }

    /// <summary>
    /// Открывает диалог выбора файлов и загружает их в редактор.
    /// </summary>
    public void OpenFileAsync()
    {
      if (_isLockedProvider())
      {
        Message.MessageBoxCustom.Show("В данный момент идёт работа с аппаратурой! Пожалуйста завершите выполнение!", "Ошибка!", MessageBoxButton.OK);
      }
      else
      {
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
          Filter = "Supported files (*.pk;*.pkw;*.opk;*.opkw;*.lst;*.lstw;*.acs;*.txt)|*.pk;*.pkw;*.opk;*.opkw;*.lst;*.lstw;*.acs;*.txt|PK/PKW files (*.pk;*.pkw)|*.pk;*.pkw|OPK/OPKW files (*.opk;*.opkw)|*.opk;*.opkw|Protocol files (*.lst;*.lstw)|*.lst;*.lstw|ACS files (*.acs)|*.acs|Text files (*.txt)|*.txt|All files (*.*)|*.*",
          Title = "Выберите файл",
          Multiselect = true
        };

        if (openFileDialog.ShowDialog() == true)
        {
          foreach (string filePath in openFileDialog.FileNames)
          {
            OpenFileWithLegacyConversion(filePath);
          }
        }
      }
    }

    /// <summary>
    /// Открывает протокол для просмотра.
    /// </summary>
    /// <param name="protocol">Модель протокола, содержащая данные для отображения.</param>
    /// <remarks>
    /// Если приложение находится в заблокированном состоянии — операция не выполняется,
    /// и пользователю отображается сообщение об ошибке.
    /// </remarks>
    public void ViewProtocol(ProtocolModel protocol)
    {
      if (_isLockedProvider())
      {
        Message.MessageBoxCustom.Show("В данный момент идёт работа с аппаратурой! Пожалуйста завершите выполнение!", "Ошибка!", MessageBoxButton.OK);
      }
      else
      {
        _multiWindow.ProtocolViewerService.ViewProtocol(protocol, ProtocolConfig.GetShowProtocolInSoftware());
      }
    }

    /// <summary>
    /// Открывает указанный файл в редакторе.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    public void OpenFileAsync(string filePath)
    {
      if (_isLockedProvider())
      {
        Message.MessageBoxCustom.Show("В данный момент идёт работа с аппаратурой! Пожалуйста завершите выполнение!", "Ошибка!", MessageBoxButton.OK);
      }
      else
      {
        OpenFileWithLegacyConversion(filePath);
      }
    }

    private void OpenFileWithLegacyConversion(string filePath)
    {
      if (string.Equals(Path.GetExtension(filePath), ".opk", StringComparison.OrdinalIgnoreCase))
      {
        var convertedPath = ConvertOpkToOpkwForOpen(filePath);
        if (string.IsNullOrWhiteSpace(convertedPath))
        {
          return;
        }

        _multiWindow.EditorDocumentService.OpenFile(convertedPath);
        return;
      }

      _multiWindow.EditorDocumentService.OpenFile(filePath);
    }

    private string? ConvertOpkToOpkwForOpen(string inputFilePath)
    {
      var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(inputFilePath));
      if (string.IsNullOrWhiteSpace(outputDirectory))
      {
        Message.MessageBoxCustom.Show(
          "Не удалось определить папку для сохранения OPKW-файла.",
          "Открытие OPK",
          MessageBoxButton.OK,
          MessageBoxImage.Error);
        return null;
      }

      var result = _opkToOpkwConverter.Convert(inputFilePath, outputDirectory);
      if (!result.Success || string.IsNullOrWhiteSpace(result.OutputPath))
      {
        Message.MessageBoxCustom.Show(
          result.ErrorMessage ?? "Не удалось преобразовать OPK в OPKW.",
          "Открытие OPK",
          MessageBoxButton.OK,
          MessageBoxImage.Error);
        return null;
      }

      return result.OutputPath;
    }

    /// <summary>
    /// Создаёт новый файл в редакторе.
    /// </summary>
    public void CreateNewFileAsync()
    {
      if (_isLockedProvider())
      {
        Message.MessageBoxCustom.Show("В данный момент идёт работа с аппаратурой! Пожалуйста завершите выполнение!", "Ошибка!", MessageBoxButton.OK);
      }
      else
      {
        _multiWindow.EditorDocumentService.CreateNewFile();
      }
    }

    /// <summary>
    /// Запускает пакетную конвертацию OPK-файлов в PK.
    /// </summary>
    public void ConvertOpkToPk()
    {
      if (_isLockedProvider())
      {
        Message.MessageBoxCustom.Show("В данный момент идёт работа с аппаратурой! Пожалуйста завершите выполнение!", "Ошибка!", MessageBoxButton.OK);
        return;
      }

      var dialog = new OpkToPkConversionWindow
      {
        Owner = _mainWindow,
      };

      try
      {
        _mainWindow.Effect = new BlurEffect { Radius = 8 };
        if (dialog.ShowDialog() != true)
        {
          return;
        }
      }
      finally
      {
        _mainWindow.Effect = null;
      }

      var results = dialog.SelectedFiles
        .Select(path => _opkToPkConverter.Convert(path, dialog.OutputDirectory))
        .ToList();

      foreach (var result in results.Where(item => item.Success && !string.IsNullOrWhiteSpace(item.OutputPath)))
      {
        _multiWindow.EditorDocumentService.OpenFile(result.OutputPath!);
      }

      ShowOpkToPkSummary(results);
    }

    /// <summary>
    /// Запускает пакетную конвертацию OPK-файлов в OPKW.
    /// </summary>
    public void ConvertOpkToOpkw()
    {
      if (_isLockedProvider())
      {
        Message.MessageBoxCustom.Show("В данный момент идёт работа с аппаратурой! Пожалуйста завершите выполнение!", "Ошибка!", MessageBoxButton.OK);
        return;
      }

      var dialog = new OpkToPkConversionWindow("OPKW")
      {
        Owner = _mainWindow,
      };

      try
      {
        _mainWindow.Effect = new BlurEffect { Radius = 8 };
        if (dialog.ShowDialog() != true)
        {
          return;
        }
      }
      finally
      {
        _mainWindow.Effect = null;
      }

      var results = dialog.SelectedFiles
        .Select(path => _opkToOpkwConverter.Convert(path, dialog.OutputDirectory))
        .ToList();

      foreach (var result in results.Where(item => item.Success && !string.IsNullOrWhiteSpace(item.OutputPath)))
      {
        _multiWindow.EditorDocumentService.OpenFile(result.OutputPath!);
      }

      ShowOpkToOpkwSummary(results);
    }

    /// <summary>
    /// Запускает конвертацию старого APK-архива в новый APKW-архив.
    /// </summary>
    public async void ConvertApkToApkw()
    {
      if (_isLockedProvider())
      {
        Message.MessageBoxCustom.Show("В данный момент идёт работа с аппаратурой! Пожалуйста завершите выполнение!", "Ошибка!", MessageBoxButton.OK);
        return;
      }

      var dialog = new ApkToApkwConversionWindow
      {
        Owner = _mainWindow,
      };

      try
      {
        _mainWindow.Effect = new BlurEffect { Radius = 8 };
        if (dialog.ShowDialog() != true)
        {
          return;
        }
      }
      finally
      {
        _mainWindow.Effect = null;
      }

      var result = await RunApkToApkwConversionAsync(dialog.InputFilePath);
      if (!result.Success)
      {
        ShowApkToApkwFailure(result);
        return;
      }

      if (!string.IsNullOrWhiteSpace(result.CreatedArchivePath))
      {
        OpenArchiveControlAndArchive(result.CreatedArchivePath);
      }

      ShowApkToApkwSummary(result);
    }

    /// <summary>
    /// Открывает интерфейс работы с архивами.
    /// </summary>
    public void OpenArchive()
    {
      if (_isLockedProvider())
      {
        Message.MessageBoxCustom.Show("В данный момент идёт работа с аппаратурой! Пожалуйста завершите выполнение!", "Ошибка!", MessageBoxButton.OK);
      }
      else
      {
        _multiWindow.WorkspaceService.AddControl("Архив", new ArchiveControl(), TypeWindow.Files);
      }
    }

    /// <summary>
    /// Инициирует создание нового архива в активном окне архивов.
    /// </summary>
    /// <remarks>
    /// Метод работает только если активный элемент рабочей области — <see cref="ArchiveControl"/>.
    /// Если приложение заблокировано — операция не выполняется.
    /// </remarks>
    public void CreateArchive()
    {
      if (_isLockedProvider())
      {
        Message.MessageBoxCustom.Show("В данный момент идёт работа с аппаратурой! Пожалуйста завершите выполнение!", "Ошибка!", MessageBoxButton.OK);
        return;
      }

      if (_multiWindow.GetActiveWorkspaceControl() is ArchiveControl archiveControl)
      {
        archiveControl.ShowCreateArchiveDialog();
      }
    }

    /// <summary>
    /// Запускает процесс скачивания всех архивов на диск через UI.
    /// </summary>
    /// <remarks>
    /// Открывает диалог выбора папки и выполняет экспорт архивов.
    /// Если приложение заблокировано — операция не выполняется.
    /// </remarks>
    public void DownloadArchives()
    {
      if (_isLockedProvider())
      {
        Message.MessageBoxCustom.Show("В данный момент идёт работа с аппаратурой! Пожалуйста завершите выполнение!", "Ошибка!", MessageBoxButton.OK);
        return;
      }

      ArchiveTransferUiService.DownloadArchives();
    }

    /// <summary>
    /// Запускает процесс загрузки архива в систему через UI.
    /// </summary>
    /// <remarks>
    /// Открывает диалог выбора файла и выполняет импорт архива.
    /// Если приложение заблокировано — операция не выполняется.
    /// </remarks>
    public void UploadArchive()
    {
      if (_isLockedProvider())
      {
        Message.MessageBoxCustom.Show("В данный момент идёт работа с аппаратурой! Пожалуйста завершите выполнение!", "Ошибка!", MessageBoxButton.OK);
        return;
      }

      ArchiveTransferUiService.UploadArchive();
    }

    /// <summary>
    /// Сохраняет текущий файл.
    /// </summary>
    public void SaveFileAsync()
    {
      _multiWindow.EditorDocumentService.SaveFile();
    }

    /// <summary>
    /// Сохраняет текущий файл под другим именем.
    /// </summary>
    public void SaveFileAsAsync()
    {
      _multiWindow.EditorDocumentService.SaveFileAs();
    }

    /// <summary>
    /// Отправляет текущий файл на печать.
    /// </summary>
    public void PrintFileAsync()
    {
      _multiWindow.EditorDocumentService.PrintFile();
    }

    /// <summary>
    /// Закрывает приложение.
    /// </summary>
    public void ExitApplicationAsync()
    {
      Application.Current.Shutdown();
    }

    /// <summary>
    /// Выполняет поиск в текущем файле.
    /// </summary>
    public async Task SearchFileAsync()
    {
      var activeEditor = await EnsureSearchWindowAsync(expandReplaceRow: false, focusReplaceField: false);
      if (activeEditor == null)
      {
        return;
      }

      string selectedText = activeEditor.TextArea.Selection.GetText();

      if (!string.IsNullOrEmpty(selectedText))
      {
        SearchEventAdapter.RaiseSearchTextRequested(selectedText);
      }
    }

    /// <summary>
    /// Открывает единый контрол выбора уже открытых файлов для сравнения.
    /// </summary>
    public void CompareFileAsync()
    {
      var openTextEditors = _multiWindow.GetOpenTextEditors();
      if (openTextEditors.Count <= 1)
      {
        return;
      }

      _multiWindow.WorkspaceService.AddControl(
        "Сравнение файлов",
        new FileCompareSelectionControl(() => _multiWindow.GetOpenTextEditors()),
        TypeWindow.Files);
    }

    private static void ShowOpkToPkSummary(IReadOnlyCollection<ConversionResult> results)
    {
      if (results.Count == 0)
      {
        return;
      }

      var successCount = results.Count(result => result.Success);
      var failedResults = results.Where(result => !result.Success).ToList();

      var summaryLines = new List<string>
      {
        $"Успешно: {successCount}",
        $"С ошибками: {failedResults.Count}",
      };

      var createdFiles = results
        .Where(result => result.Success && !string.IsNullOrWhiteSpace(result.OutputPath))
        .Select(result => $"  {Path.GetFileName(result.OutputPath)}")
        .Take(10)
        .ToList();

      if (createdFiles.Count > 0)
      {
        summaryLines.Add(string.Empty);
        summaryLines.Add("Созданы файлы:");
        summaryLines.AddRange(createdFiles);
      }

      if (failedResults.Count > 0)
      {
        summaryLines.Add(string.Empty);
        summaryLines.Add("Ошибки:");
        summaryLines.AddRange(failedResults
          .Take(10)
          .Select(result => $"  {Path.GetFileName(result.InputPath)}: {result.ErrorMessage}"));
      }

      var icon = successCount == 0
        ? MessageBoxImage.Error
        : failedResults.Count == 0
          ? MessageBoxImage.Information
          : MessageBoxImage.Warning;

      Message.MessageBoxCustom.Show(
        string.Join(Environment.NewLine, summaryLines),
        "Конвертация OPK в PK",
        MessageBoxButton.OK,
        icon);
    }

    private static void ShowOpkToOpkwSummary(IReadOnlyCollection<OpkToOpkwConversionResult> results)
    {
      if (results.Count == 0)
      {
        return;
      }

      var successCount = results.Count(result => result.Success);
      var failedResults = results.Where(result => !result.Success).ToList();

      var summaryLines = new List<string>
      {
        $"Успешно: {successCount}",
        $"С ошибками: {failedResults.Count}",
      };

      var createdFiles = results
        .Where(result => result.Success && !string.IsNullOrWhiteSpace(result.OutputPath))
        .Select(result => $"  {Path.GetFileName(result.OutputPath)}")
        .Take(10)
        .ToList();

      if (createdFiles.Count > 0)
      {
        summaryLines.Add(string.Empty);
        summaryLines.Add("Созданы файлы:");
        summaryLines.AddRange(createdFiles);
      }

      if (failedResults.Count > 0)
      {
        summaryLines.Add(string.Empty);
        summaryLines.Add("Ошибки:");
        summaryLines.AddRange(failedResults
          .Take(10)
          .Select(result => $"  {Path.GetFileName(result.InputPath)}: {result.ErrorMessage}"));
      }

      var icon = successCount == 0
        ? MessageBoxImage.Error
        : failedResults.Count == 0
          ? MessageBoxImage.Information
          : MessageBoxImage.Warning;

      Message.MessageBoxCustom.Show(
        string.Join(Environment.NewLine, summaryLines),
        "Конвертация OPK в OPKW",
        MessageBoxButton.OK,
        icon);
    }

    private async Task<ApkToApkwConversionResult> RunApkToApkwConversionAsync(string inputFilePath)
    {
      var owner = Application.Current?.MainWindow;
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
          "Конвертация APK в APKW",
          "Подготовка конвертации",
          "Проверяем архив, собираем список записей и готовим промежуточные файлы.");

        if (owner != null)
        {
          owner.Effect = new BlurEffect { Radius = 8 };
        }

        progressWindow.Show();
        await WaitForProgressWindowAsync(progressWindow);

        var progress = new Progress<ApkToApkwProgressInfo>(info =>
        {
          progressWindow.SetProgress(info.Percent);

          var status = info.TotalEntries > 0
            ? $"{info.Stage} ({System.Math.Min(info.ProcessedEntries, info.TotalEntries)}/{info.TotalEntries})"
            : info.Stage;

          progressWindow.SetStage(status, info.Hint);
        });

        return await _apkToApkwConverter.ConvertAsync(inputFilePath, progress);
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

    private static async Task WaitForProgressWindowAsync(ProgressWindow progressWindow)
    {
      await progressWindow.Dispatcher.InvokeAsync(
        progressWindow.UpdateLayout,
        DispatcherPriority.Background);

      await progressWindow.Dispatcher.InvokeAsync(
        progressWindow.UpdateLayout,
        DispatcherPriority.Render);

      await progressWindow.Dispatcher.InvokeAsync(
        () => { },
        DispatcherPriority.ContextIdle);
    }

    private void OpenArchiveControlAndArchive(string archivePath)
    {
      if (_multiWindow.GetActiveWorkspaceControl() is not ArchiveControl archiveControl)
      {
        _multiWindow.WorkspaceService.AddControl("Архив", new ArchiveControl(), TypeWindow.Files);
        archiveControl = _multiWindow.GetActiveWorkspaceControl() as ArchiveControl;
      }

      if (archiveControl == null)
      {
        return;
      }

      if (File.Exists(archivePath))
      {
        _ = archiveControl.OpenArchivePathAsync(archivePath);
        return;
      }

      if (Directory.Exists(archivePath))
      {
        _ = archiveControl.OpenReviewArchivePathAsync(archivePath);
      }
    }

    private static void ShowApkToApkwSummary(ApkToApkwConversionResult result)
    {
      var summaryLines = new List<string>
      {
        $"Создан архив: {Path.GetFileName(result.CreatedArchivePath)}",
        $"Записей перенесено: {result.EntriesCount}",
      };

      Message.MessageBoxCustom.Show(
        string.Join(Environment.NewLine, summaryLines),
        "Конвертация APK в APKW",
        MessageBoxButton.OK,
        MessageBoxImage.Information);
    }

    private void ShowApkToApkwFailure(ApkToApkwConversionResult result)
    {
      var message = result.ErrorMessage ?? "Не удалось выполнить конвертацию APK в APKW.";
      var hasIntermediateDirectory = !string.IsNullOrWhiteSpace(result.IntermediateDirectoryPath)
        && Directory.Exists(result.IntermediateDirectoryPath);

      if (hasIntermediateDirectory)
      {
        OpenArchiveControlAndArchive(result.IntermediateDirectoryPath!);
      }

      if (hasIntermediateDirectory)
      {
        message += Environment.NewLine
          + Environment.NewLine
          + "Архив на проверке открыт во вкладке архивов.";

        message += Environment.NewLine
          + Environment.NewLine
          + $"Открыть эту папку в проводнике?{Environment.NewLine}{result.IntermediateDirectoryPath}";
      }

      var dialogResult = Message.MessageBoxCustom.Show(
        message,
        "Конвертация APK в APKW",
        hasIntermediateDirectory ? MessageBoxButton.YesNo : MessageBoxButton.OK,
        MessageBoxImage.Error);

      if (dialogResult == MessageBoxResult.Yes && hasIntermediateDirectory)
      {
        Process.Start(new ProcessStartInfo
        {
          FileName = "explorer.exe",
          Arguments = $"\"{result.IntermediateDirectoryPath}\"",
          UseShellExecute = true
        });
      }
    }

    /// <summary>
    /// Создает новый файл трансляции (.opkw) в редакторе.
    /// </summary>
    public ITextEditorView CreateTranslationFileAsync(string parentFilePath)
    {
      if (_isLockedProvider())
      {
        Message.MessageBoxCustom.Show("В данный момент идёт работа с аппаратурой! Пожалуйста завершите выполнение!", "Ошибка!", MessageBoxButton.OK, image: MessageBoxImage.Error);
        return null;
      }
      else
      {
        return _multiWindow.TranslationService.CreateTranslationFile(parentFilePath);
      }
    }
    internal void OpenFolder() => _multiWindow.EditorDocumentService.OpenFolder();

    /// <summary>
    /// Открывает окно поиска сразу с раскрытой строкой замены.
    /// </summary>
    public async Task SearchReplaceFileAsync()
    {
      string selectedText = null;
      // вычислим заранее, т.к. EnsureSearchWindowAsync может менять фокус
      var editorForSelection = _multiWindow.GetActiveTextEditor();
      if (editorForSelection != null)
      {
        selectedText = editorForSelection.TextArea.Selection.GetText();
      }

      bool focusReplaceField = _mainWindow.SearchWindow.HasSearchText() || !string.IsNullOrEmpty(selectedText);

      var activeEditor = await EnsureSearchWindowAsync(expandReplaceRow: true, focusReplaceField: focusReplaceField);
      if (activeEditor == null)
      {
        return;
      }

      if (string.IsNullOrEmpty(selectedText))
      {
        selectedText = activeEditor.TextArea.Selection.GetText();
        if (!string.IsNullOrEmpty(selectedText))
        {
          focusReplaceField = true;
        }
      }

      if (!string.IsNullOrEmpty(selectedText))
      {
        SearchEventAdapter.RaiseSearchTextRequested(selectedText);
      }
      if (focusReplaceField)
      {
        _mainWindow.SearchWindow.FocusReplaceField();
      }
    }

    private async Task<TextEditorUI?> EnsureSearchWindowAsync(bool expandReplaceRow, bool focusReplaceField)
    {
      var activeEditor = _multiWindow.GetActiveTextEditor();
      if (activeEditor == null)
      {
        return null;
      }

      if (!_isSearchWindowOpen)
      {
        _mainWindow.SearchWindow.Owner = _mainWindow;
        if (!_selectFileHandlerAttached)
        {
          _mainWindow.SearchWindow.SelectFileForSearch += OpenFileAsync;
          _selectFileHandlerAttached = true;
        }
        _isSearchWindowOpen = true;
      }

      if (!expandReplaceRow && _mainWindow.SearchWindow.IsReplaceExpanded)
      {
        await _mainWindow.SearchWindow.CollapseReplaceRowAsync();
      }

      await _mainWindow.SearchWindow.ShowWindow(expandReplaceRow, focusReplaceField);
      return activeEditor;
    }
  }
}
