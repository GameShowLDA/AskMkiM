using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Parser.Eht;

public sealed class EhtResistanceManagerTests
{
  [Fact(DisplayName = "ЭТ транслятор: отсутствующий диапазон заменяется пределами 0,01–200 Ом")]
  public void ProcessResistance_EmptyRangeUsesCommandDefaults()
  {
    var model = CreateModel();

    ResistanceManager.ProcessResistance(
      model,
      string.Empty,
      string.Empty,
      string.Empty,
      string.Empty,
      string.Empty,
      "10",
      "ЭТ",
      10);

    Assert.Empty(model.Errors);
    Assert.Equal(0.01, model.LowerLimitResistance);
    Assert.Equal(200, model.HigherLimitResistance);
    Assert.Equal("Ом", model.ResistanceUnit);
  }

  [Fact(DisplayName = "ЭТ транслятор: верхняя граница 200 Ом допустима")]
  public void ProcessResistance_MaximumBoundaryIsAccepted()
  {
    var model = CreateModel();

    ResistanceManager.ProcessResistance(
      model,
      "0.01",
      "200",
      string.Empty,
      "Ом",
      string.Empty,
      "10",
      "ЭТ",
      10);

    Assert.Empty(model.Errors);
    Assert.Equal(200, model.HigherLimitResistance);
  }

  [Theory(DisplayName = "ЭТ транслятор: выход за минимальную или максимальную границу даёт ошибку")]
  [InlineData("0.009", "200")]
  [InlineData("0.01", "200.001")]
  public void ProcessResistance_ValueOutsideMetadataRangeReturnsError(string lower, string upper)
  {
    var model = CreateModel();

    ResistanceManager.ProcessResistance(
      model,
      lower,
      upper,
      string.Empty,
      "Ом",
      string.Empty,
      "10",
      "ЭТ",
      10);

    Assert.Contains(model.Errors, error => error.Code == ErrorCode.Eht_ResistanceLimitsConflict);
  }

  [Fact(DisplayName = "ЭТ транслятор: нижняя граница выше верхней даёт ошибку")]
  public void ProcessResistance_LowerBoundAboveUpperBoundReturnsError()
  {
    var model = CreateModel();

    ResistanceManager.ProcessResistance(
      model,
      "101",
      "100",
      string.Empty,
      "Ом",
      string.Empty,
      "10",
      "ЭТ",
      10);

    var error = Assert.Single(model.Errors);
    Assert.Equal(ErrorCode.Eht_ResistanceLimitsConflict, error.Code);
    Assert.Contains("Нижняя граница больше верхней", error.Description);
  }

  [Fact(DisplayName = "ЭТ транслятор: килоомы преобразуются в омы до проверки максимума")]
  public void ParameterPipeline_KiloOhmsAreValidatedInOhms()
  {
    var model = CreateModel();
    var context = ParameterContext.Create("10", "ЭТ", 10);

    var remainder = EhtParameterPipeline.Execute(model, "0,01<кОм<0,2", context);

    Assert.Equal(string.Empty, remainder);
    Assert.Empty(model.Errors);
    Assert.Equal(10, model.LowerLimitResistance);
    Assert.Equal(200, model.HigherLimitResistance);
    Assert.Equal("Ом", model.ResistanceUnit);
  }

  [Fact(DisplayName = "ЭТ транслятор: сопротивление кабеля нормализуется в омы")]
  public void ParameterPipeline_CableResistanceIsNormalizedToOhms()
  {
    var model = CreateModel();
    var context = ParameterContext.Create("10", "ЭТ", 10);

    var remainder = EhtParameterPipeline.Execute(model, "10<Ом<20, 0,002 кОм", context);

    Assert.Equal(string.Empty, remainder);
    Assert.Equal(2, model.CabelResistance);
    Assert.Equal("Ом", model.CabelResistanceUnit);
  }

  private static EhtCommandModel CreateModel() => new()
  {
    CommandNumber = "10",
    StartLineNumber = 10
  };
}
