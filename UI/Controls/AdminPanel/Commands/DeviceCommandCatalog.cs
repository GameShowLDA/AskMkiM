using Ask.Core.Shared.DTO.Devices.Base;

namespace UI.Controls.AdminPanel.Commands
{
  /// <summary>
  /// Предоставляет каталог низкоуровневых команд сервисной консоли.
  /// </summary>
  internal static class DeviceCommandCatalog
  {
    private static readonly IReadOnlyDictionary<string, DeviceHelpInfo> Devices =
      new Dictionary<string, DeviceHelpInfo>(StringComparer.OrdinalIgnoreCase)
      {
        ["MKR"] = Create(
          "MKR",
          Command(1, "Инициализация", "1.0.0.a.", "a: 0 — инициализация, 1 — включить, 2 — отключить"),
          Command(2, "Сброс коммутатора", "2.1.0.0."),
          Command(4, "Замкнуть или разомкнуть шину", "4.a.b.c.",
            "a: 1 — A, 2 — B, 3 — AB; b: 1–4 или 11–14; c: 1 — замкнуть, 2 — разомкнуть"),
          Command(5, "Включить или отключить измеритель", "5.a.0.0.",
            "a: 1 — включить, 2 — отключить"),
          Command(6, "Самоконтроль точки", "6.a.0.0.", "a: номер точки"),
          Command(7, "Получить ответ измерителя", "7.0.0.0."),
          Command(8, "Подключить или отключить точку", "8.a.b.c.",
            "a: номер точки; b: 1 — A, 2 — B, 3 — AB; c: 1 — подключить, 2 — отключить"),
          Command(81, "Переподключить точку", "81.a.b.0.",
            "a: номер точки; b: 1 — A, 2 — B"),
          Command(82, "Подключить точку с контролем", "82.a.b.c.",
            "a: номер точки; b: 1 — A, 2 — B; c: 1 — подключить, 2 — отключить"),
          Command(9, "Подключить все точки к шине", "9.a.b.0.",
            "a: 1 — A, 2 — B; b: 1 — подключить, 2 — отключить"),
          Command(10, "Самоконтроль внешней шины", "10.a.0.0.", "a: номер шины 1–4"),
          Command(11, "Подключить диапазон точек", "11.a.b.c.",
            "a/b: первая и последняя точки; c: 11/12 для A, 21/22 для B")),
        ["DBC"] = Create(
          "DeviceBusCommutation",
          Command(1, "Инициализация", "1.0.0.0."),
          Command(2, "Сброс всех реле", "2.1.0.0."),
          Command(4, "Цепь самоконтроля", "4.a.b.c.",
            "a: тип цепи 1–7; b: контакт; c: 1 — замкнуть, 2 — разомкнуть"),
          Command(41, "Главное реле цепи", "41.a.b.c.",
            "a: тип цепи и номер реле; b: контакт; c: 1 — замкнуть, 2 — разомкнуть"),
          Command(5, "Подключить оборудование", "5.a.b.c.",
            "a: тип оборудования; b: шина/контакт; c: 1 — подключить, 2 — отключить"),
          Command(51, "Получить замкнутую цепь", "51.0.0.0."),
          Command(6, "Резистор или конденсатор", "6.a.b.c.",
            "a: 1 — резистор, 2 — конденсатор; b: номер; c: 1 — замкнуть, 2 — разомкнуть")),
        ["MS"] = Create(
          "МШ",
          Command(1, "Инициализация", "1.0.0.0."),
          Command(21, "Включить источники 3/4", "2.1.1.0."),
          Command(22, "Выключить источники 3/4", "2.2.1.0."),
          Command(7, "Проверить питание", "7.0.0.0."))
      };

    /// <summary>
    /// Возвращает зарегистрированные короткие имена устройств.
    /// </summary>
    public static IEnumerable<string> DeviceAliases => Devices.Keys;

    /// <summary>
    /// Ищет справочник команд устройства.
    /// </summary>
    /// <param name="alias">Короткое имя устройства.</param>
    /// <param name="help">Найденный справочник.</param>
    /// <returns><see langword="true"/>, если устройство зарегистрировано.</returns>
    public static bool TryGetDevice(string alias, out DeviceHelpInfo help) =>
      Devices.TryGetValue(alias, out help!);

    /// <summary>
    /// Возвращает все команды каталога.
    /// </summary>
    public static IEnumerable<(string Alias, DeviceCommandInfo Command)> GetCommands() =>
      Devices.SelectMany(device => device.Value.Commands.Select(command => (device.Key, command)));

    private static DeviceHelpInfo Create(string name, params DeviceCommandInfo[] commands) =>
      new() { DeviceName = name, Commands = commands };

    private static DeviceCommandInfo Command(int id, string name, string syntax, string variables = "-") =>
      new()
      {
        Id = id,
        Name = name,
        Syntax = syntax,
        Variables = variables,
        Response = "Ответ зависит от прошивки устройства."
      };
  }
}
