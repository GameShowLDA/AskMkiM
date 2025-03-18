namespace NewCore.Base.Function.DBC
{
  public interface IConnectorDeviceBusCommutation
  {
    Task ConnectXs9ToXs4();
    Task DisconnectXs9ToXs4();
    Task ConnectToBreakdownTester();
    Task DisconnectToBreakdownTester();
  }
}
