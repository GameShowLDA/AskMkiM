using Ask.Core.Services.App;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using DataBaseConfiguration.Services.Device;

namespace Ask.Engine.ControlCommandExecutor.Execution
{
  /// <summary>
  /// Статический сервис для анализа и хранения используемого оборудования на основе точек подключения.
  /// Выполняет валидацию наличия шасси, модулей МКР и их инициализацию.
  /// </summary>
  public static class EquipmentService
  {
    /// <summary>
    /// Список всех валидных модулей коммутации реле (МКР), найденных в конфигурации.
    /// Если при анализе возникли ошибки — значение будет <c>null</c>.
    /// </summary>
    public static List<IRelaySwitchModule>? ValidRelayModules { get; private set; }
    public static ISwitchingDevice? ValidSwitchingDevice { get; private set; }

    /// <summary>
    /// Сохранённый экземпляр пробойной установки (BreakdownTester), получаемый по запросу.
    /// </summary>
    private static IBreakdownTester? ValidBreakdownTester { get; set; }

    /// <summary>
    /// Сохранённый экземпляр быстрого измерителя (FastMeter), получаемый по запросу.
    /// </summary>
    private static IFastMeter? ValidFastMeter { get; set; }

    /// <summary>
    /// Сохранённый список точек подключения, переданных при вызове <see cref="AnalyzePoints"/>.
    /// Используется для повторной проверки и валидации.
    /// </summary>
    private static List<PointModel>? AnalyzedPoints { get; set; }

    static private Dictionary<string, string> PointsMap { get; set; } = new();


    /// <summary>
    /// Основной метод анализа точек подключения. Проверяет существование всех шасси и модулей, инициализирует их.
    /// </summary>
    /// <param name="points">Список всех точек подключения для анализа.</param>
    /// <param name="userMessageService">Сервис отображения сообщений пользователю.</param>
    /// <exception cref="Exception">Выбрасывается при наличии ошибок в конфигурации оборудования.</exception>
    public static async Task AnalyzePoints(List<PointModel> points, Dictionary<string, string> keyValuePairs, IUserInteractionService userMessageService)
    {
      PointsMap = keyValuePairs;
      AnalyzedPoints = null;
      ValidRelayModules = new List<IRelaySwitchModule>();

      var validChassisNumbers = await CheckChassisManagersAsync(points, userMessageService);
      if (validChassisNumbers == null)
      {
        ValidSwitchingDevice = null;
        ValidRelayModules = null;
        throw new Exception("Ошибка данных: не найдено одно или несколько устройств шасси.");
      }

      if (!await CheckSwitchingDeviceAsync(validChassisNumbers, userMessageService))
      {
        ValidSwitchingDevice = null;
        ValidRelayModules = null;
        throw new Exception("Ошибка данных: не найдено устройство коммутации.");
      }

      var modules = await CheckRelayModulesAsync(points, validChassisNumbers, userMessageService);
      if (modules == null)
      {
        ValidSwitchingDevice = null;
        ValidRelayModules = null;
        throw new Exception("Ошибка данных: не найдено одно или несколько модулей коммутации реле.");
      }

      ValidRelayModules = modules;

      await InitializeModulesAsync(modules, ValidSwitchingDevice, userMessageService);
      AnalyzedPoints = points;
    }

    /// <summary>
    /// Проверяет наличие менеджеров шасси, указанных в точках.
    /// </summary>
    /// <param name="points">Список точек подключения.</param>
    /// <param name="messageService">Сервис отображения сообщений пользователю.</param>
    /// <returns>Список валидных номеров шасси или <c>null</c>, если найдены ошибки.</returns>
    private static async Task<List<int>?> CheckChassisManagersAsync(List<PointModel> points, IUserInteractionService messageService)
    {
      var validNumbers = new List<int>();
      var allNumbers = GetUniqueDeviceNumbers(points);
      bool error = false;

      foreach (var chassisNumber in allNumbers)
      {
        if (new ChassisManagerServices().GetByNumber(chassisNumber) == null)
        {
          await messageService.ShowMessageAsync(new ShowMessageModel($"Менеджер шасси {chassisNumber}",
            message: "Устройство не найдено в конфигурации.", type: ShowMessageModel.MessageType.Error)
          { IndentLevel = 1 }, skipPause: true);
          error = true;
        }
        else
        {
          validNumbers.Add(chassisNumber);
        }
      }

      return error ? null : validNumbers;
    }

    /// <summary>
    /// Проверяет наличие и корректность модулей коммутации реле (МКР) в конфигурации.
    /// </summary>
    /// <param name="points">Список точек подключения.</param>
    /// <param name="validChassis">Список валидных номеров шасси.</param>
    /// <param name="messageService">Сервис отображения сообщений пользователю.</param>
    /// <returns>Список валидных модулей или <c>null</c> при ошибках.</returns>
    private static async Task<List<IRelaySwitchModule>?> CheckRelayModulesAsync(List<PointModel> points, List<int> validChassis, IUserInteractionService messageService)
    {
      bool error = false;
      var result = new List<IRelaySwitchModule>();

      var grouped = points
        .Where(p => validChassis.Contains(p.DeviceNumber))
        .Select(p => new { p.DeviceNumber, p.ModuleNumber })
        .Distinct()
        .ToList();

      foreach (var item in grouped)
      {
        var allModules = new RelaySwitchModuleServices().GetDevicesByNumberChassis(item.DeviceNumber);
        if (allModules == null || allModules.Count == 0)
        {
          await messageService.ShowMessageAsync(new ShowMessageModel($"Модуль коммутации реле[{item.DeviceNumber}.{item.ModuleNumber}]",
            message: "Устройство не найдено в конфигурации.", type: ShowMessageModel.MessageType.Error)
          { IndentLevel = 1 }, skipPause: true);
          error = true;
          continue;
        }

        var module = allModules.FirstOrDefault(m => m.Number == item.ModuleNumber);
        if (module == null)
        {
          await messageService.ShowMessageAsync(new ShowMessageModel($"Модуль коммутации реле[{item.DeviceNumber}.{item.ModuleNumber}]",
            message: "Модуль не найден в конфигурации.", type: ShowMessageModel.MessageType.Error)
          { IndentLevel = 1 }, skipPause: true);
          error = true;
          continue;
        }

        var pointsForModule = points.Where(p => p.DeviceNumber == item.DeviceNumber && p.ModuleNumber == item.ModuleNumber).ToList();
        var invalidPoint = pointsForModule.FirstOrDefault(p => p.PointNumber > module.PointCount);

        if (invalidPoint != null)
        {
          await messageService.ShowMessageAsync(new ShowMessageModel($"{module.Name}[{item.DeviceNumber}.{item.ModuleNumber}]",
            message: $"Указана несуществующая точка: {invalidPoint.PointNumber} (максимум {module.PointCount}).", type: ShowMessageModel.MessageType.Error)
          { IndentLevel = 1 }, skipPause: true);
          error = true;
          continue;
        }

        result.Add(module);
      }

      return error ? null : result;
    }

    /// <summary>
    /// Проверяет наличие устройства коммутации (<see cref="ISwitchingDevice"/>) для одного из валидных шасси.
    /// Сохраняет первый найденный экземпляр в <see cref="ValidSwitchingDevice"/>.
    /// </summary>
    /// <param name="validChassisNumbers">Список номеров шасси, прошедших проверку.</param>
    /// <param name="messageService">Сервис отображения сообщений пользователю.</param>
    /// <returns>
    /// <c>true</c>, если устройство найдено и сохранено; <c>false</c> — если ни одно устройство не найдено.
    /// </returns>
    private static async Task<bool> CheckSwitchingDeviceAsync(List<int> validChassisNumbers, IUserInteractionService messageService)
    {
      ValidSwitchingDevice = null;

      foreach (int number in validChassisNumbers)
      {
        var device = new SwitchingDeviceServices().GetDevicesByNumberChassis(number).FirstOrDefault();
        if (device != null)
        {
          ValidSwitchingDevice = device;
          return true;
        }
      }

      await messageService.ShowMessageAsync(new ShowMessageModel("Устройство коммутации",
        message: "Не найдено ни одно устройство коммутации для переданных шасси.", type: ShowMessageModel.MessageType.Error)
      { IndentLevel = 1 }, skipPause: true);

      return false;
    }

    /// <summary>
    /// Выполняет инициализацию всех переданных модулей МКР.
    /// </summary>
    /// <param name="modules">Список валидных модулей.</param>
    /// <param name="messageService">Сервис отображения сообщений пользователю.</param>
    /// <exception cref="Exception">Если инициализация любого модуля завершилась неудачей.</exception>
    private static async Task InitializeModulesAsync(List<IRelaySwitchModule> modules, ISwitchingDevice switchingDevice, IUserInteractionService messageService)
    {
      foreach (var module in modules)
      {
        await module.ConnectableManager.InitializeAsync(messageService);
        await module.ConnectableManager.ResetAsync(messageService);
      }

      await switchingDevice.ConnectableManager.InitializeAsync(messageService);
      await switchingDevice.ConnectableManager.ResetAsync(messageService);
    }

    /// <summary>
    /// Извлекает список уникальных номеров устройств (шасси) из точек подключения.
    /// </summary>
    /// <param name="points">Список точек подключения.</param>
    /// <returns>Список уникальных номеров шасси.</returns>
    private static List<int> GetUniqueDeviceNumbers(List<PointModel> points) =>
      points.Select(p => p.DeviceNumber).Distinct().ToList();

    /// <summary>
    /// Возвращает модуль коммутации реле (МКР), соответствующий указанной точке подключения.
    /// </summary>
    /// <param name="point">Точка подключения.</param>
    /// <returns>
    /// Объект <see cref="IRelaySwitchModule"/>, если найден.
    /// Возвращает <c>null</c>, если <see cref="ValidRelayModules"/> не проинициализирован
    /// или соответствующий модуль не найден.
    /// </returns>
    public static IRelaySwitchModule? GetModuleByPoint(PointModel point)
    {
      if (ValidRelayModules == null)
        return null;

      return ValidRelayModules.FirstOrDefault(m =>
        m.NumberChassis == point.DeviceNumber &&
        m.Number == point.ModuleNumber);
    }

    /// <summary>
    /// Возвращает уникальные модули коммутации реле (МКР),
    /// соответствующие переданным точкам подключения.
    /// </summary>
    /// <param name="points">Список точек подключения.</param>
    /// <returns>
    /// Список уникальных объектов <see cref="IRelaySwitchModule"/>,
    /// соответствующих переданным точкам.  
    /// Возвращает пустой список, если <see cref="ValidRelayModules"/> не проинициализирован
    /// или ни один модуль не найден.
    /// </returns>
    public static List<IRelaySwitchModule> GetUniqueModulesByPoints(IEnumerable<PointModel> points)
    {
      if (points == null || ValidRelayModules == null)
        return new List<IRelaySwitchModule>();

      return points
        .Select(GetModuleByPoint)
        .Where(m => m != null)
        .DistinctBy(m => (m.NumberChassis, m.Number))
        .ToList()!;
    }

    /// <summary>
    /// Возвращает сохранённое устройство коммутации, найденное при анализе точек.
    /// </summary>
    /// <returns>Объект <see cref="ISwitchingDevice"/>.</returns>
    /// <exception cref="Exception">Если <see cref="ValidSwitchingDevice"/> не инициализировано.</exception>
    public static ISwitchingDevice GetSwitchingDevice()
    {
      return ValidSwitchingDevice ?? throw new Exception("Устройство коммутации не инициализировано. Необходимо вызвать AnalyzePoints.");
    }

    /// <summary>
    /// Возвращает устройство пробойной установки (<see cref="IBreakdownTester"/>), связанное с одним из задействованных МКР.
    /// Если устройство ещё не найдено — выполняется попытка поиска по номеру шасси.
    /// </summary>
    /// <param name="messageService">Сервис отображения сообщений пользователю.</param>
    /// <returns>Экземпляр <see cref="IBreakdownTester"/>.</returns>
    /// <exception cref="Exception">Если устройство не найдено или <see cref="ValidRelayModules"/> ещё не проинициализировано.</exception>
    public static async Task<IBreakdownTester> GetBreakdownTesterOrThrow(IUserInteractionService messageService = null)
    {
      if (ValidBreakdownTester != null)
        return ValidBreakdownTester;

      if (ValidRelayModules == null || ValidRelayModules.Count == 0)
        throw new Exception("Модули МКР не инициализированы. Необходимо вызвать AnalyzePoints.");

      var chassisNumbers = ValidRelayModules
        .Select(m => m.NumberChassis)
        .Distinct()
        .ToList();

      foreach (var number in chassisNumbers)
      {
        var tester = ServiceLocator.GetRequired<BreakdownTesterServices>().GetDevicesByNumberChassis(number).FirstOrDefault();
        if (tester != null)
        {
          ValidBreakdownTester = tester;
          return tester;
        }
      }
      if (messageService != null)
      {
        messageService.ShowMessageAsync(new ShowMessageModel("Пробойная установка",
          message: "Не найдено устройство пробойной установки (BreakdownTester) для используемых шасси.",
          type: ShowMessageModel.MessageType.Error)
        { IndentLevel = 1 }, skipPause: true).Wait();
      }

      throw new Exception("Ошибка конфигурации: не найдено устройство пробойной установки.");
    }

    /// <summary>
    /// Возвращает устройство быстрого измерителя (<see cref="IFastMeter"/>), связанное с одним из задействованных МКР.
    /// Если устройство ещё не найдено — выполняется попытка поиска по номеру шасси.
    /// </summary>
    /// <param name="messageService">Сервис отображения сообщений пользователю.</param>
    /// <returns>Экземпляр <see cref="IFastMeter"/>.</returns>
    /// <exception cref="Exception">Если устройство не найдено или <see cref="ValidRelayModules"/> ещё не проинициализировано.</exception>
    public static IFastMeter GetFastMeterOrThrow(IUserInteractionService messageService)
    {
      if (ValidFastMeter != null)
        return ValidFastMeter;

      if (ValidRelayModules == null || ValidRelayModules.Count == 0)
        throw new Exception("Модули МКР не инициализированы. Необходимо вызвать AnalyzePoints.");

      var chassisNumbers = ValidRelayModules
        .Select(m => m.NumberChassis)
        .Distinct()
        .ToList();

      foreach (var number in chassisNumbers)
      {
        var meter = new FastMeterServices().GetDevicesByNumberChassis(number).FirstOrDefault();
        if (meter != null)
        {
          ValidFastMeter = meter;
          return meter;
        }
      }

      messageService.ShowMessageAsync(new ShowMessageModel("Быстрый измеритель",
        message: "Не найдено устройство быстрого измерителя (FastMeter) для используемых шасси.",
        type: ShowMessageModel.MessageType.Error)
      { IndentLevel = 1 }, skipPause: true).Wait();

      throw new Exception("Ошибка конфигурации: не найдено устройство быстрого измерителя.");
    }

    /// <summary>
    /// Проверяет, что все указанные точки ранее были переданы в <see cref="AnalyzePoints"/>.
    /// Использует только <see cref="AnalyzedPoints"/> без проверки модулей.
    /// Если хотя бы одна точка не найдена — выбрасывает исключение и выводит сообщение.
    /// </summary>
    /// <param name="points">Список точек для проверки.</param>
    /// <param name="messageService">Сервис отображения сообщений.</param>
    /// <returns>Задача, представляющая завершение проверки.</returns>
    /// <exception cref="Exception">Если хотя бы одна точка не найдена.</exception>
    public static async Task ValidatePointsExistInAnalyzedPointsAsync(List<PointModel> points, IUserInteractionService messageService)
    {
      if (AnalyzedPoints == null)
        throw new Exception("Список точек не инициализирован. Необходимо вызвать AnalyzePoints.");

      foreach (var point in points)
      {
        bool pointExists = AnalyzedPoints.Any(p =>
          p.DeviceNumber == point.DeviceNumber &&
          p.ModuleNumber == point.ModuleNumber &&
          p.PointNumber == point.PointNumber);

        if (!pointExists)
        {
          await messageService.ShowMessageAsync(new ShowMessageModel(
            $"Системная ошибка при трансляции: [{point.DeviceNumber}.{point.ModuleNumber}.{point.PointNumber}]",
            message: "Точка такая-то не существует.",
            type: ShowMessageModel.MessageType.Error)
          { IndentLevel = 1 }, skipPause: true);

          throw new Exception($"Системная ошибка при трансляции: [{point.DeviceNumber}.{point.ModuleNumber}.{point.PointNumber}]");
        }
      }
    }

    /// <summary>
    /// Возвращает ключ из <see cref="PointsMap"/>, соответствующий переданной точке.
    /// Поскольку словарь построен по строкам, выполняется поиск по совпадению параметров точки.
    /// </summary>
    /// <param name="point">Искомая точка.</param>
    /// <returns>Ключ, если найден; <c>null</c> если не найден.</returns>
    public static string? GetPointKey(PointModel point)
    {
      foreach (var kvp in PointsMap)
      {
        var value = kvp.Value;

        var parts = value.Split('.');
        if (parts.Length != 3)
          continue;

        if (int.TryParse(parts[0], out int device)
         && int.TryParse(parts[1], out int module)
         && int.TryParse(parts[2], out int number))
        {
          if (point.DeviceNumber == device &&
              point.ModuleNumber == module &&
              point.PointNumber == number)
          {
            return kvp.Key;
          }
        }
      }

      return null;
    }

    /// <summary>
    /// Возвращает все точки из AnalyzePoints, которые идут до указанной точки по порядку.
    /// Используется для определения ранее замкнутых точек.
    /// </summary>
    /// <param name="currentChain">Текущая цепь, относительно которой выбираются предыдущие.</param>
    /// <returns>Список точек, предшествующих указанной точке в AnalyzePoints.</returns>
    public static GroupModel GetDisconnectChainsBefore(SchemeModel schemeModel, ChainModel currentChain)
    {
      List<ChainModel> result = new();
      foreach (var chain in schemeModel.GetPointsDisconnected().ChainModels)
      {
        if (chain == currentChain)
        {
          return new GroupModel(result);
        }
        else
        {
          result.Add(chain);
        }
      }

      return new GroupModel(result);
    }

    /// <summary>
    /// Возвращает все точки из AnalyzePoints, которые идут до указанной точки по порядку.
    /// Используется для определения ранее замкнутых точек.
    /// </summary>
    /// <param name="currentPoint">Текущая точка, относительно которой выбираются предыдущие.</param>
    /// <returns>Список точек, предшествующих указанной точке в AnalyzePoints.</returns>
    public static List<PointModel> GetPointsBefore(List<PointModel> analyzedPoints, PointModel currentPoint)
    {
      if (analyzedPoints == null)
        throw new Exception("Список точек не инициализирован. Необходимо вызвать AnalyzePoints.");

      var index = analyzedPoints.FindIndex(p =>
        p.DeviceNumber == currentPoint.DeviceNumber &&
        p.ModuleNumber == currentPoint.ModuleNumber &&
        p.PointNumber == currentPoint.PointNumber);

      if (index < 0)
        throw new Exception($"Указанная точка [{currentPoint.DeviceNumber}.{currentPoint.ModuleNumber}.{currentPoint.PointNumber}] не найдена в AnalyzePoints.");

      return analyzedPoints.Take(index).ToList();
    }
  }
}
