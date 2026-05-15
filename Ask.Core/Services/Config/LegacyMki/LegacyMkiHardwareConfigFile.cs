using System;
using System.Text.Json.Serialization;

namespace Ask.Core.Services.Config.LegacyMki;

/// <summary>
/// Полное содержимое файла mki_hrd.cfg.
/// </summary>
public sealed class LegacyMkiHardwareConfigFile
{
  public const int ProfileCount = 4;

  public byte ActiveProfileIndex { get; set; }

  public LegacyMkiHardwareProfile M1 { get; set; } = new();

  public LegacyMkiHardwareProfile R1 { get; set; } = new();

  public LegacyMkiHardwareProfile M2 { get; set; } = new();

  public LegacyMkiHardwareProfile R2 { get; set; } = new();

  [JsonIgnore]
  public LegacyMkiHardwareProfile[] Profiles => new[] { M1, R1, M2, R2 };

  public LegacyMkiHardwareProfile GetProfile(LegacyMkiProfileKind kind) => kind switch
  {
    LegacyMkiProfileKind.M1 => M1,
    LegacyMkiProfileKind.R1 => R1,
    LegacyMkiProfileKind.M2 => M2,
    LegacyMkiProfileKind.R2 => R2,
    _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
  };

  public void SetProfile(LegacyMkiProfileKind kind, LegacyMkiHardwareProfile profile)
  {
    switch (kind)
    {
      case LegacyMkiProfileKind.M1:
        M1 = profile;
        break;
      case LegacyMkiProfileKind.R1:
        R1 = profile;
        break;
      case LegacyMkiProfileKind.M2:
        M2 = profile;
        break;
      case LegacyMkiProfileKind.R2:
        R2 = profile;
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
    }
  }
}
