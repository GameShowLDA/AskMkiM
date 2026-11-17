using System.Text;
using UI.Services;

namespace Ask.UI.UnitTests.Services;

public class EncodingServiceTests
{
  [Fact]
  public void Decode_LegacyRussianText_UsesCp866EvenWithUtf8Default()
  {
    byte[] bytes = [0x31, 0x30, 0x20, 0x8E, 0x8A];

    var result = EncodingService.Decode(bytes, Encoding.UTF8);

    Assert.Equal("10 ОК", result.Text);
    Assert.Equal(866, result.Encoding.CodePage);
    Assert.Equal(EncodingService.DetectionReason.InvalidUtf8, result.Reason);
  }

  [Fact]
  public void Decode_Utf8RussianTextWithoutBom_UsesTextHeuristic()
  {
    const string source = "10 ОК   СТ6737.000.000";

    var result = EncodingService.Decode(Encoding.UTF8.GetBytes(source), GetCp866());

    Assert.Equal(source, result.Text);
    Assert.Equal(65001, result.Encoding.CodePage);
    Assert.Empty(result.Encoding.GetPreamble());
    Assert.Equal(EncodingService.DetectionReason.TextHeuristic, result.Reason);
  }

  [Fact]
  public void Decode_Ascii_KeepsFormatDefaultExceptForBackup()
  {
    byte[] bytes = Encoding.ASCII.GetBytes("10 OK");

    var original = EncodingService.Decode(bytes, GetCp866());
    var backup = EncodingService.Decode(bytes, GetCp866(), isBackup: true);

    Assert.Equal(original.Text, backup.Text);
    Assert.Equal(866, original.Encoding.CodePage);
    Assert.Equal(65001, backup.Encoding.CodePage);
    Assert.Equal(EncodingService.DetectionReason.Backup, backup.Reason);
  }

  [Fact]
  public void Decode_AmbiguousBytes_UsesFormatDefault()
  {
    byte[] bytes = [0xD0, 0xA0];

    Assert.Equal("╨а", EncodingService.Decode(bytes, GetCp866()).Text);
    Assert.Equal("Р", EncodingService.Decode(bytes, Encoding.UTF8).Text);
  }

  [Theory]
  [InlineData(65001)]
  [InlineData(1200)]
  [InlineData(1201)]
  [InlineData(12000)]
  [InlineData(12001)]
  public void Decode_Bom_TakesPriorityAndIsRemovedFromText(int codePage)
  {
    const string source = "10 ОК";
    var encoding = Encoding.GetEncoding(codePage);
    byte[] bytes = [.. encoding.GetPreamble(), .. encoding.GetBytes(source)];

    var result = EncodingService.Decode(bytes, GetCp866(), isBackup: true);

    Assert.Equal(source, result.Text);
    Assert.Equal(codePage, result.Encoding.CodePage);
    Assert.Equal(EncodingService.DetectionReason.ByteOrderMark, result.Reason);
  }

  private static Encoding GetCp866()
  {
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    return Encoding.GetEncoding(866);
  }
}
