using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.Breakdown;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Mode;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using NewCore.Device;
using NewCore.Function.GPT;
using NewCore.Function.Helpers;

namespace NewCore.FunctionAdapters.GPT
{
  /// <summary>
  /// Адаптер режима ACW для устройства GPT-79904 с отображением сообщений.
  /// </summary>
  internal class AcwModeAdapter : IAcwModeBreakdown
  {
    /// <summary>
    /// Устройство GPT-79904, с которым связан данный адаптер.
    /// </summary>
    private readonly GPT79904 _device;

    /// <summary>
    /// Экземпляр класса <see cref="AcwMode"/>, предоставляющий доступ ко всем внутренним функциям режима ACW.
    /// </summary>
    private readonly AcwMode _acwMode;

    /// <summary>
    /// Адаптер, обеспечивающий управление режимом работы ACW (установка и чтение текущего режима).
    /// </summary>
    public IModeConfigurable Mode { get; set; }

    /// <summary>
    /// Адаптер, обеспечивающий управление параметрами напряжения в режиме ACW.
    /// </summary>
    public IVoltageConfigurable Voltage { get; set; }

    /// <summary>
    /// Адаптер, обеспечивающий управление пределами тока (верхним и нижним) в режиме ACW.
    /// </summary>
    public ICurrentLimitsConfigurable CurrentLimits { get; set; }

    /// <summary>
    /// Адаптер, обеспечивающий управление параметрами времени теста и времени нарастания (Ramp Time) в режиме ACW.
    /// </summary>
    public ITimeConfigurable Time { get; set; }

    /// <summary>
    /// Адаптер, обеспечивающий управление параметром смещения (Offset) в режиме ACW.
    /// </summary>
    public IOffsetConfigurable Offset { get; set; }

    /// <summary>
    /// Адаптер, обеспечивающий управление параметром дугового тока (Arc Current) в режиме ACW.
    /// </summary>
    public IArcCurrentConfigurable ArcCurrent { get; set; }

    /// <summary>
    /// Адаптер, обеспечивающий управление параметром частоты (Frequency) в режиме ACW.
    /// </summary>
    public IFrequencyConfigurable FrequencyConfigurable { get; set; }

    /// <summary>
    /// Адаптер, предоставляющий функциональность измерений в режиме ACW:
    /// выполнение измерений, подача напряжения и остановка теста.
    /// </summary>
    public IMeasurable Measure { get; set; }

    /// <summary>
    /// Адаптер, обеспечивающий операции чтения и сброса конфигурации режима ACW.
    /// </summary>
    public IConfigurationProvider<AcwConfiguration> Config { get; set; }

    public BreakdownTypeMode ModeType => _acwMode.ModeType;

    private AcwConfiguration _config = new AcwConfiguration();

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="AcwModeAdapter"/>,
    /// создавая все подадаптеры, обеспечивающие доступ к функциональности режима ACW
    /// (тест прочности изоляции) устройства GPT-79904.
    /// </summary>
    /// <param name="device">
    /// Экземпляр устройства <see cref="GPT79904"/>, с которым будет работать адаптер.
    /// Не может быть <see langword="null"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Генерируется, если параметр <paramref name="device"/> равен <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// В конструкторе создаются экземпляры всех адаптеров, соответствующих частям режима ACW:
    /// <list type="bullet">
    ///   <item><term><see cref="Voltage"/></term> — управление напряжением.</item>
    ///   <item><term><see cref="Mode"/></term> — установка и получение режима работы.</item>
    ///   <item><term><see cref="CurrentLimits"/></term> — настройка верхнего и нижнего пределов тока.</item>
    ///   <item><term><see cref="Time"/></term> — управление временем теста и временем нарастания (Ramp Time).</item>
    ///   <item><term><see cref="Offset"/></term> — настройка параметра смещения (Offset).</item>
    ///   <item><term><see cref="ArcCurrent"/></term> — настройка параметра дугового тока.</item>
    ///   <item><term><see cref="FrequencyConfigurable"/></term> — управление частотой теста.</item>
    ///   <item><term><see cref="Measure"/></term> — выполнение измерений и подача напряжения.</item>
    ///   <item><term><see cref="Config"/></term> — операции чтения и сброса конфигурации.</item>
    /// </list>
    /// Все адаптеры используют общий экземпляр <see cref="AcwMode"/>, связанный с данным устройством.
    /// </remarks>
    public AcwModeAdapter(GPT79904 device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _acwMode = new AcwMode(device);

      Mode = new AcwAdapterMode(_acwMode, _device);
      Voltage = new VoltageAdapterMode(_acwMode, _device);
      CurrentLimits = new CurrentLimitsAdapterMode(_acwMode, _device);
      Time = new TimeAdapterMode(_acwMode, _device);
      Offset = new OffsetAdapterMode(_acwMode, _device);
      ArcCurrent = new ArcCurrentAdapterMode(_acwMode, _device);
      FrequencyConfigurable = new FrequencyAdapterMode(_acwMode, _device);
      Measure = new MeasureAdapterMode(_acwMode, _device);
      Config = new ConfigAdapterMode(_acwMode, device);
    }

    #region Адаптеры

    /// <summary>
    /// Представляет адаптер для работы с режимом ACW (тест прочности изоляции постоянным током)
    /// на устройстве GPT-79904.
    /// </summary>
    /// <remarks>
    /// Класс реализует интерфейс <see cref="IModeConfigurable"/> и инкапсулирует логику
    /// установки и получения режима ACW, обеспечивая единый способ взаимодействия
    /// с функциональностью класса <see cref="AcwMode"/>.
    /// </remarks>
    public class AcwAdapterMode : IModeConfigurable
    {
      /// <summary>
      /// Экземпляр класса <see cref="AcwMode"/>, обеспечивающий доступ к настройкам режима ACW.
      /// </summary>
      private readonly AcwMode _acwMode;

      /// <summary>
      /// Экземпляр устройства <see cref="GPT79904"/>, для которого применяется конфигурация режима.
      /// </summary>
      private readonly GPT79904 _device;

      /// <summary>
      /// Инициализирует новый экземпляр класса <see cref="AcwAdapterMode"/>.
      /// </summary>
      /// <param name="acwMode">Экземпляр класса <see cref="AcwMode"/>, управляющий режимом ACW.</param>
      /// <param name="device">Экземпляр устройства <see cref="GPT79904"/>, связанный с данным режимом.</param>
      public AcwAdapterMode(AcwMode acwMode, GPT79904 device)
      {
        _acwMode = acwMode;
        _device = device;
      }

      /// <summary>
      /// Асинхронно устанавливает режим ACW на устройстве GPT-79904.
      /// </summary>
      /// <param name="userMessageService">
      /// Сервис отображения сообщений пользователю.  
      /// Если передан, используется для уведомления о ходе и результате установки режима.
      /// </param>
      /// <returns>
      /// Кортеж <c>(Success, Message)</c>, где:
      /// <list type="bullet">
      ///   <item><term>Success</term> — признак успешной установки режима;</item>
      ///   <item><term>Message</term> — текстовое описание результата или ошибки.</item>
      /// </list>
      /// </returns>
      /// <exception cref="Exception">
      /// Генерируется, если установка режима завершилась с ошибкой.  
      /// Сообщение исключения содержит подробное описание причины сбоя.
      /// </exception>
      public async Task<(bool Success, string Message)> SetModeAsync(IUserInteractionService? userMessageService = null)
      {
        var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
        {
          var succes = await _acwMode.Mode.SetModeAsync();

          if (!succes.Success || DeviceDisplayConfig.GetConnectionInfoVisibility())
          {
            await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка режима ACW", succes.Success ? "ACW" : succes.Message, succes.Success, 1, userMessageService);
            return succes;
          }

          return succes;
        }, userMessageService, deviceTask: true);


        if (!result.Connect)
          throw AcwExceptionFactory.SetModeFailed(_device.Name, _device.NumberChassis, _device.Number);

        return result;
      }

      /// <summary>
      /// Асинхронно получает текущий режим ACW, установленный на устройстве GPT-79904.
      /// </summary>
      /// <returns>
      /// Кортеж <c>(Success, Message)</c>, где:
      /// <list type="bullet">
      ///   <item><term>Success</term> — признак успешного получения режима;</item>
      ///   <item><term>Message</term> — описание результата или сообщение об ошибке.</item>
      /// </list>
      /// </returns>
      /// <remarks>
      /// Метод выполняет прямой вызов <see cref="AcwMode.Mode.GetModeAsync"/>, возвращая результат операции без обработки.
      /// </remarks>
      public async Task<(bool Success, string Message)> GetModeAsync() => await _acwMode.Mode.GetModeAsync();
    }

    /// <summary>
    /// Представляет адаптер для управления напряжением в режиме ACW (тест прочности изоляции)
    /// на устройстве GPT-79904.
    /// </summary>
    /// <remarks>
    /// Класс реализует интерфейс <see cref="IVoltageConfigurable"/> и обеспечивает унифицированный
    /// способ установки и получения напряжения на устройстве через функциональность класса
    /// <see cref="AcwMode"/>.
    /// </remarks>
    public class VoltageAdapterMode : IVoltageConfigurable
    {
      /// <summary>
      /// Экземпляр класса <see cref="AcwMode"/>, предоставляющий доступ к управлению напряжением ACW.
      /// </summary>
      private readonly AcwMode _acwMode;

      /// <summary>
      /// Экземпляр устройства <see cref="GPT79904"/>, на котором выполняются операции с напряжением.
      /// </summary>
      private readonly GPT79904 _device;

      /// <summary>
      /// Инициализирует новый экземпляр класса <see cref="VoltageAdapterMode"/>.
      /// </summary>
      /// <param name="acwMode">Экземпляр класса <see cref="AcwMode"/>, реализующий управление напряжением.</param>
      /// <param name="device">Экземпляр устройства <see cref="GPT79904"/>, для которого выполняется настройка напряжения.</param>
      public VoltageAdapterMode(AcwMode acwMode, GPT79904 device)
      {
        _acwMode = acwMode;
        _device = device;
      }

      /// <summary>
      /// Асинхронно устанавливает значение напряжения ACW на устройстве GPT-79904.
      /// </summary>
      /// <param name="value">Значение напряжения в вольтах, которое необходимо установить.</param>
      /// <param name="userMessageService">
      /// Сервис отображения сообщений пользователю.  
      /// Если передан, используется для уведомления о ходе и результате установки напряжения.
      /// </param>
      /// <returns>
      /// Кортеж <c>(Success, Message)</c>, где:
      /// <list type="bullet">
      ///   <item><term>Success</term> — признак успешной установки напряжения;</item>
      ///   <item><term>Message</term> — сообщение с результатом или текст ошибки.</item>
      /// </list>
      /// </returns>
      /// <remarks>
      /// При успешной установке выводится сообщение через <see cref="DeviceMessageBuilder"/>.
      /// В случае ошибки метод возвращает кортеж с описанием ошибки без выбрасывания исключения.
      /// </remarks>
      public async Task<(bool Success, string Message)> SetVoltageAsync(double value, IUserInteractionService? userMessageService = null)
      {
        var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
        {
          var succes = await _acwMode.Voltage.SetVoltageAsync(value);

          if (!succes.Success || DeviceDisplayConfig.GetConnectionInfoVisibility())
          {
            await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка напряжения ACW", succes.Success ? $"{value} В" : succes.Message, succes.Success, 1, userMessageService);
          }

          return succes;
        }, userMessageService, deviceTask: true);

        if (!result.Connect)
        {
          throw AcwExceptionFactory.SetVoltageFailed(_device.Name, _device.NumberChassis, _device.Number);
        }

        return result;
      }

      /// <summary>
      /// Асинхронно получает текущее установленное значение напряжения ACW с устройства GPT-79904.
      /// </summary>
      /// <returns>
      /// Значение напряжения в вольтах, считанное с устройства.
      /// </returns>
      /// <remarks>
      /// Метод выполняет прямой вызов <see cref="AcwMode.Voltage.GetVoltageAsync"/> без дополнительной обработки.
      /// </remarks>
      public Task<double> GetVoltageAsync() => _acwMode.Voltage.GetVoltageAsync();
    }

    /// <summary>
    /// Представляет адаптер для управления пределами тока (верхним и нижним)
    /// в режиме ACW на устройстве GPT-79904.
    /// </summary>
    /// <remarks>
    /// Класс реализует интерфейс <see cref="ICurrentLimitsConfigurable"/> и обеспечивает
    /// унифицированный доступ к установке и считыванию токовых пределов
    /// через функциональность класса <see cref="AcwMode"/>.
    /// </remarks>
    public class CurrentLimitsAdapterMode : ICurrentLimitsConfigurable
    {
      /// <summary>
      /// Экземпляр класса <see cref="AcwMode"/>, предоставляющий доступ к управлению токовыми пределами.
      /// </summary>
      private readonly AcwMode _acwMode;

      /// <summary>
      /// Экземпляр устройства <see cref="GPT79904"/>, на котором выполняются операции с токовыми пределами.
      /// </summary>
      private readonly GPT79904 _device;

      /// <summary>
      /// Инициализирует новый экземпляр класса <see cref="CurrentLimitsAdapterMode"/>.
      /// </summary>
      /// <param name="acwMode">Экземпляр класса <see cref="AcwMode"/>, реализующий управление токовыми пределами.</param>
      /// <param name="device">Экземпляр устройства <see cref="GPT79904"/>, для которого выполняется настройка пределов тока.</param>
      public CurrentLimitsAdapterMode(AcwMode acwMode, GPT79904 device)
      {
        _acwMode = acwMode;
        _device = device;
      }

      /// <summary>
      /// Асинхронно получает текущее значение верхнего предела тока ACW.
      /// </summary>
      /// <returns>Значение верхнего предела тока в миллиамперах.</returns>
      public Task<double> GetHighCurrentLimitAsync() => _acwMode.CurrentLimits.GetHighCurrentLimitAsync();

      /// <summary>
      /// Асинхронно получает текущее значение нижнего предела тока ACW.
      /// </summary>
      /// <returns>Значение нижнего предела тока в миллиамперах.</returns>
      public Task<double> GetLowCurrentLimitAsync() => _acwMode.CurrentLimits.GetLowCurrentLimitAsync();

      /// <summary>
      /// Асинхронно устанавливает верхний предел тока ACW на устройстве GPT-79904.
      /// </summary>
      /// <param name="value">Значение верхнего предела тока в миллиамперах.</param>
      /// <param name="userMessageService">
      /// Сервис отображения сообщений пользователю.  
      /// Если передан, используется для уведомления о ходе и результате установки параметра.
      /// </param>
      /// <returns>
      /// Кортеж <c>(Success, Message)</c>, где:
      /// <list type="bullet">
      ///   <item><term>Success</term> — признак успешной установки верхнего предела тока;</item>
      ///   <item><term>Message</term> — описание результата или сообщение об ошибке.</item>
      /// </list>
      /// </returns>
      /// <exception cref="Exception">
      /// Генерируется, если установка верхнего предела тока завершилась с ошибкой.  
      /// Сообщение исключения содержит текст ошибки, полученный от устройства.
      /// </exception>
      public async Task<(bool, string)> SetHighCurrentLimitAsync(double value, IUserInteractionService? userMessageService = null)
      {
        var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
        {
          var succes = await _acwMode.CurrentLimits.SetHighCurrentLimitAsync(value);

          if (!succes.Success || DeviceDisplayConfig.GetConnectionInfoVisibility())
          {
            await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка верхнего предела тока ACW", succes.Success ? $"{value} мА" : succes.Message, succes.Success, 1, userMessageService);
          }

          return succes;
        }, userMessageService, deviceTask: true);

        if (!result.Connect)
          throw AcwExceptionFactory.SetHighLimitFailed(_device.Name, _device.NumberChassis, _device.Number);

        return result;
      }

      /// <summary>
      /// Асинхронно устанавливает нижний предел тока ACW на устройстве GPT-79904.
      /// </summary>
      /// <param name="value">Значение нижнего предела тока в миллиамперах.</param>
      /// <param name="userMessageService">
      /// Сервис отображения сообщений пользователю.  
      /// Если передан, используется для уведомления о ходе и результате установки параметра.
      /// </param>
      /// <returns>
      /// Кортеж <c>(Success, Message)</c>, где:
      /// <list type="bullet">
      ///   <item><term>Success</term> — признак успешной установки нижнего предела тока;</item>
      ///   <item><term>Message</term> — описание результата или сообщение об ошибке.</item>
      /// </list>
      /// </returns>
      /// <exception cref="Exception">
      /// Генерируется, если установка нижнего предела тока завершилась с ошибкой.  
      /// Сообщение исключения содержит текст ошибки, полученный от устройства.
      /// </exception>
      public async Task<(bool, string)> SetLowCurrentLimitAsync(double value, IUserInteractionService? userMessageService = null)
      {
        var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
        {
          var succes = await _acwMode.CurrentLimits.SetLowCurrentLimitAsync(value);

          if (!succes.Success || DeviceDisplayConfig.GetConnectionInfoVisibility())
          {
            await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка нижнего предела тока ACW", succes.Success ? $"{value} мА" : succes.Message, succes.Success, 1, userMessageService);
          }

          return succes;
        }, userMessageService, deviceTask: true);

        if (!result.Connect)
          throw AcwExceptionFactory.SetLowLimitFailed(_device.Name, _device.NumberChassis, _device.Number);

        return result;
      }
    }

    /// <summary>
    /// Представляет адаптер для управления временными параметрами режима ACW
    /// (тест прочности изоляции) на устройстве GPT-79904.
    /// </summary>
    /// <remarks>
    /// Класс реализует интерфейс <see cref="ITimeConfigurable"/> и обеспечивает
    /// унифицированный доступ к установке и считыванию параметров времени теста и времени нарастания (Ramp Time)
    /// через функциональность класса <see cref="AcwMode"/>.
    /// </remarks>
    public class TimeAdapterMode : ITimeConfigurable
    {
      /// <summary>
      /// Экземпляр класса <see cref="AcwMode"/>, предоставляющий доступ к управлению временными параметрами.
      /// </summary>
      private readonly AcwMode _acwMode;

      /// <summary>
      /// Экземпляр устройства <see cref="GPT79904"/>, на котором выполняются операции с параметрами времени.
      /// </summary>
      private readonly GPT79904 _device;

      /// <summary>
      /// Инициализирует новый экземпляр класса <see cref="TimeAdapterMode"/>.
      /// </summary>
      /// <param name="acwMode">Экземпляр класса <see cref="AcwMode"/>, реализующий управление параметрами времени.</param>
      /// <param name="device">Экземпляр устройства <see cref="GPT79904"/>, для которого выполняется настройка времени.</param>
      public TimeAdapterMode(AcwMode acwMode, GPT79904 device)
      {
        _acwMode = acwMode;
        _device = device;
      }

      /// <summary>
      /// Асинхронно устанавливает время выполнения теста ACW на устройстве GPT-79904.
      /// </summary>
      /// <param name="value">Время выполнения теста в секундах.</param>
      /// <param name="userMessageService">
      /// Сервис отображения сообщений пользователю.  
      /// Если передан, используется для уведомления о ходе и результате установки параметра.
      /// </param>
      /// <returns>
      /// Кортеж <c>(Success, Message)</c>, где:
      /// <list type="bullet">
      ///   <item><term>Success</term> — признак успешной установки времени теста;</item>
      ///   <item><term>Message</term> — описание результата или сообщение об ошибке.</item>
      /// </list>
      /// </returns>
      /// <exception cref="Exception">
      /// Генерируется, если установка времени теста завершилась с ошибкой.  
      /// Сообщение исключения содержит текст ошибки, полученный от устройства.
      /// </exception>
      public async Task<(bool, string)> SetTestTimeAsync(double value, IUserInteractionService? userMessageService = null)
      {
        var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
        {
          var succes = await _acwMode.Time.SetTestTimeAsync(value);

          if (!succes.Success || DeviceDisplayConfig.GetConnectionInfoVisibility())
          {
            await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка времени теста ACW", succes.Success ? $"{value} сек" : succes.Message, succes.Success, 1, userMessageService);
          }

          return succes;
        }, userMessageService, deviceTask: true);

        if (!result.Connect)
          throw AcwExceptionFactory.SetTestTimeFailed(_device.Name, _device.NumberChassis, _device.Number);

        return result;
      }

      /// <summary>
      /// Асинхронно получает текущее установленное время выполнения теста ACW с устройства GPT-79904.
      /// </summary>
      /// <returns>Значение времени теста в секундах.</returns>
      /// <remarks>
      /// Метод выполняет прямой вызов <see cref="AcwMode.Time.GetTestTimeAsync"/> без дополнительной обработки.
      /// </remarks>
      public Task<double> GetTestTimeAsync() => _acwMode.Time.GetTestTimeAsync();

      /// <summary>
      /// Асинхронно устанавливает время нарастания (Ramp Time) режима ACW на устройстве GPT-79904.
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
      /// <exception cref="Exception">
      /// Генерируется, если установка времени нарастания завершилась с ошибкой.  
      /// Сообщение исключения содержит текст ошибки, полученный от устройства.
      /// </exception>
      public async Task<(bool, string)> SetRampTimeAsync(double value, IUserInteractionService? userMessageService = null)
      {
        var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
        {
          var succes = await _acwMode.Time.SetRampTimeAsync(value);
          if (!succes.Success || DeviceDisplayConfig.GetConnectionInfoVisibility())
          {
            await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка Ramp Time ACW", succes.Success ? $"{value} сек" : succes.Message, succes.Success, 1, userMessageService);
          }

          return succes;
        }, userMessageService, deviceTask: true);

        if (!result.Connect)
          throw AcwExceptionFactory.SetRampTimeFailed(_device.Name, _device.NumberChassis, _device.Number);

        return result;
      }

      /// <summary>
      /// Асинхронно получает текущее установленное значение времени нарастания (Ramp Time) ACW.
      /// </summary>
      /// <returns>Значение времени нарастания в секундах.</returns>
      /// <remarks>
      /// Метод выполняет прямой вызов <see cref="AcwMode.Time.GetRampTimeAsync"/> без дополнительной обработки.
      /// </remarks>
      public Task<double> GetRampTimeAsync() => _acwMode.Time.GetRampTimeAsync();
    }

    /// <summary>
    /// Представляет адаптер для управления частотой режима ACW
    /// (тест прочности изоляции) на устройстве GPT-79904.
    /// </summary>
    /// <remarks>
    /// Класс реализует интерфейс <see cref="IFrequencyConfigurable"/> и обеспечивает
    /// унифицированный доступ к установке и считыванию частоты теста ACW
    /// через функциональность класса <see cref="AcwMode"/>.
    /// </remarks>
    public class FrequencyAdapterMode : IFrequencyConfigurable
    {
      /// <summary>
      /// Экземпляр класса <see cref="AcwMode"/>, предоставляющий доступ к управлению частотой ACW.
      /// </summary>
      private readonly AcwMode _acwMode;

      /// <summary>
      /// Экземпляр устройства <see cref="GPT79904"/>, на котором выполняются операции с параметрами частоты.
      /// </summary>
      private readonly GPT79904 _device;

      /// <summary>
      /// Инициализирует новый экземпляр класса <see cref="FrequencyAdapterMode"/>.
      /// </summary>
      /// <param name="acwMode">Экземпляр класса <see cref="AcwMode"/>, реализующий управление частотой режима ACW.</param>
      /// <param name="device">Экземпляр устройства <see cref="GPT79904"/>, для которого выполняется настройка частоты.</param>
      public FrequencyAdapterMode(AcwMode acwMode, GPT79904 device)
      {
        _acwMode = acwMode;
        _device = device;
      }

      /// <summary>
      /// Асинхронно устанавливает частоту теста ACW на устройстве GPT-79904.
      /// </summary>
      /// <param name="frequency">Частота в герцах (Гц), которую необходимо установить.</param>
      /// <param name="userMessageService">
      /// Сервис отображения сообщений пользователю.  
      /// Если передан, используется для уведомления о ходе и результате установки параметра.
      /// </param>
      /// <returns>
      /// Кортеж <c>(Success, Message)</c>, где:
      /// <list type="bullet">
      ///   <item><term>Success</term> — признак успешной установки частоты;</item>
      ///   <item><term>Message</term> — описание результата или сообщение об ошибке.</item>
      /// </list>
      /// </returns>
      /// <exception cref="Exception">
      /// Генерируется, если установка частоты завершилась с ошибкой.  
      /// Сообщение исключения содержит текст ошибки, полученный от устройства.
      /// </exception>
      public async Task<(bool, string)> SetFrequencyAsync(int frequency, IUserInteractionService? userMessageService = null)
      {
        var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
        {
          var succes = await _acwMode.FrequencyConfigurable.SetFrequencyAsync(frequency);

          if (!succes.Success || DeviceDisplayConfig.GetConnectionInfoVisibility())
          {
            await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка частоты ACW", succes.Success ? $"{frequency} Гц" : succes.Message, succes.Success, 1, userMessageService);
          }

          return succes;
        }, userMessageService, deviceTask: true);

        if (!result.Connect)
          throw AcwExceptionFactory.SetFrequencyFailed(_device.Name, _device.NumberChassis, _device.Number);

        return result;
      }

      /// <summary>
      /// Асинхронно получает текущее установленное значение частоты ACW с устройства GPT-79904.
      /// </summary>
      /// <returns>Значение частоты в герцах (Гц).</returns>
      /// <remarks>
      /// Метод выполняет прямой вызов <see cref="AcwMode.FrequencyConfigurable.GetFrequencyAsync"/> без дополнительной обработки.
      /// </remarks>
      public Task<int> GetFrequencyAsync() => _acwMode.FrequencyConfigurable.GetFrequencyAsync();
    }

    /// <summary>
    /// Представляет адаптер для управления параметром смещения (Offset)
    /// в режиме ACW (тест прочности изоляции) на устройстве GPT-79904.
    /// </summary>
    /// <remarks>
    /// Класс реализует интерфейс <see cref="IOffsetConfigurable"/> и обеспечивает
    /// унифицированный доступ к установке и считыванию параметра смещения
    /// через функциональность класса <see cref="AcwMode"/>.
    /// </remarks>
    public class OffsetAdapterMode : IOffsetConfigurable
    {
      /// <summary>
      /// Экземпляр класса <see cref="AcwMode"/>, предоставляющий доступ к управлению параметром смещения ACW.
      /// </summary>
      private readonly AcwMode _acwMode;

      /// <summary>
      /// Экземпляр устройства <see cref="GPT79904"/>, на котором выполняются операции с параметром смещения.
      /// </summary>
      private readonly GPT79904 _device;

      /// <summary>
      /// Инициализирует новый экземпляр класса <see cref="OffsetAdapterMode"/>.
      /// </summary>
      /// <param name="acwMode">Экземпляр класса <see cref="AcwMode"/>, реализующий управление смещением ACW.</param>
      /// <param name="device">Экземпляр устройства <see cref="GPT79904"/>, для которого выполняется настройка смещения.</param>
      public OffsetAdapterMode(AcwMode acwMode, GPT79904 device)
      {
        _acwMode = acwMode;
        _device = device;
      }

      /// <summary>
      /// Асинхронно устанавливает значение смещения (Offset) в режиме ACW на устройстве GPT-79904.
      /// </summary>
      /// <param name="value">Значение смещения в миллиамперах (мА).</param>
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
      /// <exception cref="Exception">
      /// Генерируется, если установка смещения завершилась с ошибкой.  
      /// Сообщение исключения содержит текст ошибки, полученный от устройства.
      /// </exception>
      public async Task<(bool, string)> SetOffsetAsync(double value, IUserInteractionService? userMessageService = null)
      {
        var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
        {
          var succes = await _acwMode.Offset.SetOffsetAsync(value);

          if (!succes.Success || DeviceDisplayConfig.GetConnectionInfoVisibility())
          {
            await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка смещения ACW", succes.Success ? $"{value} мА" : succes.Message, succes.Success, 1, userMessageService);
          }

          return succes;
        }, userMessageService, deviceTask: true);

        if (!result.Connect)
          throw new Exception($"Ошибка при установке смещения ACW: {result.Answer}");

        return result;
      }

      /// <summary>
      /// Асинхронно получает текущее установленное значение смещения (Offset) ACW.
      /// </summary>
      /// <returns>Значение смещения в миллиамперах (мА).</returns>
      /// <remarks>
      /// Метод выполняет прямой вызов <see cref="AcwMode.Offset.GetOffsetAsync"/> без дополнительной обработки.
      /// </remarks>
      public Task<double> GetOffsetAsync() => _acwMode.Offset.GetOffsetAsync();
    }

    /// <summary>
    /// Представляет адаптер для управления параметром дугового тока (Arc Current)
    /// в режиме ACW (тест прочности изоляции) на устройстве GPT-79904.
    /// </summary>
    /// <remarks>
    /// Класс реализует интерфейс <see cref="IArcCurrentConfigurable"/> и обеспечивает
    /// унифицированный доступ к установке и считыванию параметра дугового тока
    /// через функциональность класса <see cref="AcwMode"/>.
    /// </remarks>
    public class ArcCurrentAdapterMode : IArcCurrentConfigurable
    {
      /// <summary>
      /// Экземпляр класса <see cref="AcwMode"/>, предоставляющий доступ к управлению параметром дугового тока ACW.
      /// </summary>
      private readonly AcwMode _acwMode;

      /// <summary>
      /// Экземпляр устройства <see cref="GPT79904"/>, на котором выполняются операции с параметром дугового тока.
      /// </summary>
      private readonly GPT79904 _device;

      /// <summary>
      /// Инициализирует новый экземпляр класса <see cref="ArcCurrentAdapterMode"/>.
      /// </summary>
      /// <param name="acwMode">Экземпляр класса <see cref="AcwMode"/>, реализующий управление дуговым током.</param>
      /// <param name="device">Экземпляр устройства <see cref="GPT79904"/>, для которого выполняется настройка дугового тока.</param>
      public ArcCurrentAdapterMode(AcwMode acwMode, GPT79904 device)
      {
        _acwMode = acwMode;
        _device = device;
      }

      /// <summary>
      /// Асинхронно устанавливает значение дугового тока (Arc Current) в режиме ACW на устройстве GPT-79904.
      /// </summary>
      /// <param name="value">Значение дугового тока в миллиамперах (мА).</param>
      /// <param name="userMessageService">
      /// Сервис отображения сообщений пользователю.  
      /// Если передан, используется для уведомления о ходе и результате установки параметра.
      /// </param>
      /// <returns>
      /// Кортеж <c>(Success, Message)</c>, где:
      /// <list type="bullet">
      ///   <item><term>Success</term> — признак успешной установки дугового тока;</item>
      ///   <item><term>Message</term> — описание результата или сообщение об ошибке.</item>
      /// </list>
      /// </returns>
      /// <exception cref="Exception">
      /// Генерируется, если установка дугового тока завершилась с ошибкой.  
      /// Сообщение исключения содержит текст ошибки, полученный от устройства.
      /// </exception>
      public async Task<(bool, string)> SetArcCurrentAsync(double value, IUserInteractionService? userMessageService = null)
      {
        var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
        {
          var succes = await _acwMode.ArcCurrent.SetArcCurrentAsync(value);

          if (!succes.Success || DeviceDisplayConfig.GetConnectionInfoVisibility())
          {
            await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка дугового тока ACW", succes.Success ? $"{value} мА" : succes.Message, succes.Success, 1, userMessageService);
          }

          return succes;
        }, userMessageService, deviceTask: true);

        if (!result.Connect)
          throw new Exception($"Ошибка при установке дугового тока ACW: {result.Answer}");

        return result;
      }

      /// <summary>
      /// Асинхронно получает текущее установленное значение дугового тока (Arc Current) ACW.
      /// </summary>
      /// <returns>Значение дугового тока в миллиамперах (мА).</returns>
      /// <remarks>
      /// Метод выполняет прямой вызов <see cref="AcwMode.ArcCurrent.GetArcCurrentAsync"/> без дополнительной обработки.
      /// </remarks>
      public Task<double> GetArcCurrentAsync() => _acwMode.ArcCurrent.GetArcCurrentAsync();
    }

    /// <summary>
    /// Представляет адаптер для выполнения измерений в режиме ACW
    /// (тест прочности изоляции) на устройстве GPT-79904.
    /// </summary>
    /// <remarks>
    /// Класс реализует интерфейс <see cref="IMeasurable"/> и обеспечивает унифицированный доступ
    /// к операциям измерения, остановке измерения и подаче испытательного напряжения
    /// через функциональность класса <see cref="AcwMode"/>.
    /// </remarks>
    public class MeasureAdapterMode : IMeasurable
    {
      /// <summary>
      /// Экземпляр класса <see cref="AcwMode"/>, предоставляющий доступ к функциям измерения ACW.
      /// </summary>
      private readonly AcwMode _acwMode;

      /// <summary>
      /// Экземпляр устройства <see cref="GPT79904"/>, на котором выполняются измерения.
      /// </summary>
      private readonly GPT79904 _device;

      /// <summary>
      /// Инициализирует новый экземпляр класса <see cref="MeasureAdapterMode"/>.
      /// </summary>
      /// <param name="acwMode">Экземпляр класса <see cref="AcwMode"/>, реализующий логику измерений ACW.</param>
      /// <param name="device">Экземпляр устройства <see cref="GPT79904"/>, с которым выполняется измерение.</param>
      public MeasureAdapterMode(AcwMode acwMode, GPT79904 device)
      {
        _acwMode = acwMode;
        _device = device;
      }

      /// <summary>
      /// Асинхронно выполняет измерение тока в режиме ACW.
      /// </summary>
      /// <param name="param">Дополнительный параметр измерения (используется для расширенных режимов).</param>
      /// <param name="rangeFrom">Нижняя граница диапазона измерения. Если не используется — значение <c>-1</c>.</param>
      /// <param name="rangeTo">Верхняя граница диапазона измерения. Если не используется — значение <c>-1</c>.</param>
      /// <param name="userMessageService">
      /// Сервис отображения сообщений пользователю.  
      /// Если передан, используется для уведомления о ходе и результате измерения.
      /// </param>
      /// <returns>Измеренное значение тока в миллиамперах (мА).</returns>
      /// <exception cref="Exception">
      /// Генерируется при ошибке выполнения измерения.  
      /// Сообщение исключения содержит текст ошибки, полученный от устройства.
      /// </exception>
      public async Task<(double value, string unit)> MeasureAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, bool waitFullTime = false, IUserInteractionService? userMessageService = null)
      {
        if (rangeTo == -1)
        {
          rangeTo = double.MaxValue;
        }

        var random = Simulated.GetSimulatedValue(rangeFrom, rangeTo, ElectricalTestFunction.DielectricWithstandAC);
        if (random != -1)
        {
          return (random, "мА");
        }

        try
        {
          var (result, unit) = await _acwMode.Measure.MeasureAsync(param, rangeFrom, rangeTo);

          await DeviceMessageBuilder.ShowConnectionMessageAsync(
            _device,
            "Измерение тока ACW",
            $"{result} мА",
            result >= 0,
            2,
            userMessageService);

          return (result, unit);
        }
        catch (Exception ex)
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(
            _device,
            "Ошибка измерения тока ACW",
            ex.Message,
            false,
            2,
            userMessageService);

          throw new Exception($"Ошибка при измерении тока ACW: {ex.Message}");
        }
      }

      /// <summary>
      /// Асинхронно останавливает текущее измерение в режиме ACW.
      /// </summary>
      /// <returns>Задача, представляющая завершение операции остановки измерения.</returns>
      public async Task StopMeasure()
      {
        await _acwMode.Measure.StopMeasure();
      }

      /// <summary>
      /// Асинхронно подаёт испытательное напряжение на объект измерения в режиме ACW.
      /// </summary>
      /// <param name="userMessageService">
      /// Сервис отображения сообщений пользователю.  
      /// Если передан, используется для уведомления о ходе и результате подачи напряжения.
      /// </param>
      /// <returns>Задача, представляющая завершение операции подачи напряжения.</returns>
      public async Task ApplyVoltageAsync(IUserInteractionService userMessageService = null)
      {
        await _acwMode.Measure.ApplyVoltageAsync(userMessageService);
      }
    }

    /// <summary>
    /// Представляет адаптер для работы с конфигурацией режима ACW
    /// (тест прочности изоляции) на устройстве GPT-79904.
    /// </summary>
    /// <remarks>
    /// Класс реализует интерфейс <see cref="IConfigurationProvider{T}"/> и обеспечивает
    /// унифицированный доступ к операциям чтения и сброса конфигурации режима ACW
    /// через функциональность класса <see cref="AcwMode"/>.
    /// </remarks>
    public class ConfigAdapterMode : IConfigurationProvider<AcwConfiguration>
    {
      /// <summary>
      /// Экземпляр класса <see cref="AcwMode"/>, предоставляющий доступ к конфигурации режима ACW.
      /// </summary>
      private readonly AcwMode _acwMode;

      /// <summary>
      /// Экземпляр устройства <see cref="GPT79904"/>, с которого выполняется чтение конфигурации.
      /// </summary>
      private readonly GPT79904 _device;

      /// <summary>
      /// Инициализирует новый экземпляр класса <see cref="ConfigAdapterMode"/>.
      /// </summary>
      /// <param name="acwMode">Экземпляр класса <see cref="AcwMode"/>, реализующий операции с конфигурацией ACW.</param>
      /// <param name="device">Экземпляр устройства <see cref="GPT79904"/>, для которого выполняется чтение и сброс конфигурации.</param>
      public ConfigAdapterMode(AcwMode acwMode, GPT79904 device)
      {
        _acwMode = acwMode;
        _device = device;
      }

      public Task<string> GetConfigurationAsTextAsync() => _acwMode.Config.GetConfigurationAsTextAsync();

      /// <summary>
      /// Асинхронно считывает текущую конфигурацию режима ACW с устройства GPT-79904.
      /// </summary>
      /// <returns>Объект <see cref="AcwConfiguration"/>, содержащий параметры текущей конфигурации режима ACW.</returns>
      /// <remarks>
      /// После успешного чтения конфигурации отображается сообщение о завершении операции.
      /// </remarks>
      public async Task<AcwConfiguration> ReadConfigurationAsync()
      {
        var config = await _acwMode.Config.ReadConfigurationAsync();

        await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Чтение конфигурации ACW",
          "Конфигурация считана",
          true,
          1);

        return config;
      }

      /// <summary>
      /// Сбрасывает текущую конфигурацию режима ACW на значения по умолчанию.
      /// </summary>
      /// <remarks>
      /// Метод выполняет прямой вызов <see cref="AcwMode.Config.ResetConfiguration"/> без дополнительной обработки.
      /// </remarks>
      public void ResetConfiguration()
      {
        _acwMode.Config.ResetConfiguration();
      }
    }
    #endregion
  }
}
