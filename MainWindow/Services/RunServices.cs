using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.Metadata.Static;
using System.Windows.Controls;
using UI.Controls.Runner;

namespace MainWindowProgram.Services
{
  public class RunServices
  {
    /// <summary>
    /// Сервис для управления многооконным интерфейсом.
    /// </summary>
    private readonly MultiWindowService _multiWindow;

    /// <summary>
    /// Сервис для работы с файлами.
    /// </summary>
    private readonly FileService _fileService;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="AdminServices"/>.
    /// </summary>
    /// <param name="multiWindow">Сервис управления многооконным интерфейсом.</param>
    /// <param name="fileService">Сервис  для работы с файлами.</param>
    public RunServices(MultiWindowService multiWindow, FileService fileService)
    {
      _multiWindow = multiWindow;
      _fileService = fileService;

      EventAggregator.Unsubscribe<FileInteractionEvents.OpenFileInEditorAgain>(e => OpenFileCommand(e.FilePath));
      EventAggregator.Subscribe<FileInteractionEvents.OpenFileInEditorAgain>(e => OpenFileCommand(e.FilePath));

      EventAggregator.Unsubscribe<EditorEvents.CloseRunItem>(e => CloseRunItem(e.RunControl));
      EventAggregator.Subscribe<EditorEvents.CloseRunItem>(e => CloseRunItem(e.RunControl));
    }

    public async void OpenFileCommand(string filePath)
    {
      await _fileService.OpenFileAsync(filePath);
    }

    public async void CloseRunItem(UserControl userControl)
    {
      if (userControl != null && userControl is RunControl runControl)
      {
        await _multiWindow.CloseRunItem(runControl, EditorType.Run);
      }
    }
  }
}
