using Ask.Core.Shared.DTO.Protocol;

namespace Ask.Engine.UnitTests.Services.Protocols;

public class ProtocolModelFormattingTests
{
  [Fact]
  public void GetProtocolWithErrorsText_RemovesSpaceBeforePointAddress()
  {
    ProtocolModel.SetErrorsTemplate("$ПРОГРАММА");

    try
    {
      var protocol = new ProtocolModel
      {
        ProgramName = "testmkim.pkw",
        Designation = "CT6737.000.000",
        Mode = "Холостой режим",
        Number = "1",
        ControlObjectName = string.Empty,
        Executor = string.Empty,
        Agent = string.Empty,
        Customer = string.Empty,
      };
      protocol.Errors["160 ЭТ"] =
      [
        new ShowMessageModel(message: "*K1/71 [1.6.71]**K1/72 [1.6.72]* R=41,219 Ом")
      ];

      var result = ProtocolModel.GetProtocolWithErrorsText(protocol);

      Assert.Contains("ERR1 160 ЭТ: *K1/71[1.6.71]**K1/72[1.6.72]* R=41,219 Ом", result);
    }
    finally
    {
      ProtocolModel.SetErrorsTemplate(string.Empty);
    }
  }
}
