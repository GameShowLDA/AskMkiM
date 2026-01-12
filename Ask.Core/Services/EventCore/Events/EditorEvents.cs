using Ask.Core.Shared.Interfaces.EventInterfaces;
using System.Windows.Controls;

namespace Ask.Core.Services.EventCore.Events
{
  /// <summary>
  /// Содержит события, связанные с активностью окон редакторов, 
  /// переключением фокуса и закрытием элементов управления.
  /// </summary>
  public static class EditorEvents
  {
    /// <summary>
    /// Событие, обозначающее активацию или деактивацию окна текстового редактора.
    /// </summary>
    public class TextEditorActive : IEvent
    {
      /// <summary>
      /// Указывает, активно ли окно текстового редактора.
      /// </summary>
      public bool IsActive { get; }

      /// <summary>
      /// Инициализирует событие активации или деактивации окна редактора.
      /// </summary>
      /// <param name="isActive">true — редактор активен, false — неактивен.</param>
      public TextEditorActive(bool isActive)
      {
        IsActive = isActive;
      }
    }

    /// <summary>
    /// Событие, обозначающее активацию конкретного экземпляра редактора.
    /// </summary>
    public class TextEditorActivated : IEvent
    {
      /// <summary>
      /// Ссылка на активированный элемент управления (UserControl), связанный с редактором.
      /// </summary>
      public UserControl ActiveEditor { get; }

      /// <summary>
      /// Инициализирует событие активации редактора.
      /// </summary>
      /// <param name="activeEditor">Активированный элемент управления.</param>
      public TextEditorActivated(UserControl activeEditor)
      {
        ActiveEditor = activeEditor;
      }
    }

    /// <summary>
    /// Событие, обозначающее активацию или деактивацию окна переводчика (TranslatorItem).
    /// </summary>
    public class TranslatorActive : IEvent
    {
      /// <summary>
      /// Указывает, активно ли окно переводчика.
      /// </summary>
      public bool IsActive { get; }

      /// <summary>
      /// Инициализирует событие активации или деактивации переводчика.
      /// </summary>
      /// <param name="isActive">true — переводчик активен, false — неактивен.</param>
      public TranslatorActive(bool isActive)
      {
        IsActive = isActive;
      }
    }

    /// <summary>
    /// Событие, обозначающее закрытие контейнера редактора.
    /// </summary>
    public class TextEditorContainerClosing : IEvent
    {
      /// <summary>
      /// Указывает, закрывается ли контейнер редактора.
      /// </summary>
      public bool IsClosing { get; }

      /// <summary>
      /// Имя или идентификатор закрываемого редактора.
      /// </summary>
      public string EditorName { get; }

      /// <summary>
      /// Инициализирует событие закрытия контейнера редактора.
      /// </summary>
      /// <param name="isClosing">true — контейнер закрывается.</param>
      /// <param name="editorName">Имя закрываемого редактора.</param>
      public TextEditorContainerClosing(bool isClosing, string editorName)
      {
        IsClosing = isClosing;
        EditorName = editorName;
      }
    }

    /// <summary>
    /// Событие, обозначающее смену активного окна редактора.
    /// </summary>
    public class ActiveEditorChanged : IEvent
    {
      /// <summary>
      /// Указывает, активно ли новое окно редактора.
      /// </summary>
      public bool IsTextEditor { get; }

      /// <summary>
      /// Инициализирует событие смены активного окна редактора.
      /// </summary>
      /// <param name="isTextEditor">true — активен TextEditor, false — другое окно.</param>
      public ActiveEditorChanged(bool isTextEditor)
      {
        IsTextEditor = isTextEditor;
      }
    }

    /// <summary>
    /// Событие, обозначающее закрытие элемента выполнения (RunItem).
    /// </summary>
    public class CloseRunItem : IEvent
    {
      /// <summary>
      /// Элемент управления (UserControl), который должен быть закрыт.
      /// </summary>
      public UserControl RunControl { get; }

      /// <summary>
      /// Инициализирует событие закрытия элемента выполнения.
      /// </summary>
      /// <param name="runControl">Элемент управления, подлежащий закрытию.</param>
      public CloseRunItem(UserControl runControl)
      {
        RunControl = runControl;
      }
    }
  }
}
