using System;
using System.Globalization;
using System.Windows.Controls;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Static.Messages;

namespace Ask.Device.Runtime.Function.Base.Multimeter.SelfCheck
{
  public class SelfTestManager : ISelfTestCheckerMultimeter
  {
    private const double IdealVoltage = 0;
    private const double VoltageRange = 0.2;
    private const double MinimumActiveResistance = 50;
    private const int RequiredCapacitanceMeasurements = 6;
    private const int MeasurementResponseDelayMs = 0;
    private const string VoltageUnit = " В";
    private const string ResistanceUnit = " Ом";
    private const string CapacitanceUnit = " нФ";
    private const double RelativeErrorMarker = -1;

    private static readonly double[] DcVoltageRanges = { 0.1, 1, 10, 100, 1000 };
    private static readonly double[] AcVoltageRanges = { 0.1, 1, 10, 100, 750 };

    private static readonly ObjectCheck[] ResistanceChecks =
    {
      new ObjectCheck(1, 2, 50),
      new ObjectCheck(2, 100, 5),
      new ObjectCheck(3, 1_000, 5),
      new ObjectCheck(4, 10_000, 5),
      new ObjectCheck(5, 100_000, 5),
      new ObjectCheck(6, 1_000_000, 5),
      new ObjectCheck(7, 10_000_000, 5),
      //new ObjectCheck(8, 85_000_000, 5)

    };

    private static readonly ObjectCheck[] CapacitanceChecks =
    {
      new ObjectCheck(1, 3.6, 10),
      new ObjectCheck(2, 11, 10),
      new ObjectCheck(3, 125, 10),
      new ObjectCheck(4, 1_000, 10),
      // Неисправны.
      //new ObjectCheck(5, 6_800, 10),
      //new ObjectCheck(6, 110_000, 10)
    };

    /// <summary>
    /// Возвращает тип перечисления с доступными проверками самоконтроля мультиметра.
    /// </summary>
    /// <returns>Тип перечисления <see cref="MultimeterTypeConnector"/>.</returns>
    public Type GetTestTypeEnum() => typeof(MultimeterTypeConnector);

    /// <summary>
    /// Запускает самоконтроль мультиметра для выбранного типа проверки.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены выполнения проверки.</param>
    /// <param name="selectedType">Выбранный тип проверки мультиметра.</param>
    /// <param name="userMessageService">Сервис вывода сообщений пользователю.</param>
    /// <param name="device">Устройство коммутации шин, используемое для подключения проверочных цепей.</param>
    /// <param name="meter">Проверяемый мультиметр.</param>
    /// <returns>Задача, представляющая выполнение самоконтроля.</returns>
    public async Task StartSelfCheck(CancellationToken cancellationToken, Enum selectedType, ActionSettings settings, IUserInteractionService? userMessageService = null, ISwitchingDevice? device = null, IMultimeter? meter = null)
    {
      ArgumentNullException.ThrowIfNull(userMessageService);
      ArgumentNullException.ThrowIfNull(device);
      ArgumentNullException.ThrowIfNull(meter);

      settings.DeviceResults.Add(new DeviceExecutionResult(meter.Name, meter.NumberChassis, meter.Number));        

      cancellationToken.ThrowIfCancellationRequested();
      await ExecutionMessages.PublishMultimeterSetupAsync(userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await ShowActionHeaderAsync("Инициализация коммутационного устройства", userMessageService);
      await device.ConnectableManager.InitializeAsync(userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await ShowActionHeaderAsync("Инициализация мультиметра", userMessageService);
      await meter.ConnectableManager.InitializeAsync(userMessageService);

      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        await ShowActionHeaderAsync("Отключение всех шин", userMessageService);
        await device.ConnectorManager.DisconnectAllBuses(userMessageService);

        switch (selectedType)
        {
          case MultimeterTypeConnector.Voltage:
            await StartVoltageMeasurementTestAsync(cancellationToken, device, meter, userMessageService);
            break;

          case MultimeterTypeConnector.Resistance:
            await StartResistanceMeasurementTestAsync(cancellationToken, device, meter, userMessageService);
            break;

          case MultimeterTypeConnector.Capacity:
            await StartCapacitanceMeasurementTestAsync(cancellationToken, device, meter, userMessageService);
            break;

          case MultimeterTypeConnector.FullCheck:
            await StartVoltageMeasurementTestAsync(cancellationToken, device, meter, userMessageService);
            await StartResistanceMeasurementTestAsync(cancellationToken, device, meter, userMessageService);
            await StartCapacitanceMeasurementTestAsync(cancellationToken, device, meter, userMessageService);
            break;
        }
      }
      finally
      {
        await device.ConnectorManager.DisconnectAllBuses(userMessageService);
      }
    }

    /// <summary>
    /// Рассчитывает допустимое отклонение для измерения объекта.
    /// </summary>
    /// <param name="idealResult">Идеальный результат измерения.</param>
    /// <param name="percentageError">Допустимая относительная погрешность в процентах.</param>
    /// <returns>Допустимое абсолютное отклонение.</returns>
    private static double ObjectTolerance(double idealResult, double percentageError) => (percentageError / 100d) * idealResult;

    /// <summary>
    /// Выполняет проверку измерения постоянного и переменного напряжения.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены выполнения проверки.</param>
    /// <param name="device">Устройство коммутации шин.</param>
    /// <param name="meter">Проверяемый мультиметр.</param>
    /// <param name="userMessageService">Сервис выводы сообщений пользователю.</param>
    /// <returns>Задача, представляющая выполнение проверки напряжения.</returns>
    private static async Task StartVoltageMeasurementTestAsync(CancellationToken cancellationToken, ISwitchingDevice device, IMultimeter meter, IUserInteractionService userMessageService)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await ShowActionHeaderAsync("Подключение мультиметра к шине AB1", userMessageService);
      await device.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB1, userMessageService);

      var relayEnabled = false;

      try
      {
        await ShowActionHeaderAsync("Включение общего реле", userMessageService);
        await device.RelayManager.EnableRelay(userMessageService);
        relayEnabled = true;

        await RunVoltageModeCheckAsync(
          cancellationToken,
          "Тест измерения постоянного напряжения:",
          "Режим измерения постоянного напряжения",
          meter.DcVoltageManager.SetDCVoltageModeAsync,
          meter.DcVoltageManager.SetDCVoltageRangeAsync,
          meter.DcVoltageManager.MeasureDCVoltageAsync,
          DcVoltageRanges,
          userMessageService);

        await RunVoltageModeCheckAsync(
          cancellationToken,
          "Тест измерения переменного напряжения:",
          "Режим измерения переменного напряжения",
          meter.AcVoltageManager.SetACVoltageModeAsync,
          meter.AcVoltageManager.SetACVoltageRangeAsync,
          meter.AcVoltageManager.MeasureACVoltageAsync,
          AcVoltageRanges,
          userMessageService);
      }
      finally
      {
        if (relayEnabled)
        {
          await device.RelayManager.DisableRelay(userMessageService);
        }

        await device.ConnectorManager.DisconnectMultimeter(SwitchingBusNew.AB1, userMessageService);
      }
    }

    /// <summary>
    /// Выполняет проверку одного режима измерения напряжения на наборе диапазонов.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены выполнения проверки.</param>
    /// <param name="header">Заголовок раздела проверки.</param>
    /// <param name="modeHeader">Заголовок шага установки режима измерения напряжения.</param>
    /// <param name="setVoltageMode">Делегат установки режима измерения напряжения.</param>
    /// <param name="setVoltageRange">Делегат установки диапазона измерения напряжения.</param>
    /// <param name="measureVoltage">Делегат измерения напряжения.</param>
    /// <param name="ranges">Набор проверяемых диапазонов напряжения.</param>
    /// <param name="userMessageService">Сервис вывода сообщений пользователю.</param>
    /// <returns>Задача, представляющая выполнение проверки режима напряжения.</returns>
    private static async Task RunVoltageModeCheckAsync(
      CancellationToken cancellationToken,
      string header,
      string modeHeader,
      Func<IUserInteractionService?, Task<bool>> setVoltageMode,
      Func<double, IUserInteractionService?, Task<bool>> setVoltageRange,
      Func<MeasurementRange, IUserInteractionService?, double, Task<double>> measureVoltage,
      double[] ranges,
      IUserInteractionService userMessageService)
    {
      await ShowSectionHeaderAsync(header, userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await ShowActionHeaderAsync(modeHeader, userMessageService);
      await setVoltageMode(userMessageService);

      try
      {
        foreach (var range in ranges)
        {
          await MeasureVoltageRangeAsync(cancellationToken, range, setVoltageRange, measureVoltage, userMessageService);
        }
      }
      finally
      {
        await setVoltageRange(0, userMessageService);
      }
    }

    /// <summary>
    /// Выполняет измерение напряжения на одном диапазоне и выводит результат проверки.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены выполнения проверки.</param>
    /// <param name="range">Проверяемый диапазон измерения напряжения.</param>
    /// <param name="setVoltageRange">Делегат установки диапазона измерения напряжения.</param>
    /// <param name="measureVoltage">Делегат измерения напряжения.</param>
    /// <param name="userMessageService">Сервис вывода сообщений пользователю.</param>
    /// <returns>Задача, представляющая выполнение измерения диапазона.</returns>
    private static async Task MeasureVoltageRangeAsync(
      CancellationToken cancellationToken,
      double range,
      Func<double, IUserInteractionService?, Task<bool>> setVoltageRange,
      Func<MeasurementRange, IUserInteractionService?, double, Task<double>> measureVoltage,
      IUserInteractionService userMessageService)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await ShowCheckStepAsync($"Проверка диапазона {range}{VoltageUnit}", userMessageService);
      await ShowActionHeaderAsync($"Установка диапазона {range}{VoltageUnit}", userMessageService);
      await setVoltageRange(range, userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await ShowActionHeaderAsync($"Измерение напряжения на диапазоне {range}{VoltageUnit}", userMessageService);
      var measurementRange = new MeasurementRange(IdealVoltage, -VoltageRange, VoltageRange);
      var result = await measureVoltage(measurementRange, userMessageService, MeasurementResponseDelayMs);

      cancellationToken.ThrowIfCancellationRequested();
      var resultStatus = SelfTestHelper.InRange(IdealVoltage, result, VoltageRange);
      await SelfTestHelper.IsCorrectRangeAsync(resultStatus, result, $"диапазона {range}", VoltageUnit, IdealVoltage, 2, userMessageService);
    }

    /// <summary>
    /// Выполняет проверку измерения сопротивления на всех заданных эталонных резисторах.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены выполнения проверки.</param>
    /// <param name="device">Устройство коммутации шин.</param>
    /// <param name="meter">Проверяемый мультиметр.</param>
    /// <param name="userMessageService">Сервис вывода сообщений пользователю.</param>
    /// <returns>Задача, представляющая выполнение проверки сопротивления.</returns>
    private static async Task StartResistanceMeasurementTestAsync(CancellationToken cancellationToken, ISwitchingDevice device, IMultimeter meter, IUserInteractionService userMessageService)
    {
      await RunWithRcRelayAsync(
        cancellationToken,
        device,
        userMessageService,
        async () =>
        {
          await ShowSectionHeaderAsync("Тест измерения сопротивления:", userMessageService);

          cancellationToken.ThrowIfCancellationRequested();
          await ShowActionHeaderAsync("Режим измерения сопротивления", userMessageService);
          await meter.ResistanceManager.SetResistanceModeAsync(userMessageService);

          foreach (var check in ResistanceChecks)
          {
            await MeasureResistanceAsync(cancellationToken, check, device, meter, userMessageService);
          }
        });
    }

    /// <summary>
    /// Подключает эталонный резистор, выполняет измерение сопротивления и выводит результат проверки.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены выполнения проверки.</param>
    /// <param name="check">Параметры проверяемого резистора.</param>
    /// <param name="device">Устройство коммутации шин.</param>
    /// <param name="meter">Проверяемый мультиметр.</param>
    /// <param name="userMessageService">Сервис вывода сообщений пользователю.</param>
    /// <returns>Задача, представляющая выполнение измерения сопротивления.</returns>
    private static async Task MeasureResistanceAsync(CancellationToken cancellationToken, ObjectCheck check, ISwitchingDevice device, IMultimeter meter, IUserInteractionService userMessageService)
    {
      var resistanceValue = FormatReferenceValue(check.IdealResult, ResistanceUnit);

      cancellationToken.ThrowIfCancellationRequested();
      await ShowCheckStepAsync(
        $"Проверка резистора {resistanceValue}",
        userMessageService);
      await ShowActionHeaderAsync($"Подключение резистора {resistanceValue}", userMessageService);
      var resistorConnected = await device.RelayManager.ConnectResistor(check.Number, userMessageService);

      try
      {
        var tolerance = ObjectTolerance(check.IdealResult, check.PercentageError);

        await ShowCheckStepAsync(
          $"Измерение сопротивления резистора {resistanceValue}",
          userMessageService,
          indentLevel: 2);

        var measurementRange = new MeasurementRange(
          check.IdealResult,
          check.IdealResult - tolerance,
          check.IdealResult + tolerance);
        var result = await meter.ResistanceManager.MeasureResistanceAsync(
          measurementRange,
          userMessageService,
          responseDelay: MeasurementResponseDelayMs);
        var resultStatus = SelfTestHelper.InRange(check.IdealResult, result, tolerance);

        cancellationToken.ThrowIfCancellationRequested();
        await SelfTestHelper.IsCorrectRangeAsync(
          resultStatus,
          result,
          check.IdealResult.ToString("N1"),
          ResistanceUnit,
          RelativeErrorMarker,
          check.PercentageError,
          userMessageService);
      }
      finally
      {
        if (resistorConnected)
        {
          await device.RelayManager.DisconnectResistor(check.Number, userMessageService);
        }
      }
    }

    /// <summary>
    /// Выполняет проверку измерения ёмкости на всех заданных эталонных конденсаторах.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены выполнения проверки.</param>
    /// <param name="device">Устройство коммутации шин.</param>
    /// <param name="meter">Проверяемый мультиметр.</param>
    /// <param name="userMessageService">Сервис вывода сообщений пользователю.</param>
    /// <returns>Задача, представляющая выполнение проверки ёмкости.</returns>
    private static async Task StartCapacitanceMeasurementTestAsync(CancellationToken cancellationToken, ISwitchingDevice device, IMultimeter meter, IUserInteractionService userMessageService)
    {
      await RunWithRcRelayAsync(
        cancellationToken,
        device,
        userMessageService,
        async () =>
        {
          await ShowSectionHeaderAsync($"Тест измерения ёмкости:", userMessageService);

          foreach (var check in CapacitanceChecks)
          {
            await MeasureCapacitanceAsync(cancellationToken, check, device, meter, userMessageService);
          }
        });
    }

    /// <summary>
    /// Подключает эталонный конденсатор, проверяет активное сопротивление и выполняет измерение ёмкости.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены выполнения проверки.</param>
    /// <param name="check">Параметры проверяемого конденсатора.</param>
    /// <param name="device">Устройство коммутации шин.</param>
    /// <param name="meter">Проверяемый мультиметр.</param>
    /// <param name="userMessageService">Сервис вывода сообщений пользователю.</param>
    /// <returns>Задача, представляющая выполнение измерения ёмкости.</returns>
    private static async Task MeasureCapacitanceAsync(CancellationToken cancellationToken, ObjectCheck check, ISwitchingDevice device, IMultimeter meter, IUserInteractionService userMessageService)
    {
      var capacitanceValue = FormatReferenceValue(check.IdealResult, CapacitanceUnit);

      cancellationToken.ThrowIfCancellationRequested();
      await ShowCheckStepAsync(
        $"Проверка конденсатора {capacitanceValue}",
        userMessageService);
      await ShowActionHeaderAsync($"Подключение конденсатора {capacitanceValue}", userMessageService);
      var capacitorConnected = await device.RelayManager.ConnectCapacitor(check.Number, userMessageService);

      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        await ShowActionHeaderAsync("Режим измерения сопротивления", userMessageService);
        await meter.ResistanceManager.SetResistanceModeAsync(userMessageService);

        cancellationToken.ThrowIfCancellationRequested();
        await ShowCheckStepAsync($"Измерение активного сопротивления конденсатора {capacitanceValue}", userMessageService);
        var activeResistanceRange = new MeasurementRange(-1, 51, 2_000_000);
        var activeResistance = await meter.ResistanceManager.MeasureResistanceAsync(
          activeResistanceRange,
          responseDelay: MeasurementResponseDelayMs);
        var activeResistanceCorrect = activeResistance > MinimumActiveResistance;

        await ShowActiveResistanceResultAsync(activeResistance, activeResistanceCorrect, capacitanceValue, userMessageService);

        cancellationToken.ThrowIfCancellationRequested();
        if (!activeResistanceCorrect)
        {
          return;
        }

        await ShowActionHeaderAsync("Режим измерения ёмкости", userMessageService);
        await meter.CapacitanceManager.SetCapacitanceModeAsync(userMessageService);

        var tolerance = ObjectTolerance(check.IdealResult, check.PercentageError);

        await ShowCheckStepAsync(
          $"Измерение ёмкости конденсатора {capacitanceValue}",
          $"требуется {RequiredCapacitanceMeasurements} положительных результатов",
          userMessageService);

        var measurementRange = new MeasurementRange(
          check.IdealResult,
          check.IdealResult - tolerance,
          check.IdealResult + tolerance);
        var result = await meter.CapacitanceManager.MeasureCapacitanceAsync(
          measurementRange,
          userMessageService: userMessageService,
          measurementCount: RequiredCapacitanceMeasurements,
          responseDelay: MeasurementResponseDelayMs);

        cancellationToken.ThrowIfCancellationRequested();
        var resultStatus = SelfTestHelper.InRange(check.IdealResult, result, tolerance);
        await SelfTestHelper.IsCorrectRangeAsync(
          resultStatus,
          result,
          check.IdealResult.ToString("N1"),
          CapacitanceUnit,
          RelativeErrorMarker,
          check.PercentageError,
          userMessageService);
      }
      finally
      {
        if (capacitorConnected)
        {
          await device.RelayManager.DisconnectCapacitor(check.Number, userMessageService);
        }
      }
    }

    /// <summary>
    /// Подключает мультиметр к RC-цепям, включает RC-реле и выполняет переданный сценарий проверки.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены выполнения проверки.</param>
    /// <param name="device">Устройство коммутации шин.</param>
    /// <param name="userMessageService">Сервис вывода сообщений пользователю.</param>
    /// <param name="runCheckAsync">Сценарий проверки, выполняемый при подключенном RC-реле.</param>
    /// <returns>Задача, представляющая выполнение сценария с RC-реле.</returns>
    private static async Task RunWithRcRelayAsync(CancellationToken cancellationToken, ISwitchingDevice device, IUserInteractionService userMessageService, Func<Task> runCheckAsync)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await ShowActionHeaderAsync("Подключение мультиметра к шине AB4", userMessageService);
      await device.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB4, userMessageService);

      var rcRelayConnected = false;

      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        await ShowActionHeaderAsync("Подключение RC реле", userMessageService);
        await device.RelayManager.ConnectRCRelay(userMessageService);
        rcRelayConnected = true;

        await runCheckAsync();
      }
      finally
      {
        if (rcRelayConnected)
        {
          await device.RelayManager.DisconnectRCRelay(userMessageService);
        }

        await device.ConnectorManager.DisconnectMultimeter(SwitchingBusNew.AB4, userMessageService);
      }
    }

    /// <summary>
    /// Выводит заголовок раздела самоконтроля.
    /// </summary>
    /// <param name="header">Текст заголовка раздела.</param>
    /// <param name="userMessageService">Сервис вывода сообщений пользователю.</param>
    /// <returns>Задача вывода сообщения.</returns>
    private static Task ShowSectionHeaderAsync(string header, IUserInteractionService userMessageService)
    {
      return SelfTestMessages.PublishCommandAsync(header, userMessageService);
    }

    /// <summary>
    /// Выводит заголовок действия только в активном пошаговом режиме.
    /// </summary>
    private static Task ShowActionHeaderAsync(string header, IUserInteractionService userMessageService)
    {
      return SelfTestMessages.PublishCommandAsync(
        header,
        userMessageService,
        onlyWhenStepMode: true);
    }

    /// <summary>
    /// Выводит сообщение шага проверки только в активном пошаговом режиме.
    /// </summary>
    private static Task ShowCheckStepAsync(
      string header,
      IUserInteractionService userMessageService,
      int indentLevel = 1)
    {
      return ShowCheckStepAsync(header, null, userMessageService, indentLevel);
    }

    /// <summary>
    /// Выводит сообщение шага проверки только в активном пошаговом режиме.
    /// </summary>
    private static Task ShowCheckStepAsync(
      string header,
      string? message,
      IUserInteractionService userMessageService,
      int indentLevel = 1)
    {
      return SelfTestMessages.PublishCommandAsync(
        header,
        userMessageService,
        message,
        indentLevel,
        onlyWhenStepMode: true);
    }

    /// <summary>
    /// Выводит результат проверки активного сопротивления конденсатора.
    /// </summary>
    /// <param name="result">Измеренное активное сопротивление.</param>
    /// <param name="isCorrect">Признак прохождения проверки активного сопротивления.</param>
    /// <param name="capacitanceValue">Эталонная ёмкость проверяемого конденсатора.</param>
    /// <param name="userMessageService">Сервис вывода сообщений пользователю.</param>
    /// <returns>Задача вывода сообщения.</returns>
    private static Task ShowActiveResistanceResultAsync(double result, bool isCorrect, string capacitanceValue, IUserInteractionService userMessageService)
    {
      return SelfTestMessages.PublishActiveResistanceResultAsync(
        result,
        isCorrect,
        capacitanceValue,
        MinimumActiveResistance,
        ResistanceUnit,
        userMessageService);
    }

    private static string FormatReferenceValue(double value, string unit)
    {
      var formattedValue = MeasurementValueFormatter.Round(value).ToString("N3", CultureInfo.CurrentCulture);
      var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

      if (formattedValue.Contains(decimalSeparator))
      {
        formattedValue = formattedValue.TrimEnd('0');

        if (formattedValue.EndsWith(decimalSeparator, StringComparison.Ordinal))
        {
          formattedValue = formattedValue.Substring(0, formattedValue.Length - decimalSeparator.Length);
        }
      }

      return $"{formattedValue}{unit}";
    }

    private readonly struct ObjectCheck
    {
      public ObjectCheck(int number, double idealResult, int percentageError)
      {
        Number = number;
        IdealResult = idealResult;
        PercentageError = percentageError;
      }
      public int Number { get; }
      public double IdealResult { get; }
      public int PercentageError { get; }
    }
  }
}
