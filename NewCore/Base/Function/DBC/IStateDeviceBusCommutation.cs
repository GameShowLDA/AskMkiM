namespace NewCore.Base.Function.DBC
{
  public interface IStateDeviceBusCommutation
  {
    Task<bool> ResetAsync();
    Task<(bool Connect, string Answer)> Initialize();
  }
}
