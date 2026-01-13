using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.Breakdown;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Mode;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Converters;
using NewCore.Device;
using NewCore.Function.GPT;
using NewCore.Function.Helpers;

namespace NewCore.FunctionAdapters.GPT
{
  /// <summary>
  /// Адаптер режима IR (измерение сопротивления изоляции) для устройства GPT-79904.
  /// </summary>
  /// <remarks>
  /// Класс реализует интерфейс <see cref="IIrModeBreakdown"/> и представляет собой фасад,
  /// объединяющий все подадаптеры, необходимые для управления режимом IR:
  /// установку параметров, выполнение измерений, управление временем, напряжением, смещением,
  /// пределами сопротивления и чтением конфигурации.
  /// </remarks>
  internal class IrModeAdapter : IIrModeBreakdown
  {
    /// <summary>
    /// Экземпляр устройства <see cref="GPT79904"/>, с которым связан данный адаптер.
    /// </summary>
    private readonly GPT79904 _device;

    /// <summary>
    /// Экземпляр класса <see cref="IrMode"/>, предоставляющий доступ к внутренней логике режима IR.
    /// </summary>
    private readonly IrMode _irMode;

    /// <summary>
    /// Адаптер, обеспечивающий управление режимом работы IR (установка и чтение текущего состояния).
    /// </summary>
    public IModeConfigurable Mode { get; set; }

    /// <summary>
    /// Адаптер, обеспечивающий управление параметрами напряжения в режиме IR.
    /// </summary>
    public IVoltageConfigurable Voltage { get; set; }

    /// <summary>
    /// Адаптер, обеспечивающий управление параметрами времени теста в режиме IR.
    /// </summary>
    public ITimeConfigurable Time { get; set; }

    /// <summary>
    /// Адаптер, обеспечивающий управление параметром смещения (Offset) в режиме IR.
    /// </summary>
    public IOffsetConfigurable Offset { get; set; }

    /// <summary>
    /// Адаптер, предоставляющий функциональность измерений сопротивления изоляции:
    /// выполнение измерений, подача напряжения и остановка теста.
    /// </summary>
    public IMeasurable Measure { get; set; }

    /// <summary>
    /// Адаптер, обеспечивающий операции чтения и сброса конфигурации режима IR.
    /// </summary>
    public IConfigurationProvider<IrConfiguration> Config { get; set; }

    /// <summary>
    /// Адаптер, обеспечивающий управление пределами сопротивления (верхний и нижний лимит)
    /// при испытании изоляции в режиме IR.
    /// </summary>
    public IResistanceLimitsConfigurable ResistanceLimits { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="IrModeAdapter"/>,
    /// создавая все подадаптеры для управления режимом IR устройства GPT-79904.
    /// </summary>
    /// <param name="device">
    /// Экземпляр устройства <see cref="GPT79904"/>, с которым будет работать адаптер.  
    /// Не может быть <see langword="null"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Генерируется, если параметр <paramref name="device"/> равен <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// В конструкторе создаются все подадаптеры режима IR:
    /// <list type="bullet">
    ///   <item><term><see cref="Mode"/></term> — управление режимом работы.</item>
    ///   <item><term><see cref="Voltage"/></term> — установка и чтение напряжения.</item>
    ///   <item><term><see cref="Time"/></term> — настройка времени теста.</item>
    ///   <item><term><see cref="Offset"/></term> — установка смещения (Offset).</item>
    ///   <item><term><see cref="Measure"/></term> — выполнение измерений сопротивления изоляции.</item>
    ///   <item><term><see cref="Config"/></term> — чтение и сброс конфигурации.</item>
    ///   <item><term><see cref="ResistanceLimits"/></term> — установка и чтение пределов сопротивления.</item>
    /// </list>
    /// </remarks>
    public IrModeAdapter(GPT79904 device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _irMode = new IrMode(device);

      Mode = new IrAdapterMode(_irMode, _device);
      Voltage = new VoltageAdapterMode(_irMode, _device);
      Time = new TimeAdapterMode(_irMode, _device);
      Offset = new OffsetAdapterMode(_irMode, _device);
      Measure = new MeasureAdapterMode(_irMode, _device);
      Config = new ConfigAdapterMode(_irMode, _device);
      ResistanceLimits = new ResistanceLimitsAdapterMode(_irMode, _device);
    }

    /// <summary>
    /// Адаптер для управления режимом IR (измерение сопротивления изоляции)
    /// на устройстве GPT-79904.
    /// </summary>
    /// <remarks>
    /// Класс реализует интерфейс <see cref="IModeConfigurable"/> и обеспечивает
    /// установку и получение текущего режима IR через функциональность класса <see cref="IrMode"/>.
    /// </remarks>
    public class IrAdapterMode : IModeConfigurable
    {
      /// <summary>
      /// Экземпляр класса <see cref="IrMode"/>, предоставляющий доступ к операциям управления режимом IR.
      /// </summary>
      private readonly IrMode _irMode;

      /// <summary>
      /// Экземпляр устройства <see cref="GPT79904"/>, для которого выполняется настройка режима IR.
      /// </summary>
      private readonly GPT79904 _device;

      /// <summary>
      /// Инициализирует новый экземпляр класса <see cref="IrAdapterMode"/>.
      /// </summary>
      /// <param name="irMode">Экземпляр класса <see cref="IrMode"/>, реализующий функциональность режима IR.</param>
      /// <param name="device">Экземпляр устройства <see cref="GPT79904"/>, на котором выполняются операции режима IR.</param>
      public IrAdapterMode(IrMode irMode, GPT79904 device)
      {
        _irMode = irMode;
        _device = device;
      }

      /// <summary>
      /// Асинхронно устанавливает режим IR на устройстве GPT-79904.
      /// </summary>
      /// <param name="userMessageService">
      /// Сервис отображения сообщений пользователю.  
      /// Если передан, используется для уведомления о ходе и результате установки режима.
      /// </param>
      /// <returns>
      /// Кортеж <c>(Success, Message)</c>, где:
      /// <list type="bullet">
      ///   <item><term>Success</term> — признак успешной установки режима;</item>
      ///   <item><term>Message</term> — описание результата или сообщение об ошибке.</item>
      /// </list>
      /// </returns>
      /// <exception cref="IrException">
      /// Генерируется при неудачной установке режима IR.  
      /// Сообщение исключения содержит подробное описание ошибки.
      /// </exception>
      public async Task<(bool, string)> SetModeAsync(IUserInteractionService? userMessageService = null)
      {
        var result = await UserActionHelper.GetRunWithUserRepeatAsync(() => _irMode.Mode.SetModeAsync(), userMessageService, deviceTask: true);

        if (!result.Connect || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Установка режима IR",
          result.Connect ? "IR" : result.Answer,
          result.Connect,
          1,
          userMessageService);
        }

        if (!result.Connect)
          throw IrExceptionFactory.SetModeFailed(_device.Name, _device.NumberChassis, _device.Number, result.Answer);

        return result;
      }

      /// <summary>
      /// Асинхронно получает текущий установленный режим IR на устройстве GPT-79904.
      /// </summary>
      /// <returns>
      /// Кортеж <c>(Success, Message)</c>, где:
      /// <list type="bullet">
      ///   <item><term>Success</term> — признак успешного получения режима;</item>
      ///   <item><term>Message</term> — текстовое описание текущего режима или сообщение об ошибке.</item>
      /// </list>
      /// </returns>
      public Task<(bool Success, string Message)> GetModeAsync() => _irMode.Mode.GetModeAsync();
    }

    /// <summary>
    /// Адаптер для управления напряжением в режиме IR (измерение сопротивления изоляции)
    /// на устройстве GPT-79904.
    /// </summary>
    /// <remarks>
    /// Класс реализует интерфейс <see cref="IVoltageConfigurable"/> и обеспечивает
    /// унифицированный доступ к операциям установки и считывания напряжения режима IR
    /// через функциональность класса <see cref="IrMode"/>.
    /// </remarks>
    public class VoltageAdapterMode : IVoltageConfigurable
    {
      /// <summary>
      /// Экземпляр класса <see cref="IrMode"/>, предоставляющий доступ к управлению напряжением IR.
      /// </summary>
      private readonly IrMode _irMode;

      /// <summary>
      /// Экземпляр устройства <see cref="GPT79904"/>, на котором выполняются операции с напряжением.
      /// </summary>
      private readonly GPT79904 _device;

      /// <summary>
      /// Инициализирует новый экземпляр класса <see cref="VoltageAdapterMode"/>.
      /// </summary>
      /// <param name="irMode">Экземпляр класса <see cref="IrMode"/>, реализующий управление напряжением IR.</param>
      /// <param name="device">Экземпляр устройства <see cref="GPT79904"/>, для которого выполняется настройка напряжения.</param>
      public VoltageAdapterMode(IrMode irMode, GPT79904 device)
      {
        _irMode = irMode;
        _device = device;
      }

      /// <summary>
      /// Асинхронно устанавливает значение напряжения IR на устройстве GPT-79904.
      /// </summary>
      /// <param name="value">Значение напряжения в вольтах (В).</param>
      /// <param name="userMessageService">
      /// Сервис отображения сообщений пользователю.  
      /// Если передан, используется для уведомления о ходе и результате установки параметра.
      /// </param>
      /// <returns>
      /// Кортеж <c>(Success, Message)</c>, где:
      /// <list type="bullet">
      ///   <item><term>Success</term> — признак успешной установки напряжения;</item>
      ///   <item><term>Message</term> — описание результата или сообщение об ошибке.</item>
      /// </list>
      /// </returns>
      /// <exception cref="IrException">
      /// Генерируется при неудачной установке напряжения.  
      /// Сообщение исключения содержит имя, номер шасси и номер устройства, вызвавшего ошибку.
      /// </exception>
      public async Task<(bool, string)> SetVoltageAsync(double value, IUserInteractionService? userMessageService = null)
      {
        var result = await UserActionHelper.GetRunWithUserRepeatAsync(() => _irMode.Voltage.SetVoltageAsync(value), userMessageService, deviceTask: true);

        if (!result.Connect || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Установка напряжения IR",
          result.Connect ? $"{value} В" : result.Answer,
          result.Connect,
          1,
          userMessageService);
        }

        if (!result.Connect)
          throw IrExceptionFactory.SetVoltageFailed(_device.Name, _device.NumberChassis, _device.Number);

        return result;
      }

      /// <summary>
      /// Асинхронно получает текущее установленное значение напряжения IR с устройства GPT-79904.
      /// </summary>
      /// <returns>Значение напряжения в вольтах (В).</returns>
      /// <remarks>
      /// После успешного чтения отображается сообщение о считанном значении.
      /// </remarks>
      public async Task<double> GetVoltageAsync()
      {
        var value = await _irMode.Voltage.GetVoltageAsync();

        await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Чтение напряжения IR",
          $"{value} В",
          value > 0,
          1);

        return value;
      }
    }

    /// <summary>
    /// Адаптер для управления пределами сопротивления (верхний и нижний) 
    /// в режиме IR (измерение сопротивления изоляции) устройства GPT-79904.
    /// </summary>
    /// <remarks>
    /// Класс реализует интерфейс <see cref="IResistanceLimitsConfigurable"/> и обеспечивает
    /// унифицированный доступ к установке и чтению верхнего и нижнего пределов сопротивления
    /// через функциональность класса <see cref="IrMode"/>.
    /// </remarks>
    public class ResistanceLimitsAdapterMode : IResistanceLimitsConfigurable
    {
      /// <summary>
      /// Экземпляр класса <see cref="IrMode"/>, предоставляющий доступ к операциям конфигурации пределов сопротивления.
      /// </summary>
      private readonly IrMode _irMode;

      /// <summary>
      /// Экземпляр устройства <see cref="GPT79904"/>, для которого выполняются операции настройки пределов сопротивления.
      /// </summary>
      private readonly GPT79904 _device;

      /// <summary>
      /// Инициализирует новый экземпляр класса <see cref="ResistanceLimitsAdapterMode"/>.
      /// </summary>
      /// <param name="irMode">Экземпляр класса <see cref="IrMode"/>, реализующий функциональность установки пределов сопротивления.</param>
      /// <param name="device">Экземпляр устройства <see cref="GPT79904"/>, на котором выполняются операции режима IR.</param>
      public ResistanceLimitsAdapterMode(IrMode irMode, GPT79904 device)
      {
        _irMode = irMode;
        _device = device;
      }

      /// <summary>
      /// Асинхронно устанавливает верхний предел сопротивления изоляции в режиме IR.
      /// </summary>
      /// <param name="value">Значение верхнего предела сопротивления в ГОм.</param>
      /// <param name="userMessageService">
      /// Сервис отображения сообщений пользователю.  
      /// Если передан, используется для уведомления о ходе и результате установки параметра.
      /// </param>
      /// <returns>
      /// Кортеж <c>(Success, Message)</c>, где:
      /// <list type="bullet">
      ///   <item><term>Success</term> — признак успешной установки предела;</item>
      ///   <item><term>Message</term> — текстовое сообщение о результате операции.</item>
      /// </list>
      /// </returns>
      /// <exception cref="IrException">
      /// Генерируется, если установка верхнего предела сопротивления завершилась с ошибкой.
      /// </exception>
      public async Task<(bool, string)> SetHighResistanceLimitAsync(double value, IUserInteractionService? userMessageService = null)
      {
        var result = await UserActionHelper.GetRunWithUserRepeatAsync(() => _irMode.ResistanceLimits.SetHighResistanceLimitAsync(value), userMessageService, deviceTask: true);

        if (!result.Connect || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Установка верхнего предела сопротивления IR",
          result.Connect ? $"{value} ГОм" : result.Answer,
          result.Connect,
          1,
          userMessageService);
        }

        if (!result.Connect)
          throw IrExceptionFactory.SetHighLimitFailed(_device.Name, _device.NumberChassis, _device.Number, result.Answer);

        return result;
      }

      /// <summary>
      /// Асинхронно получает текущее установленное значение верхнего предела сопротивления изоляции.
      /// </summary>
      /// <returns>Значение верхнего предела сопротивления в ГОм.</returns>
      public Task<double> GetHighResistanceLimitAsync() => _irMode.ResistanceLimits.GetHighResistanceLimitAsync();

      /// <summary>
      /// Асинхронно устанавливает нижний предел сопротивления изоляции в режиме IR.
      /// </summary>
      /// <param name="value">Значение нижнего предела сопротивления в МОм.</param>
      /// <param name="userMessageService">
      /// Сервис отображения сообщений пользователю.  
      /// Если передан, используется для уведомления о ходе и результате установки параметра.
      /// </param>
      /// <returns>
      /// Кортеж <c>(Success, Message)</c>, где:
      /// <list type="bullet">
      ///   <item><term>Success</term> — признак успешной установки предела;</item>
      ///   <item><term>Message</term> — текстовое сообщение о результате операции.</item>
      /// </list>
      /// </returns>
      /// <exception cref="IrException">
      /// Генерируется, если установка нижнего предела сопротивления завершилась с ошибкой.
      /// </exception>
      public async Task<(bool, string)> SetLowResistanceLimitAsync(double value, IUserInteractionService? userMessageService = null)
      {
        var result = await UserActionHelper.GetRunWithUserRepeatAsync(() => _irMode.ResistanceLimits.SetLowResistanceLimitAsync(value), userMessageService, deviceTask: true);

        if (!result.Connect || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Установка нижнего предела сопротивления IR",
          result.Connect ? $"{value} МОм" : result.Answer,
          result.Connect,
          1,
          userMessageService);
        }

        if (!result.Connect)
          throw IrExceptionFactory.SetLowLimitFailed(_device.Name, _device.NumberChassis, _device.Number, result.Answer);

        return result;
      }

      /// <summary>
      /// Асинхронно получает текущее установленное значение нижнего предела сопротивления изоляции.
      /// </summary>
      /// <returns>Значение нижнего предела сопротивления в МОм.</returns>
      public Task<double> GetLowResistanceLimitAsync() => _irMode.ResistanceLimits.GetLowResistanceLimitAsync();
    }

    /// <summary>
    /// Адаптер для управления временными параметрами в режиме IR (измерение сопротивления изоляции)
    /// устройства GPT-79904.
    /// </summary>
    /// <remarks>
    /// Класс реализует интерфейс <see cref="ITimeConfigurable"/> и обеспечивает
    /// установку и чтение параметров времени измерения и времени нарастания (Ramp Time)
    /// через функциональность класса <see cref="IrMode"/>.
    /// </remarks>
    public class TimeAdapterMode : ITimeConfigurable
    {
      /// <summary>
      /// Экземпляр класса <see cref="IrMode"/>, предоставляющий доступ к операциям управления временем.
      /// </summary>
      private readonly IrMode _irMode;

      /// <summary>
      /// Экземпляр устройства <see cref="GPT79904"/>, на котором выполняются операции настройки времени.
      /// </summary>
      private readonly GPT79904 _device;

      /// <summary>
      /// Инициализирует новый экземпляр класса <see cref="TimeAdapterMode"/>.
      /// </summary>
      /// <param name="irMode">Экземпляр класса <see cref="IrMode"/>, реализующий управление временными параметрами.</param>
      /// <param name="device">Экземпляр устройства <see cref="GPT79904"/>, для которого выполняется настройка времени.</param>
      public TimeAdapterMode(IrMode irMode, GPT79904 device)
      {
        _irMode = irMode;
        _device = device;
      }

      /// <summary>
      /// Асинхронно устанавливает время измерения в режиме IR.
      /// </summary>
      /// <param name="value">Время измерения в секундах.</param>
      /// <param name="userMessageService">
      /// Сервис отображения сообщений пользователю.  
      /// Если передан, используется для уведомления о ходе и результате установки параметра.
      /// </param>
      /// <returns>
      /// Кортеж <c>(Success, Message)</c>, где:
      /// <list type="bullet">
      ///   <item><term>Success</term> — признак успешной установки времени измерения;</item>
      ///   <item><term>Message</term> — описание результата или сообщение об ошибке.</item>
      /// </list>
      /// </returns>
      /// <exception cref="IrException">
      /// Генерируется, если установка времени измерения завершилась с ошибкой.  
      /// Сообщение исключения содержит подробное описание ошибки.
      /// </exception>
      public async Task<(bool, string)> SetTestTimeAsync(double value, IUserInteractionService? userMessageService = null)
      {
        var result = await UserActionHelper.GetRunWithUserRepeatAsync(() => _irMode.Time.SetTestTimeAsync(value), userMessageService, deviceTask: true);

        if (!result.Connect || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Установка времени измерения IR",
          result.Connect ? $"{value} сек" : result.Answer,
          result.Connect,
          1,
          userMessageService);
        }

        if (!result.Connect)
          throw IrExceptionFactory.SetTestTimeFailed(_device.Name, _device.NumberChassis, _device.Number, result.Answer);

        return result;
      }

      /// <summary>
      /// Асинхронно получает текущее установленное время измерения в режиме IR.
      /// </summary>
      /// <returns>Значение времени измерения в секундах.</returns>
      public Task<double> GetTestTimeAsync() => _irMode.Time.GetTestTimeAsync();

      /// <summary>
      /// Асинхронно устанавливает время нарастания (Ramp Time) в режиме IR.
      /// </summary>
      /// <param name="value">Время нарастания в секундах.</param>
      /// <param name="userMessageService">
      /// Сервис отображения сообщений пользователю.  
      /// Если передан, используется для уведомления о ходе и результате установки параметра.
      /// </param>
      /// <returns>
      /// Кортеж <c>(Success, Message)</c>, где:
      /// <list type="bullet">
      ///   <item><term>Success</term> — признак успешной установки времени нарастания;</item>
      ///   <item><term>Message</term> — описание результата или сообщение об ошибке.</item>
      /// </list>
      /// </returns>
      /// <exception cref="IrException">
      /// Генерируется, если установка времени нарастания завершилась с ошибкой.
      /// </exception>
      public async Task<(bool Success, string Message)> SetRampTimeAsync(double value, IUserInteractionService? userMessageService = null)
      {
        var result = await UserActionHelper.GetRunWithUserRepeatAsync(() => _irMode.Time.SetRampTimeAsync(value), userMessageService, deviceTask: true);

        if (!result.Connect || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Установка времени нарастания IR",
          result.Connect ? $"{value} сек" : result.Answer,
          result.Connect,
          1,
          userMessageService);
        }

        if (!result.Connect)
          throw IrExceptionFactory.SetTestTimeFailed(_device.Name, _device.NumberChassis, _device.Number, result.Answer);

        return result;
      }

      /// <summary>
      /// Асинхронно получает текущее установленное значение времени нарастания (Ramp Time) в режиме IR.
      /// </summary>
      /// <returns>Значение времени нарастания в секундах.</returns>
      public Task<double> GetRampTimeAsync() => _irMode.Time.GetRampTimeAsync();
    }

    /// <summary>
    /// Адаптер для управления параметром смещения (Offset)
    /// в режиме IR (измерение сопротивления изоляции) устройства GPT-79904.
    /// </summary>
    /// <remarks>
    /// Класс реализует интерфейс <see cref="IOffsetConfigurable"/> и обеспечивает
    /// установку и считывание значения смещения через функциональность класса <see cref="IrMode"/>.
    /// </remarks>
    public class OffsetAdapterMode : IOffsetConfigurable
    {
      /// <summary>
      /// Экземпляр класса <see cref="IrMode"/>, предоставляющий доступ к операциям управления смещением.
      /// </summary>
      private readonly IrMode _irMode;

      /// <summary>
      /// Экземпляр устройства <see cref="GPT79904"/>, на котором выполняются операции настройки смещения.
      /// </summary>
      private readonly GPT79904 _device;

      /// <summary>
      /// Инициализирует новый экземпляр класса <see cref="OffsetAdapterMode"/>.
      /// </summary>
      /// <param name="irMode">Экземпляр класса <see cref="IrMode"/>, реализующий управление параметром смещения.</param>
      /// <param name="device">Экземпляр устройства <see cref="GPT79904"/>, для которого выполняется настройка смещения.</param>
      public OffsetAdapterMode(IrMode irMode, GPT79904 device)
      {
        _irMode = irMode;
        _device = device;
      }

      /// <summary>
      /// Асинхронно устанавливает значение смещения (Offset) в режиме IR.
      /// </summary>
      /// <param name="value">Значение смещения в ГОм.</param>
      /// <param name="userMessageService">
      /// Сервис отображения сообщений пользователю.  
      /// Если передан, используется для уведомления о ходе и результате установки параметра.
      /// </param>
      /// <returns>
      /// Кортеж <c>(Success, Message)</c>, где:
      /// <list type="bullet">
      ///   <item><term>Success</term> — признак успешной установки смещения;</item>
      ///   <item><term>Message</term> — описание результата или сообщение об ошибке.</item>
      /// </list>
      /// </returns>
      /// <exception cref="IrException">
      /// Генерируется, если установка смещения завершилась с ошибкой.  
      /// Сообщение исключения содержит имя, номер шасси и номер устройства.
      /// </exception>
      public async Task<(bool, string)> SetOffsetAsync(double value, IUserInteractionService? userMessageService = null)
      {
        var result = await UserActionHelper.GetRunWithUserRepeatAsync(() => _irMode.Offset.SetOffsetAsync(value), userMessageService, deviceTask: true);

        if (!result.Connect || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Установка смещения IR",
          result.Connect ? $"{value} ГОм" : result.Answer,
          result.Connect,
          1,
          userMessageService);
        }

        if (!result.Connect)
          throw IrExceptionFactory.SetOffsetFailed(_device.Name, _device.NumberChassis, _device.Number, result.Answer);

        return result;
      }

      /// <summary>
      /// Асинхронно получает текущее установленное значение смещения (Offset) в режиме IR.
      /// </summary>
      /// <returns>Значение смещения в ГОм.</returns>
      public Task<double> GetOffsetAsync() => _irMode.Offset.GetOffsetAsync();
    }

    /// <summary>
    /// Адаптер для выполнения измерений в режиме IR (измерение сопротивления изоляции)
    /// устройства GPT-79904.
    /// </summary>
    /// <remarks>
    /// Класс реализует интерфейс <see cref="IMeasurable"/> и обеспечивает выполнение измерений,
    /// остановку измерений и управление процессом подачи напряжения через функциональность класса <see cref="IrMode"/>.
    /// </remarks>
    public class MeasureAdapterMode : IMeasurable
    {
      /// <summary>
      /// Экземпляр класса <see cref="IrMode"/>, предоставляющий доступ к операциям измерения сопротивления.
      /// </summary>
      private readonly IrMode _irMode;

      /// <summary>
      /// Экземпляр устройства <see cref="GPT79904"/>, на котором выполняются измерения сопротивления изоляции.
      /// </summary>
      private readonly GPT79904 _device;

      /// <summary>
      /// Инициализирует новый экземпляр класса <see cref="MeasureAdapterMode"/>.
      /// </summary>
      /// <param name="irMode">Экземпляр класса <see cref="IrMode"/>, реализующий функциональность измерений.</param>
      /// <param name="device">Экземпляр устройства <see cref="GPT79904"/>, для которого выполняются измерения.</param>
      public MeasureAdapterMode(IrMode irMode, GPT79904 device)
      {
        _irMode = irMode;
        _device = device;
      }

      /// <summary>
      /// Асинхронно выполняет измерение сопротивления изоляции в заданном диапазоне.
      /// </summary>
      /// <param name="param">Дополнительный параметр, используемый в некоторых режимах измерения.</param>
      /// <param name="rangeFrom">Нижняя граница диапазона измерения (по умолчанию — -1, без ограничения).</param>
      /// <param name="rangeTo">Верхняя граница диапазона измерения (по умолчанию — 60000 МОм).</param>
      /// <param name="userMessageService">
      /// Сервис отображения сообщений пользователю.  
      /// Если передан, используется для уведомления о ходе и результате измерения.
      /// </param>
      /// <returns>
      /// Результат измерения сопротивления изоляции в МОм.  
      /// В случае ошибки возвращает значение <c>-1</c>.
      /// </returns>
      public async Task<(double value, string unit)> MeasureAsync(double param = 0, double rangeFrom = -1, double rangeTo = 600000, bool waitFullTime = false, IUserInteractionService? userMessageService = null)
      {
        if (rangeTo == -1) rangeTo = 600000;

        try
        {
          var (result, unit) = await _irMode.Measure.MeasureAsync(param, rangeFrom, rangeTo);

          var unitEnum = ResistanceConverter.ParseUnit(unit, "мом");
          result = ResistanceConverter.ToMegaOhms(result, unitEnum);

          await DeviceMessageBuilder.ShowConnectionMessageAsync(
            _device,
            "Измерение сопротивления изоляции",
            $"{result} МОм",
            result >= rangeFrom && result <= rangeTo,
            2,
            userMessageService);


          return (result, unit);
        }
        catch (Exception ex)
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(
            _device,
            "Ошибка измерения сопротивления изоляции",
            ex.Message,
            false,
            2,
            userMessageService);

          return (-1, string.Empty);
        }
      }

      /// <summary>
      /// Асинхронно подаёт напряжение на объект измерения в режиме IR.
      /// </summary>
      /// <param name="userMessageService">
      /// Сервис отображения сообщений пользователю.  
      /// В текущей реализации метод не поддерживается и выбрасывает исключение.
      /// </param>
      /// <exception cref="NotImplementedException">
      /// Генерируется, если вызов метода не реализован.
      /// </exception>
      public Task ApplyVoltageAsync(IUserInteractionService? userMessageService = null)
      {
        throw new NotImplementedException();
      }

      /// <summary>
      /// Асинхронно останавливает текущий процесс измерения сопротивления изоляции.
      /// </summary>
      public async Task StopMeasure()
      {
        await _irMode.Measure.StopMeasure();
      }
    }

    /// <summary>
    /// Адаптер для работы с конфигурацией режима IR (измерение сопротивления изоляции)
    /// устройства GPT-79904.
    /// </summary>
    /// <remarks>
    /// Класс реализует интерфейс <see cref="IConfigurationProvider{IrConfiguration}"/> и обеспечивает
    /// чтение и сброс конфигурации режима IR через функциональность класса <see cref="IrMode"/>.
    /// </remarks>
    public class ConfigAdapterMode : IConfigurationProvider<IrConfiguration>
    {
      /// <summary>
      /// Экземпляр класса <see cref="IrMode"/>, предоставляющий доступ к операциям чтения и сброса конфигурации.
      /// </summary>
      private readonly IrMode _irMode;

      /// <summary>
      /// Экземпляр устройства <see cref="GPT79904"/>, для которого выполняются операции с конфигурацией.
      /// </summary>
      private readonly GPT79904 _device;

      /// <summary>
      /// Инициализирует новый экземпляр класса <see cref="ConfigAdapterMode"/>.
      /// </summary>
      /// <param name="irMode">Экземпляр класса <see cref="IrMode"/>, реализующий функциональность управления конфигурацией.</param>
      /// <param name="device">Экземпляр устройства <see cref="GPT79904"/>, для которого выполняются операции конфигурации.</param>
      public ConfigAdapterMode(IrMode irMode, GPT79904 device)
      {
        _irMode = irMode;
        _device = device;
      }

      /// <summary>
      /// Асинхронно считывает текущую конфигурацию режима IR с устройства GPT-79904.
      /// </summary>
      /// <returns>
      /// Объект <see cref="IrConfiguration"/>, содержащий все параметры текущей конфигурации режима IR.
      /// </returns>
      /// <remarks>
      /// После успешного чтения выводится сообщение о том, что конфигурация считана.
      /// </remarks>
      public async Task<IrConfiguration> ReadConfigurationAsync()
      {
        var config = await _irMode.Config.ReadConfigurationAsync();

        await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Чтение конфигурации IR",
          "Конфигурация считана",
          true,
          1);

        return config;
      }

      /// <summary>
      /// Выполняет сброс конфигурации режима IR до значений по умолчанию.
      /// </summary>
      /// <remarks>
      /// Метод вызывает внутреннюю реализацию сброса конфигурации в классе <see cref="IrMode"/>.
      /// </remarks>
      public void ResetConfiguration()
      {
        _irMode.Config.ResetConfiguration();
      }
    }
  }
}
