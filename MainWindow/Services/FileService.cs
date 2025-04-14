using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AppConfiguration.Base;
using MainWindowProgram.Infrastructure;
using Microsoft.Win32;
using UI.Components.ArchiveControls;
using UI.Components.ArchiveManager.Models;
using UI.Controls.Search;
using UI.Controls.TextEditor;
using static UI.Components.Invoke.OpenFileButton;

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
    /// Делегат, предоставляющий актуальное знанчение состояния блокировки приложения.
    /// </summary>
    private readonly Func<bool> _isLockedProvider;

    private bool _isSearchWindowOpen;

    private Action SearchWindowClosing;

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
      EventAggregator.SearchWindowClosing += OnSearchWindowClosing;
    }

    private void OnSearchWindowClosing(bool closing)
    {
      _isSearchWindowOpen = false;
      EventAggregator.RaiseInfoMessage(string.Empty);
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
          Filter = "Text files (*.txt)|*.txt|RTF files (*.rtf)|*.rtf|PK files (*.pk, *.Pk, *.PK)|*.pk; *.Pk; *.PK",
          Title = "Выберите текстовый файл",
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
      if (_isSearchWindowOpen == false)
      {
        _mainWindow.SearchWindow.Owner = _mainWindow;
        _mainWindow.SearchWindow.SelectFileForSearch += OpenFileAsync;
        _mainWindow.SearchWindow.ShowWindow();
        _isSearchWindowOpen = true;
      }

      TextEditorUI activeEditor = await _multiWindow.GetActiveTextEditor();
      string selectedText = activeEditor?.TextArea.Selection.GetText();

      if (!string.IsNullOrEmpty(selectedText))
      {
        EventAggregator.RaiseSearchTextRequested(selectedText);
      }
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
    public async Task OpenArchiveAsync()
    {
      var allArchives = new TableAllArchivesControl();
      await _multiWindow.AddControlAsync("Архив", allArchives, TypeWindow.Files);
      allArchives.ArchiveSelected += ArchiveControl_ArchiveSelected;
    }

    private async void ArchiveControl_ArchiveSelected(object sender, MouseButtonEventArgs e)
    {
      var dataGrid = e.Source as DataGrid;
      if (dataGrid?.SelectedItem is ApkArchive selectedArchive)
      {
        if (selectedArchive != null)
        {
          var archiveName = selectedArchive.ArchiveName;
          await _multiWindow.AddControlAsync(archiveName, new TableApkArchiveControl(archiveName), TypeWindow.Files);
        }
      }
    }
  }
}
