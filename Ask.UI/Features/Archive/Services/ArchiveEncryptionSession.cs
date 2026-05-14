using Ask.Core.Services.FilesUtility;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace Ask.UI.Features.Archive.Services
{
  /// <summary>
  /// Управляет жизненным циклом расшифровки/шифрования архива с поддержкой вложенных операций.
  /// </summary>
  internal static class ArchiveEncryptionSession
  {
    /// <summary>
    /// Активные сессии шифрования архивов по пути к файлу.
    /// </summary>
    private static readonly ConcurrentDictionary<string, SessionState> Sessions =
      new ConcurrentDictionary<string, SessionState>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Метки внутренних изменений файлов архивов для подавления лишних событий обновления.
    /// </summary>
    private static readonly ConcurrentDictionary<string, DateTime> InternalMutationMarks =
      new ConcurrentDictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Время жизни метки внутреннего изменения архива.
    /// </summary>
    private static readonly TimeSpan MutationMarkLifetime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Открывает сессию работы с архивом с автоматической расшифровкой и учётом ссылок.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <returns>Объект сессии, освобождающий ресурсы при завершении.</returns>
    public static IDisposable Acquire(string archivePath)
    {
      if (string.IsNullOrWhiteSpace(archivePath))
      {
        throw new ArgumentException("Требуется указать путь к архиву.", nameof(archivePath));
      }

      var fullArchivePath = Path.GetFullPath(archivePath);
      var sessionState = Sessions.GetOrAdd(fullArchivePath, static _ => new SessionState());

      lock (sessionState.SyncRoot)
      {
        if (sessionState.ReferenceCount == 0)
        {
          MarkInternalMutation(fullArchivePath);
          FileEncryptionManager.DecryptFile(fullArchivePath);
          MarkInternalMutation(fullArchivePath);
        }

        sessionState.ReferenceCount++;
      }

      return new SessionHandle(fullArchivePath, sessionState);
    }

    /// <summary>
    /// Завершает сессию работы с архивом и выполняет повторное шифрование при отсутствии активных ссылок.
    /// </summary>
    /// <param name="fullArchivePath">Полный путь к архиву.</param>
    /// <param name="sessionState">Состояние сессии архива.</param>
    private static void Release(string fullArchivePath, SessionState sessionState)
    {
      lock (sessionState.SyncRoot)
      {
        if (sessionState.ReferenceCount <= 0)
        {
          return;
        }

        sessionState.ReferenceCount--;
        if (sessionState.ReferenceCount == 0)
        {
          MarkInternalMutation(fullArchivePath);
          FileEncryptionManager.EncryptFile(fullArchivePath);
          MarkInternalMutation(fullArchivePath);
        }
      }
    }

    /// <summary>
    /// Проверяет, был ли архив недавно изменен в рамках текущей сессии.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <returns>True, если архив был недавно изменен, иначе False.</returns>
    public static bool WasRecentlyMutatedBySession(string archivePath)
    {
      if (string.IsNullOrWhiteSpace(archivePath))
      {
        return false;
      }

      var fullArchivePath = Path.GetFullPath(archivePath);
      if (!InternalMutationMarks.TryGetValue(fullArchivePath, out var lastMutationUtc))
      {
        return false;
      }

      if (DateTime.UtcNow - lastMutationUtc > MutationMarkLifetime)
      {
        InternalMutationMarks.TryRemove(fullArchivePath, out _);
        return false;
      }

      return true;
    }

    /// <summary>
    /// Помечает архив как внутренне изменённый.
    /// </summary>
    /// <param name="fullArchivePath">Полный путь к архиву.</param>
    private static void MarkInternalMutation(string fullArchivePath)
    {
      InternalMutationMarks[fullArchivePath] = DateTime.UtcNow;
    }

    /// <summary>
    /// Хранит состояние активной сессии работы с архивом.
    /// </summary>
    private sealed class SessionState
    {
      /// <summary>
      /// Объект синхронизации доступа к состоянию сессии.
      /// </summary>
      public object SyncRoot { get; } = new object();

      /// <summary>
      /// Количество активных ссылок на сессию.
      /// </summary>
      public int ReferenceCount { get; set; }
    }

    /// <summary>
    /// Представляет дескриптор сессии архива с автоматическим освобождением ресурсов.
    /// </summary>
    private sealed class SessionHandle : IDisposable
    {
      /// <summary>
      /// Полный путь к архиву.
      /// </summary>
      private readonly string _fullArchivePath;

      /// <summary>
      /// Состояние текущей сессии архива.
      /// </summary>
      private readonly SessionState _sessionState;

      /// <summary>
      /// Признак освобождения ресурсов.
      /// </summary>
      private int _isDisposed;

      /// <summary>
      /// Инициализирует новый экземпляр дескриптора сессии архива.
      /// </summary>
      /// <param name="fullArchivePath">Полный путь к архиву.</param>
      /// <param name="sessionState">Состояние сессии.</param>
      public SessionHandle(string fullArchivePath, SessionState sessionState)
      {
        _fullArchivePath = fullArchivePath;
        _sessionState = sessionState;
      }

      /// <summary>
      /// Освобождает сессию архива и связанные ресурсы.
      /// </summary>
      public void Dispose()
      {
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
        {
          return;
        }

        Release(_fullArchivePath, _sessionState);
      }
    }
  }
}
