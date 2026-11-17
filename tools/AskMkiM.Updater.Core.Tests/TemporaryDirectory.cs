namespace AskMkiM.Updater.Core.Tests;

internal sealed class TemporaryDirectory : IDisposable
{
  internal TemporaryDirectory()
  {
    Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"AskMkiM.Updater.Tests.{Guid.NewGuid():N}");
    Directory.CreateDirectory(Path);
  }

  internal string Path { get; }

  internal string Combine(params string[] parts) =>
    parts.Aggregate(Path, System.IO.Path.Combine);

  public void Dispose()
  {
    try
    {
      if (Directory.Exists(Path)) Directory.Delete(Path, true);
    }
    catch
    {
      // Тестовая очистка не должна скрывать результат проверки.
    }
  }
}
