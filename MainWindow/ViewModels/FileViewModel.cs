using CommunityToolkit.Mvvm.Input;
using MainWindowProgram.Services;
using System.Threading.Tasks;

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
    private async Task OpenFile() => await _fileService.OpenFileAsync();

    /// <summary>Команда сохранения файла.</summary>
    [RelayCommand]
    private async Task SaveFile() => await _fileService.SaveFileAsync();

    /// <summary>Команда сохранения файла под другим именем.</summary>
    [RelayCommand]
    private async Task SaveFileAs() => await _fileService.SaveFileAsAsync();

    /// <summary>Команда открытия папки.</summary>
    [RelayCommand]
    private async Task OpenFolder() => await _fileService.OpenFolder();

    /// <summary>Команда печати файла.</summary>
    [RelayCommand]
    private async Task PrintFile() => await _fileService.PrintFileAsync();

    /// <summary>Команда завершения работы приложения.</summary>
    [RelayCommand]
    private async Task ExitApplication() => await _fileService.ExitApplicationAsync();

    /// <summary>Команда поиска по файлу.</summary>
    [RelayCommand]
    private async Task SearchFile() => await _fileService.SearchFileAsync();

    /// <summary>Команда сравнения файлов.</summary>
    [RelayCommand]
    private async Task CompareFile() => await _fileService.CompareFileAsync();

    /// <summary>Команда открытия архива.</summary>
    [RelayCommand]
    private async Task OpenArchive() => await _fileService.OpenArchiveAsync();

    /// <summary>Команда создания нового файла.</summary>
    [RelayCommand]
    private async Task CreateNewFile() => await _fileService.CreateNewFileAsync();
  }
}
