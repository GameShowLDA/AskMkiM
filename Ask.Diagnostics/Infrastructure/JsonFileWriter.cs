using System.Text.Encodings.Web;
using System.Text.Json;

namespace Ask.Diagnostics.Infrastructure
{
  internal static class JsonFileWriter
  {
    private static readonly JsonSerializerOptions Options = new()
    {
      WriteIndented = true,
      Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static async Task WriteAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
      await using var stream = new FileStream(
        path,
        FileMode.Create,
        FileAccess.Write,
        FileShare.Read,
        bufferSize: 16 * 1024,
        useAsync: true);

      await JsonSerializer.SerializeAsync(stream, value, Options, cancellationToken)
        .ConfigureAwait(false);
    }
  }
}
