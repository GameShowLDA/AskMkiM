namespace Ask.Engine.ControlCommandExecutor.BaseStrategies.Data
{
  internal sealed class DisconnectionCheckRequest
  {
    internal List<string> AlgorithmKey { get; set; }
    internal NodeFullContext NodeFullContext { get; set; }
    internal MethodExecutionContext MethodExecutionContext { get; set; }
    internal PairwiseFirstPointContext PairwiseFirstPointContext { get; set; }
    internal NodeAccumulationContext NodeAccumulationContext { get; set; }
    internal PairwiseFirstPointAltContext PairwiseFirstPointAltContext { get; set; }
    internal bool UseAltPairwiseFirstPoint { get; set; }
  }
}
