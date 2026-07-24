using Ask.Core.Services.Protocols;
using Ask.Core.Shared.DTO.Protocol;

namespace Ask.Engine.UnitTests.Services.Protocols;

public class ExecutionProtocolLineFormatterTests
{
  [Fact]
  public void Format_IncludesColonTimeAndIndentWithoutDebugSource()
  {
    var message = new ShowMessageModel
    {
      Header = "Проверка подключения",
      Message = "Соединение установлено [НОРМА]",
      Time = "00:00.125",
      Debug = "DeviceService.cs → Connect (строка 42)",
      IndentLevel = 1
    };

    string result = ExecutionProtocolLineFormatter.Format(message);

    Assert.Equal(
      "  Проверка подключения: Соединение установлено [НОРМА] | 00:00.125",
      result);
  }

  [Fact]
  public void Format_DoesNotAddColonWithoutMessage()
  {
    var message = new ShowMessageModel
    {
      Header = "Настройка оборудования"
    };

    string result = ExecutionProtocolLineFormatter.Format(message);

    Assert.Equal("Настройка оборудования", result);
  }
}
