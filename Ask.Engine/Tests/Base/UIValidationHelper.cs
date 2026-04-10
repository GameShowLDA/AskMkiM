using Ask.Core.Services.Errors.Device.Chassis;
using Ask.Core.Services.Errors.Device.ModuleRelayControl;
using Ask.Core.Services.Errors.Metrology;
using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.DataBase.Engine.Static.Devices;
using System.Globalization;

namespace Ask.Engine.Tests.Base
{
  /// <summary>
  /// Предоставляет методы для безопасной валидации пользовательского ввода из элемента управления ProtocolUI.
  /// </summary>
  public static class UIValidationHelper
  {
    private static IInputFieldAccessor? inputField;

    /// <summary>
    /// Выполняет безопасную валидацию пользовательского ввода для метрологических команд.
    /// При обнаружении ошибки отображает сообщение пользователю и выбрасывает исключение для остановки алгоритма.
    /// </summary>
    /// <param name="protocolUI">Экземпляр интерфейса <see cref="ProtocolUI"/>, содержащий ввод пользователя.</param>
    /// <param name="timeCheck">Флаг проверки времени измерения.</param>
    /// <param name="voltageCheck">Флаг проверки напряжения измерения.</param>
    /// <param name="timeRampCheck">Флаг проверки времени нарастания.</param>
    /// <param name="busCheck">Флаг проверки заданной шины.</param>
    /// <param name="pairBusCheck">Флаг проверки пары шин.</param>
    /// <returns>Объект <see cref="DataModel"/> с проверенными параметрами и точками подключения.</returns>
    /// <exception cref="SystemExceptionBase">
    /// Выбрасывается, если данные пользователя некорректны, отсутствует оборудование,
    /// либо нарушены метрологические ограничения (например, неверное время, напряжение, точка и т.д.).
    /// </exception>
    public static async Task<DataModel> EnsureValidMetrologyInputAsync(
        IInputFieldProvider protocolUI,
        IMessageOutputService messageOutputService,
        bool timeCheck = false,
        bool voltageCheck = false,
        bool timeRampCheck = false,
        bool busCheck = false,
        bool pairBusCheck = false)
    {
      try
      {
        var result = UIValidationHelper.TryValidateAndParseInputWithEquipment(
            protocolUI,
            timeCheck: timeCheck,
            voltageCheck: voltageCheck,
            timeRampCheck: timeRampCheck,
            busCheck: busCheck,
            pairBusCheck: pairBusCheck);

        return result;
      }
      catch (SystemExceptionBase ex)
      {
        await messageOutputService.ShowMessageAsync(new ShowMessageModel("Ошибка данных", message: ex.Description, type: ShowMessageModel.MessageType.Error), SkipStepModeCheck: true);
        throw;
      }
    }


    /// <summary>
    /// Выполняет полную валидацию пользовательского ввода из элемента <see cref="InputField"/>,
    /// включая проверку корректности точек подключения, оборудования, уникальности точек,
    /// а также дополнительных метрологических параметров (время, напряжение, шину, время нарастания).
    /// </summary>
    /// <typeparam name="T">Тип измерения, наследуемый от <c>BaseMeasurement</c>.</typeparam>
    /// <param name="protocolUI">Экземпляр пользовательского интерфейса <see cref="ProtocolUI"/>, содержащий поле ввода данных.</param>
    /// <param name="messageOnSuccess">Определяет, требуется ли отображать сообщение при успешной валидации.</param>
    /// <param name="timeCheck">Указывает, следует ли выполнять проверку параметра времени (для режимов ППУ).</param>
    /// <param name="voltageCheck">Указывает, следует ли выполнять проверку параметра напряжения (для режимов ППУ).</param>
    /// <param name="timeRampCheck">Указывает, следует ли выполнять проверку времени нарастания (для режимов ППУ).</param>
    /// <param name="busCheck">Указывает, следует ли проверять корректность заданной шины подключения.</param>
    /// <param name="pairBusCheck">Указывает, следует ли проверить корректность заданной пары шин.</param>
    /// <returns>
    /// Кортеж, содержащий:
    /// <list type="bullet">
    ///   <item><term>Success</term> — результат выполнения проверки (true, если все данные корректны);</item>
    ///   <item><term>Message</term> — текстовое сообщение об ошибке или <c>"OK"</c> при успешной валидации;</item>
    ///   <item><term>DataModel</term> — объект, содержащий результаты разбора точек, параметров и настроек режима.</item>
    /// </list>
    /// </returns>
    /// <exception cref="SystemExceptionBase">
    /// Может быть выброшено при нарушении условий валидации данных. В частности:
    /// <list type="bullet">
    ///   <item>
    ///     Элемент ввода данных не найден —
    ///     выбрасывается исключение <see cref="MetrologyValidationErrors.InputFieldNotFound()"/>.
    ///   </item>
    ///   <item>
    ///     Некорректный формат первой точки подключения —
    ///     выбрасывается исключение <see cref="MetrologyValidationErrors.InvalidFirstPoint()"/>.
    ///   </item>
    ///   <item>
    ///     Некорректный формат второй точки подключения —
    ///     выбрасывается исключение <see cref="MetrologyValidationErrors.InvalidSecondPoint()"/>.
    ///   </item>
    ///   <item>
    ///     Электрический параметр не удалось преобразовать в число —
    ///     выбрасывается исключение <see cref="MetrologyValidationErrors.InvalidElectricalValue()"/>.
    ///   </item>
    ///   <item>
    ///     Обнаружено отсутствие оборудования (шасси, модуля или точки) —
    ///     выбрасывается одно из исключений:
    ///     <see cref="ChassisValidationErrors.NotFound(int)"/>,
    ///     <see cref="ModuleRelayControlValidationErrors.ModuleNotFound(int, int)"/>,
    ///     <see cref="ModuleRelayControlValidationErrors.PointOutOfRange(int, int, int, int)"/>.
    ///   </item>
    ///   <item>
    ///     Точки подключения не являются уникальными —
    ///     выбрасывается исключение <see cref="MetrologyValidationErrors.PointsNotUnique(string, string)"/>.
    ///   </item>
    ///   <item>
    ///     Некорректное значение времени выполнения —
    ///     выбрасывается исключение <see cref="MetrologyValidationErrors.InvalidTime()"/>.
    ///   </item>
    ///   <item>
    ///     Некорректное значение напряжения —
    ///     выбрасывается исключение <see cref="MetrologyValidationErrors.InvalidVoltage()"/>.
    ///   </item>
    ///   <item>
    ///     Некорректная шина подключения —
    ///     выбрасывается исключение <see cref="MetrologyValidationErrors.InvalidBusSelection()"/>.
    ///   </item>
    /// </list>
    /// </exception>
    /// <remarks>
    /// Метод используется в метрологических режимах для предварительной проверки корректности входных данных перед выполнением измерений или тестов.
    /// При возникновении ошибок активируются события из <see cref="InputValidationEvents"/>:
    /// <list type="bullet">
    ///   <item><see cref="InputValidationEvents.TriggerInvalidFirstPoint"/> — при ошибке первой точки;</item>
    ///   <item><see cref="InputValidationEvents.TriggerInvalidSecondPoint"/> — при ошибке второй точки;</item>
    ///   <item><see cref="InputValidationEvents.TriggerInvalidParameter"/> — при ошибке параметра;</item>
    ///   <item><see cref="InputValidationEvents.TriggerInvalidFirstPoint"/> — при отсутствии элемента ввода.</item>
    /// </list>
    /// При успешной проверке возвращает объект <see cref="DataModel"/>, готовый для передачи в алгоритмы измерений.
    /// </remarks>
    private static DataModel TryValidateAndParseInputWithEquipment(
      IInputFieldProvider protocolUI,
      bool messageOnSuccess = true,
      bool timeCheck = false,
      bool voltageCheck = false,
      bool timeRampCheck = false,
      bool busCheck = false,
      bool pairBusCheck = false)
    {
      var (first, second, parameter) = TryValidateAndParseInput(protocolUI, messageOnSuccess);

      // Проверяем оборудование
      var equipmentValidation = CheckEquipmentExists(first, second);
      if (!equipmentValidation.Success)
      {
        throw new SystemExceptionBase(new ErrorItem
        {
          Code = ErrorCode.Metrology_Validation_EquipmentNotFound,
          Description = equipmentValidation.Message
        });
      }

      TryCheckPointsAreUnique(first, second);

      double time = timeCheck ? TryCheckTime() : -1;
      double voltage = voltageCheck ? CheckVoltage() : -1;
      double ramp = timeRampCheck ? TryCheckTimeRamp() : -1;
      BusPoint bus = busCheck ? TryCheckBus() : BusPoint.A;
      SwitchingBusNew pairBus = pairBusCheck ? TryCheckPairBus() : SwitchingBusNew.AB1;

      return new DataModel(first, second, parameter)
      {
        Time = time,
        Voltage = voltage,
        RampTime = ramp,
        ActiveBus = bus,
        ActivePairBus = pairBus
      };
    }

    /// <summary>
    /// Выполняет комплексную валидацию пользовательского ввода из элемента <see cref="InputField"/>
    /// в составе интерфейса <see cref="ProtocolUI"/>, а при успешной проверке — разбирает данные
    /// и возвращает модели точек и электрического параметра.
    /// </summary>
    /// <typeparam name="T">Тип измерения, наследуемый от <c>BaseMeasurement</c>.</typeparam>
    /// <param name="protocolUI">Экземпляр пользовательского интерфейса <see cref="ProtocolUI"/>, содержащий поле ввода.</param>
    /// <param name="messageOnSuccess">Определяет, требуется ли отображать сообщение при успешной валидации.</param>
    /// <returns>
    /// Кортеж, содержащий:
    /// <list type="bullet">
    ///   <item><term>First</term> — модель первой точки подключения (<see cref="PointModel"/>);</item>
    ///   <item><term>Second</term> — модель второй точки подключения (<see cref="PointModel"/>);</item>
    ///   <item><term>Parameter</term> — числовое значение электрического параметра.</item>
    /// </list>
    /// </returns>
    /// <exception cref="SystemExceptionBase">
    /// Может быть выброшено в следующих случаях:
    /// <list type="bullet">
    ///   <item>
    ///     Элемент ввода данных не найден —  
    ///     выбрасывается исключение <see cref="MetrologyValidationErrors.InputFieldNotFound()"/>.
    ///   </item>
    ///   <item>
    ///     Указана некорректная первая точка подключения —  
    ///     выбрасывается исключение <see cref="MetrologyValidationErrors.InvalidFirstPoint()"/>.
    ///   </item>
    ///   <item>
    ///     Указана некорректная вторая точка подключения —  
    ///     выбрасывается исключение <see cref="MetrologyValidationErrors.InvalidSecondPoint()"/>.
    ///   </item>
    ///   <item>
    ///     Электрический параметр не удалось преобразовать в число —  
    ///     выбрасывается исключение <see cref="MetrologyValidationErrors.InvalidElectricalValue()"/>.
    ///   </item>
    /// </list>
    /// </exception>
    /// <remarks>
    /// При обнаружении ошибок метод активирует соответствующие события из <see cref="InputValidationEvents"/>,
    /// чтобы пользовательский интерфейс мог отреагировать на неверные данные:
    /// <list type="bullet">
    ///   <item><see cref="InputValidationEvents.TriggerInvalidFirstPoint"/> — при ошибке в первой точке;</item>
    ///   <item><see cref="InputValidationEvents.TriggerInvalidSecondPoint"/> — при ошибке во второй точке;</item>
    ///   <item><see cref="InputValidationEvents.TriggerInvalidParameter"/> — при ошибке в электрическом параметре.</item>
    /// </list>
    /// </remarks>
    private static (PointModel First, PointModel Second, double Parameter) TryValidateAndParseInput(IInputFieldProvider protocolUI, bool messageOnSuccess = true)
    {
      inputField = protocolUI.GetInputFieldAccessor();
      if (inputField == null)
      {
        InputValidationEvents.TriggerInvalidFirstPoint = true;
        throw MetrologyValidationErrors.InputFieldNotFound();
      }

      var (point1, point2, parameterStr) = inputField.GetValues();

      var first = PointModel.ParsePointString(point1);
      var second = PointModel.ParsePointString(point2);

      if (first == null)
      {
        InputValidationEvents.TriggerInvalidFirstPoint = true;
        throw MetrologyValidationErrors.InvalidFirstPoint();
      }

      if (second == null)
      {
        InputValidationEvents.TriggerInvalidSecondPoint = true;
        throw MetrologyValidationErrors.InvalidSecondPoint();
      }

      if (!double.TryParse(parameterStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double parameter))
      {
        InputValidationEvents.TriggerInvalidParameter = true;
        throw MetrologyValidationErrors.InvalidElectricalValue();
      }

      return (first, second, parameter);
    }

    /// <summary>
    /// Проверяет наличие необходимого оборудования для двух заданных точек измерения.
    /// </summary>
    /// <param name="first">Первая точка подключения.</param>
    /// <param name="second">Вторая точка подключения.</param>
    /// <returns>
    /// Кортеж, содержащий:
    /// <list type="bullet">
    ///   <item><term>Success</term> — результат проверки (true, если обе точки корректны);</item>
    ///   <item><term>Message</term> — текстовое описание ошибки, если одна из точек не прошла проверку.</item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// Метод выполняет внутренние вызовы <see cref="TryIsValidPointExists(PointModel)"/> для каждой точки.
    /// При возникновении исключения <see cref="SystemExceptionBase"/> активируются соответствующие события:
    /// <list type="bullet">
    ///   <item><see cref="InputValidationEvents.TriggerInvalidFirstPoint"/> — если ошибка связана с первой точкой.</item>
    ///   <item><see cref="InputValidationEvents.TriggerInvalidSecondPoint"/> — если ошибка связана со второй точкой.</item>
    /// </list>
    /// </remarks>
    /// <exception cref="SystemExceptionBase">
    /// Может быть выброшено при проверке внутренних точек методом
    /// <see cref="TryIsValidPointExists(PointModel)"/>, если:
    /// <list type="bullet">
    ///   <item>Шасси с указанным номером не найдено (<see cref="MetrologyValidationErrors.ChassisNotFound(int)"/>);</item>
    ///   <item>Модуль коммутации не найден в указанном шасси (<see cref="MetrologyValidationErrors.ModuleNotFound(int, int)"/>);</item>
    ///   <item>Точка подключения выходит за пределы диапазона (<see cref="MetrologyValidationErrors.PointOutOfRange(int, int)"/>).</item>
    /// </list>
    /// </exception>
    private static (bool Success, string Message) CheckEquipmentExists(PointModel first, PointModel second)
    {
      try
      {
        TryIsValidPointExists(first);
      }
      catch (SystemExceptionBase ex)
      {
        InputValidationEvents.TriggerInvalidFirstPoint = true;
        return (false, $"Ошибка для первой точки: {ex.Description}");
      }

      try
      {
        TryIsValidPointExists(second);
      }
      catch (SystemExceptionBase ex)
      {
        InputValidationEvents.TriggerInvalidSecondPoint = true;
        return (false, $"Ошибка для второй точки: {ex.Description}");
      }

      return (true, null);
    }

    /// <summary>
    /// Проверяет корректность существования заданной точки измерения в системе оборудования.
    /// </summary>
    /// <param name="point">Модель точки подключения, содержащая номер шасси, модуля и точки.</param>
    /// <returns>
    /// <c>true</c>, если шасси, модуль и точка существуют и находятся в допустимых пределах;
    /// в противном случае генерируется соответствующее исключение.
    /// </returns>
    /// <exception cref="SystemExceptionBase">
    /// Выбрасывается в следующих случаях:
    /// <list type="bullet">
    ///   <item>
    ///     Если шасси с указанным номером не найдено —  
    ///     выбрасывается исключение <see cref="ChassisValidationErrors.NotFound(int)"/>.
    ///   </item>
    ///   <item>
    ///     Если модуль коммутации с заданным номером не найден в указанном шасси —  
    ///     выбрасывается исключение <see cref="ModuleRelayControlValidationErrors.ModuleNotFound(int, int)"/>.
    ///   </item>
    ///   <item>
    ///     Если точка подключения выходит за допустимый диапазон модуля —  
    ///     выбрасывается исключение <see cref="ModuleRelayControlValidationErrors.PointOutOfRange(int, int, int, int)"/>.
    ///   </item>
    /// </list>
    /// </exception>
    /// <remarks>
    /// Метод выполняет последовательную проверку существования шасси, наличия модуля
    /// и корректности диапазона точки в модуле коммутации.
    /// Используется в процессе метрологической валидации пользовательских данных.
    /// </remarks>
    private static bool TryIsValidPointExists(PointModel point)
    {
      var chassisExists = ChassisManagers.GetByNumberAsync(1).GetAwaiter().GetResult() != null;
      if (!chassisExists)
      {
        throw ChassisValidationErrors.NotFound(point.DeviceNumber);
      }

      var modules = RelaySwitchModules.GetDevicesByNumberChassisAsync(point.DeviceNumber).GetAwaiter().GetResult();
      var module = modules.FirstOrDefault(m => m.Number == point.ModuleNumber);

      if (module == null)
      {
        throw ModuleRelayControlValidationErrors.ModuleNotFound(point.DeviceNumber, point.ModuleNumber);
      }

      if (point.PointNumber < 1 || point.PointNumber > module.PointCount)
      {
        throw ModuleRelayControlValidationErrors.PointOutOfRange(point.DeviceNumber, point.ModuleNumber, point.PointNumber, module.PointCount);
      }

      return true;
    }

    /// <summary>
    /// Выполняет проверку уникальности двух точек подключения.
    /// </summary>
    /// <param name="first">Модель первой точки подключения.</param>
    /// <param name="second">Модель второй точки подключения.</param>
    /// <returns>
    /// <see langword="true"/>, если точки уникальны (не совпадают по адресу, модулю и шасси);
    /// в противном случае возбуждается исключение.
    /// </returns>
    /// <exception cref="SystemExceptionBase">
    /// Выбрасывается, если точки подключения не являются уникальными.  
    /// Исключение создаётся методом <see cref="MetrologyValidationErrors.PointsNotUnique(string, string)"/>  
    /// и содержит описание формата ошибки и обе конфликтующие точки.
    /// </exception>
    /// <remarks>
    /// Метод активирует событие <see cref="InputValidationEvents.TriggerInvalidSecondPoint"/> при обнаружении
    /// неуникальной второй точки, что может использоваться для визуального уведомления пользователя в UI.
    /// </remarks>
    private static bool TryCheckPointsAreUnique(PointModel first, PointModel second)
    {
      var result = first.ValidateUnique(second);
      if (!result)
      {
        InputValidationEvents.TriggerInvalidSecondPoint = true;
        throw MetrologyValidationErrors.PointsNotUnique(first.ToString(), second.ToString());
      }

      return true;
    }

    /// <summary>
    /// Выполняет проверку корректности значения времени выполнения, указанного пользователем в элементе <see cref="InputField"/>.
    /// </summary>
    /// <returns>
    /// Числовое значение времени выполнения (в секундах), если оно указано корректно.
    /// </returns>
    /// <exception cref="SystemExceptionBase">
    /// Возникает в двух случаях:
    /// <list type="bullet">
    ///   <item>
    ///     Если элемент ввода данных (<see cref="InputField"/>) отсутствует —
    ///     выбрасывается исключение <see cref="MetrologyValidationErrors.InputFieldNotFound()"/>.
    ///   </item>
    ///   <item>
    ///     Если введённое значение времени имеет некорректный формат —
    ///     выбрасывается исключение <see cref="MetrologyValidationErrors.InvalidTime()"/>.
    ///   </item>
    /// </list>
    /// </exception>
    /// <remarks>
    /// Метод активирует событие <see cref="InputValidationEvents.TriggerInvalidFirstPoint"/>,  
    /// если элемент <see cref="InputField"/> отсутствует в текущем контексте.
    /// </remarks>
    private static double TryCheckTime()
    {
      if (inputField == null)
      {
        InputValidationEvents.TriggerInvalidFirstPoint = true;
        throw MetrologyValidationErrors.InputFieldNotFound();
      }

      var timeString = inputField.GetTime();

      if (double.TryParse(timeString, out double result))
      {
        return result;
      }
      else
      {
        throw MetrologyValidationErrors.InvalidTime();
      }
    }

    /// <summary>
    /// Проверяет корректность введённого пользователем значения времени нарастания (ramp time)
    /// для режима измерения или испытания.
    /// </summary>
    /// <returns>
    /// Числовое значение времени нарастания в секундах, если ввод корректен.
    /// </returns>
    /// <exception cref="SystemExceptionBase">
    /// Возникает в двух случаях:
    /// <list type="bullet">
    ///   <item>
    ///     Если элемент ввода данных (<see cref="InputField"/>) отсутствует —
    ///     выбрасывается исключение <see cref="MetrologyValidationErrors.InputFieldNotFound()"/>.
    ///   </item>
    ///   <item>
    ///     Если введённое значение времени нарастания имеет некорректный формат —
    ///     выбрасывается исключение <see cref="MetrologyValidationErrors.InvalidTime()"/>.
    ///   </item>
    /// </list>
    /// </exception>
    /// <remarks>
    /// Метод активирует событие <see cref="InputValidationEvents.TriggerInvalidFirstPoint"/>,  
    /// если элемент <see cref="InputField"/> не найден в текущем контексте интерфейса.
    /// </remarks>
    private static double TryCheckTimeRamp()
    {
      if (inputField == null)
      {
        InputValidationEvents.TriggerInvalidFirstPoint = true;
        throw MetrologyValidationErrors.InputFieldNotFound();
      }

      var timeString = inputField.GetTimeRamp();

      if (double.TryParse(timeString, out double result))
      {
        return result;
      }
      else
      {
        throw MetrologyValidationErrors.InvalidTime();
      }
    }

    /// <summary>
    /// Проверяет корректность выбора шины подключения (Bus) в элементе <see cref="InputField"/>.
    /// </summary>
    /// <returns>
    /// Значение выбранной шины <see cref="BusPoint"/>, если выбор выполнен корректно.
    /// </returns>
    /// <exception cref="SystemExceptionBase">
    /// Возникает в двух случаях:
    /// <list type="bullet">
    ///   <item>
    ///     Если элемент ввода данных (<see cref="InputField"/>) отсутствует —
    ///     выбрасывается исключение <see cref="MetrologyValidationErrors.InputFieldNotFound()"/>.
    ///   </item>
    ///   <item>
    ///     Если шина подключения не выбрана или указана некорректно —
    ///     выбрасывается исключение <see cref="MetrologyValidationErrors.InvalidBusSelection()"/>.
    ///   </item>
    /// </list>
    /// </exception>
    /// <remarks>
    /// Метод активирует событие <see cref="InputValidationEvents.TriggerInvalidFirstPoint"/>,  
    /// если элемент <see cref="InputField"/> отсутствует в текущем контексте пользовательского интерфейса.
    /// </remarks>
    private static BusPoint TryCheckBus()
    {
      if (inputField == null)
      {
        InputValidationEvents.TriggerInvalidFirstPoint = true;
        throw MetrologyValidationErrors.InputFieldNotFound();
      }

      var bus = inputField.GetBus();

      if (bus != default)
      {
        return bus;
      }
      else
      {
        throw MetrologyValidationErrors.InvalidBusSelection();
      }
    }

    /// <summary>
    /// Проверяет корректность выбора пары шин (pair bus) в элементе <see cref="InputField"/>.
    /// </summary>
    /// <returns>
    /// Значение выбранной пары шин <see cref="SwitchingBusNew"/>, если выбор выполнен корректно.
    /// </returns>
    /// <exception cref="SystemExceptionBase">
    /// Возникает в двух случаях:
    /// <list type="bullet">
    ///   <item>
    ///     Если элемент ввода данных (<see cref="InputField"/>) отсутствует —
    ///     выбрасывается исключение <see cref="MetrologyValidationErrors.InputFieldNotFound()"/>.
    ///   </item>
    ///   <item>
    ///     Если пара шин подключения не выбрана или указана некорректно —
    ///     выбрасывается исключение <see cref="MetrologyValidationErrors.InvalidBusSelection()"/>.
    ///   </item>
    /// </list>
    /// </exception>
    /// <remarks>
    /// Метод активирует событие <see cref="InputValidationEvents.TriggerInvalidFirstPoint"/>,  
    /// если элемент <see cref="InputField"/> отсутствует в текущем контексте пользовательского интерфейса.
    /// </remarks>
    private static SwitchingBusNew TryCheckPairBus()
    {
      if (inputField == null)
      {
        InputValidationEvents.TriggerInvalidFirstPoint = true;
        throw MetrologyValidationErrors.InputFieldNotFound();
      }

      var bus = inputField.GetPairBus();

      if (bus != default)
      {
        return bus;
      }
      else
      {
        throw MetrologyValidationErrors.InvalidBusSelection();
      }
    }

    /// <summary>
    /// Проверяет корректность введённого пользователем значения напряжения
    /// в элементе управления <see cref="InputField"/>.
    /// </summary>
    /// <returns>
    /// Числовое значение напряжения в вольтах, если ввод выполнен корректно.
    /// </returns>
    /// <exception cref="SystemExceptionBase">
    /// Возникает в двух случаях:
    /// <list type="bullet">
    ///   <item>
    ///     Если элемент ввода данных (<see cref="InputField"/>) отсутствует —
    ///     выбрасывается исключение <see cref="MetrologyValidationErrors.InputFieldNotFound()"/>.
    ///   </item>
    ///   <item>
    ///     Если значение напряжения имеет некорректный формат —
    ///     выбрасывается исключение <see cref="MetrologyValidationErrors.InvalidVoltage()"/>.
    ///   </item>
    /// </list>
    /// </exception>
    /// <remarks>
    /// Метод активирует событие <see cref="InputValidationEvents.TriggerInvalidFirstPoint"/>,  
    /// если элемент <see cref="InputField"/> не найден в текущем контексте пользовательского интерфейса.
    /// </remarks>
    private static double CheckVoltage()
    {
      if (inputField == null)
      {
        InputValidationEvents.TriggerInvalidFirstPoint = true;
        throw MetrologyValidationErrors.InputFieldNotFound();
      }

      var voltageString = inputField.GetVoltage();

      if (double.TryParse(voltageString, out double result))
      {
        return result;
      }
      else
      {
        throw MetrologyValidationErrors.InvalidVoltage();
      }
    }

    /// <summary>
    /// Модель данных элемента.
    /// </summary>
    public class DataModel
    {
      /// <summary>
      /// Модель первой точки.
      /// </summary>
      public PointModel FirstPoint { get; set; }

      /// <summary>
      /// Модель второй точки.
      /// </summary>
      public PointModel SecondPoint { get; set; }

      /// <summary>
      /// Значение электрического параметра.
      /// </summary>
      public double Param { get; set; }

      /// <summary>
      /// Значение времени при выполнения теста (ППУ).
      /// </summary>
      public double Time { get; set; }

      /// <summary>
      /// Значение нарастания времени при выполнения теста (ППУ).
      /// </summary>
      public double RampTime { get; set; }

      /// <summary>
      /// Значение напряжения при выполнения теста (ППУ).
      /// </summary>
      public double Voltage { get; set; }

      /// <summary>
      /// Заданная шина.
      /// </summary>
      public BusPoint ActiveBus { get; set; }

      /// <summary>
      /// Заданная пара шин.
      /// </summary>
      public SwitchingBusNew ActivePairBus { get; set; }

      /// <summary>
      /// Инициализирует новый экземпляр класса <see cref="DataModel"/>.
      /// </summary>
      /// <param name="first">Первая точка.</param>
      /// <param name="second">Вторая точка.</param>
      /// <param name="param">Значение электрического параметра.</param>
      public DataModel(PointModel first, PointModel second, double param)
      {
        FirstPoint = first;
        SecondPoint = second;
        Param = param;
      }

      /// <summary>
      /// Инициализирует новый экземпляр класса <see cref="DataModel"/>.
      /// </summary>
      /// <param name="first">Первая точка.</param>
      /// <param name="second">Вторая точка.</param>
      /// <param name="param">Значение электрического параметра.</param>
      public DataModel() { }
    }
  }
}
