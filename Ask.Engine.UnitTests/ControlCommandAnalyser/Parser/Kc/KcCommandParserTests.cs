using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.Entity.Devices;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Model.Ks;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;
using Ask.Engine.ControlCommandAnalyser.Parser.Kc;
using DataBaseConfiguration;
using DataBaseConfiguration.Services.Device;

namespace Ask.Engine.UnitTests.ControlCommandAnalyser.Parser.Kc;

public sealed class KcCommandParserFixture : IDisposable
{
  private readonly int? createdFastMeterId;

  public KcCommandParserFixture()
  {
    DataBaseConfig.InitializeDB().GetAwaiter().GetResult();

    var fastMeterServices = new FastMeterServices();
    fastMeterServices.ReloadCache();

    if (fastMeterServices.GetAll().Any())
    {
      return;
    }

    var entity = new FastMeterEntity
    {
      Name = "Unit test fast meter",
      Description = "Temporary fast meter for KS parser tests",
      NumberChassis = 9999,
      Number = 1,
      ConnectionDetails = "UNIT-TEST",
      DeviceClass = typeof(FastMeterEntity).AssemblyQualifiedName ?? typeof(FastMeterEntity).FullName ?? nameof(FastMeterEntity),
      MaxContinuityResistance = 100
    };

    fastMeterServices.Create(entity);
    createdFastMeterId = entity.Id;
  }

  public void Dispose()
  {
    if (!createdFastMeterId.HasValue)
    {
      return;
    }

    var fastMeterServices = new FastMeterServices();
    var entity = fastMeterServices
      .GetAllEntities()
      .FirstOrDefault(item => item.Id == createdFastMeterId.Value);

    if (entity is not null)
    {
      fastMeterServices.Delete(entity);
    }
  }
}

public class KcCommandParserTests : IClassFixture<KcCommandParserFixture>, IDisposable
{
  private readonly KcCommandParser parser = new();

  public KcCommandParserTests(KcCommandParserFixture fixture)
  {
    CommandsModel.Clear();
    CommandsModel.CommandModels.Add(CreateRmCommand());
  }

  /// <summary>
  /// Проверяет корректный разбор команды КС с закрытым диапазоном сопротивления и парой точек.
  /// </summary>
  [Fact(DisplayName = "КС parser: корректная команда без ошибок разбирает диапазон и точки")]
  public void Parse_WithClosedResistanceRangeAndPoints_ParsesCommandWithoutErrors()
  {
    var model = ParseBody("10<Ом<15 *X1,X2*");

    AssertErrorCodes(model);
    Assert.Equal(10, model.LowerLimitResistance);
    Assert.Equal(15, model.HigherLimitResistance);
    Assert.Equal("10 Ом", model.LowerLimitResistanceSource);
    Assert.Equal("15 Ом", model.HigherLimitResistanceSource);
    Assert.Equal("Ом", model.ResistanceUnit);
    Assert.Equal("*X1,X2*", model.PointsSourse);
    Assert.NotNull(model.Scheme);
    Assert.Equal(2, model.Scheme.CountPoints());
    Assert.Equal(new[] { "1.1.1", "1.1.2" }, model.Scheme.EnumeratePoints().Select(point => point.ToString()).ToArray());
  }

  /// <summary>
  /// Проверяет, что открытая верхняя граница КС корректно превращается в бесконечность.
  /// </summary>
  [Fact(DisplayName = "КС parser: открытая верхняя граница сохраняется как бесконечность")]
  public void Parse_WithOpenUpperLimit_StoresInfinityAsUpperSource()
  {
    var model = ParseBody("10<Ом *X1,X2*");

    AssertErrorCodes(model);
    Assert.Equal(10, model.LowerLimitResistance);
    Assert.Null(model.HigherLimitResistance);
    Assert.Equal("∞ Ом", model.HigherLimitResistanceSource);
  }

  /// <summary>
  /// Проверяет, что открытая нижняя граница КС подставляет минимальное значение из метаданных команды.
  /// </summary>
  [Fact(DisplayName = "КС parser: открытая нижняя граница подставляет минимальное значение из метаданных")]
  public void Parse_WithOpenLowerLimit_UsesMinimumResistanceAsLowerLimit()
  {
    var model = ParseBody("Ом<15 *X1,X2*");

    AssertErrorCodes(model);
    Assert.Equal(0, model.LowerLimitResistance);
    Assert.Equal(15, model.HigherLimitResistance);
    Assert.Equal("0 Ом", model.LowerLimitResistanceSource);
  }

  /// <summary>
  /// Проверяет ошибку KS001, когда в КС не задан ни один предел сопротивления.
  /// </summary>
  [Fact(DisplayName = "КС parser: отсутствие диапазона сопротивления даёт KS001")]
  public void Parse_WithoutResistance_ReturnsKs001()
  {
    var model = ParseBody("*X1,X2*");

    AssertErrorCodes(model, ErrorCode.Ks_EmptyResistance);
    Assert.Null(model.LowerLimitResistance);
    Assert.Null(model.HigherLimitResistance);
  }

  /// <summary>
  /// Проверяет ошибку KS002 и фиксацию хвоста, когда параметры КС не удаётся распознать.
  /// </summary>
  [Fact(DisplayName = "КС parser: мусорные параметры дают KS002 и общую ошибку нераспознанного хвоста")]
  public void Parse_WithGarbageParametersAndPoints_ReturnsKs002AndUnrecognizedParameters()
  {
    var model = ParseBody("abc *X1,X2*");

    AssertErrorCodes(model, ErrorCode.Gen_UnrecognizedParameters, ErrorCode.Ks_CannotParseParameters);
    Assert.Contains("abc", model.UnparsedParameters);
  }

  /// <summary>
  /// Проверяет ошибку KS003, когда диапазон есть, а список точек отсутствует.
  /// </summary>
  [Fact(DisplayName = "КС parser: отсутствие точек даёт KS003")]
  public void Parse_WithoutPoints_ReturnsKs003()
  {
    var model = ParseBody("10<Ом<15");

    AssertErrorCodes(model, ErrorCode.Ks_EmptyPoints);
  }

  /// <summary>
  /// Проверяет ошибку KS004, когда после мнемоники КС тело команды отсутствует.
  /// </summary>
  [Fact(DisplayName = "КС parser: пустое тело команды даёт KS004")]
  public void Parse_WithoutBody_ReturnsKs004()
  {
    var model = ParseBody(string.Empty);

    AssertErrorCodes(model, ErrorCode.Ks_EmptyCommandBody);
  }

  /// <summary>
  /// Проверяет ошибку KS005, когда блок точек КС указан раньше диапазона сопротивления.
  /// </summary>
  [Fact(DisplayName = "КС parser: точки раньше диапазона дают KS005")]
  public void Parse_WithPointsBeforeResistance_ReturnsKs005()
  {
    var model = ParseBody("*X1,X2* 10<Ом<15");

    AssertErrorCodes(model, ErrorCode.Ks_InvalidParameterOrder);
    Assert.Equal(10, model.LowerLimitResistance);
    Assert.Equal(15, model.HigherLimitResistance);
    Assert.NotNull(model.Scheme);
  }

  /// <summary>
  /// Проверяет, что недопустимый для КС ключ фиксируется как ошибка трансляции.
  /// </summary>
  [Fact(DisplayName = "КС parser: недопустимый ключ даёт ошибку Gen_WrongKey")]
  public void Parse_WithNotAllowedKey_ReturnsWrongKeyError()
  {
    var model = ParseBody("ЗР 10<Ом<15 *X1,X2*");

    AssertErrorCodes(model, ErrorCode.Gen_WrongKey);
    Assert.Empty(model.AlgorithmKey);
  }

  /// <summary>
  /// Проверяет конфликт диапазона КС, когда нижняя граница равна верхней.
  /// </summary>
  [Fact(DisplayName = "КС parser: нижняя граница не может быть больше или равна верхней")]
  public void Parse_WhenLowerLimitEqualsOrExceedsUpperLimit_ReturnsKs009()
  {
    var model = ParseBody("15<Ом<15 *X1,X2*");
    var error = AssertSingleError(model, ErrorCode.Ks_CapacityLimitsConflict);

    Assert.Contains("больше или равна верхней", error.Description);
  }

  /// <summary>
  /// Проверяет конфликт диапазона КС, когда нижняя граница выше максимально допустимой.
  /// </summary>
  [Fact(DisplayName = "КС parser: нижняя граница выше максимума даёт KS009")]
  public void Parse_WhenLowerLimitAboveMaximum_ReturnsKs009()
  {
    var model = ParseBody("11<МОм *X1,X2*");
    var error = AssertSingleError(model, ErrorCode.Ks_CapacityLimitsConflict);

    Assert.Contains("больше максимально возможной", error.Description);
  }

  /// <summary>
  /// Проверяет конфликт диапазона КС, когда верхняя граница выше максимально допустимой.
  /// </summary>
  [Fact(DisplayName = "КС parser: верхняя граница выше максимума даёт KS009")]
  public void Parse_WhenUpperLimitAboveMaximum_ReturnsKs009()
  {
    var model = ParseBody("10<Ом<10000001 *X1,X2*");
    var error = AssertSingleError(model, ErrorCode.Ks_CapacityLimitsConflict);

    Assert.Contains("Верхняя граница сопротивления", error.Description);
    Assert.Contains("больше максимально возможной", error.Description);
  }

  /// <summary>
  /// Проверяет, что лишний хвост после корректных параметров не ломает разбор, а фиксируется отдельно.
  /// </summary>
  [Fact(DisplayName = "КС parser: хвостовые параметры фиксируются как нераспознанные без потери корректного разбора")]
  public void Parse_WithTrailingUnrecognizedParameter_KeepsParsedCommandAndAddsGeneralError()
  {
    var model = ParseBody("10<Ом<15 tail *X1,X2*");

    AssertErrorCodes(model, ErrorCode.Gen_UnrecognizedParameters);
    Assert.Contains("tail", model.UnparsedParameters);
    Assert.Equal(10, model.LowerLimitResistance);
    Assert.Equal(15, model.HigherLimitResistance);
    Assert.NotNull(model.Scheme);
  }

  /// <summary>
  /// Проверяет специфическое правило КС: одиночную точку в блоке точек задавать нельзя.
  /// </summary>
  [Fact(DisplayName = "КС parser: одиночная точка даёт ошибку Gen_InvalidOnePointUse")]
  public void Parse_WithSinglePoint_ReturnsInvalidOnePointUse()
  {
    var model = ParseBody("10<Ом<15 *X1*");

    AssertErrorCodes(model, ErrorCode.Gen_InvalidOnePointUse);
    Assert.NotNull(model.Scheme);
    Assert.Equal(1, model.Scheme.CountPoints());
  }

  /// <summary>
  /// Проверяет ветку KS009 в helper-валидации, когда нижняя граница КС уходит ниже минимального значения метаданных.
  /// </summary>
  [Fact(DisplayName = "КС resistance helper: нижняя граница ниже минимума даёт KS009")]
  public void ProcessResistance_WhenLowerLimitBelowMetadataMinimum_ReturnsKs009()
  {
    var model = CreateResistanceModel();

    ResistanceManager.ProcessResistance(model, "-1", "15", "Ом", "10", "КС", 10);

    var error = AssertSingleError(model, ErrorCode.Ks_CapacityLimitsConflict);
    Assert.Contains("меньше минимально измеряемого", error.Description);
  }

  /// <summary>
  /// Проверяет ветку KS009 в helper-валидации, когда верхняя граница КС уходит ниже минимального значения метаданных.
  /// </summary>
  [Fact(DisplayName = "КС resistance helper: верхняя граница ниже минимума даёт KS009")]
  public void ProcessResistance_WhenUpperLimitBelowMetadataMinimum_ReturnsKs009()
  {
    var model = CreateResistanceModel();

    ResistanceManager.ProcessResistance(model, string.Empty, "-1", "Ом", "10", "КС", 10);

    var error = AssertSingleError(model, ErrorCode.Ks_CapacityLimitsConflict);
    Assert.Contains("меньше минимально измеряемого", error.Description);
  }

  public void Dispose()
  {
    CommandsModel.Clear();
  }

  private KsCommandModel ParseBody(string body)
  {
    string line = string.IsNullOrWhiteSpace(body) ? "10 КС" : $"10 КС {body}";
    return Assert.IsType<KsCommandModel>(parser.Parse("10", "КС", 10, new List<string> { line }));
  }

  private static ErrorItem AssertSingleError(KsCommandModel model, ErrorCode expectedCode)
  {
    var error = Assert.Single(model.Errors);
    Assert.Equal(expectedCode, error.Code!.Value);
    Assert.Equal(10, error.SourceLineNumber);
    Assert.Equal("10 КС", error.Command);
    return error;
  }

  private static void AssertErrorCodes(KsCommandModel model, params ErrorCode[] expectedCodes)
  {
    var actualCodes = model.Errors
      .Select(error => error.Code!.Value)
      .OrderBy(code => code.ToString())
      .ToArray();

    var expected = expectedCodes
      .OrderBy(code => code.ToString())
      .ToArray();

    Assert.Equal(expected, actualCodes);
  }

  private static RmCommandModel CreateRmCommand()
  {
    return new RmCommandModel
    {
      CommandNumber = "1",
      StartLineNumber = 1,
      PointsMap = new Dictionary<string, string>
      {
        ["X1"] = "1.1.1",
        ["X2"] = "1.1.2",
        ["X3"] = "1.1.3"
      }
    };
  }

  private static KsCommandModel CreateResistanceModel()
  {
    return new KsCommandModel
    {
      CommandNumber = "10",
      StartLineNumber = 10
    };
  }
}
