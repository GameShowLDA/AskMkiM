using Ask.Core.Shared.Metadata.Enums.FileEnums;

namespace Ask.Engine.UnitTests.Metadata;

public class InspectionProtocolFileTypeTests
{
  [Theory]
  [InlineData(".askresult")]
  [InlineData(".askreport")]
  [InlineData(".rtlst")]
  public void DetermineFromExtension_ResultOrReport_ReturnsInspectionProtocol(string extension)
  {
    Assert.Equal(FileType.InspectionProtocol, FileTypeResolver.DetermineFromExtension(extension));
  }

  [Theory]
  [InlineData(".asktrace")]
  [InlineData(".lst")]
  [InlineData(".lstw")]
  public void DetermineFromExtension_Trace_ReturnsProtocol(string extension)
  {
    Assert.Equal(FileType.Protocol, FileTypeResolver.DetermineFromExtension(extension));
  }

  [Fact]
  public void InspectionProtocol_UsesDedicatedHighlightingResource()
  {
    Assert.Equal(
      "MKI_RESULT_PROTOCOL.xshd",
      FileTypeResolver.GetHighlightingResourceName(FileType.InspectionProtocol));
  }

  [Fact]
  public void InspectionProtocol_UsesUtf8Encoding()
  {
    Assert.True(FileTypeResolver.UsesUtf8Encoding(FileType.InspectionProtocol));
  }
}
