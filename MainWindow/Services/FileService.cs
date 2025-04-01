using System.Windows;
using System.Windows.Input;
using MainWindowProgram.Infrastructure;
using Microsoft.Win32;

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

    /// <summary>
    /// Делегат, предоставляющий актуальное значение состояния блокировки приложения.
    /// </summary>
    private readonly Func<bool> _isLockedProvider;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="FileService"/>.
    /// </summary>
    /// <param name="multiWindow">Сервис для работы с окнами редакторов.</param>
    /// <param name="isLockedProvider">Функция, возвращающая признак блокировки интерфейса.</param>
    public FileService(MultiWindowService multiWindow, Func<bool> isLockedProvider)
    {
      _multiWindow = multiWindow;
      _isLockedProvider = isLockedProvider;
    }

    /// <summary>
    /// Открывает диалог выбора файла и загружает его в редактор.
    /// </summary>
    public async Task OpenFileAsync()
    {
      if (_isLockedProvider())
      {
        MessageBox.Show("В данный момент идёт работа с аппаратурой! Пожалуйста завершите выполнение!", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
      }
      else
      {
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
          Filter = "All files (*.*)|*.*|Text files (*.txt)|*.txt|RTF files (*.rtf)|*.rtf",
          Title = "Выберите текстовый файл"
        };

        if (openFileDialog.ShowDialog() == true)
        {
          string filePath = openFileDialog.FileName;
          await _multiWindow.OpenFileInEditor(filePath);
        }
      }
    }

    /// <summary>
    /// Создаёт новый файл в редакторе.
    /// </summary>
    public async Task CreateNewFileAsync()
    {
      if (_isLockedProvider())
      {
        MessageBox.Show("В данный момент идёт работа с аппаратурой! Пожалуйста завершите выполнение!", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
      }
      else
      {
        await _multiWindow.CreateNewFile();
      }
    }

    /// <summary>
    /// Сохраняет текущий файл.
    /// </summary>
    public async Task SaveFileAsync()
    {
      await _multiWindow.SaveFile();
    }

    /// <summary>
    /// Сохраняет текущий файл под другим именем.
    /// </summary>
    public async Task SaveFileAsAsync()
    {
      await _multiWindow.SaveFileAs();
    }

    /// <summary>
    /// Отправляет текущий файл на печать.
    /// </summary>
    public async Task PrintFileAsync()
    {
      await _multiWindow.PrintFile();
    }

    /// <summary>
    /// Закрывает приложение.
    /// </summary>
    public async Task ExitApplicationAsync()
    {
      Application.Current.Shutdown();
      await Task.CompletedTask;
    }

    /// <summary>
    /// Выполняет поиск в текущем файле.
    /// </summary>
    public async Task SearchFileAsync()
    {
      throw new Exception("Настроить поиск в файле");
    }

    /// <summary>
    /// Выполняет операцию сравнения файлов.
    /// </summary>
    public async Task CompareFileAsync()
    {
      throw new Exception("Настроить сравнение файлов");
    }

    /// <summary>
    /// Открывает диалог выбора архива и загружает его в редактор.
    /// </summary>
    public Task OpenArchiveAsync()
    {
      throw new Exception("Настроить открытие архива");
    }
  }
}
