using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using static Ask.Engine.ControlCommandExecutor.BaseStrategies.ConnectedPointChecker;

namespace Ask.Engine.ControlCommandExecutor.BaseStrategies.Data
{
  internal class ConnectedPointContext : ExecutorContext
  {
    internal PerformMeasurementAsync PerformMeasurementAsync { get; set; }

    /// <summary>
    /// Признак того, что в текущем прогоне ожидается перегрузка измерителя как нормальный результат.
    /// </summary>
    internal bool IsOverloadExpected { get; set; }

    /// <summary>
    /// Эффективный знак направления для текущего прогона NE, используемый при формировании строк протокола.
    /// </summary>
    internal string? CurrentNeDirectionSign { get; set; }

    /// <summary>
    /// Результирующий ССИРТ после проверки (не используется для последующих запусков).
    /// </summary>
    internal SchemeModel? NewScheme { get; set; }
  }
}
