using NewCore.Device;

namespace NewCore.Function.Keysight3466new
{
  public class KeysightConnection
  {
    private readonly KeysightDevice _device;
    private readonly KeysightCommunication _communication;

    public bool IsConnected => _device.IsConnected;

    public KeysightConnection(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _communication = device.KeysightCommunication;
    }

    public async Task<bool> ConnectAsync()
    {
      if (await _communication.ConnectAsync())
      {
        string idn = await _communication.QueryAsync("*IDN?");
        if (!string.IsNullOrEmpty(idn))
        {
          return true;
        }
      }
      return false;
    }

    public void Disconnect() => _communication.Disconnect();

    public async Task SendCommandAsync(string command) => await _communication.SendCommandAsync(command);
    public async Task<string> QueryAsync(string command) => await _communication.QueryAsync(command);
  }
}
