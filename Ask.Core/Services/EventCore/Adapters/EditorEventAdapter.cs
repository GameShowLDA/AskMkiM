using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using System.Windows.Controls;

namespace Ask.Core.Services.EventCore.Adapters
{
  /// <summary>
  /// Адаптер для генерации событий <see cref="EditorEvents"/>,
  /// обеспечивающий обратную совместимость со старым API вызовов EventAggregator.
  /// </summary>
  public static class EditorEventAdapter
  {
    /// <summary>
    /// Генерирует событие активации или деактивации окна текстового редактора.
    /// </summary>
    public static void RaiseTextEditorActive(bool isActive) =>
      EventAggregator.Publish(new EditorEvents.TextEditorActive(isActive));

    /// <summary>
    /// Генерирует событие активации конкретного экземпляра редактора.
    /// </summary>
    public static void RaiseTextEditorActivated(UserControl activeEditor) =>
      EventAggregator.Publish(new EditorEvents.TextEditorActivated(activeEditor));

    /// <summary>
    /// Генерирует событие активации или деактивации окна переводчика.
    /// </summary>
    public static void RaiseTranslatorActive(bool isActive) =>
      EventAggregator.Publish(new EditorEvents.TranslatorActive(isActive));

    /// <summary>
    /// Генерирует событие закрытия контейнера редактора.
    /// </summary>
    public static void RaiseTextEditorContainerClosing(bool isClosing, string editorName) =>
      EventAggregator.Publish(new EditorEvents.TextEditorContainerClosing(isClosing, editorName));

    /// <summary>
    /// Генерирует событие смены активного окна редактора.
    /// </summary>
    public static void RaiseActiveEditorChanged(bool isTextEditor) =>
      EventAggregator.Publish(new EditorEvents.ActiveEditorChanged(isTextEditor));

    /// <summary>
    /// Генерирует событие закрытия элемента выполнения.
    /// </summary>
    public static void RaiseCloseRunItem(UserControl runControl) =>
      EventAggregator.Publish(new EditorEvents.CloseRunItem(runControl));
  }
}
