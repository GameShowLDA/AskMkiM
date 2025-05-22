using Utilities.Models;

namespace Utilities
{
  public static class DelegateManager
  {
    /// <summary>
    /// Делегат для выполнения предварительных действий.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    public delegate Task PreActionDelegate(CancellationToken cancellationToken);

    /// <summary>
    /// Делегат для метода запуска.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    public delegate Task StartDelegate(CancellationToken cancellationToken);

    /// <summary>
    /// Делегат для метода остановки.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    public delegate Task StopDelegate(CancellationToken cancellationToken);

    /// <summary>
    /// Делегат для метода повтора и зацикливания.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    public delegate Task ReturnDelegate(CancellationToken cancellationToken);
  }
}
