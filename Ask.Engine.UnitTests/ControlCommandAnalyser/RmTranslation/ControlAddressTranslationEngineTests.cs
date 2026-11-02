using Ask.Engine.ControlCommandAnalyser.RmTranslation.Diagnostics;
using Ask.Engine.ControlCommandAnalyser.RmTranslation.Models;
using Ask.Engine.ControlCommandAnalyser.RmTranslation.Translation;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.RmTranslation;

public class ControlAddressTranslationEngineTests
{
  [Fact]
  public void Translate_WithConnectorRange_MapsContactsAcrossBlocks()
  {
    var result = new ControlAddressTranslationEngine().Translate("К1-К6/1 =1.6.1-6");

    AssertSuccess(result);
    Assert.Equal(6, result.Entries.Count);
    for (var i = 1; i <= 6; i++)
      AssertEntry(result.Entries[i - 1], $"К{i}/1", $"1.{i}.1");
  }

  [Theory(DisplayName = "Движок РМ: одиночные примеры из документации разбираются без ошибок")]
  [InlineData("X1/A1-A30=1.1.1-1.1.30", 30, "X1/A1", "1.1.1", "X1/A30", "1.1.30")]
  [InlineData("X1/B1-B31=1.1.31-1.1.61", 31, "X1/B1", "1.1.31", "X1/B31", "1.1.61")]
  [InlineData("X10/1-50=1.2.1-50", 50, "X10/1", "1.2.1", "X10/50", "1.2.50")]
  [InlineData("16/1-32=1.2.51-82", 32, "16/1", "1.2.51", "16/32", "1.2.82")]
  [InlineData("19A=1.2.100", 1, "19A", "1.2.100", "19A", "1.2.100")]
  [InlineData("X20/1=1.2.100", 1, "X20/1", "1.2.100", "X20/1", "1.2.100")]
  [InlineData("101-200=1.3.1-100", 100, "101", "1.3.1", "200", "1.3.100")]
  [InlineData("X16/2=1.2.83", 1, "X16/2", "1.2.83", "X16/2", "1.2.83")]
  public void Translate_WithDocumentationExample_ExpandsExpectedEntries(
    string input,
    int expectedCount,
    string firstObject,
    string firstMachine,
    string lastObject,
    string lastMachine)
  {
    var result = new ControlAddressTranslationEngine().Translate(input);

    AssertSuccess(result);
    Assert.Equal(expectedCount, result.Entries.Count);
    AssertEntry(result.Entries[0], firstObject, firstMachine);
    AssertEntry(result.Entries[^1], lastObject, lastMachine);
  }

  [Fact(DisplayName = "Движок РМ: синонимы с диапазоном и шагом разворачиваются вместе с точками")]
  public void Translate_WithSteppedSynonymRange_ExpandsSynonyms()
  {
    var result = new ControlAddressTranslationEngine().Translate("301-310==XS1:1-10=1.6.2-20(2)");

    AssertSuccess(result);
    Assert.Equal(10, result.Entries.Count);
    AssertEntry(result.Entries[0], "301", "1.6.2", "XS1:1");
    AssertEntry(result.Entries[^1], "310", "1.6.20", "XS1:10");
  }

  [Fact(DisplayName = "Движок РМ: координатные синонимы с десятичным шагом разворачиваются полностью")]
  public void Translate_WithCoordinateSynonymRange_ExpandsDecimalCoordinates()
  {
    var result = new ControlAddressTranslationEngine().Translate("A15-1(-1)==X11.00-28.50(1.25)/Y17.25=1.1.1-15");

    AssertSuccess(result);
    Assert.Equal(15, result.Entries.Count);
    AssertEntry(result.Entries[0], "A15", "1.1.1", "X11.00/Y17.25");
    AssertEntry(result.Entries[^1], "A1", "1.1.15", "X28.50/Y17.25");
  }

  [Theory(DisplayName = "Движок РМ: одиночные координатные синонимы разбираются без ошибок")]
  [InlineData("D12.3==X14.125/Y16.375=1.2.15", "D12.3", "X14.125/Y16.375", "1.2.15")]
  [InlineData("R2:1==X4.000/Y5.625=1.3.16", "R2:1", "X4.000/Y5.625", "1.3.16")]
  public void Translate_WithSingleCoordinateSynonym_StoresSynonym(
    string input,
    string objectAddress,
    string synonym,
    string machineAddress)
  {
    var result = new ControlAddressTranslationEngine().Translate(input);

    AssertSuccess(result);
    var entry = Assert.Single(result.Entries);
    AssertEntry(entry, objectAddress, machineAddress, synonym);
  }

  [Fact(DisplayName = "Движок РМ: несколько записей в одной строке через пробелы и табы разбираются отдельно")]
  public void Translate_WithMixedWhitespace_ParsesSeveralMappings()
  {
    var result = new ControlAddressTranslationEngine().Translate("X2/1=1.1.62\tX12/1=1.1.63  X14/1=1.1.64");

    AssertSuccess(result);
    Assert.Equal(new[] { "X2/1", "X12/1", "X14/1" }, result.Entries.Select(entry => entry.ObjectAddress.Value).ToArray());
  }

  [Fact(DisplayName = "Движок РМ: поиск работает по ОК, синониму и машинному адресу")]
  public void CreateIndex_WithTranslatedEntries_SearchesAllKeys()
  {
    var result = new ControlAddressTranslationEngine().Translate("X1/1==S1=1.1.1 X2/1=1.1.2");
    var index = result.CreateIndex();

    Assert.True(index.TryGetByObjectAddress("X1/1", out var byObject));
    Assert.True(index.TryGetBySynonym("S1", out var bySynonym));
    Assert.True(index.TryGetByMachineAddress(new MachineAddress(1, 1, 2), out var byMachine));
    Assert.Equal("1.1.1", byObject.MachineAddress.ToString());
    Assert.Equal("X1/1", bySynonym.ObjectAddress.Value);
    Assert.Equal("X2/1", byMachine.ObjectAddress.Value);
  }

  [Fact(DisplayName = "Движок РМ: альтернативный порядок синонима поддерживает запись синоним-ОК-АСК")]
  public void Translate_WithSynonymThenObjectMode_BindsLeftSideAsSynonym()
  {
    var engine = new ControlAddressTranslationEngine(new RmTranslationOptions(SynonymBindingMode.SynonymThenObject));

    var result = engine.Translate("S1==X1/1=1.1.1");

    AssertSuccess(result);
    var entry = Assert.Single(result.Entries);
    AssertEntry(entry, "X1/1", "1.1.1", "S1");
  }

  [Fact(DisplayName = "Движок РМ: legacy-mapper преобразует машинные адреса до семантической проверки")]
  public void Translate_WithLegacyAddressMapper_ReturnsRealMachineAddresses()
  {
    var mapper = new RelaySwitchModuleLegacyAddressMapper(new[]
    {
      new LegacyRelaySwitchModuleInfo(2, 350),
      new LegacyRelaySwitchModuleInfo(4, 350),
      new LegacyRelaySwitchModuleInfo(6, 350)
    });
    var engine = new ControlAddressTranslationEngine(new RmTranslationOptions(
      SynonymBindingMode.ObjectThenSynonym,
      mapper));

    var result = engine.Translate("X1/1=1.1.25 X2/1=1.2.100 X3/1=1.3.100");

    AssertSuccess(result);
    Assert.Equal(new[] { "1.2.25", "1.2.200", "1.2.300" }, result.Entries.Select(entry => entry.MachineAddress.ToString()).ToArray());
  }

  [Fact(DisplayName = "Движок РМ: отсутствующий legacy-хвост сворачивается в одну ошибку диапазона")]
  public void Translate_WithLegacyTailOutsideConfiguredPointCount_ReturnsSingleRangeDiagnostic()
  {
    var mapper = new RelaySwitchModuleLegacyAddressMapper(new[]
    {
      new LegacyRelaySwitchModuleInfo(2, 350),
      new LegacyRelaySwitchModuleInfo(4, 350)
    });
    var engine = new ControlAddressTranslationEngine(new RmTranslationOptions(
      SynonymBindingMode.ObjectThenSynonym,
      mapper));

    const string input = "X1-X8=1.7.93-100 X9-X18=1.8.1-10 X19-X28=1.8.11-20";
    var result = engine.Translate(input);

    var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == RmDiagnosticCode.MachineAddressNotConfigured);
    Assert.Contains("Адреса с 1.8.1 по 1.8.20", diagnostic.Message);
    Assert.Contains("конфигурации модулей релейного коммутатора", diagnostic.Message);
    Assert.DoesNotContain("RelaySwitchModules", diagnostic.Message);
    Assert.DoesNotContain("PointCount", diagnostic.Message);
    Assert.Equal("1.8.1-10", input.Substring(diagnostic.Span.Start, diagnostic.Span.Length));
    Assert.Equal(new MachineAddress(1, 4, 350), result.Entries[^1].MachineAddress);
  }

  [Fact(DisplayName = "Движок РМ: отсутствие знака равно даёт диагностическое сообщение")]
  public void Translate_WithoutEquals_ReturnsExpectedEqualsDiagnostic()
  {
    var result = new ControlAddressTranslationEngine().Translate("X1/1 1.1.1");

    Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == RmDiagnosticCode.ExpectedEquals);
  }

  [Fact(DisplayName = "Движок РМ: разные длины диапазонов дают ошибку")]
  public void Translate_WithRangeLengthMismatch_ReturnsDiagnostic()
  {
    const string input = "X1/1-30=1.1.1-20";
    var result = new ControlAddressTranslationEngine().Translate(input);

    var diagnostic = Assert.Single(
      result.Diagnostics,
      item => item.Code == RmDiagnosticCode.RangeLengthMismatch);
    Assert.Equal("X1/1-30", input.Substring(diagnostic.Span.Start, diagnostic.Span.Length));
  }

  [Fact(DisplayName = "Движок РМ: несовпадение длины синонимов указывает только диапазон синонимов")]
  public void Translate_WithSynonymRangeLengthMismatch_SelectsSynonymRange()
  {
    const string input = "X1/1-2==S1-S3=1.1.1-2";
    var result = new ControlAddressTranslationEngine().Translate(input);

    var diagnostic = Assert.Single(
      result.Diagnostics,
      item => item.Code == RmDiagnosticCode.RangeLengthMismatch);
    Assert.Equal("S1-S3", input.Substring(diagnostic.Span.Start, diagnostic.Span.Length));
  }

  [Fact(DisplayName = "Движок РМ: неправильный машинный адрес даёт ошибку")]
  public void Translate_WithInvalidMachineAddress_ReturnsDiagnostic()
  {
    var result = new ControlAddressTranslationEngine().Translate("X1/1=1.2");

    Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == RmDiagnosticCode.InvalidMachineAddress);
  }

  [Theory(DisplayName = "Движок РМ: дубликаты и пересечения диапазонов диагностируются")]
  [InlineData("X1/1=1.1.1 X1/1=1.1.2", RmDiagnosticCode.DuplicateObjectAddress)]
  [InlineData("X1/1=1.1.1 X2/1=1.1.1", RmDiagnosticCode.DuplicateMachineAddress)]
  [InlineData("X1/1==S1=1.1.1 X2/1==S1=1.1.2", RmDiagnosticCode.DuplicateSynonym)]
  [InlineData("X1/1-3=1.1.1-3 X1/3-5=1.1.4-6", RmDiagnosticCode.DuplicateObjectAddress)]
  public void Translate_WithDuplicateAddresses_ReturnsDiagnostic(string input, RmDiagnosticCode expectedCode)
  {
    var result = new ControlAddressTranslationEngine().Translate(input);

    Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == expectedCode);
  }

  [Fact(DisplayName = "Движок РМ: пересечение диапазонов ОК сворачивается в одну ошибку")]
  public void Translate_WithOverlappingObjectAddressRanges_ReturnsSingleRangeDiagnostic()
  {
    const string input = "X1/1-5=1.1.1-5 X1/3-7=1.2.1-5";

    var result = new ControlAddressTranslationEngine().Translate(input);

    var diagnostic = Assert.Single(
      result.Diagnostics,
      item => item.Code == RmDiagnosticCode.DuplicateObjectAddress);
    Assert.Equal("Повторяющиеся адреса ОК: X1/3-X1/5.", diagnostic.Message);
    Assert.Equal(
      "X1/3-7=1.2.1-5",
      input.Substring(diagnostic.Span.Start, diagnostic.Span.Length));
  }

  [Fact(DisplayName = "Движок РМ: пересечение диапазонов машинных адресов сворачивается в одну ошибку")]
  public void Translate_WithOverlappingMachineAddressRanges_ReturnsSingleRangeDiagnostic()
  {
    const string input = "X1/1-5=1.1.1-5 X2/1-5=1.1.3-7";

    var result = new ControlAddressTranslationEngine().Translate(input);

    var diagnostic = Assert.Single(
      result.Diagnostics,
      item => item.Code == RmDiagnosticCode.DuplicateMachineAddress);
    Assert.Equal(
      "Повторяющиеся машинные адреса: 1.1.3-1.1.5.",
      diagnostic.Message);
    Assert.Equal(
      "X2/1-5=1.1.3-7",
      input.Substring(diagnostic.Span.Start, diagnostic.Span.Length));
  }

  [Fact(DisplayName = "Движок РМ: пересечение диапазонов синонимов сворачивается в одну ошибку")]
  public void Translate_WithOverlappingSynonymRanges_ReturnsSingleRangeDiagnostic()
  {
    const string input =
      "X1/1-5==S1-S5=1.1.1-5 X2/1-5==S3-S7=1.2.1-5";

    var result = new ControlAddressTranslationEngine().Translate(input);

    var diagnostic = Assert.Single(
      result.Diagnostics,
      item => item.Code == RmDiagnosticCode.DuplicateSynonym);
    Assert.Equal("Синонимы уже используются: S3-S5.", diagnostic.Message);
    Assert.Equal(
      "X2/1-5==S3-S7=1.2.1-5",
      input.Substring(diagnostic.Span.Start, diagnostic.Span.Length));
  }

  [Fact(DisplayName = "Движок РМ: недопустимый символ даёт ошибку лексера")]
  public void Translate_WithForbiddenCharacter_ReturnsDiagnostic()
  {
    var result = new ControlAddressTranslationEngine().Translate("X1/$1=1.1.1");

    Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == RmDiagnosticCode.UnexpectedCharacter);
  }

  private static void AssertSuccess(TranslationResult result)
  {
    Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
  }

  private static void AssertEntry(AddressMapping entry, string objectAddress, string machineAddress, string? synonym = null)
  {
    Assert.Equal(objectAddress, entry.ObjectAddress.Value);
    Assert.Equal(machineAddress, entry.MachineAddress.ToString());
    Assert.Equal(synonym, entry.Synonym?.Value);
  }
}
