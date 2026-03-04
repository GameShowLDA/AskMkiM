using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Atributes;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Interface;
using Ask.Engine.ControlCommandAnalyser.Model.Pr;
using Ask.Engine.ControlCommandExecutor.Execution;
using System.Reflection;
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
