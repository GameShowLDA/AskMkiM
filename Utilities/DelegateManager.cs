using Utilities.Models;

namespace Utilities
{
  public static class DelegateManager
  {
    /// <summary>
    /// Делегат для отображения сообщений.
    /// </summary>
    public delegate Task MessageDelegate(ShowMessageModel showMessageModel);

    /// <summary>
    /// Делегат для выполнения предварительных действий.
    /// </summary>
    public delegate void PreActionDelegate();

    /// <summary>
    /// Делегат для метода запуска.
    /// </summary>
    public delegate Task StartDelegate(CancellationToken cancellationToken);

    /// <summary>
    /// Делегат для метода остановки.
    /// </summary>
    public delegate Task StopDelegate(CancellationToken cancellationToken);

    /// <summary>
    /// Делегат для метода повтора и зацикливания.
    /// </summary>
    public delegate Task ReturnDelegate(CancellationToken cancellationToken);
  }
}
