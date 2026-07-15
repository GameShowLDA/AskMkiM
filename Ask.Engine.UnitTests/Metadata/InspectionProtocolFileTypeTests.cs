using Ask.Core.Shared.Metadata.Enums.FileEnums;

namespace Ask.Engine.UnitTests.Metadata;

public class InspectionProtocolFileTypeTests
{
  [Fact]
  public void DetermineFromExtension_Rtlst_ReturnsInspectionProtocol()
  {
    Assert.Equal(FileType.InspectionProtocol, FileTypeResolver.DetermineFromExtension(".rtlst"));
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
