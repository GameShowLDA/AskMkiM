namespace Ask.Engine.ControlCommandAnalyser.Model.Chains
{
  public record ParsedExprPart(
        string CleanExpr,
        char? Sign   // '+', '-', null
    );
}
