using Ask.Core.Services.Config.AppSettings;

namespace Ask.UI.UnitTests.Services.Config;

public sealed class ProtocolConfigTests
{
  [Fact]
  public void ShouldShowProtocolInfoDialog_UsesPrintOrSoftwareOutputSettings()
  {
    bool originalPrintProtocol = ProtocolConfig.GetPrintProtocol();
    bool originalShowProtocolInSoftware = ProtocolConfig.GetShowProtocolInSoftware();

    try
    {
      AssertDialogVisibility(printProtocol: false, showProtocolInSoftware: false, expected: false);
      AssertDialogVisibility(printProtocol: true, showProtocolInSoftware: false, expected: true);
      AssertDialogVisibility(printProtocol: false, showProtocolInSoftware: true, expected: true);
      AssertDialogVisibility(printProtocol: true, showProtocolInSoftware: true, expected: true);
    }
    finally
    {
      ProtocolConfig.SetPrintProtocol(originalPrintProtocol);
      ProtocolConfig.SetShowProtocolInSoftware(originalShowProtocolInSoftware);
    }
  }

  private static void AssertDialogVisibility(
    bool printProtocol,
    bool showProtocolInSoftware,
    bool expected)
  {
    ProtocolConfig.SetPrintProtocol(printProtocol);
    ProtocolConfig.SetShowProtocolInSoftware(showProtocolInSoftware);

    Assert.Equal(expected, ProtocolConfig.ShouldShowProtocolInfoDialog());
  }
}
