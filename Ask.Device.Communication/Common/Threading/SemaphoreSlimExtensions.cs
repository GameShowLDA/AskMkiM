namespace Ask.Device.Communication.Common
{
  /// <summary>
  /// Содержит расширения для <see cref="SemaphoreSlim"/>, упрощающие захват семафора через <c>using</c>.
  /// </summary>
  public static class SemaphoreSlimExtensions
  {
    /// <summary>
    /// Асинхронно захватывает семафор и возвращает объект, освобождающий его при вызове <see cref="IDisposable.Dispose"/>.
    /// </summary>
    /// <param name="semaphore">Семафор, который требуется захватить.</param>
    /// <param name="cancellationToken">Токен отмены ожидания семафора.</param>
    /// <returns>Объект, освобождающий захваченный семафор.</returns>
    /// <exception cref="ArgumentNullException">Выбрасывается, если <paramref name="semaphore"/> равен <see langword="null"/>.</exception>
    public static async Task<IDisposable> LockAsync(this SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
    {
      ArgumentNullException.ThrowIfNull(semaphore);

      await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
      return new Releaser(semaphore);
    }

    /// <summary>
    /// Освобождает семафор при завершении блока <c>using</c>.
    /// </summary>
    private sealed class Releaser : IDisposable
    {
      /// <summary>
      /// Семафор, который требуется освободить.
      /// </summary>
      private readonly SemaphoreSlim _semaphore;

      /// <summary>
      /// Инициализирует новый экземпляр <see cref="Releaser"/>.
      /// </summary>
      /// <param name="semaphore">Захваченный семафор.</param>
      public Releaser(SemaphoreSlim semaphore)
      {
        _semaphore = semaphore;
      }

      /// <summary>
      /// Освобождает ранее захваченный семафор.
      /// </summary>
      public void Dispose()
      {
        _semaphore.Release();
      }
    }
  }
}
