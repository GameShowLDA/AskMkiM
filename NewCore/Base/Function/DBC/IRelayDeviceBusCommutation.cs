namespace NewCore.Base.Function.DBC
{
  public interface IRelayDeviceBusCommutation
  {
    bool ConnectRelayIdleMode(int numberRelay);
    bool DisconnectRelayIdleMode(int numberRelay);
    Task<bool> ConnectRelay(int numberRelay);
    Task<bool> DisconnectRelay(int numberRelay);
  }
}
