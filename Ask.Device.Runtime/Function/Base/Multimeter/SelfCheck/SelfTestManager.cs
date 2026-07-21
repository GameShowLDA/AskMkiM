using System;
using System.Windows.Media;
using Ask.Core.Shared.DTO.Protocol;
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
    private const double VoltageRangeFrom = -0.2;
    private const double VoltageRangeTo = 0.2;
    private const double MinimumActiveResistance = 50;
    private const int RequiredCapacitanceMeasurements = 6;
    private const int MaxCapacitanceMeasurementAttempts = RequiredCapacitanceMeasurements * 5;
    private const string VoltageUnit = " В";
    private const string ResistanceUnit = " Ом";
    private const string CapacitanceUnit = " нФ";
    private const double RelativeErrorMarker = -1;

    private static readonly Color? HeaderColor = new ShowMessageModel(
        type: ShowMessageModel.MessageType.CommandBlock)
        .GetColorMessage();

    private static readonly double[] DcVoltageRanges = new[] { 0.1, 1, 10, 100, 1000 };
    private static readonly double[] AcVoltageRanges = new[] { 0.1, 1, 10, 100, 750 };

    private static readonly ResistanceCheck[] ResistanceChecks =
    {
      new ResistanceCheck(1, 2, 50),
      new ResistanceCheck(2, 100, 1),
      new ResistanceCheck(3, 1_050, 1),
      new ResistanceCheck(4, 10_000, 1),
      new ResistanceCheck(5, 100_000, 1),
      new ResistanceCheck(6, 1_000_000, 1),
      new ResistanceCheck(7, 10_000_000, 6),
      new ResistanceCheck(8, 86_000_000, 1),
    };

    private static readonly CapacitanceCheck[] CapacitanceChecks =
    {
      new CapacitanceCheck(1, 3.3),
      new CapacitanceCheck(2, 10),
      new CapacitanceCheck(3, 130),
      new CapacitanceCheck(4, 1_000),
      // Неисправен.
      // new CapacitanceCheck(5, 6_800),
      new CapacitanceCheck(6, 86_000),
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
    public async Task StartSelfCheck(CancellationToken cancellationToken, Enum selectedType, IUserInteractionService? userMessageService = null, ISwitchingDevice? device = null, IMultimeter? meter = null)
    {
      ArgumentNullException.ThrowIfNull(userMessageService);
      ArgumentNullException.ThrowIfNull(device);
      ArgumentNullException.ThrowIfNull(meter);

      cancellationToken.ThrowIfCancellationRequested();
      await userMessageService.ShowMessageAsync(ExecutorMessageBuilder.BuildMultimeterSetupMessage());

      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectableManager.InitializeAsync(userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await meter.ConnectableManager.InitializeAsync(userMessageService);

      try
      {
        cancellationToken.ThrowIfCancellationRequested();
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
    /// Рассчитывает допустимое отклонение для измерения напряжения.
    /// </summary>
    /// <param name="voltage">Эталонное напряжение.</param>
    /// <returns>Допустимое абсолютное отклонение напряжения.</returns>
    private static double VoltageTolerance(double voltage) => (0.1 * voltage) + 0.02;

    /// <summary>
    /// Рассчитывает допустимое отклонение для измерения сопротивления.
    /// </summary>
    /// <param name="resistance">Эталонное сопротивление.</param>
    /// <param name="percentageError">Допустимая относительная погрешность в процентах.</param>
    /// <returns>Допустимое абсолютное отклонение сопротивления.</returns>
    private static double ResistanceTolerance(double resistance, double percentageError) => (percentageError / 100) * resistance;

    /// <summary>
    /// Рассчитывает допустимое отклонение для измерения ёмкости.
    /// </summary>
    /// <param name="capacity">Эталонная ёмкость.</param>
    /// <returns>Допустимое абсолютное отклонение ёмкости.</returns>
    private static double CapacityTolerance(double capacity) => (0.05 * capacity) + 1;

    /// <summary>
    /// Выполняет проверку измерения постоянного и переменного напряжения.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены выполнения проверки.</param>
    /// <param name="device">Устройство коммутации шин.</param>
    /// <param name="meter">Проверяемый мультиметр.</param>
    /// <param name="userMessageService">Сервис вывода сообщений пользователю.</param>
    /// <returns>Задача, представляющая выполнение проверки напряжения.</returns>
    private static async Task StartVoltageMeasurementTestAsync(CancellationToken cancellationToken, ISwitchingDevice device, IMultimeter meter, IUserInteractionService userMessageService)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB1, userMessageService);

      var relayEnabled = false;

      try
      {
        await device.RelayManager.EnableRelay(userMessageService);
        relayEnabled = true;

        await RunVoltageModeCheckAsync(
          cancellationToken,
          "Тест измерения постоянного напряжения:",
          meter.DcVoltageManager.SetDCVoltageModeAsync,
          meter.DcVoltageManager.SetDCVoltageRangeAsync,
          meter.DcVoltageManager.MeasureDCVoltageAsync,
          DcVoltageRanges,
          userMessageService);

        await RunVoltageModeCheckAsync(
          cancellationToken,
          "Тест измерения переменного напряжения:",
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
    /// <param name="setVoltageMode">Делегат установки режима измерения напряжения.</param>
    /// <param name="setVoltageRange">Делегат установки диапазона измерения напряжения.</param>
    /// <param name="measureVoltage">Делегат измерения напряжения.</param>
    /// <param name="ranges">Набор проверяемых диапазонов напряжения.</param>
    /// <param name="userMessageService">Сервис вывода сообщений пользователю.</param>
    /// <returns>Задача, представляющая выполнение проверки режима напряжения.</returns>
    private static async Task RunVoltageModeCheckAsync(
      CancellationToken cancellationToken,
      string header,
      Func<IUserInteractionService?, Task<bool>> setVoltageMode,
      Func<double, IUserInteractionService?, Task<bool>> setVoltageRange,
      Func<double, double, double, IUserInteractionService?, Task<double>> measureVoltage,
      double[] ranges,
      IUserInteractionService userMessageService)
    {
      await ShowSectionHeaderAsync(header, userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
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
      Func<double, double, double, IUserInteractionService?, Task<double>> measureVoltage,
      IUserInteractionService userMessageService)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await setVoltageRange(range, userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      var result = await measureVoltage(IdealVoltage, VoltageRangeFrom, VoltageRangeTo, userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      var resultStatus = SelfTestHelper.InRange(IdealVoltage, result, VoltageTolerance(IdealVoltage));
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
    private static async Task MeasureResistanceAsync(CancellationToken cancellationToken, ResistanceCheck check, ISwitchingDevice device, IMultimeter meter, IUserInteractionService userMessageService)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var resistorConnected = await device.RelayManager.ConnectResistor(check.Number, userMessageService);

      try
      {
        var tolerance = ResistanceTolerance(check.IdealResult, check.PercentageError);

        cancellationToken.ThrowIfCancellationRequested();
        var result = await meter.ResistanceManager.MeasureResistanceAsync(
          check.IdealResult,
          check.IdealResult - tolerance,
          check.IdealResult + tolerance,
          userMessageService);

        cancellationToken.ThrowIfCancellationRequested();
        var resultStatus = SelfTestHelper.InRange(check.IdealResult, result, tolerance);
        await SelfTestHelper.IsCorrectRangeAsync(
          resultStatus,
          result,
          check.IdealResult.ToString("N0"),
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
          await ShowSectionHeaderAsync("Тест измерения ёмкости:", userMessageService);

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
    private static async Task MeasureCapacitanceAsync(CancellationToken cancellationToken, CapacitanceCheck check, ISwitchingDevice device, IMultimeter meter, IUserInteractionService userMessageService)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var capacitorConnected = await device.RelayManager.ConnectCapacitor(check.Number, userMessageService);

      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        await meter.ResistanceManager.SetResistanceModeAsync(userMessageService);

        cancellationToken.ThrowIfCancellationRequested();
        var activeResistance = await meter.ResistanceManager.MeasureResistanceAsync();
        var activeResistanceCorrect = activeResistance > MinimumActiveResistance;

        await ShowActiveResistanceResultAsync(activeResistance, activeResistanceCorrect, userMessageService);

        cancellationToken.ThrowIfCancellationRequested();
        if (!activeResistanceCorrect)
        {
          return;
        }

        await meter.CapacitanceManager.SetCapacitanceModeAsync(userMessageService);

        var tolerance = CapacityTolerance(check.IdealResult);
        await meter.CapacitanceManager.SetCapacitanceRangeAsync(check.IdealResult, userMessageService);

        var result = await MeasureAverageCapacitanceAsync(cancellationToken, meter, check.IdealResult, tolerance, userMessageService);

        cancellationToken.ThrowIfCancellationRequested();
        var resultStatus = SelfTestHelper.InRange(check.IdealResult, result, tolerance);
        await SelfTestHelper.IsCorrectRangeAsync(
          resultStatus,
          result,
          check.IdealResult.ToString("N0"),
          CapacitanceUnit,
          RelativeErrorMarker,
          5,
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
    /// Выполняет серию измерений ёмкости и возвращает среднее значение успешных измерений.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены выполнения проверки.</param>
    /// <param name="meter">Проверяемый мультиметр.</param>
    /// <param name="idealResult">Эталонная ёмкость.</param>
    /// <param name="tolerance">Допустимое отклонение ёмкости.</param>
    /// <param name="userMessageService">Сервис вывода сообщений пользователю.</param>
    /// <returns>Среднее значение ёмкости или -1, если не удалось получить нужное количество успешных измерений.</returns>
    private static async Task<double> MeasureAverageCapacitanceAsync(CancellationToken cancellationToken, IMultimeter meter, double idealResult, double tolerance, IUserInteractionService userMessageService)
    {
      var successfulMeasurements = 0;
      var attempts = 0;
      var sum = 0d;

      while (successfulMeasurements < RequiredCapacitanceMeasurements && attempts < MaxCapacitanceMeasurementAttempts)
      {
        cancellationToken.ThrowIfCancellationRequested();
        attempts++;

        var result = await meter.CapacitanceManager.MeasureCapacitanceAsync(
          0,
          idealResult - tolerance,
          idealResult + tolerance,
          userMessageService: userMessageService);

        if (result <= 0)
        {
          continue;
        }

        sum += result;
        successfulMeasurements++;
      }

      return successfulMeasurements == RequiredCapacitanceMeasurements
        ? sum / RequiredCapacitanceMeasurements
        : -1;
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
      await device.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB4, userMessageService);

      var rcRelayConnected = false;

      try
      {
        cancellationToken.ThrowIfCancellationRequested();
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
      return userMessageService.ShowMessageAsync(
        new ShowMessageModel(
          header: header,
          headerColor: HeaderColor,
          type: ShowMessageModel.MessageType.Info));
    }

    /// <summary>
    /// Выводит результат проверки активного сопротивления конденсатора.
    /// </summary>
    /// <param name="result">Измеренное активное сопротивление.</param>
    /// <param name="isCorrect">Признак прохождения проверки активного сопротивления.</param>
    /// <param name="userMessageService">Сервис вывода сообщений пользователю.</param>
    /// <returns>Задача вывода сообщения.</returns>
    private static Task ShowActiveResistanceResultAsync(double result, bool isCorrect, IUserInteractionService userMessageService)
    {
      var resultType = isCorrect
        ? ShowMessageModel.MessageType.Success
        : ShowMessageModel.MessageType.Error;
      var meaning = MeasurementValueFormatter.IsOverloadValue(result) ? "Overload" : $"{result} Ом";

      return userMessageService.ShowMessageAsync(
        new ShowMessageModel(
          header: $"Тест активного сопротивления (>{MinimumActiveResistance:N0} Ом)",
          message: $"{meaning} ",
          type: resultType)
        {
          IndentLevel = 1,
        });
    }

    private readonly struct ResistanceCheck
    {
      public ResistanceCheck(int number, double idealResult, int percentageError)
      {
        Number = number;
        IdealResult = idealResult;
        PercentageError = percentageError;
      }

      public int Number { get; }

      public double IdealResult { get; }

      public int PercentageError { get; }
    }

    private readonly struct CapacitanceCheck
    {
      public CapacitanceCheck(int number, double idealResult)
      {
        Number = number;
        IdealResult = idealResult;
      }

      public int Number { get; }

      public double IdealResult { get; }
    }
  }
}