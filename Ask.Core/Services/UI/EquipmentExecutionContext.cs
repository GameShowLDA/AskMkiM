using System.Threading;

namespace Ask.Core.Services.UI;

/// <summary>
/// Содержит контекст выполнения обязательных завершающих операций оборудования.
/// </summary>
public static class EquipmentExecutionContext
{
  private static readonly AsyncLocal<int> MandatoryFinalizationDepth = new();
  private static readonly AsyncLocal<CancellationToken> ExecutionToken = new();

  /// <summary>Токен запуска; обязательный сброс не отменяется вместе с измерением.</summary>
  public static CancellationToken CancellationToken => IsMandatoryFinalization
    ? CancellationToken.None : ExecutionToken.Value;

  /// <summary>Передаёт отмену запуска вложенным аппаратным операциям.</summary>
  /// <param name="cancellationToken">Токен отмены запуска.</param>
  /// <returns>Область, восстанавливающая предыдущий контекст.</returns>
  public static IDisposable EnterExecution(CancellationToken cancellationToken)
  {
    var previous = ExecutionToken.Value;
    ExecutionToken.Value = cancellationToken;
    return new ExecutionScope(previous);
  }

  private sealed class ExecutionScope(CancellationToken previous) : IDisposable
  {
    public void Dispose() => ExecutionToken.Value = previous;
  }

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
