using System.Threading;

namespace Ask.Engine.ControlCommandAnalyser.Diagnostics
{
  /// <summary>
  /// Хранит режим текущего разбора команд.
  /// </summary>
  /// <remarks>
  /// Интерактивная проверка текста не должна зависеть от текущего состава
  /// оборудования. Контекст позволяет парсерам пропускать обращения к БД и
  /// аппаратные ограничения, не меняя поведение обычной трансляции.
  /// </remarks>
  internal static class CommandAnalysisContext
  {
    private static readonly AsyncLocal<int> TextDiagnosticsDepth = new();

    public static bool IsTextDiagnostics => TextDiagnosticsDepth.Value > 0;

    public static IDisposable EnterTextDiagnostics()
    {
      TextDiagnosticsDepth.Value++;
      return new Scope();
    }

    private sealed class Scope : IDisposable
    {
      private bool _disposed;

      public void Dispose()
      {
        if (_disposed)
          return;

        TextDiagnosticsDepth.Value = Math.Max(0, TextDiagnosticsDepth.Value - 1);
        _disposed = true;
      }
    }
  }
}
