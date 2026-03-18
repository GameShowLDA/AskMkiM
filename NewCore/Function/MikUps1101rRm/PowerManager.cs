using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using System.Text.Json;

namespace NewCore.Function.MikUps1101rRm
{
  /// <summary>
  /// Power control logic for MIK-UPS-1101R-RM.
  /// </summary>
  public class PowerManager : IPower
  {
    private const string StartPowerCommand = "UPS:POWER:START";
    private const string StopPowerCommand = "UPS:POWER:STOP";
    private const string VerifyPowerCommand = "UPS:POWER:VERIFY";

    private readonly IUninterruptiblePowerSupply _device;

    public PowerManager(IUninterruptiblePowerSupply device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <inheritdoc />
    public async Task StopPowerAsync(IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return;
      }

      string result = await _device.DeviceProtocol.QueryAsync(StopPowerCommand, timeout: 30_000);
      EnsureCommandSucceeded(result, "UPS power off");
    }

    /// <inheritdoc />
    public async Task StartPowerAsync(IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return;
      }

      string result = await _device.DeviceProtocol.QueryAsync(StartPowerCommand, timeout: 30_000);
      EnsureCommandSucceeded(result, "UPS power on");
    }

    /// <inheritdoc />
    public async Task<bool> VerifyPowerAsync(IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      string result = await _device.DeviceProtocol.QueryAsync(VerifyPowerCommand, timeout: 1_000);
      return ReadVerifyState(result);
    }

    private static void EnsureCommandSucceeded(string payload, string operationName)
    {
      if (string.IsNullOrWhiteSpace(payload))
      {
        throw new InvalidOperationException($"{operationName} failed: empty UPS response.");
      }

      try
      {
        using var json = JsonDocument.Parse(payload);
        if (json.RootElement.TryGetProperty("Success", out var success) &&
            (success.ValueKind == JsonValueKind.True || success.ValueKind == JsonValueKind.False) &&
            success.GetBoolean())
        {
          return;
        }

        string error = ReadText(json.RootElement, "Error");
        if (string.IsNullOrWhiteSpace(error))
        {
          error = ReadText(json.RootElement, "Message");
        }

        throw new InvalidOperationException(string.IsNullOrWhiteSpace(error)
          ? $"{operationName} failed."
          : error);
      }
      catch (JsonException)
      {
        throw new InvalidOperationException($"{operationName} failed: invalid UPS response.");
      }
    }

    private static bool ReadVerifyState(string payload)
    {
      if (string.IsNullOrWhiteSpace(payload))
      {
        return false;
      }

      try
      {
        using var json = JsonDocument.Parse(payload);
        JsonElement root = json.RootElement;
        if (root.TryGetProperty("OutputOn", out var outputOn) &&
            (outputOn.ValueKind == JsonValueKind.True || outputOn.ValueKind == JsonValueKind.False))
        {
          return outputOn.GetBoolean();
        }

        if (root.TryGetProperty("Success", out var success) &&
            success.ValueKind == JsonValueKind.False)
        {
          string error = ReadText(root, "Error");
          if (!string.IsNullOrWhiteSpace(error))
          {
            throw new InvalidOperationException(error);
          }
        }
      }
      catch (JsonException)
      {
      }

      return false;
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
  }
}
