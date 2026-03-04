using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model.Interface;
using Ask.Engine.ControlCommandExecutor.Execution;
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
    public MethodExecutionContext() { }
    internal MethodExecutionContext(
      CommandExecutionContext context,
      BaseCommandModel command,
      IHasScheme hasScheme,
      double value = 0,
      double lowerLimit = 0,
      double higherLimit = 0) : base(context, command, hasScheme, value) { }
  }
}
