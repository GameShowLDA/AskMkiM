using static NewCore.Enum.DeviceEnum;

namespace NewCore.Base.Function.DBC
{
  public interface IBusDeviceBusCommutation
  {
    Task<bool> ConnectChainCircuit(int numberBlock, int numberChain);
    Task<bool> DisconnectChainCircuit(int numberBlock, int numberChain);
    Task<bool> ConnectBusSelfControlAsync(char numberBus);
    Task<bool> DisconnectBusSelfControlAsync(char numberBus);
    Task<string> ConnectBusAsync(MeterConnector connector, SwitchingBus bus, bool lowVoltage, bool polarityReversed);
    Task<string> DisconnectBusAsync(MeterConnector connector, SwitchingBus bus, bool lowVoltage, bool polarityReversed);
  }
}
