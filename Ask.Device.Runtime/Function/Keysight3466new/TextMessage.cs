using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device;

namespace Ask.Device.Runtime.Function.Keysight3466new
{
  public class TextMessage : ITextMessage
  {
    private readonly KeysightDevice _device;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="AcVoltageMeasurement"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства Keysight.</param>
    /// <exception cref="ArgumentNullException">Выбрасывается, если переданный прибор равен <c>null</c>.</exception>
    public TextMessage(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    public async Task ClearMessage(IUserInteractionService? userMessageService = null)
    {
      await _device.DeviceProtocol.QueryAsync("DISPlay:TEXT:CLEar");

    }

    public async Task Message(string text, IUserInteractionService? userMessageService = null)
    {
      await _device.DeviceProtocol.QueryAsync($"DISPlay:TEXT:DATA \"{text}\"");
    }
  }
}
