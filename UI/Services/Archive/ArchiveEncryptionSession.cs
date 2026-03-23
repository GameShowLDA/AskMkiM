using Ask.Core.Services.FilesUtility;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace UI.Services.Archive
{
  /// <summary>
  /// Управляет жизненным циклом расшифровки/шифрования архива с поддержкой вложенных операций.
  /// </summary>
  internal static class ArchiveEncryptionSession
  {
    private static readonly ConcurrentDictionary<string, SessionState> Sessions =
      new ConcurrentDictionary<string, SessionState>(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, DateTime> InternalMutationMarks =
      new ConcurrentDictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan MutationMarkLifetime = TimeSpan.FromSeconds(2);

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

    private static void MarkInternalMutation(string fullArchivePath)
    {
      InternalMutationMarks[fullArchivePath] = DateTime.UtcNow;
    }

    private sealed class SessionState
    {
      public object SyncRoot { get; } = new object();
      public int ReferenceCount { get; set; }
    }

    private sealed class SessionHandle : IDisposable
    {
      private readonly string _fullArchivePath;
      private readonly SessionState _sessionState;
      private int _isDisposed;

      public SessionHandle(string fullArchivePath, SessionState sessionState)
      {
        _fullArchivePath = fullArchivePath;
        _sessionState = sessionState;
      }

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
