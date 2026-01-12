using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Metadata.Static
{
  public static class DelegateManager
  {
    /// <summary>
    /// Делегат для выполнения предварительных действий.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    public delegate Task PreActionDelegate(CancellationToken cancellationToken);

    /// <summary>
    /// Делегат, представляющий метод запуска выполнения алгоритма.
    /// </summary>
    /// <param name="messageService">
    /// Сервис взаимодействия с пользователем (вывод сообщений, диалогов, ошибок).
    /// </param>
    /// <param name="inputFieldProvider">
    /// Провайдер, предоставляющий доступ к значениям полей ввода.
    /// </param>
    /// <param name="inputHighlightService">
    /// Сервис подсветки полей ввода при возникновении ошибок.
    /// </param>
    /// <param name="cancellationToken">
    /// Токен отмены, позволяющий прервать выполнение.
    /// </param>
    public delegate Task StartDelegate(IUserInteractionService _messageService, IInputFieldProvider inputFieldProvider, IInputHighlightService inputHighlightService, CancellationToken cancellationToken);

    /// <summary>
    /// Делегат для метода остановки.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    public delegate Task StopDelegate(CancellationToken cancellationToken);

    /// <summary>
    /// Делегат для метода повтора и зацикливания.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    public delegate Task<bool> ReturnDelegate(CancellationToken cancellationToken);
  }
}
