using Ask.Engine.ControlCommandAnalyser.RmTranslation.Diagnostics;
using Ask.Engine.ControlCommandAnalyser.RmTranslation.Models;
using Ask.Engine.ControlCommandAnalyser.RmTranslation.Translation;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.RmTranslation;

public class RelaySwitchModuleLegacyAddressMapperTests
{
  private static readonly TextSpan Span = new(0, 0, 1, 1);

  [Theory(DisplayName = "Режим совместимости РМ: старые блоки по 100 точек раскладываются по PointCount реальных модулей")]
  [InlineData(1, 1, 2, 1)]
  [InlineData(1, 100, 2, 100)]
  [InlineData(2, 1, 2, 101)]
  [InlineData(3, 100, 2, 300)]
  [InlineData(4, 50, 2, 350)]
  [InlineData(4, 51, 4, 1)]
  [InlineData(5, 1, 4, 51)]
  public void Map_WithContinuousLegacyBlocks_ReturnsRealModuleAndPoint(
    int legacyModule,
    int legacyPoint,
    int expectedModule,
    int expectedPoint)
  {
    var mapper = CreateLegacyMapper((2, 350), (4, 350), (6, 350));

    var result = mapper.Map(new MachineAddress(1, legacyModule, legacyPoint), Span);

    Assert.True(result.IsSuccess);
    Assert.Equal(new MachineAddress(1, expectedModule, expectedPoint), result.Address);
  }

  [Fact(DisplayName = "Режим совместимости РМ: старый адрес за пределами общего PointCount даёт диагностическую ошибку")]
  public void Map_WithMissingLegacyPoint_ReturnsDiagnostic()
  {
    var mapper = CreateLegacyMapper((2, 350), (4, 350), (6, 350));

    var result = mapper.Map(new MachineAddress(1, 12, 1), Span);

    var diagnostic = Assert.Single(result.Diagnostics);
    Assert.False(result.IsSuccess);
    Assert.Equal(RmDiagnosticCode.LegacyCompatibilityAddress, diagnostic.Code);
    Assert.Contains("1.12.1", diagnostic.Message);
    Assert.Contains("1050", diagnostic.Message);
  }

  [Fact(DisplayName = "Режим совместимости РМ: точка больше 100 в старом блоке даёт диагностическую ошибку")]
  public void Map_WithLegacyPointOutOfRange_ReturnsDiagnostic()
  {
    var mapper = CreateLegacyMapper((2, 350), (4, 350), (6, 350));

    var result = mapper.Map(new MachineAddress(1, 1, 400), Span);

    var diagnostic = Assert.Single(result.Diagnostics);
    Assert.False(result.IsSuccess);
    Assert.Equal(RmDiagnosticCode.LegacyCompatibilityAddress, diagnostic.Code);
    Assert.Contains("1.1.400", diagnostic.Message);
    Assert.Contains("100", diagnostic.Message);
  }

  [Fact(DisplayName = "Прямой режим РМ: адрес существующего модуля проходит без преобразования")]
  public void DirectValidator_WithConfiguredModule_ReturnsSameAddress()
  {
    var validator = CreateDirectValidator((2, 350), (4, 350), (6, 350));

    var result = validator.Map(new MachineAddress(1, 4, 9), Span);

    Assert.True(result.IsSuccess);
    Assert.Equal(new MachineAddress(1, 4, 9), result.Address);
  }

  [Fact(DisplayName = "Прямой режим РМ: отсутствующий модуль даёт диагностическую ошибку")]
  public void DirectValidator_WithMissingModule_ReturnsDiagnostic()
  {
    var validator = CreateDirectValidator((2, 350), (4, 350), (6, 350));

    var result = validator.Map(new MachineAddress(1, 1, 9), Span);

    var diagnostic = Assert.Single(result.Diagnostics);
    Assert.False(result.IsSuccess);
    Assert.Equal(RmDiagnosticCode.MachineAddressNotConfigured, diagnostic.Code);
    Assert.Contains("модуль 1 отсутствует", diagnostic.Message);
  }

  [Fact(DisplayName = "Прямой режим РМ: точка больше PointCount даёт диагностическую ошибку")]
  public void DirectValidator_WithPointOutOfRange_ReturnsDiagnostic()
  {
    var validator = CreateDirectValidator((2, 350), (4, 350), (6, 350));

    var result = validator.Map(new MachineAddress(1, 2, 400), Span);

    var diagnostic = Assert.Single(result.Diagnostics);
    Assert.False(result.IsSuccess);
    Assert.Equal(RmDiagnosticCode.MachineAddressNotConfigured, diagnostic.Code);
    Assert.Contains("точка 400 отсутствует", diagnostic.Message);
    Assert.Contains("350", diagnostic.Message);
  }

  private static RelaySwitchModuleLegacyAddressMapper CreateLegacyMapper(params (int Number, int PointCount)[] modules)
    => new(modules.Select(module => new LegacyRelaySwitchModuleInfo(module.Number, module.PointCount)));

  private static RelaySwitchModuleAddressValidator CreateDirectValidator(params (int Number, int PointCount)[] modules)
    => new(modules.Select(module => new LegacyRelaySwitchModuleInfo(module.Number, module.PointCount)));
}
