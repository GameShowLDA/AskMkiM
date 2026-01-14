using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.Breakdown;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Mode;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Device;
using NewCore.Function.GPT;
using NewCore.Function.Helpers;

namespace NewCore.FunctionAdapters.GPT
{
  /// <summary>
  /// Адаптер режима DCW (испытание прочности изоляции постоянным напряжением)
  /// для устройства GPT-79904 с поддержкой отображения сообщений пользователю.
  /// </summary>
  /// <remarks>
  /// Класс реализует интерфейс <see cref="IDcwModeBreakdown"/> и инкапсулирует
  /// все аспекты управления режимом DCW, включая настройку параметров, выполнение измерений
  /// и чтение конфигурации.  
  /// 
  /// В процессе работы все операции сопровождаются сообщениями, формируемыми через
  /// <see cref="DeviceMessageBuilder"/>, для удобства взаимодействия с пользователем.
  /// </remarks>
  internal class DcwModeAdapter : IDcwModeBreakdown
  {
    /// <summary>
    /// Экземпляр устройства <see cref="GPT79904"/>, для которого выполняется управление режимом DCW.
    /// </summary>
    private readonly GPT79904 _device;

    /// <summary>
    /// Экземпляр класса <see cref="DcwMode"/>, предоставляющий доступ к базовым методам управления режимом DCW.
    /// </summary>
    private readonly DcwMode _dcwMode;

    /// <summary>
    /// Управление общим режимом DCW (установка и получение режима).
    /// </summary>
    public IModeConfigurable Mode { get; set; }

    /// <summary>
    /// Управление напряжением DCW (установка и получение значения).
    /// </summary>
    public IVoltageConfigurable Voltage { get; set; }

    /// <summary>
    /// Управление пределами тока в режиме DCW.
    /// </summary>
    public ICurrentLimitsConfigurable CurrentLimits { get; set; }

    /// <summary>
    /// Управление временными параметрами (время теста и нарастания) в режиме DCW.
    /// </summary>
    public ITimeConfigurable Time { get; set; }

    /// <summary>
    /// Управление параметром смещения (Offset) для режима DCW.
    /// </summary>
    public IOffsetConfigurable Offset { get; set; }

    /// <summary>
    /// Управление параметром дугового тока (Arc Current) в режиме DCW.
    /// </summary>
    public IArcCurrentConfigurable ArcCurrent { get; set; }

    /// <summary>
    /// Выполнение измерений тока утечки или сопротивления в режиме DCW.
    /// </summary>
    public IMeasurable Measure { get; set; }

    /// <summary>
    /// Чтение и сброс конфигурации режима DCW.
    /// </summary>
    public IConfigurationProvider<DcwConfiguration> Config { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DcwModeAdapter"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства <see cref="GPT79904"/>, на котором выполняется управление режимом DCW.</param>
    /// <exception cref="ArgumentNullException">Выбрасывается, если <paramref name="device"/> равен <c>null</c>.</exception>
    public DcwModeAdapter(GPT79904 device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _dcwMode = new DcwMode(device);

      Mode = new DcwAdapterMode(_dcwMode, _device);
      Voltage = new VoltageAdapterMode(_dcwMode, _device);
      CurrentLimits = new CurrentLimitsAdapterMode(_dcwMode, _device);
      Time = new TimeAdapterMode(_dcwMode, _device);
      Offset = new OffsetAdapterMode(_dcwMode, _device);
      ArcCurrent = new ArcCurrentAdapterMode(_dcwMode, _device);
      Measure = new MeasureAdapterMode(_dcwMode, _device);
      Config = new ConfigAdapterMode(_dcwMode, device);
    }

    /// <summary>
    /// Адаптер для управления основным режимом DCW (испытание прочности изоляции постоянным напряжением)
    /// устройства GPT-79904.
    /// </summary>
    /// <remarks>
    /// Класс реализует интерфейс <see cref="IModeConfigurable"/> и обеспечивает
    /// установку и чтение режима DCW через функциональность класса <see cref="DcwMode"/>.  
    /// Все операции сопровождаются уведомлениями пользователю посредством <see cref="DeviceMessageBuilder"/>.
    /// </remarks>
    public class DcwAdapterMode : IModeConfigurable
    {
      /// <summary>
      /// Экземпляр класса <see cref="DcwMode"/>, предоставляющий доступ к управлению режимом DCW.
      /// </summary>
      private readonly DcwMode _dcwMode;

      /// <summary>
      /// Экземпляр устройства <see cref="GPT79904"/>, на котором выполняется установка и чтение режима.
      /// </summary>
      private readonly GPT79904 _device;

      /// <summary>
      /// Инициализирует новый экземпляр класса <see cref="DcwAdapterMode"/>.
      /// </summary>
      /// <param name="dcwMode">Экземпляр класса <see cref="DcwMode"/>, реализующий управление режимом DCW.</param>
      /// <param name="device">Экземпляр устройства <see cref="GPT79904"/>, для которого выполняется установка режима.</param>
      public DcwAdapterMode(DcwMode dcwMode, GPT79904 device)
      {
        _dcwMode = dcwMode;
        _device = device;
      }

      /// <summary>
      /// Асинхронно устанавливает режим DCW на устройстве GPT-79904.
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
      /// <exception cref="DcwException">
      /// Генерируется, если установка режима DCW завершилась с ошибкой.  
      /// Сообщение исключения содержит имя устройства, номер шасси и текст ошибки.
      /// </exception>
      public async Task<(bool, string)> SetModeAsync(IUserInteractionService? userMessageService = null)
      {
        var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
        {
          var succes = await _dcwMode.Mode.SetModeAsync();

          if (!succes.Success || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
          {
            await DeviceMessageBuilder.ShowConnectionMessageAsync(
            _device,
            "Установка режима DCW",
            succes.Success ? "DCW" : succes.Message,
            succes.Success,
            1,
            userMessageService);
          }

          return succes;
        }, userMessageService, deviceTask: true);

        if (!result.Connect)
          throw DcwExceptionFactory.SetModeFailed(_device.Name, _device.NumberChassis, _device.Number, result.Answer);

        return result;
      }

      /// <summary>
      /// Асинхронно получает текущий установленный режим DCW с устройства GPT-79904.
      /// </summary>
      /// <returns>
      /// Кортеж <c>(Success, Message)</c>, где:
      /// <list type="bullet">
      ///   <item><term>Success</term> — признак успешного получения режима;</item>
      ///   <item><term>Message</term> — описание текущего состояния режима.</item>
      /// </list>
      /// </returns>
      public Task<(bool Success, string Message)> GetModeAsync() => _dcwMode.Mode.GetModeAsync();
    }

    /// <summary>
    /// Адаптер для управления напряжением в режиме DCW (испытание прочности изоляции постоянным напряжением)
    /// устройства GPT-79904.
    /// </summary>
    /// <remarks>
    /// Класс реализует интерфейс <see cref="IVoltageConfigurable"/> и обеспечивает
    /// установку и получение значения напряжения в режиме DCW через функциональность класса <see cref="DcwMode"/>.  
    /// Все операции сопровождаются уведомлениями пользователю посредством <see cref="DeviceMessageBuilder"/>.
    /// </remarks>
    public class VoltageAdapterMode : IVoltageConfigurable
    {
      /// <summary>
      /// Экземпляр класса <see cref="DcwMode"/>, предоставляющий доступ к операциям управления напряжением DCW.
      /// </summary>
      private readonly DcwMode _dcwMode;

      /// <summary>
      /// Экземпляр устройства <see cref="GPT79904"/>, на котором выполняются операции с напряжением.
      /// </summary>
      private readonly GPT79904 _device;

      /// <summary>
      /// Инициализирует новый экземпляр класса <see cref="VoltageAdapterMode"/>.
      /// </summary>
      /// <param name="dcwMode">Экземпляр класса <see cref="DcwMode"/>, реализующий управление напряжением DCW.</param>
      /// <param name="device">Экземпляр устройства <see cref="GPT79904"/>, для которого выполняется установка напряжения.</param>
      public VoltageAdapterMode(DcwMode dcwMode, GPT79904 device)
      {
        _dcwMode = dcwMode;
        _device = device;
      }

      /// <summary>
      /// Асинхронно устанавливает значение напряжения DCW на устройстве GPT-79904.
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
      /// <exception cref="DcwException">
      /// Генерируется, если установка напряжения завершилась с ошибкой.  
      /// Сообщение исключения содержит имя устройства, номер шасси и текст ошибки.
      /// </exception>
      public async Task<(bool, string)> SetVoltageAsync(double value, IUserInteractionService? userMessageService = null)
      {
        var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
        {
          var succes = await _dcwMode.Voltage.SetVoltageAsync(value);

          if (!succes.Success || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
          {
            await DeviceMessageBuilder.ShowConnectionMessageAsync(
            _device,
            "Установка напряжения DCW",
            succes.Success ? $"{value} В" : succes.Message,
            succes.Success,
            1,
            userMessageService);
          }

          return succes;
        }, userMessageService, deviceTask: true);

        if (!result.Connect)
          throw DcwExceptionFactory.SetVoltageFailed(_device.Name, _device.NumberChassis, _device.Number, result.Answer);

        return result;
      }

      /// <summary>
      /// Асинхронно получает текущее установленное значение напряжения DCW с устройства GPT-79904.
      /// </summary>
      /// <returns>Значение напряжения в вольтах (В).</returns>
      public Task<double> GetVoltageAsync() => _dcwMode.Voltage.GetVoltageAsync();
    }

    /// <summary>
    /// Адаптер для управления пределами тока (верхним и нижним) 
    /// в режиме DCW (испытание прочности изоляции постоянным напряжением)
    /// устройства GPT-79904.
    /// </summary>
    /// <remarks>
    /// Класс реализует интерфейс <see cref="ICurrentLimitsConfigurable"/> и обеспечивает
    /// установку и получение верхнего и нижнего пределов тока через функциональность класса <see cref="DcwMode"/>.  
    /// Все операции сопровождаются уведомлениями пользователю посредством <see cref="DeviceMessageBuilder"/>.
    /// </remarks>
    public class CurrentLimitsAdapterMode : ICurrentLimitsConfigurable
    {
      /// <summary>
      /// Экземпляр класса <see cref="DcwMode"/>, предоставляющий доступ к операциям управления пределами тока.
      /// </summary>
      private readonly DcwMode _dcwMode;

      /// <summary>
      /// Экземпляр устройства <see cref="GPT79904"/>, на котором выполняются операции настройки пределов тока.
      /// </summary>
      private readonly GPT79904 _device;

      /// <summary>
      /// Инициализирует новый экземпляр класса <see cref="CurrentLimitsAdapterMode"/>.
      /// </summary>
      /// <param name="dcwMode">Экземпляр класса <see cref="DcwMode"/>, реализующий управление пределами тока.</param>
      /// <param name="device">Экземпляр устройства <see cref="GPT79904"/>, для которого выполняются операции.</param>
      public CurrentLimitsAdapterMode(DcwMode dcwMode, GPT79904 device)
      {
        _dcwMode = dcwMode;
        _device = device;
      }

      /// <summary>
      /// Асинхронно устанавливает верхний предел тока для режима DCW.
      /// </summary>
      /// <param name="value">Значение верхнего предела тока в миллиамперах (мА).</param>
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
      /// <exception cref="DcwException">
      /// Генерируется, если установка верхнего предела тока завершилась с ошибкой.  
      /// Сообщение исключения содержит имя устройства, номер шасси и текст ошибки.
      /// </exception>
      public async Task<(bool, string)> SetHighCurrentLimitAsync(double value, IUserInteractionService? userMessageService = null)
      {
        var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
        {
          var succes = await _dcwMode.CurrentLimits.SetHighCurrentLimitAsync(value);

          if (!succes.Success || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
          {
            await DeviceMessageBuilder.ShowConnectionMessageAsync(
            _device,
            "Установка верхнего предела тока DCW",
            succes.Success ? $"{value} мА" : succes.Message,
            succes.Success,
            1,
            userMessageService);
          }

          return succes;
        }, userMessageService, deviceTask: true);

        if (!result.Connect)
          throw DcwExceptionFactory.SetHighLimitFailed(_device.Name, _device.NumberChassis, _device.Number, result.Answer);

        return result;
      }

      /// <summary>
      /// Асинхронно получает текущее установленное значение верхнего предела тока для режима DCW.
      /// </summary>
      /// <returns>Значение верхнего предела тока в миллиамперах (мА).</returns>
      public Task<double> GetHighCurrentLimitAsync() => _dcwMode.CurrentLimits.GetHighCurrentLimitAsync();

      /// <summary>
      /// Асинхронно устанавливает нижний предел тока для режима DCW.
      /// </summary>
      /// <param name="value">Значение нижнего предела тока в миллиамперах (мА).</param>
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
      /// <exception cref="DcwException">
      /// Генерируется, если установка нижнего предела тока завершилась с ошибкой.  
      /// Сообщение исключения содержит имя устройства, номер шасси и текст ошибки.
      /// </exception>
      public async Task<(bool, string)> SetLowCurrentLimitAsync(double value, IUserInteractionService? userMessageService = null)
      {
        var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
        {
          var succes = await _dcwMode.CurrentLimits.SetLowCurrentLimitAsync(value);

          if (!succes.Success || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
          {
            await DeviceMessageBuilder.ShowConnectionMessageAsync(
            _device,
            "Установка нижнего предела тока DCW",
            succes.Success ? $"{value} мА" : succes.Message,
            succes.Success,
            1,
            userMessageService);
          }

          return succes;
        }, userMessageService, deviceTask: true);

        if (!result.Connect)
          throw DcwExceptionFactory.SetLowLimitFailed(_device.Name, _device.NumberChassis, _device.Number, result.Answer);

        return result;
      }

      /// <summary>
      /// Асинхронно получает текущее установленное значение нижнего предела тока для режима DCW.
      /// </summary>
      /// <returns>Значение нижнего предела тока в миллиамперах (мА).</returns>
      public Task<double> GetLowCurrentLimitAsync() => _dcwMode.CurrentLimits.GetLowCurrentLimitAsync();
    }

    /// <summary>
    /// Адаптер для управления временными параметрами режима DCW 
    /// (испытание прочности изоляции постоянным напряжением) устройства GPT-79904.
    /// </summary>
    /// <remarks>
    /// Класс реализует интерфейс <see cref="ITimeConfigurable"/> и обеспечивает
    /// установку и чтение параметров времени теста и времени нарастания (Ramp Time)
    /// через функциональность класса <see cref="DcwMode"/>.  
    /// Все операции сопровождаются уведомлениями пользователю посредством <see cref="DeviceMessageBuilder"/>.
    /// </remarks>
    public class TimeAdapterMode : ITimeConfigurable
    {
      /// <summary>
      /// Экземпляр класса <see cref="DcwMode"/>, предоставляющий доступ к операциям управления временем DCW.
      /// </summary>
      private readonly DcwMode _dcwMode;

      /// <summary>
      /// Экземпляр устройства <see cref="GPT79904"/>, на котором выполняются операции с временными параметрами.
      /// </summary>
      private readonly GPT79904 _device;

      /// <summary>
      /// Инициализирует новый экземпляр класса <see cref="TimeAdapterMode"/>.
      /// </summary>
      /// <param name="dcwMode">Экземпляр класса <see cref="DcwMode"/>, реализующий управление временем.</param>
      /// <param name="device">Экземпляр устройства <see cref="GPT79904"/>, для которого выполняется настройка времени.</param>
      public TimeAdapterMode(DcwMode dcwMode, GPT79904 device)
      {
        _dcwMode = dcwMode;
        _device = device;
      }

      /// <summary>
      /// Асинхронно устанавливает время теста в режиме DCW.
      /// </summary>
      /// <param name="value">Время теста в секундах.</param>
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
      /// <exception cref="DcwException">
      /// Генерируется, если установка времени теста завершилась с ошибкой.  
      /// Сообщение исключения содержит имя устройства, номер шасси и текст ошибки.
      /// </exception>
      public async Task<(bool, string)> SetTestTimeAsync(double value, IUserInteractionService? userMessageService = null)
      {
        var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
        {
          var succes = await _dcwMode.Time.SetTestTimeAsync(value);
          if (!succes.Success || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
          {
            await DeviceMessageBuilder.ShowConnectionMessageAsync(
            _device,
            "Установка времени теста DCW",
            succes.Success ? $"{value} сек" : succes.Message,
            succes.Success,
            1,
            userMessageService);
          }

          return succes;
        }, userMessageService, deviceTask: true);

        if (!result.Connect)
          throw DcwExceptionFactory.SetTestTimeFailed(_device.Name, _device.NumberChassis, _device.Number, result.Answer);

        return result;
      }

      /// <summary>
      /// Асинхронно получает текущее установленное значение времени теста в режиме DCW.
      /// </summary>
      /// <returns>Значение времени теста в секундах.</returns>
      public Task<double> GetTestTimeAsync() => _dcwMode.Time.GetTestTimeAsync();

      /// <summary>
      /// Асинхронно устанавливает время нарастания (Ramp Time) в режиме DCW.
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
      /// <exception cref="DcwException">
      /// Генерируется, если установка времени нарастания завершилась с ошибкой.  
      /// Сообщение исключения содержит имя устройства, номер шасси и текст ошибки.
      /// </exception>
      public async Task<(bool, string)> SetRampTimeAsync(double value, IUserInteractionService? userMessageService = null)
      {
        var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
        {
          var succes = await _dcwMode.Time.SetRampTimeAsync(value);
          if (!succes.Success || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
          {
            await DeviceMessageBuilder.ShowConnectionMessageAsync(
            _device,
            "Установка Ramp Time DCW",
            succes.Success ? $"{value} сек" : succes.Message,
            succes.Success,
            1,
            userMessageService);
          }

          return succes;
        }, userMessageService, deviceTask: true);

        if (!result.Connect)
          throw DcwExceptionFactory.SetRampTimeFailed(_device.Name, _device.NumberChassis, _device.Number, result.Answer);

        return result;
      }

      /// <summary>
      /// Асинхронно получает текущее установленное значение времени нарастания (Ramp Time) в режиме DCW.
      /// </summary>
      /// <returns>Значение времени нарастания в секундах.</returns>
      public Task<double> GetRampTimeAsync() => _dcwMode.Time.GetRampTimeAsync();
    }

    /// <summary>
    /// Адаптер для управления параметром смещения (Offset)
    /// в режиме DCW (испытание прочности изоляции постоянным напряжением)
    /// устройства GPT-79904.
    /// </summary>
    /// <remarks>
    /// Класс реализует интерфейс <see cref="IOffsetConfigurable"/> и обеспечивает
    /// установку и считывание значения смещения через функциональность класса <see cref="DcwMode"/>.  
    /// Все операции сопровождаются уведомлениями пользователю посредством <see cref="DeviceMessageBuilder"/>.
    /// </remarks>
    public class OffsetAdapterMode : IOffsetConfigurable
    {
      /// <summary>
      /// Экземпляр класса <see cref="DcwMode"/>, предоставляющий доступ к операциям управления смещением.
      /// </summary>
      private readonly DcwMode _dcwMode;

      /// <summary>
      /// Экземпляр устройства <see cref="GPT79904"/>, на котором выполняются операции настройки смещения.
      /// </summary>
      private readonly GPT79904 _device;

      /// <summary>
      /// Инициализирует новый экземпляр класса <see cref="OffsetAdapterMode"/>.
      /// </summary>
      /// <param name="dcwMode">Экземпляр класса <see cref="DcwMode"/>, реализующий управление параметром смещения.</param>
      /// <param name="device">Экземпляр устройства <see cref="GPT79904"/>, для которого выполняется настройка смещения.</param>
      public OffsetAdapterMode(DcwMode dcwMode, GPT79904 device)
      {
        _dcwMode = dcwMode;
        _device = device;
      }

      /// <summary>
      /// Асинхронно устанавливает значение смещения (Offset) в режиме DCW.
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
      /// <exception cref="DcwException">
      /// Генерируется, если установка смещения завершилась с ошибкой.  
      /// Сообщение исключения содержит имя устройства, номер шасси и текст ошибки.
      /// </exception>
      public async Task<(bool, string)> SetOffsetAsync(double value, IUserInteractionService? userMessageService = null)
      {
        var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
        {
          var succes = await _dcwMode.Offset.SetOffsetAsync(value, userMessageService);

          if (!succes.Success || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
          {
            await DeviceMessageBuilder.ShowConnectionMessageAsync(
            _device,
            "Установка смещения DCW",
            succes.Success ? $"{value} мА" : succes.Message,
            succes.Success,
            1,
            userMessageService);
          }

          return succes;
        }, userMessageService);

        if (!result.Connect)
          throw DcwExceptionFactory.SetOffsetFailed(_device.Name, _device.NumberChassis, _device.Number, result.Answer);

        return result;
      }

      /// <summary>
      /// Асинхронно получает текущее установленное значение смещения (Offset) в режиме DCW.
      /// </summary>
      /// <returns>Значение смещения в миллиамперах (мА).</returns>
      public Task<double> GetOffsetAsync() => _dcwMode.Offset.GetOffsetAsync();
    }

    /// <summary>
    /// Адаптер для управления параметром дугового тока (Arc Current)
    /// в режиме DCW (испытание прочности изоляции постоянным напряжением)
    /// устройства GPT-79904.
    /// </summary>
    /// <remarks>
    /// Класс реализует интерфейс <see cref="IArcCurrentConfigurable"/> и обеспечивает
    /// установку и считывание значения дугового тока через функциональность класса <see cref="DcwMode"/>.  
    /// Все операции сопровождаются уведомлениями пользователю посредством <see cref="DeviceMessageBuilder"/>.
    /// </remarks>
    public class ArcCurrentAdapterMode : IArcCurrentConfigurable
    {
      /// <summary>
      /// Экземпляр класса <see cref="DcwMode"/>, предоставляющий доступ к операциям управления дуговым током.
      /// </summary>
      private readonly DcwMode _dcwMode;

      /// <summary>
      /// Экземпляр устройства <see cref="GPT79904"/>, на котором выполняются операции настройки дугового тока.
      /// </summary>
      private readonly GPT79904 _device;

      /// <summary>
      /// Инициализирует новый экземпляр класса <see cref="ArcCurrentAdapterMode"/>.
      /// </summary>
      /// <param name="dcwMode">Экземпляр класса <see cref="DcwMode"/>, реализующий управление параметром дугового тока.</param>
      /// <param name="device">Экземпляр устройства <see cref="GPT79904"/>, для которого выполняется настройка дугового тока.</param>
      public ArcCurrentAdapterMode(DcwMode dcwMode, GPT79904 device)
      {
        _dcwMode = dcwMode;
        _device = device;
      }

      /// <summary>
      /// Асинхронно устанавливает значение дугового тока (Arc Current) в режиме DCW.
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
      /// <exception cref="DcwException">
      /// Генерируется, если установка дугового тока завершилась с ошибкой.  
      /// Сообщение исключения содержит имя устройства, номер шасси и текст ошибки.
      /// </exception>
      public async Task<(bool, string)> SetArcCurrentAsync(double value, IUserInteractionService? userMessageService = null)
      {
        var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () => 
        {
          var succes = await _dcwMode.ArcCurrent.SetArcCurrentAsync(value);

          if (!succes.Success || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
          {
            await DeviceMessageBuilder.ShowConnectionMessageAsync(
            _device,
            "Установка дугового тока DCW",
            succes.Success ? $"{value} мА" : succes.Message,
            succes.Success,
            1,
            userMessageService);
          }

          return succes;
        }, userMessageService, deviceTask: true);

        if (!result.Connect)
          throw DcwExceptionFactory.SetArcCurrentFailed(_device.Name, _device.NumberChassis, _device.Number, result.Answer);

        return result;
      }

      /// <summary>
      /// Асинхронно получает текущее установленное значение дугового тока (Arc Current) в режиме DCW.
      /// </summary>
      /// <returns>Значение дугового тока в миллиамперах (мА).</returns>
      public Task<double> GetArcCurrentAsync() => _dcwMode.ArcCurrent.GetArcCurrentAsync();
    }

    /// <summary>
    /// Адаптер для выполнения измерений в режиме DCW 
    /// (испытание прочности изоляции постоянным напряжением) устройства GPT-79904.
    /// </summary>
    /// <remarks>
    /// Класс реализует интерфейс <see cref="IMeasurable"/> и обеспечивает
    /// выполнение измерений тока утечки, а также управление процессом измерения
    /// (подача напряжения и остановка измерений) через функциональность класса <see cref="DcwMode"/>.  
    /// Все операции сопровождаются уведомлениями пользователю посредством <see cref="DeviceMessageBuilder"/>.
    /// </remarks>
    public class MeasureAdapterMode : IMeasurable
    {
      /// <summary>
      /// Экземпляр класса <see cref="DcwMode"/>, предоставляющий доступ к операциям измерения.
      /// </summary>
      private readonly DcwMode _dcwMode;

      /// <summary>
      /// Экземпляр устройства <see cref="GPT79904"/>, на котором выполняются операции измерения.
      /// </summary>
      private readonly GPT79904 _device;

      /// <summary>
      /// Инициализирует новый экземпляр класса <see cref="MeasureAdapterMode"/>.
      /// </summary>
      /// <param name="dcwMode">Экземпляр класса <see cref="DcwMode"/>, реализующий функции измерения.</param>
      /// <param name="device">Экземпляр устройства <see cref="GPT79904"/>, на котором выполняются измерения.</param>
      public MeasureAdapterMode(DcwMode dcwMode, GPT79904 device)
      {
        _dcwMode = dcwMode;
        _device = device;
      }

      /// <summary>
      /// Асинхронно выполняет измерение тока утечки в режиме DCW.
      /// </summary>
      /// <param name="param">Дополнительный параметр измерения (не используется в данном режиме).</param>
      /// <param name="rangeFrom">Минимально допустимое значение диапазона. По умолчанию — не ограничено.</param>
      /// <param name="rangeTo">Максимально допустимое значение диапазона. По умолчанию — не ограничено.</param>
      /// <param name="userMessageService">
      /// Сервис отображения сообщений пользователю.  
      /// Если передан, используется для уведомления о ходе и результате измерения.
      /// </param>
      /// <returns>
      /// Измеренное значение тока утечки в миллиамперах (мА).  
      /// В случае ошибки возвращается <c>-1</c>.
      /// </returns>
      public async Task<(double value, string unit)> MeasureAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, bool waitFullTime = false, IUserInteractionService? userMessageService = null)
      {
        try
        {
          var (result, unit) = await _dcwMode.Measure.MeasureAsync(param);

          await DeviceMessageBuilder.ShowConnectionMessageAsync(
            _device,
            "Измерение тока DCW",
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
            "Ошибка измерения тока DCW",
            ex.Message,
            false,
            2,
            userMessageService);

          return (-1, string.Empty);
        }
      }

      /// <summary>
      /// Асинхронно подаёт напряжение на устройство для выполнения измерения.
      /// </summary>
      /// <param name="userMessageService">Сервис отображения сообщений пользователю (опционально).</param>
      public async Task ApplyVoltageAsync(IUserInteractionService userMessageService = null)
      {
        await _dcwMode.Measure.ApplyVoltageAsync(userMessageService);
      }

      /// <summary>
      /// Асинхронно останавливает текущее измерение в режиме DCW.
      /// </summary>
      public async Task StopMeasure()
      {
        await _dcwMode.Measure.StopMeasure();
      }
    }

    /// <summary>
    /// Адаптер для работы с конфигурацией режима DCW 
    /// (испытание прочности изоляции постоянным напряжением)
    /// устройства GPT-79904.
    /// </summary>
    /// <remarks>
    /// Класс реализует интерфейс <see cref="IConfigurationProvider{T}"/> и обеспечивает
    /// чтение и сброс конфигурации режима DCW через функциональность класса <see cref="DcwMode"/>.  
    /// Все операции сопровождаются уведомлениями пользователю посредством <see cref="DeviceMessageBuilder"/>.
    /// </remarks>
    public class ConfigAdapterMode : IConfigurationProvider<DcwConfiguration>
    {
      /// <summary>
      /// Экземпляр класса <see cref="DcwMode"/>, предоставляющий доступ к операциям с конфигурацией режима DCW.
      /// </summary>
      private readonly DcwMode _dcwMode;

      /// <summary>
      /// Экземпляр устройства <see cref="GPT79904"/>, на котором выполняются операции чтения и сброса конфигурации.
      /// </summary>
      private readonly GPT79904 _device;

      /// <summary>
      /// Инициализирует новый экземпляр класса <see cref="ConfigAdapterMode"/>.
      /// </summary>
      /// <param name="dcwMode">Экземпляр класса <see cref="DcwMode"/>, обеспечивающий работу с конфигурацией.</param>
      /// <param name="device">Экземпляр устройства <see cref="GPT79904"/>, для которого выполняются операции конфигурации.</param>
      public ConfigAdapterMode(DcwMode dcwMode, GPT79904 device)
      {
        _dcwMode = dcwMode;
        _device = device;
      }

      /// <summary>
      /// Асинхронно выполняет чтение текущей конфигурации режима DCW с устройства GPT-79904.
      /// </summary>
      /// <returns>Объект <see cref="DcwConfiguration"/>, содержащий параметры конфигурации режима DCW.</returns>
      /// <remarks>
      /// После успешного чтения пользователю отображается уведомление с сообщением "Конфигурация считана".
      /// </remarks>
      public async Task<DcwConfiguration> ReadConfigurationAsync()
      {
        var config = await _dcwMode.Config.ReadConfigurationAsync();

        await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Чтение конфигурации DCW",
          "Конфигурация считана",
          true,
          1);

        return config;
      }

      /// <summary>
      /// Выполняет сброс текущей конфигурации режима DCW до значений по умолчанию.
      /// </summary>
      public void ResetConfiguration()
      {
        _dcwMode.Config.ResetConfiguration();
      }
    }
  }
}
