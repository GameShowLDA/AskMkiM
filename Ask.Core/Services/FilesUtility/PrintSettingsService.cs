using Ask.Core.Services.Config.AppSettings;
using System.Windows.Documents;
using System.Windows.Media;

namespace Ask.Core.Services.FilesUtility
{
  public sealed record PrintFontSettings(string FontFamily, double FontSize)
  {
    public const string DefaultFontFamily = "Consolas";
    public const double DefaultFontSize = 10;

    public static PrintFontSettings Default { get; } = new(DefaultFontFamily, DefaultFontSize);
  }

  public static class PrintSettingsService
  {
    public static PrintFontSettings GetSettings()
    {
      var model = ProtocolConfig.GetProtocolModel();

      return Normalize(model.PrintFontFamily, model.PrintFontSize);
    }

    public static PrintFontSettings Normalize(string? fontFamily, double fontSize)
    {
      var family = string.IsNullOrWhiteSpace(fontFamily)
        ? PrintFontSettings.DefaultFontFamily
        : fontFamily.Trim();

      var size = fontSize is >= 6 and <= 72
        ? fontSize
        : PrintFontSettings.DefaultFontSize;

      return new PrintFontSettings(family, size);
    }

    public static void ApplyTo(FlowDocument document)
    {
      var settings = GetSettings();
      document.FontFamily = new FontFamily(settings.FontFamily);
      document.FontSize = settings.FontSize;
    }
  }
}
