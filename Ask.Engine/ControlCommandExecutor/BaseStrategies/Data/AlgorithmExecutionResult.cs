using Ask.Core.Shared.DTO.Protocol;

namespace Ask.Engine.ControlCommandExecutor.BaseStrategies.Data
{
  internal sealed class AlgorithmExecutionResult
  {
    public List<ShowMessageModel> Errors { get; }
    public List<ShowMessageModel> Info { get; }

    internal AlgorithmExecutionResult(
        List<ShowMessageModel> errors,
        List<ShowMessageModel> info)
    {
      Errors = errors;
      Info = info;
    }

    /// <summary>
    /// Добавляет сообщения из другого результата выполнения алгоритма.
    /// </summary>
    public void AddRange(AlgorithmExecutionResult other)
    {
      if (other == null)
        return;

      Errors.AddRange(other.Errors);
      Info.AddRange(other.Info);
    }

    public static AlgorithmExecutionResult FromErrors(List<ShowMessageModel> errors) => new(errors, new List<ShowMessageModel>());
  }
}
