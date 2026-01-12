namespace Ask.Engine.ControlCommandAnalyser.Model.Chains
{
  /// <summary>
  /// Модель данных, содержащая все части цепей в одной цепи.
  /// </summary>
  public class GroupModel
  {
    public List<ChainModel> ChainModels;

    public GroupModel(List<ChainModel> chainModels)
    {
      this.ChainModels = chainModels;
    }
  }
}
