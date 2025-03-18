namespace NewCore.Base.Function.ManagerChassis
{
  public interface IPowerManagerChassis
  {
    Task StopPowerAsync();
    Task StartPowerAsync();
  }
}
