using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Protocol;
using Microsoft.Win32;
using System.Windows;
using UI.Components.FileComparerControls;
using UI.Controls.ProtocolNew;
using UI.Controls.Search;
using UI.Controls.TextEditor;


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
            _multiWindow.OpenFile(filePath);
          }
        }
      }
    }

    public void ViewProtocol(ProtocolModel protocol)
    {
      if (_isLockedProvider())
      {
        Message.MessageBoxCustom.Show("В данный момент идёт работа с аппаратурой! Пожалуйста завершите выполнение!", "Ошибка!", MessageBoxButton.OK);
      }
      else
      {
        _multiWindow.ViewProtocol(protocol, ProtocolConfig.GetShowProtocolInSoftware());
      }
    }

    /// <summary>
    /// Открывает диалог выбора файла и загружает его в редактор.
    /// </summary>
    public void OpenFileAsync(string filePath)
    {
      if (_isLockedProvider())
      {
        Message.MessageBoxCustom.Show("В данный момент идёт работа с аппаратурой! Пожалуйста завершите выполнение!", "Ошибка!", MessageBoxButton.OK);
      }
      else
      {
        _multiWindow.OpenFile(filePath);
      }
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
        _multiWindow.CreateNewFile();
      }
    }

    /// <summary>
    /// Сохраняет текущий файл.
    /// </summary>
    public void SaveFileAsync()
    {
      _multiWindow.SaveFile();
    }

    /// <summary>
    /// Сохраняет текущий файл под другим именем.
    /// </summary>
    public void SaveFileAsAsync()
    {
      _multiWindow.SaveFileAs();
    }

    /// <summary>
    /// Отправляет текущий файл на печать.
    /// </summary>
    public void PrintFileAsync()
    {
      _multiWindow.PrintFile();
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
    /// Выполняет операцию сравнения файлов.
    /// </summary>
    public async Task CompareFileAsync()
    {
      _mainWindow.Effect = new System.Windows.Media.Effects.BlurEffect();
      var fileCompareWindow = new FileCompareWindow();
      fileCompareWindow.DialogClosed += Dialog_Closed;
      fileCompareWindow.Owner = _mainWindow;
      fileCompareWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
      fileCompareWindow.ShowDialog();
      _mainWindow.Effect = null;
    }

    private async void Dialog_Closed(object sender, EventArgs e)
    {
      _mainWindow.Effect = null;
    }

    /// <summary>
    /// Создает новый файл трансляции (.opkw) в редакторе.
    /// </summary>
    public TextEditorUI CreateTranslationFileAsync(string parentFilePath)
    {
      if (_isLockedProvider())
      {
        Message.MessageBoxCustom.Show("В данный момент идёт работа с аппаратурой! Пожалуйста завершите выполнение!", "Ошибка!", MessageBoxButton.OK, image: MessageBoxImage.Error);
        return null;
      }
      else
      {
        return _multiWindow.CreateTranslationFileAsync(parentFilePath);
      }
    }
    internal void OpenFolder() => _multiWindow.OpenFolder();

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
