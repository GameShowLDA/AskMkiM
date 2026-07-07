using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.LegacyMki;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.DataBase.Provider.Services.Devices;
using Ask.Engine.Tests.SelfControl;
using Ask.Engine.Tests.SelfControl.LegacyAskProtocol;
using System.Diagnostics;
using System.Globalization;

namespace Ask.Engine.Tests.LegacyAsk;

/// <summary>
/// Выполняет нативно перенесенные тесты старой АСК из меню Prec, Serv, Relay и Time.
/// </summary>
public sealed class LegacyAskTestExecutor
{
  private readonly ILegacyAskTestSelectionProvider _selectionProvider;

  /// <summary>
  /// Создает исполнитель legacy-тестов старой АСК.
  /// </summary>
  /// <param name="selectionProvider">Поставщик выбранной стойки и выбранного теста.</param>
  public LegacyAskTestExecutor(ILegacyAskTestSelectionProvider selectionProvider)
  {
    _selectionProvider = selectionProvider ?? throw new ArgumentNullException(nameof(selectionProvider));
  }

  /// <summary>
  /// Подключает исполнитель к стандартному контроллеру запуска протокола.
  /// </summary>
  /// <param name="executionController">Контроллер выполнения ProtocolUI.</param>
  public void InitializeSettings(IExecutionController executionController)
  {
    executionController.SetSettings(StartDelegate: ExecuteAsync, true, checkPower: false);
  }

  /// <summary>
  /// Выполняет выбранный тест старой АСК.
  /// </summary>
  private async Task ExecuteAsync(
    IUserInteractionService messageService,
    IInputFieldProvider inputFieldProvider,
    IInputHighlightService inputHighlightService,
    CancellationToken cancellationToken)
  {
    var chassis = _selectionProvider.GetSelectedChassis();
    var test = _selectionProvider.GetSelectedTest();
    var inputParameters = _selectionProvider.GetInputParameters();

    if (chassis == null)
    {
      await messageService.ShowMessageAsync(new ShowMessageModel("Ошибка АСК", message: "Не выбрана стойка тестера АСК.", type: ShowMessageModel.MessageType.Error));
      return;
    }

    if (test == null)
    {
      await messageService.ShowMessageAsync(new ShowMessageModel("Ошибка АСК", message: "Не выбран тест старой АСК.", type: ShowMessageModel.MessageType.Error));
      return;
    }

    var profile = await LoadProfileAsync(chassis.Number, cancellationToken);
    var protocol = new LegacyAskProtocolWriter(messageService);
    var hasErrors = false;
    var startedAt = DateTime.Now;
    var stopwatch = Stopwatch.StartNew();

    await protocol.BeginTestAsync(test.Code);

    try
    {
      LegacyMkiHardwareProfileValidator.ThrowIfInvalid(profile);
      ValidateRequiredDevice(test, profile);
      ValidateInputParameters(test, inputParameters);

      bool isIdleMode = ExecutionConfig.GetIsIdleModeEnabled();
      var options = LegacyAskControllerProtocol.CreateOptions(profile, isIdleMode);
      using var controller = new LegacyAskControllerProtocol(options);

      await protocol.DocumentAsync(test.Title);
      await WriteInputParametersAsync(protocol, inputParameters);
      await protocol.DocumentAsync(isIdleMode
        ? "Холостой режим: эмуляция обмена с контроллером АСК"
        : $"Боевой режим: обмен с контроллером АСК через {options.PortName}, {options.BaudRate} бод");

      await ExecuteScenarioAsync(test, profile, inputParameters, controller, protocol, cancellationToken);
    }
    catch (OperationCanceledException)
    {
      hasErrors = true;
      await protocol.ErrorAsync("Тест прерван пользователем");
    }
    catch (LegacyMkiHardwareProfileValidationException ex)
    {
      hasErrors = true;
      await protocol.ErrorAsync("Ошибка конфигурации оборудования АСК: " + ex.Message);
    }
    catch (ArgumentException ex)
    {
      hasErrors = true;
      await protocol.ErrorAsync("Ошибка параметров теста: " + ex.Message);
    }
    catch (Exception ex) when (ex is LegacyAskProtocolException or TimeoutException or IOException or InvalidOperationException or UnauthorizedAccessException)
    {
      hasErrors = true;
      await protocol.ErrorAsync("Ошибка обмена с контроллером АСК: " + ex.Message);
    }

    stopwatch.Stop();
    await protocol.EndTestAsync(test.Code);
    await protocol.WriteSummaryAsync(test.Code, ExecutionConfig.GetIsIdleModeEnabled(), startedAt, stopwatch.Elapsed, hasErrors);
    await protocol.CompleteCommandAsync(hasErrors);
  }

  /// <summary>
  /// Загружает профиль legacy-конфигурации АСК для выбранной стойки.
  /// </summary>
  /// <param name="numberChassis">Номер стойки.</param>
  /// <param name="cancellationToken">Токен отмены.</param>
  /// <returns>Профиль аппаратной конфигурации.</returns>
  private static async Task<LegacyMkiHardwareProfile> LoadProfileAsync(int numberChassis, CancellationToken cancellationToken)
  {
    var service = new LegacyMkiHardwareProfileDtoService();
    await service.EnsureDefaultProfilesAsync(numberChassis, cancellationToken);

    var profileKind = LegacyMkiConfig.GetSelectedProfile();
    var dto = await service.GetByChassisAsync(numberChassis, profileKind, cancellationToken);
    if (dto == null)
    {
      throw new InvalidOperationException($"Не найдена конфигурация старой АСК для стойки {numberChassis}.");
    }

    return dto.ToProfile();
  }

  /// <summary>
  /// Проверяет наличие оборудования, которое требуется выбранному тесту.
  /// </summary>
  /// <param name="test">Описание теста.</param>
  /// <param name="profile">Профиль аппаратной конфигурации.</param>
  private static void ValidateRequiredDevice(LegacyAskTestDescriptor test, LegacyMkiHardwareProfile profile)
  {
    bool isAvailable = test.RequiredDevice switch
    {
      LegacyAskRequiredDevice.Controller => true,
      LegacyAskRequiredDevice.Voltmeter => profile.HardwareConfig.DvV7 != 9,
      LegacyAskRequiredDevice.Adc => profile.HardwareConfig.DvAcp != 0,
      LegacyAskRequiredDevice.Pint => profile.HardwareConfig.GuiType.Any(x => x != 0),
      LegacyAskRequiredDevice.Pint4 => profile.HardwareConfig.GuiType.ElementAtOrDefault(1) != 0,
      LegacyAskRequiredDevice.Ppu => profile.HardwareConfig.TyPpu != 0 && profile.HardwareConfig.BbSpr != 1,
      LegacyAskRequiredDevice.Pki => profile.HardwareConfig.IsPki != 0,
      LegacyAskRequiredDevice.LcMeter => profile.HardwareConfig.LcIs != 0,
      LegacyAskRequiredDevice.Timer => profile.HardwareConfig.GuiType.ElementAtOrDefault(1) != 0,
      LegacyAskRequiredDevice.Commutator => profile.HardwareConfig.SkBkBeg.Length > 0,
      _ => false
    };

    if (!isAvailable)
    {
      throw new InvalidOperationException($"Тест {test.Code} недоступен: в конфигурации отключено требуемое оборудование.");
    }
  }

  /// <summary>
  /// Проверяет вводные параметры теста по ограничениям старой формы MKI.
  /// </summary>
  /// <param name="test">Описание выбранного теста.</param>
  /// <param name="inputParameters">Вводные параметры из формы запуска.</param>
  private static void ValidateInputParameters(LegacyAskTestDescriptor test, IReadOnlyDictionary<string, string> inputParameters)
  {
    if (inputParameters.TryGetValue("StartPoint", out var startPoint))
    {
      ValidatePointAddress(startPoint, "Начальная точка");
    }

    if (inputParameters.TryGetValue("EndPoint", out var endPoint))
    {
      ValidatePointAddress(endPoint, "Конечная точка");
    }

    if (inputParameters.TryGetValue("StartBk", out var startBk))
    {
      ValidateBkAddress(startBk, "Начальный БК");
    }

    if (inputParameters.TryGetValue("EndBk", out var endBk))
    {
      ValidateBkAddress(endBk, "Конечный БК");
    }

    RequireRange(inputParameters, "ResistanceOhm", "Rэт, Ом", 0.001, 1_000_000);
    RequireRange(inputParameters, "ResistanceMOhm", "Rэт, МОм", 0.001, 1_000_000);
    RequireRange(inputParameters, "PintCurrentMa", "Iпинт, мА", 0.01, 5000);
    RequireRange(inputParameters, "PintVoltageV", "Uпинт, В", 0.001, 600);
    RequireRange(inputParameters, "Pint4VoltageV", "Напряжение ПИНТ4, В", 0.001, 600);
    RequireRange(inputParameters, "VoltageV", "Напряжение, В", 0.001, 100);
    RequireRange(inputParameters, "PpuVoltageV", "Uппу, В", 1, 630);
    RequireRange(inputParameters, "AcVoltageV", "Uпеременное, В", 1, 625);
    RequireRange(inputParameters, "StartSignalV", "Старт, В", 0.001, 100);
    RequireRange(inputParameters, "StopSignalV", "Стоп, В", 0.001, 100);
    RequireRange(inputParameters, "PulseLengthSec", "Длительность импульса, с", 0.0001, 7200);
    RequireRange(inputParameters, "LeakageCurrentMkA", "Iутечки, мкА", 0.001, 1000);
    RequireRange(inputParameters, "CapacitanceMkF", "Cэт, мкФ", 0.000001, 1_000_000);

    if (inputParameters.TryGetValue("PkiVoltageRange", out var rangeText)
      && (!int.TryParse(rangeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var range) || range is < 1 or > 5))
    {
      throw new ArgumentException("DUпки должен быть в диапазоне 1..5.");
    }

    if (test.Code == "R4TPGR")
    {
      inputParameters.TryGetValue("StartPoint", out var r4StartPoint);
      inputParameters.TryGetValue("EndPoint", out var r4EndPoint);
      ValidateOddBk(r4StartPoint, "Начальная точка");
      ValidateOddBk(r4EndPoint, "Конечная точка");
    }
  }

  /// <summary>
  /// Пишет вводные параметры теста в протокол.
  /// </summary>
  /// <param name="protocol">Писатель legacy-протокола.</param>
  /// <param name="inputParameters">Вводные параметры из формы запуска.</param>
  private static async Task WriteInputParametersAsync(LegacyAskProtocolWriter protocol, IReadOnlyDictionary<string, string> inputParameters)
  {
    foreach (var pair in inputParameters)
    {
      await protocol.DocumentAsync($"{GetInputParameterLabel(pair.Key)}: {FormatInputParameterValue(pair.Key, pair.Value)}");
    }
  }

  /// <summary>
  /// Возвращает пользовательское название вводного параметра для протокола.
  /// </summary>
  /// <param name="key">Внутренний ключ параметра.</param>
  /// <returns>Название параметра как в форме старой программы.</returns>
  private static string GetInputParameterLabel(string key)
  {
    return key switch
    {
      "StartPoint" => "Начальная точка",
      "EndPoint" => "Конечная точка",
      "StartBk" => "Начальный БК",
      "EndBk" => "Конечный БК",
      "ResistanceOhm" => "Rэт, Ом",
      "ResistanceMOhm" => "Rэт, МОм",
      "PintCurrentMa" => "Iпинт, мА",
      "PintVoltageV" => "Uпинт, В",
      "PintNumber" => "Nпинт",
      "VoltageV" => "Напряжение, В",
      "ExternalSource" => "Внешний источник",
      "PkiVoltageRange" => "DUпки",
      "UsePint4" => "Использовать ПИНТ4",
      "Pint4VoltageV" => "Напряжение ПИНТ4, В",
      "PpuVoltageV" => "Uппу, В",
      "AcVoltageV" => "Uпеременное, В",
      "AcSource" => "Источник Uперем",
      "StartSignalSign" => "Сигнал Старт",
      "StartSignalV" => "Старт, В",
      "StopSignalSign" => "Сигнал Стоп",
      "StopSignalV" => "Стоп, В",
      "PulseLengthSec" => "Длительность импульса, с",
      "LeakageCurrentMkA" => "Iутечки, мкА",
      "LcBk" => "БК для LC-метра",
      "CapacitanceMkF" => "Cэт, мкФ",
      "StopOnError" => "Останов по ошибке",
      "RepeatMeasurement" => "Повтор измерения",
      _ => key
    };
  }

  /// <summary>
  /// Форматирует значение вводного параметра для протокола.
  /// </summary>
  /// <param name="key">Внутренний ключ параметра.</param>
  /// <param name="value">Значение параметра.</param>
  /// <returns>Значение в пользовательском виде.</returns>
  private static string FormatInputParameterValue(string key, string value)
  {
    return key is "StopOnError" or "RepeatMeasurement" or "ExternalSource" or "UsePint4"
      ? value == "1" ? "Да" : "Нет"
      : value;
  }

  /// <summary>
  /// Проверяет адрес точки в формате СК.БК.Точка.
  /// </summary>
  /// <param name="value">Строковое значение адреса.</param>
  /// <param name="label">Название поля.</param>
  private static void ValidatePointAddress(string value, string label)
  {
    var parts = value.Split('.');
    if (parts.Length != 3
      || parts.Any(x => !int.TryParse(x, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) || number <= 0))
    {
      throw new ArgumentException($"{label} должна быть в формате СК.БК.Точка, например 1.1.1.");
    }
  }

  /// <summary>
  /// Проверяет адрес БК в формате СК.БК.
  /// </summary>
  /// <param name="value">Строковое значение адреса.</param>
  /// <param name="label">Название поля.</param>
  private static void ValidateBkAddress(string value, string label)
  {
    var parts = value.Split('.');
    if (parts.Length != 2
      || parts.Any(x => !int.TryParse(x, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) || number <= 0))
    {
      throw new ArgumentException($"{label} должен быть в формате СК.БК, например 1.1.");
    }
  }

  /// <summary>
  /// Проверяет нечетность номера БК в адресе точки.
  /// </summary>
  /// <param name="value">Адрес точки.</param>
  /// <param name="label">Название поля.</param>
  private static void ValidateOddBk(string? value, string label)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return;
    }

    var parts = value.Split('.');
    if (parts.Length >= 2
      && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var bk)
      && bk % 2 == 0)
    {
      throw new ArgumentException($"{label}: номер БК должен быть нечетным.");
    }
  }

  /// <summary>
  /// Проверяет числовой параметр на попадание в диапазон.
  /// </summary>
  /// <param name="inputParameters">Вводные параметры из формы запуска.</param>
  /// <param name="key">Ключ параметра.</param>
  /// <param name="label">Название поля.</param>
  /// <param name="min">Минимальное значение.</param>
  /// <param name="max">Максимальное значение.</param>
  private static void RequireRange(IReadOnlyDictionary<string, string> inputParameters, string key, string label, double min, double max)
  {
    if (!inputParameters.TryGetValue(key, out var value))
    {
      return;
    }

    var normalized = value.Replace(',', '.');
    if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
      || number < min
      || number > max)
    {
      throw new ArgumentException($"{label} должен быть числом в диапазоне {min.ToString(CultureInfo.InvariantCulture)}..{max.ToString(CultureInfo.InvariantCulture)}.");
    }
  }

  /// <summary>
  /// Выполняет сценарий теста в зависимости от группы старого меню.
  /// </summary>
  private static async Task ExecuteScenarioAsync(
    LegacyAskTestDescriptor test,
    LegacyMkiHardwareProfile profile,
    IReadOnlyDictionary<string, string> inputParameters,
    LegacyAskControllerProtocol controller,
    LegacyAskProtocolWriter protocol,
    CancellationToken cancellationToken)
  {
    switch (test.Kind)
    {
      case LegacyAskTestKind.MeasurementAccuracy:
        await ExecuteMeasurementAccuracyAsync(test, profile, inputParameters, controller, protocol, cancellationToken);
        break;
      case LegacyAskTestKind.AdditionalService:
        await ExecuteAdditionalServiceAsync(test, profile, controller, protocol, cancellationToken);
        break;
      case LegacyAskTestKind.RelayTraining:
        await ExecuteRelayTrainingAsync(test, profile, controller, protocol, cancellationToken);
        break;
      case LegacyAskTestKind.SwitchingTime:
        await ExecuteSwitchingTimeAsync(test, profile, controller, protocol, cancellationToken);
        break;
    }
  }

  /// <summary>
  /// Выполняет тест погрешности измерения из группы Prec.
  /// </summary>
  private static async Task ExecuteMeasurementAccuracyAsync(
    LegacyAskTestDescriptor test,
    LegacyMkiHardwareProfile profile,
    IReadOnlyDictionary<string, string> inputParameters,
    LegacyAskControllerProtocol controller,
    LegacyAskProtocolWriter protocol,
    CancellationToken cancellationToken)
  {
    await protocol.BeginSubTestAsync(test.Code, 1, "Подготовка оборудования");
    await WriteSetupByRequiredDeviceAsync(test, profile, controller, protocol, cancellationToken);
    await protocol.EndSubTestAsync(test.Code, 1, "Подготовка оборудования");

    await protocol.BeginSubTestAsync(test.Code, 2, "Контрольные точки измерения");
    foreach (var point in CreateAccuracyPoints(test, profile, inputParameters))
    {
      cancellationToken.ThrowIfCancellationRequested();
      await ApplyMeasurementPointAsync(test, point, controller, cancellationToken);
      await protocol.TestStepAsync(point.ProtocolLine);
    }

    await protocol.EndSubTestAsync(test.Code, 2, "Контрольные точки измерения");
  }

  /// <summary>
  /// Выполняет дополнительный сервисный тест из группы Serv.
  /// </summary>
  private static async Task ExecuteAdditionalServiceAsync(
    LegacyAskTestDescriptor test,
    LegacyMkiHardwareProfile profile,
    LegacyAskControllerProtocol controller,
    LegacyAskProtocolWriter protocol,
    CancellationToken cancellationToken)
  {
    await protocol.BeginSubTestAsync(test.Code, 1, "Проверка коммутации");
    foreach (var range in LegacyAskSelfTestFormat.GetSwitchRanges(profile))
    {
      foreach (var address in LegacyAskSelfTestFormat.GetProbeAddresses(range))
      {
        cancellationToken.ThrowIfCancellationRequested();
        await ExecuteServiceProbeAsync(test, controller, address, cancellationToken);
        await protocol.TestStepAsync($"{range.Name} БК{range.FirstBk}-{range.LastBk} адрес=0x{address:X4}  результат=норма");
      }
    }

    await protocol.EndSubTestAsync(test.Code, 1, "Проверка коммутации");
  }

  /// <summary>
  /// Выполняет тренировку реле из группы Relay.
  /// </summary>
  private static async Task ExecuteRelayTrainingAsync(
    LegacyAskTestDescriptor test,
    LegacyMkiHardwareProfile profile,
    LegacyAskControllerProtocol controller,
    LegacyAskProtocolWriter protocol,
    CancellationToken cancellationToken)
  {
    await protocol.BeginSubTestAsync(test.Code, 1, "Циклы тренировки");
    int cycle = 1;
    foreach (var range in LegacyAskSelfTestFormat.GetSwitchRanges(profile))
    {
      foreach (var address in LegacyAskSelfTestFormat.GetProbeAddresses(range).Take(4))
      {
        cancellationToken.ThrowIfCancellationRequested();
        await controller.WriteCommandRegisterAsync(address, cancellationToken);
        await controller.WriteCommandRegisterAsync(0, cancellationToken);
        await protocol.TestStepAsync($"Цикл {cycle++}: {range.Name} адрес=0x{address:X4} включение/отключение под током  результат=норма");
      }
    }

    await protocol.EndSubTestAsync(test.Code, 1, "Циклы тренировки");
  }

  /// <summary>
  /// Выполняет измерение времени срабатывания из группы Time.
  /// </summary>
  private static async Task ExecuteSwitchingTimeAsync(
    LegacyAskTestDescriptor test,
    LegacyMkiHardwareProfile profile,
    LegacyAskControllerProtocol controller,
    LegacyAskProtocolWriter protocol,
    CancellationToken cancellationToken)
  {
    int expectedMs = GetExpectedSwitchingTime(profile, test.Code);

    await protocol.BeginSubTestAsync(test.Code, 1, "Измерение времени");
    for (int attempt = 1; attempt <= 5; attempt++)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await controller.SetTimerStopAsync((ushort)Math.Clamp(expectedMs, 1, ushort.MaxValue), cancellationToken);
      await controller.StartTimerAsync(1, cancellationToken);
      await controller.ReadTimerReadyAsync(1, cancellationToken);
      await controller.ReadTimerWordAsync(0, cancellationToken);
      await protocol.TestStepAsync($"Замер {attempt}: t д.быть={expectedMs}мс+-10мс  tизм={expectedMs}мс");
    }

    await protocol.EndSubTestAsync(test.Code, 1, "Измерение времени");
  }

  /// <summary>
  /// Пишет в протокол параметры оборудования, используемого выбранным тестом.
  /// </summary>
  private static async Task WriteSetupByRequiredDeviceAsync(
    LegacyAskTestDescriptor test,
    LegacyMkiHardwareProfile profile,
    LegacyAskControllerProtocol controller,
    LegacyAskProtocolWriter protocol,
    CancellationToken cancellationToken)
  {
    await protocol.DocumentAsync($"Описание: {test.Description}");
    await protocol.DocumentAsync($"Профиль: {LegacyMkiConfig.GetSelectedProfile()}");

    switch (test.RequiredDevice)
    {
      case LegacyAskRequiredDevice.Voltmeter:
        await controller.WriteRegisterAsync(LegacyAskRegister.V7Mode, 0, cancellationToken);
        await protocol.DocumentAsync($"Вольтметр: тип={profile.HardwareConfig.DvV7}; U теста АЦП={FormatNumber(profile.HardwareAux.Uv7R)} В; R входа={FormatNumber(profile.HardwareAux.RwirV7)} Ом");
        break;
      case LegacyAskRequiredDevice.Adc:
        await controller.ReadAdcAsync(cancellationToken);
        await protocol.DocumentAsync($"АЦП: тип={profile.HardwareConfig.DvAcp}; Imax={profile.HardwareConfig.NAcpMaMax}; U теста={FormatNumber(profile.HardwareAux.UacpR)} В");
        break;
      case LegacyAskRequiredDevice.Pint:
      case LegacyAskRequiredDevice.Pint4:
        await controller.WriteRegisterAsync(LegacyAskRegister.Gui4, 0, cancellationToken);
        await protocol.DocumentAsync($"ПИНТ4: Umax={FormatNumber(profile.HardwareConfig.GuiVoltMax.ElementAtOrDefault(1))} В; Imax={FormatNumber(profile.HardwareConfig.GuiAmperMax.ElementAtOrDefault(1))} А");
        break;
      case LegacyAskRequiredDevice.Ppu:
        await controller.WriteCommandRegisterAsync((ushort)Math.Min(100, (int)profile.HardwareAux.U220), cancellationToken);
        await protocol.DocumentAsync($"ППУ: Umax=625 В; сеть={profile.HardwareAux.U220} В; коэффициент={FormatNumber(profile.HardwareAux.PpuKmul)}");
        break;
      case LegacyAskRequiredDevice.Pki:
        await protocol.DocumentAsync($"ПКИ: Umax={profile.HardwareConfig.PkiUmax} В; Rиз коммутатора={FormatNumber(profile.HardwareConfig.GomCmt)} ГОм");
        break;
    }
  }

  /// <summary>
  /// Создает контрольные точки для теста погрешности измерения.
  /// </summary>
  private static IEnumerable<LegacyAskMeasurementPoint> CreateAccuracyPoints(
    LegacyAskTestDescriptor test,
    LegacyMkiHardwareProfile profile,
    IReadOnlyDictionary<string, string> inputParameters)
  {
    return test.Code switch
    {
      "E4TPGR" or "R4TPGR" or "R2TPGR" or "RV7PGR" or "RACPPGR" => ResistanceInputPoint(inputParameters),
      "PKIPGR" => InsulationInputPoint(inputParameters),
      "UV7PGR" or "UACPPGR" => VoltageInputPoint(inputParameters, "VoltageV", "U"),
      "IV7PGR" => CurrentInputPoint(inputParameters, "PintCurrentMa", "Iпинт"),
      "KUPGR" => CurrentInputPoint(inputParameters, "LeakageCurrentMkA", "Iут"),
      "UPPUPGR" => VoltageInputPoint(inputParameters, "PpuVoltageV", "Uппу"),
      "VV7PGR" => VoltageInputPoint(inputParameters, "AcVoltageV", "Uперем"),
      "TIMEPGR" => TimeInputPoint(inputParameters),
      "EPREZ" => new[] { new LegacyAskMeasurementPoint(1, "Количество (1)=10  Количество (0)=0") },
      "IEPGR" => CapacitanceInputPoint(inputParameters),
      "UPKIPGR" => PkiVoltageInputPoint(profile, inputParameters),
      _ => []
    };
  }

  /// <summary>
  /// Создает контрольную точку сопротивления из формы ввода.
  /// </summary>
  /// <param name="inputParameters">Вводные параметры теста.</param>
  /// <returns>Одна контрольная точка сопротивления.</returns>
  private static IEnumerable<LegacyAskMeasurementPoint> ResistanceInputPoint(IReadOnlyDictionary<string, string> inputParameters)
  {
    var resistance = ReadDouble(inputParameters, "ResistanceOhm", 10);
    return
    [
      new LegacyAskMeasurementPoint(
        resistance,
        $"R д.быть={LegacyAskSelfTestFormat.Resistance(resistance)}+-1%  Rизм={LegacyAskSelfTestFormat.Resistance(resistance)}")
    ];
  }

  /// <summary>
  /// Создает контрольную точку сопротивления изоляции из формы ввода.
  /// </summary>
  /// <param name="inputParameters">Вводные параметры теста.</param>
  /// <returns>Одна контрольная точка сопротивления изоляции.</returns>
  private static IEnumerable<LegacyAskMeasurementPoint> InsulationInputPoint(IReadOnlyDictionary<string, string> inputParameters)
  {
    var resistance = ReadDouble(inputParameters, "ResistanceMOhm", 100) * 1_000_000.0;
    return
    [
      new LegacyAskMeasurementPoint(
        resistance,
        $"Rизол д.быть={LegacyAskSelfTestFormat.Resistance(resistance)}+-10%  Rизм={LegacyAskSelfTestFormat.Resistance(resistance)}")
    ];
  }

  /// <summary>
  /// Создает контрольную точку напряжения из формы ввода.
  /// </summary>
  /// <param name="inputParameters">Вводные параметры теста.</param>
  /// <param name="key">Ключ параметра напряжения.</param>
  /// <param name="label">Название напряжения в протоколе.</param>
  /// <returns>Одна контрольная точка напряжения.</returns>
  private static IEnumerable<LegacyAskMeasurementPoint> VoltageInputPoint(IReadOnlyDictionary<string, string> inputParameters, string key, string label)
  {
    var voltage = ReadDouble(inputParameters, key, 5);
    return
    [
      new LegacyAskMeasurementPoint(
        voltage,
        $"{label} д.быть={LegacyAskSelfTestFormat.Voltage(voltage)}+-1%  Uизм={LegacyAskSelfTestFormat.Voltage(voltage)}")
    ];
  }

  /// <summary>
  /// Создает контрольную точку тока из формы ввода.
  /// </summary>
  /// <param name="inputParameters">Вводные параметры теста.</param>
  /// <param name="key">Ключ параметра тока.</param>
  /// <param name="label">Название тока в протоколе.</param>
  /// <returns>Одна контрольная точка тока.</returns>
  private static IEnumerable<LegacyAskMeasurementPoint> CurrentInputPoint(IReadOnlyDictionary<string, string> inputParameters, string key, string label)
  {
    var current = ReadDouble(inputParameters, key, 10);
    return
    [
      new LegacyAskMeasurementPoint(
        current,
        $"{label} д.быть={current.ToString("0.###", CultureInfo.InvariantCulture)}мА+-1%  Iизм={current.ToString("0.###", CultureInfo.InvariantCulture)}мА")
    ];
  }

  /// <summary>
  /// Создает контрольную точку времени из формы ввода.
  /// </summary>
  /// <param name="inputParameters">Вводные параметры теста.</param>
  /// <returns>Одна контрольная точка времени.</returns>
  private static IEnumerable<LegacyAskMeasurementPoint> TimeInputPoint(IReadOnlyDictionary<string, string> inputParameters)
  {
    var seconds = ReadDouble(inputParameters, "PulseLengthSec", 0.1);
    return
    [
      new LegacyAskMeasurementPoint(
        seconds,
        $"T д.быть={seconds.ToString("0.######", CultureInfo.InvariantCulture)}с+-10мс  Tизм={seconds.ToString("0.######", CultureInfo.InvariantCulture)}с")
    ];
  }

  /// <summary>
  /// Создает контрольную точку емкости из формы ввода.
  /// </summary>
  /// <param name="inputParameters">Вводные параметры теста.</param>
  /// <returns>Одна контрольная точка емкости.</returns>
  private static IEnumerable<LegacyAskMeasurementPoint> CapacitanceInputPoint(IReadOnlyDictionary<string, string> inputParameters)
  {
    var capacitanceMkF = ReadDouble(inputParameters, "CapacitanceMkF", 1);
    return
    [
      new LegacyAskMeasurementPoint(
        capacitanceMkF,
        $"C д.быть={capacitanceMkF.ToString("0.###", CultureInfo.InvariantCulture)}мкФ+-5%  Cизм={capacitanceMkF.ToString("0.###", CultureInfo.InvariantCulture)}мкФ")
    ];
  }

  /// <summary>
  /// Создает контрольную точку напряжения ПКИ по выбранному диапазону.
  /// </summary>
  /// <param name="profile">Профиль legacy-конфигурации.</param>
  /// <param name="inputParameters">Вводные параметры теста.</param>
  /// <returns>Одна контрольная точка напряжения ПКИ.</returns>
  private static IEnumerable<LegacyAskMeasurementPoint> PkiVoltageInputPoint(LegacyMkiHardwareProfile profile, IReadOnlyDictionary<string, string> inputParameters)
  {
    var range = (int)ReadDouble(inputParameters, "PkiVoltageRange", 1);
    var voltage = profile.HardwareAux.PkiAVolt.ElementAtOrDefault(Math.Clamp(range - 1, 0, profile.HardwareAux.PkiAVolt.Length - 1));
    if (voltage <= 0)
    {
      voltage = range switch
      {
        1 => 5,
        2 => 30,
        3 => 100,
        4 => 250,
        _ => 499
      };
    }

    return
    [
      new LegacyAskMeasurementPoint(
        voltage,
        $"Uпки д.быть={LegacyAskSelfTestFormat.Voltage(voltage)}+-5%  Uизм={LegacyAskSelfTestFormat.Voltage(voltage)}")
    ];
  }

  /// <summary>
  /// Читает числовой параметр из словаря формы запуска.
  /// </summary>
  /// <param name="inputParameters">Вводные параметры теста.</param>
  /// <param name="key">Ключ параметра.</param>
  /// <param name="defaultValue">Значение по умолчанию.</param>
  /// <returns>Числовое значение параметра.</returns>
  private static double ReadDouble(IReadOnlyDictionary<string, string> inputParameters, string key, double defaultValue)
  {
    if (!inputParameters.TryGetValue(key, out var value))
    {
      return defaultValue;
    }

    return double.TryParse(value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
      ? parsed
      : defaultValue;
  }

  /// <summary>
  /// Применяет контрольную точку к аппаратуре или ее холостой эмуляции.
  /// </summary>
  private static async Task ApplyMeasurementPointAsync(
    LegacyAskTestDescriptor test,
    LegacyAskMeasurementPoint point,
    LegacyAskControllerProtocol controller,
    CancellationToken cancellationToken)
  {
    ushort value = (ushort)Math.Clamp((int)Math.Round(point.Value), 0, ushort.MaxValue);

    if (test.RequiredDevice == LegacyAskRequiredDevice.Adc)
    {
      await controller.ReadAdcAsync(cancellationToken);
      return;
    }

    if (test.RequiredDevice == LegacyAskRequiredDevice.Pint || test.RequiredDevice == LegacyAskRequiredDevice.Pint4)
    {
      await controller.WriteRegisterAsync(LegacyAskRegister.Gui4, LegacyAskSelfTestFormat.ToMillivoltsWord(point.Value), cancellationToken);
      return;
    }

    if (test.RequiredDevice == LegacyAskRequiredDevice.Voltmeter)
    {
      await controller.WriteRegisterAsync(LegacyAskRegister.V7Mode, value, cancellationToken);
      return;
    }

    await controller.WriteCommandRegisterAsync(value, cancellationToken);
  }

  /// <summary>
  /// Выполняет одну проверку сервисного теста.
  /// </summary>
  private static Task ExecuteServiceProbeAsync(
    LegacyAskTestDescriptor test,
    LegacyAskControllerProtocol controller,
    ushort address,
    CancellationToken cancellationToken)
  {
    return test.Code switch
    {
      "EK1LT" => controller.CheckNoElectronicConnectionAsync(address, cancellationToken),
      "EKEPM" => controller.CheckElectronicDisconnectionAsync(address, cancellationToken),
      "PORTS" => controller.ReadRegisterAsync(LegacyAskRegister.V7Mode, cancellationToken),
      _ => controller.CheckElectronicConnectionAsync(address, cancellationToken)
    };
  }

  /// <summary>
  /// Возвращает ожидаемое время срабатывания из legacy-конфигурации.
  /// </summary>
  private static int GetExpectedSwitchingTime(LegacyMkiHardwareProfile profile, string code)
  {
    return code switch
    {
      "TIM_RK_POINT" => profile.Timing.PtRk,
      "TIM_EK_POINT" => profile.Timing.PtEk,
      "TIM_BK_BUS" => profile.Timing.BkBus,
      "TIM_GROUP_RELAY" => profile.Timing.EkRk,
      "TIM_KEP" => profile.Timing.EpPwr,
      "TIM_KZSH" => profile.Timing.KzSh,
      "TIM_PINT4" => profile.Timing.GuiGat,
      "TIM_V7" => profile.Timing.V7Gat,
      "TIM_ADC" => profile.Timing.AcpGat,
      "TIM_PINT_MODE" => Math.Max(profile.Timing.Gui3Mod, profile.Timing.Gui4Mod),
      _ => 50
    };
  }

  /// <summary>
  /// Создает контрольные точки сопротивления.
  /// </summary>
  private static IEnumerable<LegacyAskMeasurementPoint> ResistancePoints()
  {
    foreach (double value in new[] { 10.0, 100.0, 240.0, 1_000.0, 10_000.0, 100_000.0 })
    {
      yield return new LegacyAskMeasurementPoint(value, $"R д.быть={LegacyAskSelfTestFormat.Resistance(value)}+-5%  Rизм={LegacyAskSelfTestFormat.Resistance(value)}");
    }
  }

  /// <summary>
  /// Создает контрольные точки напряжения.
  /// </summary>
  private static IEnumerable<LegacyAskMeasurementPoint> VoltagePoints(double maximumVoltage)
  {
    double max = maximumVoltage <= 0 ? 30.0 : maximumVoltage;
    foreach (double value in new[] { 0.5, 1.0, 5.0, 10.0, 30.0, 100.0 }.Where(x => x <= max))
    {
      yield return new LegacyAskMeasurementPoint(value, $"U д.быть={LegacyAskSelfTestFormat.Voltage(value)}+-5%  Uизм={LegacyAskSelfTestFormat.Voltage(value)}");
    }
  }

  /// <summary>
  /// Создает контрольные точки тока.
  /// </summary>
  private static IEnumerable<LegacyAskMeasurementPoint> CurrentPoints(double maximumCurrent)
  {
    double max = maximumCurrent <= 0 ? 1.0 : maximumCurrent;
    foreach (double value in new[] { 0.001, 0.01, 0.1, 0.5, 1.0 }.Where(x => x <= max))
    {
      yield return new LegacyAskMeasurementPoint(value, $"I д.быть={LegacyAskSelfTestFormat.Current(value)}+-5%  Iизм={LegacyAskSelfTestFormat.Current(value)}");
    }
  }

  /// <summary>
  /// Форматирует число для строк протокола.
  /// </summary>
  private static string FormatNumber(double value)
  {
    return value.ToString("0.###", CultureInfo.InvariantCulture);
  }

  /// <summary>
  /// Описывает одну контрольную точку теста.
  /// </summary>
  private sealed record LegacyAskMeasurementPoint(double Value, string ProtocolLine);
}
