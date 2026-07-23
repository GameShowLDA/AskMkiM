using System.Threading;

namespace Ask.Core.Services.UI;

/// <summary>
/// Содержит контекст выполнения обязательных завершающих операций оборудования.
/// </summary>
public static class EquipmentExecutionContext
{
  private static readonly AsyncLocal<int> MandatoryFinalizationDepth = new();

  /// <summary>
  /// Признак выполнения обязательной завершающей операции.
  /// </summary>
  public static bool IsMandatoryFinalization => MandatoryFinalizationDepth.Value > 0;

  /// <summary>
  /// Открывает область обязательного завершения без интерактивных повторов.
  /// </summary>
  /// <returns>Объект, закрывающий область обязательного завершения.</returns>
  public static IDisposable EnterMandatoryFinalization()
  {
    MandatoryFinalizationDepth.Value++;
    return new MandatoryFinalizationScope();
  }

  private sealed class MandatoryFinalizationScope : IDisposable
  {
    private bool _disposed;

    public void Dispose()
    {
      if (_disposed)
      {
        return;
      }

      _disposed = true;
      MandatoryFinalizationDepth.Value = Math.Max(0, MandatoryFinalizationDepth.Value - 1);
    }
  }
}
