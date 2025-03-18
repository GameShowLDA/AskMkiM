namespace NewCore.Base.Function.DBC
{
  public interface ICapacitorDeviceBusCommutation
  {
    Task<bool> ConnectCapacitor(string number);
    Task<bool> DisconnectCapacitor(string number);
  }
}
