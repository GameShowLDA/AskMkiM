namespace NewCore.Base.Function.ManagerChassis
{
  public interface IStateManagerChassis
  {
    Task<(bool Connect, string Answer)> Initialize();
  }
}
