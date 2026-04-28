using System.IO;
using System.Text.Json;
using System.Windows;

namespace Ask.UI.Controls.ErrorList;

public static class ErrorListLayoutSettings
{
  private const double DefaultScreenPart = 0.5;
  private const double MinHeight = 220.0;
  private const double FallbackHeight = 320.0;
  private const double MaxHeight = 525.0;
  private const string FileName = "error-list-layout.json";

  private static readonly string SettingsPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "AskMkiM",
    FileName);

  public static double GetInitialHeight()
  {
    var savedHeight = LoadHeight();
    if (savedHeight.HasValue)
      return ClampHeight(savedHeight.Value);

    return ClampHeight(GetWorkAreaHeight() * DefaultScreenPart);
  }

  public static double GetMaxHeight()
  {
    return MaxHeight;
  }

  public static double GetMinHeight()
  {
    return MinHeight;
  }

  public static double ClampHeight(double height)
  {
    if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0)
      return Math.Clamp(FallbackHeight, MinHeight, MaxHeight);

    return Math.Clamp(height, MinHeight, MaxHeight);
  }

  public static void SaveHeight(double height)
  {
    try
    {
      Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
      var payload = new ErrorListLayoutSettingsDto { Height = ClampHeight(height) };
      File.WriteAllText(SettingsPath, JsonSerializer.Serialize(payload));
    }
    catch
    {
      // UI geometry is non-critical. Ignore persistence failures.
    }
  }

  private static double? LoadHeight()
  {
    try
    {
      if (!File.Exists(SettingsPath))
        return null;

      var payload = JsonSerializer.Deserialize<ErrorListLayoutSettingsDto>(
        File.ReadAllText(SettingsPath));

      return payload?.Height;
    }
    catch
    {
      return null;
    }
  }

  private static double GetWorkAreaHeight()
  {
    return SystemParameters.WorkArea.Height > 0
      ? SystemParameters.WorkArea.Height
      : SystemParameters.PrimaryScreenHeight;
  }

  private sealed class ErrorListLayoutSettingsDto
  {
    public double Height { get; set; }
  }
}
