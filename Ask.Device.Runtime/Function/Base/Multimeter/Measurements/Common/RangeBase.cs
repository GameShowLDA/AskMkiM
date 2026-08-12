using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;
using Ask.Device.Emulator;
using Ask.Device.ResponseProcessor.Multimeter.ResponseProcessing;
using Ask.Device.Runtime.Function.Helpers;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Ask.Device.Runtime.Function.Base.Multimeter.Measurements.Common
{
  /// <summary>
  /// Устанавливает автоматические и ручные диапазоны измерений мультиметра.
  /// </summary>
  internal static class RangeBase
  {
    /// <summary>
    /// Последние установленные диапазоны для экземпляров мультиметров и режимов измерения.
    /// </summary>
    private static readonly ConcurrentDictionary<string, double> SelectedRanges = new();

    /// <summary>
    /// Устанавливает диапазон для текущего режима измерения мультиметра.
    /// </summary>
    /// <param name="device">Мультиметр, для которого устанавливается диапазон.</param>
    /// <param name="range">Требуемый диапазон; неположительное значение включает автоматический выбор.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <returns><see langword="true"/>, если диапазон установлен успешно.</returns>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если текущий режим не поддерживает установку диапазона или прибор сообщил об ошибке.
    /// </exception>
    public static async Task<bool> SetRangeAsync(
        IMultimeter device,
        double range,
        IUserInteractionService? userMessageService = null)
    {
      return device.TypeMode switch
      {
        MultimeterTypeMode.AcVoltage => await SetACVoltageRangeAsync(device, range, userMessageService),
        MultimeterTypeMode.DcVoltage => await SetDCVoltageRangeAsync(device, range, userMessageService),
        MultimeterTypeMode.Capacitance => await SetCapacitanceRangeAsync(device, range, userMessageService),
        MultimeterTypeMode.Resistance => await SetResistanceRangeAsync(device, range, userMessageService),
        _ => throw new InvalidOperationException($"Невозможно установить диапазон для режима {device.TypeMode}.")
      };
    }

    /// <summary>
    /// Устанавливает диапазон для измерения, сохраняя ранее выбранный диапазон при отсутствии целевого значения.
    /// </summary>
    /// <param name="device">Мультиметр, для которого устанавливается диапазон.</param>
    /// <param name="range">
    /// Целевое значение измерения; при неположительном значении используется последний выбранный диапазон.
    /// </param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <returns><see langword="true"/>, если диапазон установлен успешно.</returns>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если текущий режим не поддерживает установку диапазона или прибор сообщил об ошибке.
    /// </exception>
    public static Task<bool> SetRangeForMeasurementAsync(
        IMultimeter device,
        double range,
        IUserInteractionService? userMessageService = null)
    {
      var effectiveRange = range <= 0
        ? GetSelectedRange(device)
        : range;

      return SetRangeAsync(device, effectiveRange, userMessageService);
    }

    /// <summary>
    /// Устанавливает диапазон измерения переменного напряжения.
    /// </summary>
    /// <param name="device">Мультиметр, для которого устанавливается диапазон.</param>
    /// <param name="range">Требуемый диапазон; неположительное значение включает автоматический выбор.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <returns><see langword="true"/>, если диапазон установлен успешно.</returns>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если прибор не подключён или сообщил об ошибке.
    /// </exception>
    private static async Task<bool> SetACVoltageRangeAsync(
      IMultimeter device,
      double range,
      IUserInteractionService? userMessageService = null)
    {
      return await SetMeasurementRangeAsync(
        device,
        device.ACVCommands,
        range,
        profile => profile.SetRange,
        profile => profile.SetAutoRange,
        profile => profile.GetRangeError,
        profile => profile.SupportedRanges,
        profile => 1d,
        userMessageService);
    }

    /// <summary>
    /// Устанавливает диапазон измерения постоянного напряжения.
    /// </summary>
    /// <param name="device">Мультиметр, для которого устанавливается диапазон.</param>
    /// <param name="range">Требуемый диапазон; неположительное значение включает автоматический выбор.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <returns><see langword="true"/>, если диапазон установлен успешно.</returns>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если прибор не подключён или сообщил об ошибке.
    /// </exception>
    private static async Task<bool> SetDCVoltageRangeAsync(
      IMultimeter device,
      double range,
      IUserInteractionService? userMessageService = null)
    {
      return await SetMeasurementRangeAsync(
        device,
        device.DCVCommands,
        range,
        profile => profile.SetRange,
        profile => profile.SetAutoRange,
        profile => profile.GetRangeError,
        profile => profile.SupportedRanges,
        profile => 1d,
        userMessageService);
    }

    /// <summary>
    /// Устанавливает диапазон измерения сопротивления.
    /// </summary>
    /// <param name="device">Мультиметр, для которого устанавливается диапазон.</param>
    /// <param name="range">Требуемый диапазон; неположительное значение включает автоматический выбор.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <returns><see langword="true"/>, если диапазон установлен успешно.</returns>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если прибор не подключён или сообщил об ошибке.
    /// </exception>
    private static async Task<bool> SetResistanceRangeAsync(
      IMultimeter device,
      double range,
      IUserInteractionService? userMessageService = null)
    {
      return await SetMeasurementRangeAsync(
        device,
        device.ResistanceCommands,
        range,
        profile => profile.SetRange,
        profile => profile.SetAutoRange,
        profile => profile.GetRangeError,
        profile => profile.SupportedRanges,
        profile => 1d,
        userMessageService);
    }

    /// <summary>
    /// Подтверждает выбор диапазона измерения ёмкости.
    /// </summary>
    /// <param name="device">Мультиметр, для которого выбирается диапазон.</param>
    /// <param name="range">Требуемый диапазон измерения ёмкости.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <returns>Всегда <see langword="true"/>.</returns>
    private static async Task<bool> SetCapacitanceRangeAsync(
      IMultimeter device,
      double range,
      IUserInteractionService? userMessageService = null)
    {
      return true;
    }

    /// <summary>
    /// Устанавливает диапазон по командам профиля измерения и запоминает выбранное значение.
    /// </summary>
    /// <typeparam name="TProfile">Тип профиля измерения.</typeparam>
    /// <param name="device">Мультиметр, для которого устанавливается диапазон.</param>
    /// <param name="profile">Профиль команд и параметров измерения.</param>
    /// <param name="range">Требуемый диапазон; неположительное значение включает автоматический выбор.</param>
    /// <param name="setRangeCommand">Функция получения команды ручной установки диапазона.</param>
    /// <param name="setAutoRangeCommand">Функция получения команды автоматического выбора диапазона.</param>
    /// <param name="getRangeErrorCommand">Функция получения команды чтения ошибки прибора.</param>
    /// <param name="getSupportedRanges">Функция получения поддерживаемых диапазонов.</param>
    /// <param name="getRangeCommandMultiplier">Функция получения множителя значения в команде.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <returns><see langword="true"/>, если диапазон установлен или уже был выбран.</returns>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если прибор не подключён, сообщил об ошибке или установка диапазона не выполнена.
    /// </exception>
    private static async Task<bool> SetMeasurementRangeAsync<TProfile>(
      IMultimeter device,
      TProfile profile,
      double range,
      Func<TProfile, string> setRangeCommand,
      Func<TProfile, string> setAutoRangeCommand,
      Func<TProfile, string?> getRangeErrorCommand,
      Func<TProfile, double[]> getSupportedRanges,
      Func<TProfile, double>? getRangeCommandMultiplier,
      IUserInteractionService? userMessageService)
      where TProfile : IMeasurementProfile
    {
      var header = GetRangeHeader(profile.TypeMode);
      var effectiveRange = range <= 0 ? 0 : ResolveRange(range, getSupportedRanges(profile));
      var rangeText = range <= 0
        ? "Авто"
        : $"{effectiveRange.ToString("G", CultureInfo.InvariantCulture)} {profile.Unit.GetUnit()}";
      var rangeKey = BuildRangeKey(device, profile.TypeMode);

      if (SelectedRanges.TryGetValue(rangeKey, out var selectedRange)
        && selectedRange.Equals(effectiveRange))
      {
        return true;
      }

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var success = await SetMeasurementRangeCoreAsync(
          device,
          profile,
          effectiveRange,
          setRangeCommand(profile),
          setAutoRangeCommand(profile),
          getRangeErrorCommand(profile),
          Array.Empty<double>(),
          getRangeCommandMultiplier?.Invoke(profile) ?? 1d);

        if (!success || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await MultimeterMessages.PublishOperationResultAsync(
            device,
            header,
            rangeText,
            success,
            1,
            userMessageService);
        }

        return success;
      }, userMessageService, deviceTask: true);

      if (!result)
      {
        throw new InvalidOperationException($"Ошибка установки диапазона \"{header}\" для {device.Name}({device.NumberChassis}.{device.Number}).");
      }

      SelectedRanges[rangeKey] = effectiveRange;
      return true;
    }

    /// <summary>
    /// Формирует заголовок операции установки диапазона для режима измерения.
    /// </summary>
    /// <param name="typeMode">Режим измерения мультиметра.</param>
    /// <returns>Заголовок операции установки диапазона.</returns>
    private static string GetRangeHeader(MultimeterTypeMode typeMode)
    {
      return typeMode switch
      {
        MultimeterTypeMode.DcVoltage => "Установка диапазона постоянного напряжения",
        MultimeterTypeMode.AcVoltage => "Установка диапазона переменного напряжения",
        MultimeterTypeMode.Resistance => "Установка диапазона сопротивления",
        MultimeterTypeMode.Capacitance => "Установка диапазона ёмкости",
        _ => $"Установка диапазона \"{EnumExtensions.GetDescription(typeMode)}\"",
      };
    }

    /// <summary>
    /// Переключает режим мультиметра, передаёт команду диапазона и проверяет ошибку прибора.
    /// </summary>
    /// <param name="device">Мультиметр, для которого устанавливается диапазон.</param>
    /// <param name="profile">Профиль команд и параметров измерения.</param>
    /// <param name="range">Диапазон; неположительное значение включает автоматический выбор.</param>
    /// <param name="setRangeCommand">Шаблон команды ручной установки диапазона.</param>
    /// <param name="setAutoRangeCommand">Команда автоматического выбора диапазона.</param>
    /// <param name="getRangeErrorCommand">Команда чтения ошибки прибора.</param>
    /// <param name="supportedRanges">Поддерживаемые диапазоны измерения.</param>
    /// <param name="rangeCommandMultiplier">Множитель диапазона при формировании команды.</param>
    /// <returns><see langword="true"/>, если команда выполнена без ошибки прибора.</returns>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если прибор не подключён или сообщил об ошибке.
    /// </exception>
    private static async Task<bool> SetMeasurementRangeCoreAsync(
      IMultimeter device,
      IMeasurementProfile profile,
      double range,
      string setRangeCommand,
      string setAutoRangeCommand,
      string? getRangeErrorCommand,
      double[] supportedRanges,
      double rangeCommandMultiplier)
    {
      if (!ExecutionConfig.GetIsIdleModeEnabled() && !device.ConnectionInfo.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      if (device.TypeMode != profile.TypeMode)
      {
        await SetModeBase.SetModeAsync(device, profile);
      }

      var command = range <= 0
        ? setAutoRangeCommand
        : BuildRangeCommand(setRangeCommand, profile, ResolveRange(range, supportedRanges), rangeCommandMultiplier);

      await DeviceProtocolEmulator.QueryMultimeterAsync(device, command, string.Empty);
      await EnsureNoInstrumentErrorAsync(device, getRangeErrorCommand, profile.Timeout);

      return true;
    }

    /// <summary>
    /// Формирует команду установки диапазона и разрешения измерения.
    /// </summary>
    /// <param name="template">Шаблон команды с заполнителями диапазона и разрешения.</param>
    /// <param name="profile">Профиль измерения, определяющий единицу измерения.</param>
    /// <param name="range">Диапазон измерения.</param>
    /// <param name="rangeCommandMultiplier">Множитель диапазона при формировании команды.</param>
    /// <returns>Команда установки диапазона и разрешения.</returns>
    /// <exception cref="FormatException">
    /// Выбрасывается, если <paramref name="template"/> имеет недопустимый составной формат.
    /// </exception>
    private static string BuildRangeCommand(string template, IMeasurementProfile profile, double range, double rangeCommandMultiplier)
    {
      var commandRange = range * rangeCommandMultiplier;
      return string.Format(
        CultureInfo.InvariantCulture,
        template,
        commandRange,
        ResolveResolution(profile, commandRange));
    }

    /// <summary>
    /// Выбирает поддерживаемый диапазон для запрошенного значения.
    /// </summary>
    /// <param name="requestedRange">Запрошенное значение диапазона.</param>
    /// <param name="supportedRanges">Поддерживаемые диапазоны измерения.</param>
    /// <returns>
    /// Минимальный поддерживаемый диапазон, включающий запрошенное значение; максимальный диапазон при
    /// превышении всех поддерживаемых значений; модуль запрошенного значения при пустом списке диапазонов.
    /// </returns>
    private static double ResolveRange(double requestedRange, double[] supportedRanges)
    {
      var requested = Math.Abs(requestedRange);
      if (supportedRanges.Length == 0)
      {
        return requested;
      }

      foreach (var supportedRange in supportedRanges.OrderBy(value => value))
      {
        if (requested <= supportedRange)
        {
          return supportedRange;
        }
      }

      return supportedRanges.Max();
    }

    /// <summary>
    /// Определяет разрешение измерения для единицы измерения и диапазона.
    /// </summary>
    /// <param name="profile">Профиль измерения, определяющий единицу измерения.</param>
    /// <param name="range">Диапазон измерения.</param>
    /// <returns>Разрешение измерения.</returns>
    private static double ResolveResolution(IMeasurementProfile profile, double range)
    {
      return profile.Unit switch
      {
        VoltageUnit => ResolveVoltageResolution(range),
        ResistanceUnit => ResolveResistanceResolution(range),
        _ => range * 0.000001d
      };
    }

    /// <summary>
    /// Определяет разрешение измерения напряжения для заданного диапазона.
    /// </summary>
    /// <param name="range">Диапазон измерения напряжения.</param>
    /// <returns>Разрешение измерения напряжения.</returns>
    private static double ResolveVoltageResolution(double range)
    {
      return range switch
      {
        <= 0.1d => 0.0000001d,
        <= 1d => 0.000001d,
        <= 10d => 0.00001d,
        <= 100d => 0.0001d,
        _ => 0.001d
      };
    }

    /// <summary>
    /// Определяет разрешение измерения сопротивления для заданного диапазона.
    /// </summary>
    /// <param name="range">Диапазон измерения сопротивления.</param>
    /// <returns>Разрешение измерения сопротивления.</returns>
    private static double ResolveResistanceResolution(double range)
    {
      return Math.Max(range * 0.000001d, 0.000001d);
    }

    /// <summary>
    /// Проверяет отсутствие ошибки мультиметра после установки диапазона.
    /// </summary>
    /// <param name="device">Мультиметр, состояние которого проверяется.</param>
    /// <param name="getRangeErrorCommand">Команда чтения ошибки прибора.</param>
    /// <param name="timeout">Время ожидания ответа прибора, мс.</param>
    /// <returns>Задача, представляющая асинхронную проверку.</returns>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если мультиметр сообщил об ошибке установки диапазона.
    /// </exception>
    private static async Task EnsureNoInstrumentErrorAsync(
      IMultimeter device,
      string? getRangeErrorCommand,
      int timeout)
    {
      if (string.IsNullOrWhiteSpace(getRangeErrorCommand))
      {
        return;
      }

      var error = await DeviceProtocolEmulator.QueryMultimeterAsync(
        device,
        getRangeErrorCommand,
        "+0,\"No error\"",
        timeout: timeout);
      if (!MultimeterResponseProcessor.CheckNoInstrumentError(error, out _))
      {
        throw new InvalidOperationException($"Ошибка установки диапазона: {error}");
      }
    }

    /// <summary>
    /// Возвращает последний установленный диапазон для текущего режима мультиметра.
    /// </summary>
    /// <param name="device">Мультиметр, для которого запрашивается диапазон.</param>
    /// <returns>Последний установленный диапазон или ноль, если диапазон не сохранён.</returns>
    private static double GetSelectedRange(IMultimeter device)
    {
      return SelectedRanges.TryGetValue(BuildRangeKey(device, device.TypeMode), out var range)
        ? range
        : 0;
    }

    /// <summary>
    /// Формирует ключ диапазона для экземпляра мультиметра и режима измерения.
    /// </summary>
    /// <param name="device">Экземпляр мультиметра.</param>
    /// <param name="typeMode">Режим измерения мультиметра.</param>
    /// <returns>Ключ сохранённого диапазона.</returns>
    private static string BuildRangeKey(IMultimeter device, MultimeterTypeMode typeMode)
    {
      return $"{RuntimeHelpers.GetHashCode(device)}:{typeMode}";
    }
  }
}
