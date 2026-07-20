using Ask.Core.Services.FilesUtility;

namespace Ask.Engine.UnitTests.FilesUtility;

public class FileFormatterTests
{
  [Theory(DisplayName = "Автоформатирование оставляет один пробел между параметрами команды")]
  [InlineData("1 КН", "1 КН")]
  [InlineData("2 УП      МЕТКА", "2 УП МЕТКА")]
  [InlineData("3 ПР   1<Ом<10,    5с", "3 ПР 1<Ом<10, 5с")]
  [InlineData("4 ЭХТ\t1<Ом<10,\t5с,   0.5Ом,  1А", "4 ЭХТ 1<Ом<10, 5с, 0.5Ом, 1А")]
  public void NormalizeProgramWhitespace_NormalizesParameterSeparators(
    string source,
    string expected)
  {
    string actual = FileFormatter.NormalizeProgramWhitespace(source);

    Assert.Equal(expected, actual);
  }

  [Theory(DisplayName = "Автоформатирование сохраняет выравнивание комментария")]
  [InlineData("1 ПР   1<Ом<10,    5с        // комментарий", "1 ПР 1<Ом<10, 5с        // комментарий")]
  [InlineData("2 КС\t1<Ом<10      { комментарий }", "2 КС 1<Ом<10      { комментарий }")]
  [InlineData("3 СИ  10В,   5<Ом  \t/* комментарий */", "3 СИ 10В, 5<Ом  \t/* комментарий */")]
  public void NormalizeProgramWhitespace_PreservesInlineCommentAlignment(
    string source,
    string expected)
  {
    string actual = FileFormatter.NormalizeProgramWhitespace(source);

    Assert.Equal(expected, actual);
  }

  [Fact(DisplayName = "Автоформатирование убирает выравнивающие пробелы перед точками ЭТ")]
  public void NormalizeProgramWhitespace_EtCommandWithPoints_NormalizesAllParameters()
  {
    const string source =
      "60 ЭТ *К1/11-15#К1/4*К1/21-25*К1/31-35*К1/41-45\r\n" +
      "    *К1/61-65*К1/71-75*К1/81-85*К1/91-95*  {д.6. 50 мА, Ом<=1}\r\n" +
      "80 ЭТ 0.01<Ом<0.15,  0.15Ом    *К1/11,К1/12*";
    string expected =
      "60 ЭТ *К1/11-15#К1/4*К1/21-25*К1/31-35*К1/41-45" + Environment.NewLine +
      "\t*К1/61-65*К1/71-75*К1/81-85*К1/91-95*  {д.6. 50 мА, Ом<=1}" + Environment.NewLine +
      "80 ЭТ 0.01<Ом<0.15, 0.15Ом *К1/11,К1/12*";

    string actual = FileFormatter.NormalizeProgramWhitespace(source);

    Assert.Equal(expected, actual);
  }

  [Theory(DisplayName = "Однострочный блочный комментарий не раскрывается")]
  [InlineData("{ ERR: К1/81-85 и К1/71-75 разобщены }")]
  [InlineData("/* ERR: К1/81-85 и К1/71-75 разобщены */")]
  public void NormalizeProgramWhitespace_SingleLineBlockComment_RemainsSingleLine(string comment)
  {
    string source = "160 ЭТ Ом<40 *К1/11-15*" + Environment.NewLine + "    " + comment;
    string expected = "160 ЭТ Ом<40 *К1/11-15*" + Environment.NewLine + "    " + comment;

    string actual = FileFormatter.NormalizeProgramWhitespace(source);

    Assert.Equal(expected, actual);
  }
}
