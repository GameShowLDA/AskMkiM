using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Services.FileFormats.Apk;
using Ask.Core.Services.FileFormats.Opk;
using Ask.Core.Services.FilesUtility;
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
using Ask.UI.Features.Archive.Views;
using UI.Controls.FileCompare;
using UI.Controls.Search;
using UI.Controls.TextEditorControl;
using Ask.UI.Features.Archive.Services;

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
          Multiselect = true,
          InitialDirectory = LastDirectoryService.GetLastDirectory()
        };

        if (openFileDialog.ShowDialog() == true)
        {
          string? directory =Path.GetDirectoryName(openFileDialog.FileName);

          if (!string.IsNullOrWhiteSpace(directory))
          {
            LastDirectoryService.SaveLastDirectory(directory);
          }

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

    /// <summary>
    /// Запускает выполнение программы контроля через внешнюю старую программу АСК-МКИ.
    /// Пользователь выбирает путь к mkiw.exe и файл программы контроля (*.acs, *.pk)
    /// в отдельном окне параметров. После завершения выполнения метод ищет последний
    /// сформированный протокол в папке HISTORY рядом с mkiw.exe и открывает его
    /// во внутреннем редакторе приложения.
    /// </summary>
    public async Task RunAskMkiAsync()
    {
      if (_isLockedProvider())
      {
        Message.MessageBoxCustom.Show(
          "В данный момент идёт работа с аппаратурой! Пожалуйста завершите выполнение!",
          "Ошибка!",
          MessageBoxButton.OK);

        return;
      }

      var dialog = new RunAskMkiWindow
      {
        Owner = _mainWindow
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

      try
      {
        var protocolPath = await RunMkiAndGetProtocolWithProgressAsync(dialog.MkiPath, dialog.ProgramPath);

        OpenFileAsync(protocolPath);
      }
      catch (Exception ex)
      {
        Message.MessageBoxCustom.Show(
          ex.Message,
          "Ошибка выполнения АСК-МКИ",
          MessageBoxButton.OK,
          MessageBoxImage.Error);
      }
    }

    /// <summary>
    /// Выполняет запуск mkiw.exe в скрытом режиме с аргументом /runheadless
    /// и возвращает путь к последнему сформированному протоколу.
    /// </summary>
    /// <param name="mkiPath">Полный путь к исполняемому файлу mkiw.exe.</param>
    /// <param name="programPath">Полный путь к программе контроля (*.acs или *.pk).</param>
    /// <param name="timeoutSeconds">Максимальное время ожидания завершения процесса в секундах.</param>
    /// <returns>Полный путь к найденному файлу протокола.</returns>
    /// <exception cref="FileNotFoundException">
    /// Возникает, если mkiw.exe, программа контроля или сформированный протокол не найдены.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Возникает, если не удалось запустить процесс или mkiw.exe завершилась с ошибкой.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// Возникает, если mkiw.exe выполнялась дольше заданного времени ожидания.
    /// </exception>
    private static async Task<string> RunMkiAndGetProtocolAsync(
      string mkiPath,
      string programPath,
      int timeoutSeconds = 30)
    {
      if (!File.Exists(mkiPath))
        throw new FileNotFoundException($"mkiw.exe не найден: {mkiPath}", mkiPath);

      if (!File.Exists(programPath))
        throw new FileNotFoundException($"Программа контроля не найдена: {programPath}", programPath);

      var mkiDir = Path.GetDirectoryName(mkiPath);

      if (string.IsNullOrWhiteSpace(mkiDir))
        throw new InvalidOperationException("Не удалось определить папку mkiw.exe.");

      var historyRoot = Path.Combine(mkiDir, "HISTORY");

      var startedUtc = DateTime.UtcNow.AddSeconds(-2);

      using var process = new Process();

      process.StartInfo = new ProcessStartInfo
      {
        FileName = mkiPath,
        WorkingDirectory = mkiDir,
        UseShellExecute = false,
        CreateNoWindow = true,
        WindowStyle = ProcessWindowStyle.Hidden
      };

      process.StartInfo.ArgumentList.Add("/runheadless");
      process.StartInfo.ArgumentList.Add(programPath);

      if (!process.Start())
        throw new InvalidOperationException("Не удалось запустить mkiw.exe.");

      using var cancellationTokenSource = new CancellationTokenSource(
        TimeSpan.FromSeconds(timeoutSeconds));

      try
      {
        await process.WaitForExitAsync(cancellationTokenSource.Token);
      }
      catch (OperationCanceledException)
      {
        try
        {
          if (!process.HasExited)
            process.Kill(entireProcessTree: true);
        }
        catch
        {
          // Ошибку принудительного завершения процесса игнорируем.
        }

        throw new TimeoutException($"mkiw.exe работала больше {timeoutSeconds} секунд.");
      }

      if (process.ExitCode != 0)
        throw new InvalidOperationException($"mkiw.exe завершилась с кодом {process.ExitCode}.");

      var protocolPath = FindLatestProtocol(historyRoot, startedUtc);

      if (protocolPath == null)
        throw new FileNotFoundException($"Протокол не найден в папке: {historyRoot}");

      return protocolPath;
    }

    /// <summary>
    /// Находит последний сформированный файл протокола в папке HISTORY.
    /// Сначала ищет файл, изменённый после запуска mkiw.exe,
    /// затем, если такой файл не найден, возвращает самый свежий файл в HISTORY.
    /// </summary>
    /// <param name="historyRoot">Путь к корневой папке HISTORY.</param>
    /// <param name="startedUtc">Время запуска mkiw.exe в UTC.</param>
    /// <returns>
    /// Полный путь к найденному протоколу или <c>null</c>, если подходящих файлов нет.
    /// </returns>
    private static string? FindLatestProtocol(string historyRoot, DateTime startedUtc)
    {
      if (!Directory.Exists(historyRoot))
        return null;

      var files = Directory
        .EnumerateFiles(historyRoot, "*.*", SearchOption.AllDirectories)
        .Select(path => new
        {
          Path = path,
          LastWriteUtc = File.GetLastWriteTimeUtc(path)
        })
        .OrderByDescending(file => file.LastWriteUtc)
        .ToList();

      var freshFile = files
        .Where(file => file.LastWriteUtc >= startedUtc)
        .Select(file => file.Path)
        .FirstOrDefault();

      if (freshFile != null)
        return freshFile;

      return files
        .Select(file => file.Path)
        .FirstOrDefault();
    }

    /// <summary>
    /// Выполняет запуск старой программы АСК-МКИ с отображением окна прогресса.
    /// Окно показывается на время ожидания завершения mkiw.exe и поиска протокола.
    /// </summary>
    /// <param name="mkiPath">Полный путь к mkiw.exe.</param>
    /// <param name="programPath">Полный путь к программе контроля (*.acs или *.pk).</param>
    /// <returns>Полный путь к сформированному протоколу.</returns>
    private async Task<string> RunMkiAndGetProtocolWithProgressAsync(
      string mkiPath,
      string programPath)
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
          "Выполнение АСК-МКИ OLD",
          "Запуск программы контроля",
          $"Выполняется файл: {Path.GetFileName(programPath)}");

        if (owner != null)
        {
          owner.Effect = new BlurEffect { Radius = 8 };
        }

        progressWindow.Show();

        await WaitForProgressWindowAsync(progressWindow);

        progressWindow.SetStage(
          "Ожидание завершения mkiw.exe",
          "Формирование протокола.");

        var protocolPath = await RunMkiAndGetProtocolAsync(mkiPath, programPath);

        progressWindow.SetStage(
          "Протокол сформирован",
          $"Открываем протокол: {Path.GetFileName(protocolPath)}");

        return protocolPath;
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

    /// <summary>
    /// Запускает текущий открытый файл программы контроля через старую программу АСК-МКИ.
    /// Путь к mkiw.exe берётся из настроек конфигурации АСК-МКИ.
    /// После выполнения открывает сформированный протокол во внутреннем редакторе приложения.
    /// </summary>
    public async Task RunAskMkiActiveFileAsync()
    {
      if (_isLockedProvider())
      {
        Message.MessageBoxCustom.Show(
          "В данный момент идёт работа с аппаратурой! Пожалуйста завершите выполнение!",
          "Ошибка!",
          MessageBoxButton.OK,
          MessageBoxImage.Error);

        return;
      }

      var activeEditor = _multiWindow.GetActiveTextEditor();

      if (activeEditor == null)
      {
        Message.MessageBoxCustom.Show(
          "Нет открытого файла для выполнения.",
          "Выполнение АСК-МКИ",
          MessageBoxButton.OK,
          MessageBoxImage.Warning);

        return;
      }

      var programPath = activeEditor.TextEditorModel?.FilePath;

      if (string.IsNullOrWhiteSpace(programPath) || !File.Exists(programPath))
      {
        Message.MessageBoxCustom.Show(
          "Текущий файл не найден на диске. Сначала сохраните файл.",
          "Выполнение АСК-МКИ",
          MessageBoxButton.OK,
          MessageBoxImage.Warning);

        return;
      }

      var extension = Path.GetExtension(programPath);

      if (!string.Equals(extension, ".acs", StringComparison.OrdinalIgnoreCase)
          && !string.Equals(extension, ".pk", StringComparison.OrdinalIgnoreCase))
      {
        Message.MessageBoxCustom.Show(
          "Через АСК-МКИ OLD можно выполнять только файлы *.acs или *.pk.",
          "Выполнение АСК-МКИ",
          MessageBoxButton.OK,
          MessageBoxImage.Warning);

        return;
      }

      var mkiPath = LegacyMkiConfig.GetMkiPath();

      if (string.IsNullOrWhiteSpace(mkiPath) || !File.Exists(mkiPath))
      {
        Message.MessageBoxCustom.Show(
          "Путь к mkiw.exe не задан или файл не найден. Укажите путь в параметрах: Конфигурация АСК-МКИ.",
          "Выполнение АСК-МКИ",
          MessageBoxButton.OK,
          MessageBoxImage.Warning);

        return;
      }

      try
      {
        // Чтобы старая программа выполняла актуальную версию файла.
        _multiWindow.EditorDocumentService.SaveFile();

        var protocolPath = await RunMkiAndGetProtocolWithProgressAsync(mkiPath, programPath);

        OpenFileAsync(protocolPath);
      }
      catch (Exception ex)
      {
        Message.MessageBoxCustom.Show(
          ex.Message,
          "Ошибка выполнения АСК-МКИ",
          MessageBoxButton.OK,
          MessageBoxImage.Error);
      }
    }
  }
}
