using System.Windows;
using System.Windows.Controls;
using static AppConfiguration.Admin.AdminConfig;
using static AppConfiguration.SystemState.SystemStateManager;
using static Utilities.LoggerUtility;


namespace AppConfiguration.Base
{
  /// <summary>
  /// Статический класс, предоставляющий централизованный механизм управления событиями для взаимодействия компонентов приложения.
  /// Используется для оповещения об изменении состояния питания, блокировки, прав администратора, а также для вывода сообщений различного уровня (ошибки, предупреждения, информация).
  /// </summary>
  public static class EventAggregator
  {
    /// <summary>
    /// Событие, которое вызывается при изменении состояния питания.
    /// </summary>
    static public event Action<bool> PowerChanged;

    /// <summary>
    /// Событие, которое вызывается при изменении состояния блокировки.
    /// </summary>
    static public event Action<bool> LockedChanged;

    /// <summary>
    /// Событие, которое вызывается при возникновении ошибки.
    /// </summary>
    static public event Action<string, bool> ErrorMessageEvent;

    /// <summary>
    /// Событие, которое вызывается при возникновении предупреждения.
    /// </summary>
    static public event Action<string, bool> WarningMessageEvent;

    /// <summary>
    /// Событие, которое вызывается при возникновении информационного сообщения.
    /// </summary>
    static public event Action<string, bool> InfoMessageEvent;

    /// <summary>
    /// Событие, которое вызывается при отчистке информационного сообщения.
    /// </summary>
    static public event Action ClearMessageEvent;

    /// <summary>
    /// Событие, которое вызывается при изменении состояния подключения USB устройства.
    /// </summary>
    static public event Action<bool> AdminRightsChanged;

    /// <summary>
    /// Событие, которое вызывается, когда активно окно типа TextEditor.
    /// </summary>
    static public event Action<bool> TextEditorActive;

    public static event Action<UserControl>? TextEditorActivated;

    /// <summary>
    /// Событие, которое вызывается, когда активно окно типа TranslatorItem.
    /// </summary>
    public static event Action<bool> TranslatorActive;

    /// <summary>
    /// Событие, которое вызывается, когда закрывается окно типа TextEditor.
    /// </summary>
    static public event Action<bool, string> TextEditorContainerClosing;

    /// <summary>
    /// Событие, которое вызывается, когда окно типа searchWindow закрывается.
    /// </summary>
    static public event Action<bool> SearchWindowClosing;

    /// <summary>
    /// Событие, которое вызывается, когда окно типа searchWindow закрывается.
    /// </summary>
    static public event Action CloseSearchWindow;

    /// <summary>
    /// Событие, которое вызывается, когда окно типа searchWindow вновь активируется.
    /// </summary>
    static public event Action<bool> SearchWindowAtivated;

    /// <summary>
    /// Событие, которое вызывается, когда нажата кнопка поиска по тексту.
    /// </summary>
    static public event Action<string> SearchButtonPressed;

    /// <summary>
    /// Событие, которое вызывается, когда нажата кнопка замены одного слова в тексте.
    /// </summary>
    static public event Action ReplaceWordButtonPressed;

    /// <summary>
    /// Событие, которое вызывается, когда нажата кнопка замены ысех найденных вхождений в тексте.
    /// </summary>
    static public event Action ReplaceAllWordsButtonPressed;

    /// <summary>
    /// Событие, которое вызывается, когда нажата кнопка для открытия окна поиска по тексту.
    /// </summary>
    public static event Action<string> SearchTextRequested;

    /// <summary>
    /// Событие, которое вызывается, когда происходит переключение активного окна.
    /// </summary>
    public static event Action<bool> ActiveEditorChanged;

    /// <summary>
    /// Событие, которое вызывается, когда выпонен двойнок клик по строке в таблице с результатми поиска оп тексту.
    /// </summary>
    public static event Action<string, int, int, string, string> FoundTextSelectRow;

    /// <summary>
    /// Событие, которое вызывается, когда нажата кнопка поиска по тексту.
    /// </summary>
    public static event Action<string, bool?, bool?, int, string> SearchText;

    /// <summary>
    /// Событие, которое вызывается, когда нажата кнопка замена слова.
    /// </summary>
    public static event Action<string, string, bool?, bool?, int, string> ReplaceText;

    /// <summary>
    /// Событие для запроса показа окна прогресса с блюром на главном окне
    /// </summary>
    public static event Action RequestShowProgress;

    /// <summary>
    /// Событие для запроса закрытия окна прогресса и снятия блюра с главного окна
    /// </summary>
    public static event Action RequestCloseProgress;

    /// <summary>
    /// Событие, которое вызывается для открытия нового Opk-файла.
    /// </summary>
    static public event Action<UserControl, string> OpenOpk;

    /// <summary>
    /// Событие, которое вызывается при нажатии на кнопку "Сравнить".
    /// </summary>
    public static event Action<string, string> CompareFiles;

    /// <summary>
    /// Событие, которое вызывается при изменении статуса прав администратора.
    /// </summary>
    static internal bool AdminRightsFlag
    {
      get => IsAdmin;
      set
      {
        if (IsAdmin != value)
        {
          IsAdmin = value;
          AdminRightsChanged?.Invoke(IsAdmin);
        }
      }
    }

    /// <summary>
    /// Флаг, указывающий, активно ли питание системы.
    /// </summary>
    static internal bool PowerFlag
    {
      get => IsActivePower;
      set
      {
        if (IsActivePower != value)
        {
          IsActivePower = value;
          PowerChanged?.Invoke(IsActivePower);
        }
      }
    }

    /// <summary>
    /// Флаг, указывающий, активно ли питание системы.
    /// </summary>
    static internal bool LockedFlag
    {
      get => IsLocked;
      set
      {
        if (IsLocked != value)
        {
          IsLocked = value;
          LockedChanged?.Invoke(IsLocked);
        }
      }
    }

    /// <summary>
    /// Возвращает текущий статус прав администратора.
    /// </summary>
    /// <returns>true, если запущено с правами администратора; false в противном случае.</returns>
    static public bool GetAdminRights()
    {
      bool result = false;
      Application.Current.Dispatcher.Invoke(() => result = IsAdmin);
      return result;
    }

    /// <summary>
    /// Метод вывода предупреждения в блок информации программы.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="clearMessage">Удалить сообщение?</param>
    static public void RaiseWarningMessage(string message, bool clearMessage = false)
    {
      Application.Current.Dispatcher.Invoke(() => WarningMessageEvent?.Invoke(message, clearMessage)); 
    }

    /// <summary>
    /// Метод вывода информации в блок информации программы.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="clearMessage">Удалить сообщение?</param>
    static public void RaiseInfoMessage(string message, bool clearMessage = false)
    {
      Application.Current.Dispatcher.Invoke(() => InfoMessageEvent?.Invoke(message, clearMessage));
    }

    static public void RaiseClearMessage()
    { 
      Application.Current.Dispatcher.Invoke(() => ClearMessageEvent?.Invoke());
    }

    /// <summary>
    /// Метод для вызова события, когда активное окно - TextEditor.
    /// </summary>
    /// <param name="elementName">Имя нового элемента.</param>
    static public void RaiseTextEditorActive(bool isTextEditorContainer)
    {
      Application.Current.Dispatcher.Invoke(() => TextEditorActive?.Invoke(isTextEditorContainer));
    }

    /// <summary>
    /// Метод для вызова события, когда активное окно - TextEditor.
    /// </summary>
    /// <param name="elementName">Имя нового элемента.</param>
    static public void RaiseTextEditorActivated(UserControl aciveTextEditorUI)
    {
      TextEditorActivated?.Invoke(aciveTextEditorUI);
    }

    /// <summary>
    /// Метод для вызова события, когда активное окно - TextEditor.
    /// </summary>
    /// <param name="elementName">Имя нового элемента.</param>
    static public void RaiseTranslatorActivated(bool aciveTextEditorUI)
    {
      TranslatorActive?.Invoke(aciveTextEditorUI);
    }

    /// <summary>
    /// Метод для вызова события, когда активное окно - TextEditor закрывается.
    /// </summary>
    /// <param name="elementName">Имя нового элемента.</param>
    static public void RaiseTextEditorContainerClosing(bool isTextEditorContainer, string textEditorName)
    {
      TextEditorContainerClosing?.Invoke(isTextEditorContainer, textEditorName);
    }

    /// <summary>
    /// Метод для вызова события, когда закрывается SearchWindow.
    /// </summary>
    /// <param name="elementName">Имя нового элемента.</param>
    static public void RaiseSearchWindowClosing(bool isOpen)
    {
      SearchWindowClosing?.Invoke(isOpen);
    }

    /// <summary>
    /// Метод для вызова события, когда начинается поиск по тексту.
    /// </summary>
    /// <param name="elementName">Имя нового элемента.</param>
    static public void RaiseSearchText(string searchText, bool? wholeWord, bool? caseWord, int searchArea, string searchParameters)
    {
      SearchText?.Invoke(searchText, wholeWord, caseWord, searchArea, searchParameters);
    }

    /// <summary>
    /// Метод для вызова события при замене текста.
    /// </summary>
    /// <param name="elementName">Имя нового элемента.</param>
    static public void RaiseReplaceText(string replaceText, string searchText, bool? wholeWord, bool? caseWord, int searchArea, string searchParameters)
    {
      ReplaceText?.Invoke(replaceText, searchText, wholeWord, caseWord, searchArea, searchParameters);
    }

    /// <summary>
    /// Метод для вызова события, когда SearchWindow закрывается.
    /// </summary>
    /// <param name="elementName">Имя нового элемента.</param>
    public static void RaiseCloseSearchWindow()
    {
      CloseSearchWindow?.Invoke();
    }

    /// <summary>
    /// Метод для вызова события, которое вызывается, когда нажата кнопка для открытия окна поиска по тексту и есть выделенный текст, который передается в окно поиска.
    /// </summary>
    public static void RaiseSearchTextRequested(string selectedText)
    {
      SearchTextRequested?.Invoke(selectedText);
    }

    /// <summary>
    /// Метод для вызова события, когда нажата кнопка поиска.
    /// </summary>
    /// <param name="elementName">Имя нового элемента.</param>
    static public void RaiseSearchButtonPressed(string searchParameters)
    {
      SearchButtonPressed?.Invoke(searchParameters);
    }

    /// <summary>
    /// Метод для вызова события, когда нажата кнопка замены одного найденного вхождения в тексте.
    /// </summary>
    static public void RaiseReplaceWordButtonPressed()
    {
      ReplaceWordButtonPressed?.Invoke();
    }

    /// <summary>
    /// Метод для вызова события, когда нажата кнопка замены всех найденных вхождений в тексте.
    /// </summary>
    static public void RaiseReplaceAllWordsButtonPressed()
    {
      ReplaceAllWordsButtonPressed?.Invoke();
    }

    /// <summary>
    /// Метод для вызова события, когда происходит смена активного окна.
    /// </summary>
    /// <param name="isTextEditor"></param>
    public static void RaiseActiveEditorChanged(bool isTextEditor)
    {
      ActiveEditorChanged?.Invoke(isTextEditor);
    }

    /// <summary>
    /// Метод для вызова события, когда выпонен двойнок клик по строке в таблице с результатми поиска оп тексту.
    /// </summary>
    /// <param name="isTextEditor"></param>
    public static void RaiseFoundTextSelectRow(string fileName, int lineNumber, int startOffset, string lineText, string searchText)
    {
      FoundTextSelectRow?.Invoke(fileName, lineNumber, startOffset, lineText, searchText);
    }

    public static void RaiseRequestShowProgress()
    {
      RequestShowProgress?.Invoke();
    }

    public static void RaiseRequestCloseProgress()
    {
      RequestCloseProgress?.Invoke();
    }

    /// <summary>
    /// Метод для вызова события добавления нового элемента.
    /// </summary>
    /// <param name="elementName">Имя нового элемента.</param>
    static public void RaiseOpenOpk(UserControl userControl,string elementName)
    {
      LogDebug($"Происходит вызов события для добавления нового элемента \"{elementName}\".");
      OpenOpk?.Invoke(userControl, elementName);
    }

    /// <summary>
    /// Метод для вызова события сравнения файлов.
    /// </summary>
    /// <param name="elementName">Имя нового элемента.</param>
    public static void RaiseCompareFiles(string firstFilePath, string secondFilePath)
    {
      CompareFiles?.Invoke(firstFilePath, secondFilePath);
    }
  }
}
