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
  /// Признак выполнения самоконтроля оборудования.
  /// </summary>
  public static bool IsSelfTest => SelfTestDepth.Value > 0;

  /// <summary>
  /// Открывает область самоконтроля без наследования сбоя тестера при обмене с МКР.
  /// </summary>
  public static IDisposable EnterSelfTest()
  {
    SelfTestDepth.Value++;
    return new SelfTestScope();
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

  private sealed class SelfTestScope : IDisposable
  {
    private bool _disposed;

    public void Dispose()
    {
      if (_disposed)
      {
        return;
      }

      _disposed = true;
      SelfTestDepth.Value = Math.Max(0, SelfTestDepth.Value - 1);
    }
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
