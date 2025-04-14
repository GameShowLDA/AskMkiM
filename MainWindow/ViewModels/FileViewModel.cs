using System.Windows.Input;
using MainWindowProgram.Infrastructure;
using MainWindowProgram.Services;

namespace MainWindowProgram.ViewModels
{
  /// <summary>
  /// ViewModel для работы с файлами.
  /// Содержит команды для открытия, создания, сохранения, печати, поиска, сравнения и выхода из приложения.
  /// </summary>
  public class FileViewModel
  {
    /// <summary>
    /// Сервис для работы с файлами.
    /// </summary>
    private readonly FileService _fileService;

    /// <summary>
    /// Команда открытия файла.
    /// </summary>
    public ICommand OpenFileCommand { get; }

    /// <summary>
    /// Команда открытия архива.
    /// </summary>
    public ICommand OpenArchiveCommand { get; }

    /// <summary>
    /// Команда сохранения файла.
    /// </summary>
    public ICommand SaveFileCommand { get; }

    /// <summary>
    /// Команда сохранения файла под другим именем.
    /// </summary>
    public ICommand SaveFileAsCommand { get; }

    /// <summary>
    /// Команда отправки файла на печать.
    /// </summary>
    public ICommand PrintFileCommand { get; }

    /// <summary>
    /// Команда завершения работы приложения.
    /// </summary>
    public ICommand ExitApplicationCommand { get; }

    /// <summary>
    /// Команда поиска по содержимому файла.
    /// </summary>
    public ICommand SearchFileCommand { get; }

    /// <summary>
    /// Команда сравнения файлов.
    /// </summary>
    public ICommand CompareFileCommand { get; }

    /// <summary>
    /// Команда создания нового файла.
    /// </summary>
    public ICommand CreateNewFileCommand { get; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="FileViewModel"/>.
    /// </summary>
    /// <param name="fileService">Сервис для работы с файлами.</param>
    public FileViewModel(FileService fileService)
    {
      _fileService = fileService;

      OpenFileCommand = new AsyncRelayCommand(_fileService.OpenFileAsync);
      SaveFileCommand = new AsyncRelayCommand(_fileService.SaveFileAsync);
      SaveFileAsCommand = new AsyncRelayCommand(_fileService.SaveFileAsAsync);
      PrintFileCommand = new AsyncRelayCommand(_fileService.PrintFileAsync);
      ExitApplicationCommand = new AsyncRelayCommand(_fileService.ExitApplicationAsync);
      SearchFileCommand = new AsyncRelayCommand(_fileService.SearchFileAsync);
      CompareFileCommand = new AsyncRelayCommand(_fileService.CompareFileAsync);
      OpenArchiveCommand = new AsyncRelayCommand(_fileService.OpenArchiveAsync);
      CreateNewFileCommand = new AsyncRelayCommand(_fileService.CreateNewFileAsync);
    }
  }
}
