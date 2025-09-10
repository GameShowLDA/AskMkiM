using AppConfiguration.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using UI.Controls.Runner;
using UI.Controls.TextEditor;

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
      EventAggregator.OpenFileInEditorAgain -= OpenFileCommand;
      EventAggregator.OpenFileInEditorAgain += OpenFileCommand;
      EventAggregator.CloseRunItem -= CloseRunItem;
      EventAggregator.CloseRunItem += CloseRunItem;
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
