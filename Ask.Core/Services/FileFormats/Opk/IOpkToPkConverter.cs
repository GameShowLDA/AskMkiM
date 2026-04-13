namespace Ask.Core.Services.FileFormats.Opk
{
  public interface IOpkToPkConverter
  {
    ConversionResult Convert(string inputPath, string outputDirectory);
  }
}
