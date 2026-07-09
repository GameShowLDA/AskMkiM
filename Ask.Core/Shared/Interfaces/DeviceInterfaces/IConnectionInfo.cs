using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces
{
  /// <summary>
  /// Предоставляет информацию о состоянии и типе подключения устройства.
  /// </summary>
  public interface IConnectionInfo
  {
    /// <summary>
    /// Признак установленного подключения.
    /// </summary>
    bool IsConnected { get; set; }

    /// <summary>
    /// Тип подключения устройства.
    /// </summary>
    ConnectionType ConnectionType { get; }

    /// <summary>
    /// Возвращает текстовое описание текущего состояния подключения.
    /// </summary>
    /// <returns>Строка с информацией о состоянии подключения.</returns>
    string GetConnectionStatus();
  }
}
