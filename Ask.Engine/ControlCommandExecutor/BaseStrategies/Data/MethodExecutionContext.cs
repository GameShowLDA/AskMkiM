using static Ask.Engine.ControlCommandExecutor.BaseStrategies.NodeFullChecker;

namespace Ask.Engine.ControlCommandExecutor.BaseStrategies.Data
{
  /// <summary>
  /// Представляет контекст выполнения метода измерения в исполнительном модуле.
  /// Содержит все необходимые данные, параметры и сервисы для проведения измерения
  /// и обработки его результатов.
  /// </summary>
  internal class MethodExecutionContext : ExecutorContext
  {
    /// <summary>
    /// Делегат, выполняющий операцию измерения.
    /// Вызывается методами исполнительного модуля для получения результата измерения.
    /// </summary>
    internal PerformMeasurementAsync PerformMeasurementAsync { get; set; }
  }
}
