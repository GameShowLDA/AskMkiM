using System.Diagnostics.CodeAnalysis;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AppConfiguration.SystemState.SystemStateManager;
using System.Windows;
using System.Windows.Controls;

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
    /// Событие, которое вызывается при изменении состояния подключения USB устройства.
    /// </summary>
    static public event Action<bool> AdminRightsChanged;

    /// <summary>
    /// Событие, которое вызывается, когда активно окно типа TextEditor.
    /// </summary>
    static public event Action<bool> TextEditorActive;

    /// <summary>
    /// Событие, которое вызывается, когда активно окно типа TextEditor.
    /// </summary>
    static public event Action<bool, string> TextEditorClosing;

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
    /// Событие, которое вызывается, когда нажата кнопка замены текста.
    /// </summary>
    static public event Action ReplaceWordButtonPressed;

    /// <summary>
    /// Событие, которое вызывается, когда нажата кнопка замены текста.
    /// </summary>
    static public event Action ReplaceAllWordsButtonPressed;

    /// <summary>
    /// Событие, которое вызывается, когда нажата кнопка для открытия окна поиска по тексту.
    /// </summary>
    public static event Action<string> SearchTextRequested;

    public static event Action<string> SearchTextUpdated;

    /// <summary>
    /// Событие, которое вызывается, когда происходиити переключение активного окна.
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
    /// Событие, которое вызывается для добавления нового элемента в MultiEditor.
    /// </summary>
    static public event Action<UserControl, string, string> OpenOpk;

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
    /// Метод вывода ошибки в блок информации программы.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="clearMessage">Удалить сообщение?</param>
    static public void RaiseErrorMessage(string message, bool clearMessage = false)
    {
      ErrorMessageEvent?.Invoke(message, clearMessage);
    }

    /// <summary>
    /// Метод вывода предупреждения в блок информации программы.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="clearMessage">Удалить сообщение?</param>
    static public void RaiseWarningMessage(string message, bool clearMessage = false)
    {
      WarningMessageEvent?.Invoke(message, clearMessage);
    }

    /// <summary>
    /// Метод вывода информации в блок информации программы.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="clearMessage">Удалить сообщение?</param>
    static public void RaiseInfoMessage(string message, bool clearMessage = false)
    {
      InfoMessageEvent?.Invoke(message, clearMessage);
    }

    /// <summary>
    /// Метод для вызова события, когда активное окно - TextEditor.
    /// </summary>
    /// <param name="elementName">Имя нового элемента.</param>
    static public void RaiseTextEditorActive(bool isTextEditor)
    {
      TextEditorActive?.Invoke(isTextEditor);
    }

    /// <summary>
    /// Метод для вызова события, когда активное окно - TextEditor.
    /// </summary>
    /// <param name="elementName">Имя нового элемента.</param>
    static public void RaiseTextEditorClosing(bool isTextEditor, string textEditorName)
    {
      TextEditorClosing?.Invoke(isTextEditor, textEditorName);
    }

    /// <summary>
    /// Метод для вызова события, когда активное окно - TextEditor.
    /// </summary>
    /// <param name="elementName">Имя нового элемента.</param>
    static public void RaiseSearchWindowClosing(bool isOpen)
    {
      SearchWindowClosing?.Invoke(isOpen);
    }

    /// <summary>
    /// Метод для вызова события, когда активное окно - TextEditor.
    /// </summary>
    /// <param name="elementName">Имя нового элемента.</param>
    static public void RaiseSearchText(string searchText, bool? wholeWord, bool? caseWord, int searchArea, string searchParameters)
    {
      SearchText?.Invoke(searchText, wholeWord, caseWord, searchArea, searchParameters);
    }

    /// <summary>
    /// Метод для вызова события, когда активное окно - TextEditor.
    /// </summary>
    /// <param name="elementName">Имя нового элемента.</param>
    static public void RaiseReplaceText(string replaceText, string searchText, bool? wholeWord, bool? caseWord, int searchArea, string searchParameters)
    {
      ReplaceText?.Invoke(replaceText, searchText, wholeWord, caseWord, searchArea, searchParameters);
    }

    /// <summary>
    /// Метод для вызова события, когда активное окно - TextEditor.
    /// </summary>
    /// <param name="elementName">Имя нового элемента.</param>
    public static void RaiseCloseSearchWindow()
    {
      CloseSearchWindow?.Invoke();
    }

    /// <summary>
    /// Метод для вызова события, когда активное окно - TextEditor.
    /// </summary>
    /// <param name="elementName">Имя нового элемента.</param>
    static public void RaiseSearchWindowActivated(bool isActivated)
    {
      SearchWindowAtivated?.Invoke(isActivated);
    }

    /// <summary>
    /// Метод для вызова события, которое вызывается, когда нажата кнопка для открытия окна поиска по тексту.
    /// </summary>
    public static void RaiseSearchTextRequested(string selectedText)
    {
      SearchTextRequested?.Invoke(selectedText);
    }

    public static void RaiseSearchTextUpdated(string text)
    {
      SearchTextUpdated?.Invoke(text);
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
    /// Метод для вызова события, когда нажата кнопка поиска.
    /// </summary>
    static public void RaiseReplaceWordButtonPressed()
    {
      ReplaceWordButtonPressed?.Invoke();
    }

    /// <summary>
    /// Метод для вызова события, когда нажата кнопка поиска.
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
    static public void RaiseOpenOpk(UserControl userControl,string elementName, string elementData)
    {
      OpenOpk?.Invoke(userControl, elementName, elementData);
    }
  }
}
