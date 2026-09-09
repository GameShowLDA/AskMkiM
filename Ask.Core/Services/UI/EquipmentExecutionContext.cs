using System.Threading;

namespace Ask.Core.Services.UI;

/// <summary>
/// Содержит контекст самоконтроля и обязательных завершающих операций оборудования.
/// </summary>
public static class EquipmentExecutionContext
{
  private static readonly AsyncLocal<int> MandatoryFinalizationDepth = new();
  private static readonly AsyncLocal<int> SelfTestDepth = new();

  /// <summary>
  /// Самоконтроль использует независимое от тестера питание и подключение МКР.
  /// </summary>
  public static bool IsSelfTest => SelfTestDepth.Value > 0;

  /// <summary>
  /// Открывает область самоконтроля, включая подготовку и сброс оборудования.
  /// Не отключает собственную симуляцию сбоя устройства.
  /// </summary>
  public static IDisposable EnterSelfTest()
  {
    SelfTestDepth.Value++;
    return new ExecutionScope(SelfTestDepth);
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
    return new ExecutionScope(MandatoryFinalizationDepth);
  }

  private sealed class ExecutionScope(AsyncLocal<int> depth) : IDisposable
  {
    private bool _disposed;

    public void Dispose()
    {
      if (_disposed)
      {
        return;
      }

      _disposed = true;
      depth.Value = Math.Max(0, depth.Value - 1);
    }
  }
}
