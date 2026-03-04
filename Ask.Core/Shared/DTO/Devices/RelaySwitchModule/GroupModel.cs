namespace Ask.Core.Shared.DTO.Devices.RelaySwitchModule
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
    public GroupModel()
    {
      ChainModels = new List<ChainModel>();
    }

  }
}
