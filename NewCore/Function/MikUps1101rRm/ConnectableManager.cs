using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Communication;
using System.Text.Json;
using static Ask.LogLib.LoggerUtility;

namespace NewCore.Function.MikUps1101rRm
{
  /// <summary>
  /// Connection manager for MIK-UPS-1101R-RM.
  /// </summary>
  public class ConnectableManager : IConnectable
  {
    private const string ConnectCommand = "UPS:CONNECT";

    private readonly IUninterruptiblePowerSupply _device;
    private bool _connected;
    private string _viewPowerPortName = string.Empty;
    private string _workMode = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectableManager"/> class.
    /// </summary>
    /// <param name="device">UPS device.</param>
    public ConnectableManager(IUninterruptiblePowerSupply device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    public event Action IsReset;

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> InitializeAsync(IUserInteractionService messageService = null)
    {
      return await ConnectAsync(messageService);
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> ConnectAsync(IUserInteractionService messageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        _connected = true;
        return (true, "Idle mode enabled");
      }

      string payload = await _device.DeviceProtocol.QueryAsync(ConnectCommand, timeout: 2_000);
      ConnectionState state = ReadState(payload);

      _connected = state.Success;
      _viewPowerPortName = state.PortName;
      _workMode = state.WorkMode;

      if (_connected)
      {
        LogInformation($"[{_device.Name}] {state.Message}", isDeviceLog: true);
        return (true, state.Message);
      }

      string error = string.IsNullOrWhiteSpace(state.Error)
        ? $"UPS \"{_device.Name}\" connection failed."
        : state.Error;

      LogWarning($"[{_device.Name}] {error}", isDeviceLog: true);
      return (false, error);
    }

    /// <inheritdoc />
    public Task<bool> DisconnectAsync(IUserInteractionService messageService = null)
    {
      _connected = false;
      _viewPowerPortName = string.Empty;
      _workMode = string.Empty;
      return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> ResetAsync(IUserInteractionService messageService = null)
    {
      _connected = false;
      _viewPowerPortName = string.Empty;
      _workMode = string.Empty;
      _device.LastResolvedDevicePath = string.Empty;
      IsReset?.Invoke();
      return Task.FromResult(true);
    }

    /// <inheritdoc />
    public string GetConnectionStatus()
    {
      if (_connected)
      {
        List<string> parts = new()
        {
          "Connected.",
        };

        if (!string.IsNullOrWhiteSpace(_device.LastResolvedDevicePath))
        {
          parts.Add($"DevicePath: {_device.LastResolvedDevicePath}");
        }

        if (!string.IsNullOrWhiteSpace(_viewPowerPortName))
        {
          parts.Add($"ViewPowerPort: {_viewPowerPortName}");
        }

        if (!string.IsNullOrWhiteSpace(_workMode))
        {
          parts.Add($"WorkMode: {_workMode}");
        }

        return string.Join(" ", parts);
      }

      return "Disconnected.";
    }

    private static ConnectionState ReadState(string payload)
    {
      if (string.IsNullOrWhiteSpace(payload))
      {
        return new ConnectionState(false, string.Empty, string.Empty, "Empty UPS response.", string.Empty);
      }

      try
      {
        using var json = JsonDocument.Parse(payload);
        JsonElement root = json.RootElement;

        bool success = root.TryGetProperty("Success", out var successProperty) &&
                       successProperty.ValueKind == JsonValueKind.True;

        return new ConnectionState(
          success,
          ReadText(root, "PortName"),
          ReadText(root, "WorkMode"),
          ReadText(root, "Error"),
          ReadText(root, "Message"));
      }
      catch (JsonException)
      {
        return new ConnectionState(false, string.Empty, string.Empty, "Invalid UPS response.", string.Empty);
      }
    }

    private static string ReadText(JsonElement root, string propertyName)
    {
      if (!root.TryGetProperty(propertyName, out var property))
      {
        return string.Empty;
      }

      return property.ValueKind == JsonValueKind.String
        ? property.GetString() ?? string.Empty
        : string.Empty;
    }

    private readonly record struct ConnectionState(
      bool Success,
      string PortName,
      string WorkMode,
      string Error,
      string Message);
  }
}
