using CommunityToolkit.Mvvm.Input;
using MainWindowProgram.Services;

namespace MainWindowProgram.ViewModels
{
  /// <summary>
  /// ViewModel для работы с файлами.
  /// Содержит команды для открытия, создания, сохранения, печати, поиска, сравнения и выхода из приложения.
  /// </summary>
  public partial class FileViewModel
  {
    private readonly FileService _fileService;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="FileViewModel"/>.
    /// </summary>
    public FileViewModel(FileService fileService)
    {
      _fileService = fileService;
    }

    /// <summary>Команда открытия файла.</summary>
    [RelayCommand]
    private void OpenFile() => _fileService.OpenFileAsync();

    /// <summary>Команда сохранения файла.</summary>
    [RelayCommand]
    private void SaveFile() => _fileService.SaveFileAsync();

    /// <summary>Команда сохранения файла под другим именем.</summary>
    [RelayCommand]
    private void SaveFileAs() => _fileService.SaveFileAsAsync();

    /// <summary>Команда открытия папки.</summary>
    [RelayCommand]
    private void OpenFolder() => _fileService.OpenFolder();

    /// <summary>Команда печати файла.</summary>
    [RelayCommand]
    private void PrintFile() => _fileService.PrintFileAsync();

    /// <summary>Команда завершения работы приложения.</summary>
    [RelayCommand]
    private void ExitApplication() => _fileService.ExitApplicationAsync();

    /// <summary>Команда поиска по файлу.</summary>
    [RelayCommand]
    private async Task SearchFile() => await _fileService.SearchFileAsync();

    /// <summary>Команда открытия окна поиска в режиме замены.</summary>
    [RelayCommand]
    private async Task SearchReplaceFile() => await _fileService.SearchReplaceFileAsync();

    /// <summary>Команда открытия выбора файлов для сравнения.</summary>
    [RelayCommand]
    private void CompareFile() => _fileService.CompareFileAsync();

    /// <summary>Команда открытия экрана архива.</summary>
    [RelayCommand]
    private void OpenArchive() => _fileService.OpenArchive();

    /// <summary>Команда создания архива на активной вкладке архива.</summary>
    [RelayCommand]
    private void CreateArchive() => _fileService.CreateArchive();

    /// <summary>Команда выгрузки всех архивов на диск.</summary>
    [RelayCommand]
    private void DownloadArchives() => _fileService.DownloadArchives();

    /// <summary>Команда загрузки архива в папку Archives.</summary>
    [RelayCommand]
    private void UploadArchive() => _fileService.UploadArchive();

    /// <summary>Команда создания нового файла.</summary>
    [RelayCommand]
    private void CreateNewFile() => _fileService.CreateNewFileAsync();

    /// <summary>Команда конвертации OPK в PK.</summary>
    [RelayCommand]
    private void ConvertOpkToPk() => _fileService.ConvertOpkToPk();

    /// <summary>Команда конвертации OPK в OPKW.</summary>
    [RelayCommand]
    private void ConvertOpkToOpkw() => _fileService.ConvertOpkToOpkw();

    /// <summary>Команда конвертации APK в APKW.</summary>
    [RelayCommand]
    private void ConvertApkToApkw() => _fileService.ConvertApkToApkw();

    /// <summary>Команда конвертации APKW в APK.</summary>
    [RelayCommand]
    private void ConvertApkwToApk() => _fileService.ConvertApkwToApk();

    /// <summary>Команда выполнения АСК-МКИ через старую программу mkiw.exe.</summary>
    [RelayCommand]
    private async Task RunAskMki() => await _fileService.RunAskMkiAsync();

    /// <summary>Команда выполнения текущего открытого файла через старую программу АСК-МКИ.</summary>
    [RelayCommand]
    private async Task RunAskMkiActiveFile() => await _fileService.RunAskMkiActiveFileAsync();
  }
}
