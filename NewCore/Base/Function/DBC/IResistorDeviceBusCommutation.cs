namespace NewCore.Base.Function.DBC
{
  public interface IResistorDeviceBusCommutation
  {
    Task<bool> ConnectResistor(string number);
    Task<bool> DisconnectResistor(string number);
  }
}
