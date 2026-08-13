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
  public void DiagnosticStorage_IsHiddenForRegularUserAndExpandedForRoot()
  {
    var message = new ShowMessageModel
    {
      Header = "Измерение",
      Message = "12 Ом",
      Debug = "Meter.cs → MeasureAsync (строка 42)",
      IsDeviceMessage = true,
      IndentLevel = 1
    };
    string stored = string.Join("\n", ExecutionProtocolDiagnosticFormatter.FormatForStorage(message));

    string regular = ExecutionProtocolDiagnosticFormatter.PrepareForDisplay(stored, false);
    string root = ExecutionProtocolDiagnosticFormatter.PrepareForDisplay(stored, true);

    Assert.Equal("  Измерение: 12 Ом", regular);
    Assert.Contains("[ОТЛАДКА ROOT] Meter.cs → MeasureAsync (строка 42)", root);
    Assert.Contains("\u2063\u2063    ↳ [ОТЛАДКА ROOT]", root);
    Assert.Contains("тип=Info", root);
    Assert.Contains("оборудование", root);
    Assert.StartsWith("\u2063\uFEFF  Измерение: 12 Ом", root);
  }

  [Fact]
  public void EnvironmentStorage_IsHiddenForRegularUserAndShownFirstForRoot()
  {
    var snapshot = new ExecutionProtocolEnvironmentSnapshot(
      DateTime.Parse("2026-08-13T12:34:56"),
      "1.2.3",
      "Root",
      "Самоконтроль",
      "SelfControl",
      "Полный",
      new Dictionary<string, string> { ["Холостой режим"] = "ВЫКЛ" },
      new[]
      {
        new ExecutionProtocolDeviceSnapshot(
          7, 1, "FastMeter", "Keysight", "Мультиметр", "COM3", "Meter", "Meter", "Connected")
      });
    string stored = ExecutionProtocolDiagnosticFormatter.FormatEnvironmentForStorage(snapshot)
      + "\nСтрока протокола";

    Assert.Equal(
      "Строка протокола",
      ExecutionProtocolDiagnosticFormatter.PrepareForDisplay(stored, false));

    string root = ExecutionProtocolDiagnosticFormatter.PrepareForDisplay(stored, true);
    Assert.StartsWith("================ ДИАГНОСТИКА ROOT", root);
    Assert.Contains("Холостой режим: ВЫКЛ", root);
    Assert.Contains("[FastMeter] Keysight", root);
    Assert.EndsWith("Строка протокола", root);
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

  [Fact]
  public void StructuredStorage_RestoresShowMessageForEveryRoleAndDebugOnlyForRoot()
  {
    var source = new ShowMessageModel
    {
      Header = "Измерение",
      Message = "12 Ом [НОРМА]",
      Time = "00:01.250",
      DiagnosticSource = "Meter.cs → MeasureAsync, строка 42",
      IsDeviceMessage = true,
      IndentLevel = 2
    };
    string stored = string.Join("\n", ExecutionProtocolDiagnosticFormatter.FormatForStorage(source));

    Assert.True(ExecutionProtocolDiagnosticFormatter.TryRestoreMessages(stored, false, out var regular));
    Assert.True(ExecutionProtocolDiagnosticFormatter.TryRestoreMessages(stored, true, out var root));
    Assert.Single(regular);
    Assert.Equal(source.Header, regular[0].Header);
    Assert.Equal(source.Message, regular[0].Message);
    Assert.Equal(source.Time, regular[0].Time);
    Assert.Equal(source.IndentLevel, regular[0].IndentLevel);
    Assert.True(string.IsNullOrEmpty(regular[0].Debug));
    Assert.Contains("[ОТЛАДКА ROOT] Meter.cs", root[0].Debug);
    Assert.Contains("тип=Info", root[0].Debug);
    Assert.Contains("оборудование", root[0].Debug);
  }

  [Fact]
  public void LegacyStorage_RestoresEveryTextLineIncludingEmptyLines()
  {
    const string legacy = "Первая строка\n\n  Вторая строка | 00:01.000";

    var messages = ExecutionProtocolDiagnosticFormatter.RestoreLegacyMessages(legacy, false);

    Assert.Equal(3, messages.Count);
    Assert.Equal("Первая строка", messages[0].Header);
    Assert.Equal(string.Empty, messages[1].Header);
    Assert.Equal("  Вторая строка | 00:01.000", messages[2].Header);
  }
}
