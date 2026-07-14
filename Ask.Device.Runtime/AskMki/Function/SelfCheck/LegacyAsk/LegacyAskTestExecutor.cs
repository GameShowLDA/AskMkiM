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
public sealed partial class LegacyAskTestExecutor
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
}
