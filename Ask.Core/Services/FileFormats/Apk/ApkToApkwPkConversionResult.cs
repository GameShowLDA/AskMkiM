namespace Ask.Core.Services.FileFormats.Apk
{
  /// <summary>
  /// Represents the part of APK conversion that translates an intermediate PK file into OPKW.
  /// </summary>
  public sealed class ApkToApkwPkConversionResult
  {
    public string? OutputPath { get; init; }

    public bool Success { get; init; }

    public int ErrorCount { get; init; }
  }
}
