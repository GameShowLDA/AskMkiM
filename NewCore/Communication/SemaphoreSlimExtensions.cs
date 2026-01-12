namespace NewCore.Communication
{
  /// <summary>
  /// Набор расширений для SemaphoreSlim для удобного использования в стиле using.
  /// </summary>
  public static class SemaphoreSlimExtensions
  {
    /// <summary>
    /// Асинхронно захватывает семафор и возвращает IDisposable,
    /// который освободит семафор при вызове Dispose().
    /// </summary>
    public static async Task<IDisposable> LockAsync(this SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
    {
      await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
      return new Releaser(semaphore);
    }

    private sealed class Releaser : IDisposable
    {
      private readonly SemaphoreSlim _semaphore;

      public Releaser(SemaphoreSlim semaphore) => _semaphore = semaphore;

      public void Dispose() => _semaphore.Release();
    }
  }
}
