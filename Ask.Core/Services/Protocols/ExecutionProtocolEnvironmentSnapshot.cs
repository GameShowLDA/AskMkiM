namespace Ask.Core.Services.Protocols;

/// <summary>
/// Содержит диагностический снимок окружения выполнения.
/// </summary>
public sealed record ExecutionProtocolEnvironmentSnapshot(
  DateTime CapturedAt,
  string ApplicationVersion,
  string Role,
  string ProcessName,
  string CheckType,
  string Mode,
  IReadOnlyDictionary<string, string> Settings,
  IReadOnlyList<ExecutionProtocolDeviceSnapshot> Equipment);

/// <summary>
/// Содержит диагностический снимок конфигурации устройства.
/// </summary>
public sealed record ExecutionProtocolDeviceSnapshot(
  int Id,
  int Number,
  string Type,
  string Name,
  string Description,
  string ConnectionDetails,
  string RuntimeClass,
  string ConfiguredClass,
  string ConnectionState);
