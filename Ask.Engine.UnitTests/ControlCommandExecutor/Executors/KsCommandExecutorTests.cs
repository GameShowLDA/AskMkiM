using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Ks;
using Ask.Engine.ControlCommandAnalyser.Parser.Kc;
using Ask.Engine.ControlCommandExecutor.Execution;
using Ask.Engine.ControlCommandExecutor.Executors;
using Ask.Engine.UnitTests.Fixtures;
using Moq;

namespace Ask.Engine.UnitTests.ControlCommandExecutor.Executors;

public class KsCommandExecutorTests : IClassFixture<FastMeterDbFixture>, IDisposable
{
  public KsCommandExecutorTests(FastMeterDbFixture fixture)
  {
  }

  /// <summary>
  /// Проверяет, что команда КС с ключом Д при успешном измерении пишет одно информационное сообщение в протокол
  /// и использует обычный режим измерения сопротивления.
  /// </summary>
  [Fact(DisplayName = "КС: ключ Д пишет успешное измерение в документирование")]
  public async Task ExecuteAsync_WithDocumentationKey_WritesInfoToProtocol()
  {
    using var harness = new KsExecutionHarness();
    var command = CreateCommand(5, 15, "\u0414");

    harness.ResistanceManagerMock
      .Setup(x => x.MeasureResistanceAsync(10, 5, 15, It.IsAny<IUserInteractionService>()))
      .ReturnsAsync(10);

    var protocol = await harness.ExecuteAsync(command);

    Assert.Empty(protocol.Errors);
    AssertProtocolMessages(protocol.Info, command, expectedCount: 1);
    AssertMessage(protocol.Info[GetCommandKey(command)][0], "X1, X2(5-15 Ом)", "Rизм= 10 Ом");
    harness.ResistanceManagerMock.Verify(x => x.SetResistanceModeAsync(It.IsAny<IUserInteractionService>()), Times.Once);
    harness.ResistanceManagerMock.Verify(x => x.MeasureResistanceAsync(10, 5, 15, It.IsAny<IUserInteractionService>()), Times.Once);
    harness.ContinuityManagerMock.Verify(x => x.SetContinuityModeAsync(It.IsAny<IUserInteractionService>()), Times.Never);
    harness.ConnectorManagerMock.Verify(x => x.ConnectMultimeter(SwitchingBusNew.AB1, It.IsAny<IUserInteractionService>()), Times.Once);
    harness.EditorMock.Verify(x => x.SetActiveLine(3), Times.Once);
  }

  /// <summary>
  /// Проверяет, что ключ Б переключает КС в режим прозвонки:
  /// используется ContinuityManager, а обычное измерение сопротивления не вызывается.
  /// </summary>
  [Fact(DisplayName = "КС: ключ Б переключает исполнение в режим прозвонки")]
  public async Task ExecuteAsync_WithContinuityKey_UsesContinuityMeasurement()
  {
    using var harness = new KsExecutionHarness();
    var command = CreateCommand(5, 15, "\u0411", "\u0414");

    harness.ContinuityManagerMock
      .Setup(x => x.CheckContinuityAsync(10, 5, 15, It.IsAny<IUserInteractionService>()))
      .ReturnsAsync(10);

    var protocol = await harness.ExecuteAsync(command);

    Assert.Empty(protocol.Errors);
    AssertProtocolMessages(protocol.Info, command, expectedCount: 1);
    harness.ContinuityManagerMock.Verify(x => x.SetContinuityModeAsync(It.IsAny<IUserInteractionService>()), Times.Once);
    harness.ContinuityManagerMock.Verify(x => x.CheckContinuityAsync(10, 5, 15, It.IsAny<IUserInteractionService>()), Times.Once);
    harness.ResistanceManagerMock.Verify(x => x.SetResistanceModeAsync(It.IsAny<IUserInteractionService>()), Times.Never);
    harness.ResistanceManagerMock.Verify(
      x => x.MeasureResistanceAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<IUserInteractionService>()),
      Times.Never);
  }

  /// <summary>
  /// Проверяет, что без ключа Д успешное измерение не попадает в блок документирования протокола.
  /// </summary>
  [Fact(DisplayName = "КС: без ключа Д успешное измерение не записывается в документирование")]
  public async Task ExecuteAsync_WithoutDocumentationKey_DoesNotWriteInfo()
  {
    using var harness = new KsExecutionHarness();
    var command = CreateCommand(5, 15);

    harness.ResistanceManagerMock
      .Setup(x => x.MeasureResistanceAsync(10, 5, 15, It.IsAny<IUserInteractionService>()))
      .ReturnsAsync(10);

    var protocol = await harness.ExecuteAsync(command);

    Assert.Empty(protocol.Errors);
    Assert.Empty(protocol.Info);
  }

  /// <summary>
  /// Проверяет, что при выходе измеренного сопротивления за допустимый диапазон
  /// команда пишет ошибку в протокол и публикует ошибку выполнения.
  /// </summary>
  [Fact(DisplayName = "КС: выход за диапазон формирует ошибку протокола и ошибку выполнения")]
  public async Task ExecuteAsync_WhenMeasurementFails_WritesErrorAndPublishesExecutionError()
  {
    using var harness = new KsExecutionHarness();
    var command = CreateCommand(5, 15);

    harness.ResistanceManagerMock
      .Setup(x => x.MeasureResistanceAsync(10, 5, 15, It.IsAny<IUserInteractionService>()))
      .ReturnsAsync(20);

    var protocol = await harness.ExecuteAsync(command);

    AssertProtocolMessages(protocol.Errors, command, expectedCount: 1);
    AssertMessage(protocol.Errors[GetCommandKey(command)][0], "X1, X2 (5-15 Ом)", "Rизм= 20 Ом");
    Assert.Empty(protocol.Info);
    Assert.Single(harness.PublishedErrors);
    Assert.Contains("X1", harness.PublishedErrors[0].Description);
    Assert.Contains("X2", harness.PublishedErrors[0].Description);
    Assert.Equal("Rизм= 20 Ом", harness.PublishedErrors[0].MeasureResult);
  }

  /// <summary>
  /// Проверяет, что при ошибочном измерении и наличии ключа Д
  /// команда одновременно пишет ошибку и сохраняет измеренное значение в документирование.
  /// </summary>
  [Fact(DisplayName = "КС: при ошибке с ключом Д измерение попадает и в ошибки, и в документирование")]
  public async Task ExecuteAsync_WhenMeasurementFailsWithDocumentationKey_WritesErrorAndInfo()
  {
    using var harness = new KsExecutionHarness();
    var command = CreateCommand(5, 15, "\u0414");

    harness.ResistanceManagerMock
      .Setup(x => x.MeasureResistanceAsync(10, 5, 15, It.IsAny<IUserInteractionService>()))
      .ReturnsAsync(20);

    var protocol = await harness.ExecuteAsync(command);

    AssertProtocolMessages(protocol.Errors, command, expectedCount: 1);
    AssertProtocolMessages(protocol.Info, command, expectedCount: 1);
    Assert.Single(harness.PublishedErrors);
  }

  /// <summary>
  /// Проверяет, что при открытой верхней границе исполнитель использует целевое значение firstValue + 10,
  /// а не пытается измерять по несуществующему верхнему пределу.
  /// </summary>
  [Fact(DisplayName = "КС: без верхней границы используется целевое значение нижней границы плюс десять")]
  public async Task ExecuteAsync_WithoutHigherLimit_UsesLowerLimitPlusTenAsMeasurementTarget()
  {
    using var harness = new KsExecutionHarness();
    var command = CreateCommand(5, null, "\u0414");

    harness.ResistanceManagerMock
      .Setup(x => x.MeasureResistanceAsync(15, 5, -1, It.IsAny<IUserInteractionService>()))
      .ReturnsAsync(15);

    var protocol = await harness.ExecuteAsync(command);

    Assert.Empty(protocol.Errors);
    AssertProtocolMessages(protocol.Info, command, expectedCount: 1);
    harness.ResistanceManagerMock.Verify(x => x.MeasureResistanceAsync(15, 5, -1, It.IsAny<IUserInteractionService>()), Times.Once);
  }

  /// <summary>
  /// Проверяет рабочий режим: из измеренного значения вычитается сопротивление коммутатора,
  /// поэтому результат может перейти из допустимого диапазона в ошибку.
  /// </summary>
  [Fact(DisplayName = "КС: в рабочем режиме вычитается сопротивление коммутатора")]
  public async Task ExecuteAsync_InWorkingMode_SubtractsSwitchResistanceBeforeValidation()
  {
    using var harness = new KsExecutionHarness(switchResistance: 1);
    var command = CreateCommand(0.5, 5);

    harness.ResistanceManagerMock
      .Setup(x => x.MeasureResistanceAsync(2.75, 0.5, 5, It.IsAny<IUserInteractionService>()))
      .ReturnsAsync(1);

    var protocol = await harness.ExecuteAsync(command);

    AssertProtocolMessages(protocol.Errors, command, expectedCount: 1);
    Assert.Empty(protocol.Info);
  }

  /// <summary>
  /// Проверяет защиту от отрицательных значений после вычитания сопротивления коммутатора:
  /// исполнитель должен прижать результат к нулю и показать это в сообщении об ошибке.
  /// </summary>
  [Fact(DisplayName = "КС: отрицательный результат после вычитания сопротивления прижимается к нулю")]
  public async Task ExecuteAsync_WhenSwitchResistanceMakesValueNegative_ClampsMeasurementToZero()
  {
    using var harness = new KsExecutionHarness(switchResistance: 10);
    var command = CreateCommand(0.5, 5);

    harness.ResistanceManagerMock
      .Setup(x => x.MeasureResistanceAsync(2.75, 0.5, 5, It.IsAny<IUserInteractionService>()))
      .ReturnsAsync(5);

    var protocol = await harness.ExecuteAsync(command);
    var error = Assert.Single(protocol.Errors[GetCommandKey(command)]);

    AssertMessage(error, "X1, X2 (0,5-5 Ом)", "Rизм= 0 Ом");
    Assert.Single(harness.PublishedErrors);
    Assert.Equal("Rизм= 0 Ом", harness.PublishedErrors[0].MeasureResult);
  }

  /// <summary>
  /// Проверяет такую же защиту от отрицательного результата в режиме прозвонки по ключу Б.
  /// </summary>
  [Fact(DisplayName = "КС: в режиме Б отрицательный результат после вычитания тоже прижимается к нулю")]
  public async Task ExecuteAsync_WithContinuityKey_WhenSwitchResistanceMakesValueNegative_ClampsMeasurementToZero()
  {
    using var harness = new KsExecutionHarness(switchResistance: 10);
    var command = CreateCommand(0.5, 5, "Б");

    harness.ContinuityManagerMock
      .Setup(x => x.CheckContinuityAsync(2.75, 0.5, 5, It.IsAny<IUserInteractionService>()))
      .ReturnsAsync(5);

    var protocol = await harness.ExecuteAsync(command);
    var error = Assert.Single(protocol.Errors[GetCommandKey(command)]);

    AssertMessage(error, "X1, X2 (0,5-5 Ом)", "Rизм= 0 Ом");
    Assert.Single(harness.PublishedErrors);
    Assert.Equal("Rизм= 0 Ом", harness.PublishedErrors[0].MeasureResult);
    harness.ContinuityManagerMock.Verify(x => x.CheckContinuityAsync(2.75, 0.5, 5, It.IsAny<IUserInteractionService>()), Times.Once);
  }

  /// <summary>
  /// Проверяет холостой режим без симуляции ошибок:
  /// сопротивление коммутатора не вычитается, поэтому измерение остается успешным.
  /// </summary>
  [Fact(DisplayName = "КС: в холостом режиме сопротивление коммутатора не вычитается")]
  public async Task ExecuteAsync_InIdleMode_DoesNotSubtractSwitchResistance()
  {
    using var harness = new KsExecutionHarness(switchResistance: 1, idleMode: true);
    var command = CreateCommand(0.5, 5);

    harness.ResistanceManagerMock
      .Setup(x => x.MeasureResistanceAsync(2.75, 0.5, 5, It.IsAny<IUserInteractionService>()))
      .ReturnsAsync(1);

    var protocol = await harness.ExecuteAsync(command);

    Assert.Empty(protocol.Errors);
    Assert.Empty(protocol.Info);
  }

  /// <summary>
  /// Проверяет холостой режим с симуляцией ошибок при открытой верхней границе:
  /// исполнитель должен принудительно сформировать ошибку, даже если прибор вернул нормальное значение.
  /// </summary>
  [Fact(DisplayName = "КС: в холостом режиме с симуляцией ошибок формируется принудительный брак")]
  public async Task ExecuteAsync_InIdleModeWithErrorSimulationAndOpenUpperLimit_ForcesFailure()
  {
    using var harness = new KsExecutionHarness(idleMode: true, errorSimulation: true);
    var command = CreateCommand(3_000_000_000, null);

    harness.ResistanceManagerMock
      .Setup(x => x.MeasureResistanceAsync(3_000_000_010, 3_000_000_000, -1, It.IsAny<IUserInteractionService>()))
      .ReturnsAsync(3_000_000_010);

    var protocol = await harness.ExecuteAsync(command);

    AssertProtocolMessages(protocol.Errors, command, expectedCount: 1);
    Assert.Empty(protocol.Info);
    Assert.Single(harness.PublishedErrors);
  }

  /// <summary>
  /// Проверяет сквозной сценарий: текст команды КС проходит через парсер, а затем корректно исполняется executor-ом.
  /// </summary>
  [Fact(DisplayName = "КС: разобранная команда корректно проходит путь от парсера к исполнителю")]
  public async Task ExecuteAsync_WithParsedCommand_RunsEndToEnd()
  {
    using var harness = new KsExecutionHarness();
    var command = ParseCommand("Д 5<Ом<15 *X1,X2*");

    harness.ResistanceManagerMock
      .Setup(x => x.MeasureResistanceAsync(10, 5, 15, It.IsAny<IUserInteractionService>()))
      .ReturnsAsync(10);

    var protocol = await harness.ExecuteAsync(command);

    Assert.Empty(protocol.Errors);
    AssertProtocolMessages(protocol.Info, command, expectedCount: 1);
    AssertMessage(protocol.Info[GetCommandKey(command)][0], "X1, X2(5-15 Ом)", "Rизм= 10 Ом");
  }

  /// <summary>
  /// Проверяет, что при схеме из трёх точек КС формирует отдельные записи для каждой проверки от первой точки.
  /// </summary>
  [Fact(DisplayName = "КС: для нескольких точек документирование создаётся по каждой проверяемой паре")]
  public async Task ExecuteAsync_WithThreePointsAndDocumentationKey_WritesInfoForEachPair()
  {
    var points = CreatePoints("X1", "X2", "X3");
    using var harness = new KsExecutionHarness(analyzedPoints: ClonePoints(points));
    var command = CreateCommand(5, 15, points, "Д");

    harness.ResistanceManagerMock
      .SetupSequence(x => x.MeasureResistanceAsync(10, 5, 15, It.IsAny<IUserInteractionService>()))
      .ReturnsAsync(10)
      .ReturnsAsync(10);

    var protocol = await harness.ExecuteAsync(command);
    var messages = protocol.Info[GetCommandKey(command)];

    Assert.Empty(protocol.Errors);
    Assert.Equal(2, messages.Count);
    Assert.Contains(messages, item => item.Header == "X1, X2(5-15 Ом)" && item.Message == "Rизм= 10 Ом");
    Assert.Contains(messages, item => item.Header == "X1, X3(5-15 Ом)" && item.Message == "Rизм= 10 Ом");
  }

  /// <summary>
  /// Проверяет, что в КС каждая неуспешная проверка отдельной пары формирует отдельное сообщение об ошибке.
  /// </summary>
  [Fact(DisplayName = "КС: несколько неуспешных измерений формируют несколько сообщений об ошибке")]
  public async Task ExecuteAsync_WithSeveralFailedPairs_WritesErrorForEachFailedMeasurement()
  {
    var points = CreatePoints("X1", "X2", "X3");
    using var harness = new KsExecutionHarness(analyzedPoints: ClonePoints(points));
    var command = CreateCommand(5, 15, points);

    harness.ResistanceManagerMock
      .SetupSequence(x => x.MeasureResistanceAsync(10, 5, 15, It.IsAny<IUserInteractionService>()))
      .ReturnsAsync(20)
      .ReturnsAsync(25);

    var protocol = await harness.ExecuteAsync(command);
    var messages = protocol.Errors[GetCommandKey(command)];

    Assert.Equal(2, messages.Count);
    Assert.Contains(messages, item => item.Header == "X1, X2 (5-15 Ом)" && item.Message == "Rизм= 20 Ом");
    Assert.Contains(messages, item => item.Header == "X1, X3 (5-15 Ом)" && item.Message == "Rизм= 25 Ом");
    Assert.Equal(2, harness.PublishedErrors.Count);
  }

  /// <summary>
  /// Проверяет ветку пустой схемы: исполнитель должен пройти подготовку, но не делать ни одного измерения.
  /// </summary>
  [Fact(DisplayName = "КС: пустая схема не запускает измерения и не пишет протокол")]
  public async Task ExecuteAsync_WithEmptyScheme_DoesNotMeasureAndLeavesProtocolEmpty()
  {
    using var harness = new KsExecutionHarness();
    var command = CreateCommand(5, 15);
    command.Scheme = new SchemeModel(new List<GroupModel>());

    var protocol = await harness.ExecuteAsync(command);

    Assert.Empty(protocol.Errors);
    Assert.Empty(protocol.Info);
    harness.ResistanceManagerMock.Verify(
      x => x.MeasureResistanceAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<IUserInteractionService>()),
      Times.Never);
    harness.ContinuityManagerMock.Verify(
      x => x.CheckContinuityAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<IUserInteractionService>()),
      Times.Never);
  }

  /// <summary>
  /// Проверяет, что отсутствие устройства коммутации останавливает выполнение КС до начала измерений.
  /// </summary>
  [Fact(DisplayName = "КС: отсутствие устройства коммутации даёт ошибку конфигурации")]
  public async Task ExecuteAsync_WithoutSwitchingDevice_ThrowsConfigurationException()
  {
    using var harness = new KsExecutionHarness(includeSwitchingDevice: false);
    var command = CreateCommand(5, 15);

    var exception = await Assert.ThrowsAsync<Exception>(() => harness.ExecuteAsync(command));

    Assert.Contains("Устройство коммутации не инициализировано", exception.Message);
    harness.ResistanceManagerMock.Verify(
      x => x.MeasureResistanceAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<IUserInteractionService>()),
      Times.Never);
  }

  /// <summary>
  /// Проверяет, что отсутствие быстрого измерителя останавливает выполнение КС с ошибкой конфигурации.
  /// </summary>
  [Fact(DisplayName = "КС: отсутствие быстрого измерителя даёт ошибку конфигурации")]
  public async Task ExecuteAsync_WithoutFastMeter_ThrowsConfigurationException()
  {
    const int missingMeterChassis = 987654;
    using var harness = new KsExecutionHarness(chassisNumber: missingMeterChassis, includeFastMeter: false);
    var command = CreateCommand(5, 15, CreatePointsForChassis(missingMeterChassis, "X1", "X2"));

    var exception = await Assert.ThrowsAsync<Exception>(() => harness.ExecuteAsync(command));

    Assert.Contains("не найдено устройство быстрого измерителя", exception.Message);
  }

  /// <summary>
  /// Проверяет, что при отсутствии модулей МКР исполнитель КС не может получить измеритель и останавливается.
  /// </summary>
  [Fact(DisplayName = "КС: отсутствие релейных модулей даёт ошибку неинициализированных МКР")]
  public async Task ExecuteAsync_WithoutRelayModules_ThrowsInitializationException()
  {
    using var harness = new KsExecutionHarness(includeRelayModules: false, includeFastMeter: false);
    var command = CreateCommand(5, 15);
    command.Scheme = new SchemeModel(new List<GroupModel>());

    var exception = await Assert.ThrowsAsync<Exception>(() => harness.ExecuteAsync(command));

    Assert.Contains("Модули МКР не инициализированы", exception.Message);
  }

  /// <summary>
  /// Проверяет, что точка, отсутствующая в заранее проанализированном наборе, вызывает системную ошибку трансляции.
  /// </summary>
  [Fact(DisplayName = "КС: точка вне проанализированных точек даёт системную ошибку трансляции")]
  public async Task ExecuteAsync_WithPointOutsideAnalyzedPoints_ThrowsTranslationException()
  {
    var commandPoints = CreatePoints("X1", "X2");
    using var harness = new KsExecutionHarness(analyzedPoints: new List<PointModel> { ClonePoint(commandPoints[0]) });
    var command = CreateCommand(5, 15, commandPoints);

    var exception = await Assert.ThrowsAsync<Exception>(() => harness.ExecuteAsync(command));

    Assert.Contains("1.1.2", exception.Message);
    harness.ResistanceManagerMock.Verify(
      x => x.MeasureResistanceAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<IUserInteractionService>()),
      Times.Never);
  }

  public void Dispose()
  {
    CommandsModel.Clear();
  }

  private static Mock<IUserInteractionService> CreateUserInteractionService()
  {
    var mock = new Mock<IUserInteractionService>();
    mock.SetupProperty(x => x.Header, string.Empty);
    mock.Setup(x => x.GetCancellationToken()).Returns(CancellationToken.None);
    mock.Setup(x => x.WaitUserActionAsync(It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(UserAction.None);
    mock.Setup(x => x.ShowMessageAsync(
        It.IsAny<ShowMessageModel>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<bool>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<int>()))
      .Returns(Task.CompletedTask);
    mock.Setup(x => x.AppendEmptyLineAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
    mock.Setup(x => x.MoveToLineAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
    mock.Setup(x => x.GetLastLineNumber()).Returns(0);
    mock.Setup(x => x.GetText()).Returns(string.Empty);
    mock.SetupGet(x => x.ButtonService).Returns(Mock.Of<IButtonService>());
    return mock;
  }

  private static KsCommandModel CreateCommand(double lowerLimit, double? higherLimit, params string[] keys)
  {
    return CreateCommand(lowerLimit, higherLimit, CreatePoints("X1", "X2"), keys);
  }

  private static KsCommandModel CreateCommand(double lowerLimit, double? higherLimit, IReadOnlyList<PointModel> points, params string[] keys)
  {
    return new KsCommandModel
    {
      CommandNumber = "10",
      LowerLimitResistance = lowerLimit,
      HigherLimitResistance = higherLimit,
      LowerLimitResistanceSource = $"{lowerLimit} \u041E\u043C",
      HigherLimitResistanceSource = higherLimit.HasValue ? $"{higherLimit} \u041E\u043C" : "\u221E \u041E\u043C",
      ResistanceUnit = "\u041E\u043C",
      StartLineNumber = 10,
      FormattedStartLineNumber = 3,
      SourceLines = new List<string> { "10 \u041A\u0421 test" },
      AlgorithmKey = keys.ToList(),
      Scheme = new SchemeModel(new List<GroupModel>
      {
        new(new List<ChainModel>
        {
          new(points.Select(ClonePoint).ToList())
        })
      })
    };
  }

  private static string GetCommandKey(KsCommandModel command)
  {
    return $"{command.CommandNumber} {command.Mnemonic}";
  }

  private static void AssertProtocolMessages(Dictionary<string, List<ShowMessageModel>> bucket, KsCommandModel command, int expectedCount)
  {
    var commandKey = GetCommandKey(command);
    Assert.True(bucket.ContainsKey(commandKey));
    Assert.Equal(expectedCount, bucket[commandKey].Count);
  }

  private static void AssertMessage(ShowMessageModel message, string expectedHeader, string expectedBody)
  {
    Assert.Equal(expectedHeader, message.Header);
    Assert.Equal(expectedBody, message.Message);
  }

  private static PointModel CreatePoint(string mnemonic, int pointNumber, PointType pointType = PointType.Star)
  {
    return new PointModel
    {
      DeviceNumber = 1,
      ModuleNumber = 1,
      PointNumber = pointNumber,
      Mnemonic = mnemonic,
      PointType = pointType
    };
  }

  private static List<PointModel> CreatePoints(params string[] mnemonics)
  {
    return CreatePointsForChassis(1, mnemonics);
  }

  private static List<PointModel> CreatePointsForChassis(int chassisNumber, params string[] mnemonics)
  {
    return mnemonics
      .Select((mnemonic, index) => CreatePoint(
        mnemonic,
        pointNumber: index + 1,
        pointType: index == 0 ? PointType.Star : PointType.Comma,
        chassisNumber: chassisNumber))
      .ToList();
  }

  private static PointModel ClonePoint(PointModel source)
  {
    return new PointModel
    {
      DeviceNumber = source.DeviceNumber,
      ModuleNumber = source.ModuleNumber,
      PointNumber = source.PointNumber,
      Mnemonic = source.Mnemonic,
      PointType = source.PointType
    };
  }

  private static List<PointModel> ClonePoints(IEnumerable<PointModel> source)
  {
    return source.Select(ClonePoint).ToList();
  }

  private static PointModel CreatePoint(string mnemonic, int pointNumber, PointType pointType, int chassisNumber)
  {
    return new PointModel
    {
      DeviceNumber = chassisNumber,
      ModuleNumber = 1,
      PointNumber = pointNumber,
      Mnemonic = mnemonic,
      PointType = pointType
    };
  }

  private static KsCommandModel ParseCommand(string body)
  {
    CommandsModel.Clear();
    CommandsModel.CommandModels.Add(CreateRmCommand());

    var parser = new KcCommandParser();
    string line = $"10 КС {body}";

    return Assert.IsType<KsCommandModel>(parser.Parse("10", "КС", 10, new List<string> { line }));
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

  private sealed class EquipmentServiceScope : IDisposable
  {
    public EquipmentServiceScope(
      List<PointModel>? analyzedPoints,
      List<IRelaySwitchModule>? relayModules,
      ISwitchingDevice? switchingDevice,
      IFastMeter? fastMeter)
    {
      SetProperty("AnalyzedPoints", analyzedPoints);
      SetProperty("ValidRelayModules", relayModules);
      SetProperty("ValidSwitchingDevice", switchingDevice);
      SetProperty("ValidFastMeter", fastMeter);
      SetProperty("ValidBreakdownTester", null);
    }

    public void Dispose()
    {
      SetProperty("AnalyzedPoints", null);
      SetProperty("ValidRelayModules", null);
      SetProperty("ValidSwitchingDevice", null);
      SetProperty("ValidFastMeter", null);
      SetProperty("ValidBreakdownTester", null);
    }

    private static void SetProperty(string propertyName, object? value)
    {
      var property = typeof(EquipmentService).GetProperty(
        propertyName,
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

      var setter = property?.GetSetMethod(nonPublic: true);
      setter?.Invoke(null, new[] { value });
    }
  }

  private sealed class KsExecutionHarness : IDisposable
  {
    private readonly EquipmentServiceScope _scope;

    public KsExecutionHarness(
      double switchResistance = 0,
      bool idleMode = false,
      bool errorSimulation = false,
      int chassisNumber = 1,
      List<PointModel>? analyzedPoints = null,
      bool includeSwitchingDevice = true,
      bool includeFastMeter = true,
      bool includeRelayModules = true)
    {
      ExecutionConfig.SetIdleMode(idleMode);
      ExecutionConfig.SetStepByStepMode(false);
      ExecutionConfig.SetIsErrorSimulationMode(errorSimulation);
      DeviceDisplayConfig.SetExecutionParametersVisibility(false);
      DeviceDisplayConfig.SetMeasurementResultsVisibility(false);
      DeviceDisplayConfig.SetMachineAddressVisibility(false);

      ConsoleMock = CreateUserInteractionService();
      EditorMock = new Mock<ITextEditorAdapter>();

      ResistanceManagerMock = new Mock<IResistanceMeasurement>();
      ResistanceManagerMock
        .Setup(x => x.SetResistanceModeAsync(It.IsAny<IUserInteractionService>()))
        .ReturnsAsync(true);

      ContinuityManagerMock = new Mock<IContinuityMeasurement>();
      ContinuityManagerMock
        .Setup(x => x.SetContinuityModeAsync(It.IsAny<IUserInteractionService>()))
        .ReturnsAsync(true);

      var meterMock = new Mock<IFastMeter>();
      meterMock.SetupGet(x => x.ResistanceManager).Returns(ResistanceManagerMock.Object);
      meterMock.SetupGet(x => x.ContinuityManager).Returns(ContinuityManagerMock.Object);

      ConnectorManagerMock = new Mock<IConnectorDeviceBusCommutation>();
      ConnectorManagerMock
        .Setup(x => x.ConnectMultimeter(SwitchingBusNew.AB1, It.IsAny<IUserInteractionService>()))
        .ReturnsAsync(true);

      var switchingDeviceMock = new Mock<ISwitchingDevice>();
      switchingDeviceMock.SetupGet(x => x.ConnectorManager).Returns(ConnectorManagerMock.Object);

      var busManagerMock = new Mock<IBusManager>();
      busManagerMock
        .Setup(x => x.ConnectBusAsync(It.IsAny<SwitchingBus>(), It.IsAny<IUserInteractionService>()))
        .ReturnsAsync(true);

      var pointManagerMock = new Mock<IPointManager>();
      pointManagerMock
        .Setup(x => x.ConnectRelayAsync(It.IsAny<BusPoint>(), It.IsAny<int>(), It.IsAny<IUserInteractionService>()))
        .ReturnsAsync(true);
      pointManagerMock
        .Setup(x => x.DisconnectRelayAsync(It.IsAny<BusPoint>(), It.IsAny<int>(), It.IsAny<IUserInteractionService>()))
        .ReturnsAsync(true);

      var relayModuleMock = new Mock<IRelaySwitchModule>();
      relayModuleMock.SetupGet(x => x.NumberChassis).Returns(chassisNumber);
      relayModuleMock.SetupGet(x => x.Number).Returns(1);
      relayModuleMock.SetupGet(x => x.BusType).Returns(SwitchingBusNew.AB1);
      relayModuleMock.SetupGet(x => x.SwitchResistance).Returns(switchResistance);
      relayModuleMock.SetupGet(x => x.PointManager).Returns(pointManagerMock.Object);
      relayModuleMock.SetupGet(x => x.BusManager).Returns(busManagerMock.Object);

      var defaultPoints = CreatePointsForChassis(chassisNumber, "X1", "X2");
      _scope = new EquipmentServiceScope(
        analyzedPoints: analyzedPoints ?? ClonePoints(defaultPoints),
        relayModules: includeRelayModules ? new List<IRelaySwitchModule> { relayModuleMock.Object } : new List<IRelaySwitchModule>(),
        switchingDevice: includeSwitchingDevice ? switchingDeviceMock.Object : null,
        fastMeter: includeFastMeter ? meterMock.Object : null);
    }

    public Mock<IUserInteractionService> ConsoleMock { get; }
    public Mock<ITextEditorAdapter> EditorMock { get; }
    public Mock<IResistanceMeasurement> ResistanceManagerMock { get; }
    public Mock<IContinuityMeasurement> ContinuityManagerMock { get; }
    public Mock<IConnectorDeviceBusCommutation> ConnectorManagerMock { get; }
    public List<ErrorItem> PublishedErrors { get; } = new();

    public async Task<ProtocolModel> ExecuteAsync(KsCommandModel command)
    {
      return await StaTestHost.RunAsync(async () =>
      {
        var manager = new CommandExecutionManager(
          ConsoleMock.Object,
          EditorMock.Object,
          new List<BaseCommandModel> { command },
          "test.opk");
        manager.AddError += PublishedErrors.Add;

        var context = new CommandExecutionContext(manager, command, ConsoleMock.Object, EditorMock.Object, "test.opk");
        var protocol = new ProtocolModel();

        await new KsCommandExecutor().ExecuteAsync(context, protocol);
        return protocol;
      });
    }

    public void Dispose()
    {
      _scope.Dispose();
      ExecutionConfig.SetIdleMode(false);
      ExecutionConfig.SetStepByStepMode(false);
      ExecutionConfig.SetIsErrorSimulationMode(false);
    }
  }

  private static class StaTestHost
  {
    private static readonly Lazy<Dispatcher> DispatcherInstance = new(CreateDispatcher);

    public static Task<T> RunAsync<T>(Func<Task<T>> action)
    {
      var dispatcher = DispatcherInstance.Value;
      return dispatcher.InvokeAsync(async () => await action()).Task.Unwrap();
    }

    private static Dispatcher CreateDispatcher()
    {
      var ready = new TaskCompletionSource<Dispatcher>(TaskCreationOptions.RunContinuationsAsynchronously);

      var thread = new Thread(() =>
      {
        EnsureApplicationWithResources();
        ready.SetResult(Dispatcher.CurrentDispatcher);
        Dispatcher.Run();
      });

      thread.IsBackground = true;
      thread.SetApartmentState(ApartmentState.STA);
      thread.Start();

      return ready.Task.GetAwaiter().GetResult();
    }

    private static void EnsureApplicationWithResources()
    {
      var application = Application.Current ?? new Application();
      application.Resources["TestsProtocolMessageSuccesForeground"] = new SolidColorBrush(Colors.Green);
      application.Resources["TestsProtocolMessageErrorForeground"] = new SolidColorBrush(Colors.Red);
      application.Resources["TestsProtocolHeaderForeground"] = new SolidColorBrush(Colors.White);
      application.Resources["TestsProtocolMessageForeground"] = new SolidColorBrush(Colors.White);
      application.Resources["TestsProtocolTimeForeground"] = new SolidColorBrush(Colors.White);
      application.Resources["YellowColorSolidColorBrush"] = new SolidColorBrush(Colors.Yellow);
      application.Resources["LightBlueColorSolidColorBrush"] = new SolidColorBrush(Colors.LightBlue);
    }
  }
}
