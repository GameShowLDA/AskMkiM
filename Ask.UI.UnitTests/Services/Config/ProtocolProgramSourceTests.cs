using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Protocol;

namespace Ask.UI.UnitTests.Services.Config;

public sealed class ProtocolProgramSourceTests
{
  [Theory]
  [InlineData(null, @"D:\Programs\nr01-pr.acs", @"nr01-pr.acs (D:\Programs\nr01-pr.acs)")]
  [InlineData("Тесты.apkw", @"C:\Temp\nr01-pr_123.acs", "nr01-pr.acs (Тесты.apkw)")]
  [InlineData(null, null, "nr01-pr.acs")]
  [InlineData("", "", "nr01-pr.acs")]
  public void BothProtocolTemplatesIncludeActualSource(string? archiveName, string? path, string expected)
  {
    try
    {
      ProtocolModel.SetTemplate("Программа проверки: $ПРОГРАММА");
      ProtocolModel.SetErrorsTemplate("Программа проверки: $ПРОГРАММА");
      var model = new ProtocolModel
      {
        ProgramName = "nr01-pr.acs", ProgramPath = path!, SourceArchiveName = archiveName, Number = "1"
      };
      Assert.Equal($"Программа проверки: {expected}", ProtocolModel.GetProtocolText(model));
      Assert.Contains($"Программа проверки: {expected}", ProtocolModel.GetProtocolWithErrorsText(model));
      Assert.Equal(path, model.ProgramPath);
      Assert.Equal("nr01-pr.acs", model.ProgramName);
    }
    finally
    {
      ProtocolModel.SetTemplate(ProtocolConfig.GetBaseTextProtocol());
      ProtocolModel.SetErrorsTemplate(ProtocolConfig.GetBaseTextErrorsProtocol());
    }
  }
}
