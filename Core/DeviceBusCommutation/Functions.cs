using System.Globalization;
using System.Net;
using Core.Communication;
using static Core.DeviceBusCommutation.Enums;
using static Utilities.LoggerUtility;

namespace Core.DeviceBusCommutation
{
  /// <summary>
  /// Класс, содержащий методы для работы с УКШ.
  /// </summary>
  public static class Functions
  {
    #region Поля и свойства.

    /// <summary>
    /// Содержит статические внутренние словари для хранения различных значений и состояний.
    /// </summary>
    static internal readonly Dictionary<string, string> GetValueLE = new Dictionary<string, string>()
    {
      ["1"] = "80",
      ["2"] = "80",
      ["3"] = "80",
      ["4"] = "80",
      ["5"] = "80",
      ["6"] = "80",
      ["7"] = "97",
      ["8"] = "97",
      ["9"] = "97",
      ["10"] = "97",
      ["11"] = "97",
      ["12"] = "97",
      ["13"] = "97",
      ["14"] = "97",
      ["15"] = "17",
      ["16"] = "17",
      ["17"] = "17",
      ["18"] = "17",
      ["19"] = "17",
      ["20"] = "17",
      ["21"] = "17",
      ["22"] = "17",
      ["23"] = "8",
      ["24"] = "8",
      ["25"] = "104",
      ["26"] = "8",
      ["27"] = "8",
      ["28"] = "8",
      ["29"] = "104",
      ["30"] = "8",
      ["31"] = "8",
      ["32"] = "8",
      ["33"] = "65",
      ["34"] = "65",
      ["35"] = "49",
      ["36"] = "104",
      ["37"] = "65",
      ["38"] = "65",
      ["39"] = "104",
      ["40"] = "49",
      ["41"] = "72",
      ["42"] = "72",
      ["43"] = "24",
      ["44"] = "24",
      ["45"] = "72",
      ["46"] = "72",
      ["47"] = "24",
      ["48"] = "24",
      ["49"] = "72",
      ["50"] = "40",
      ["51"] = "49",
      ["52"] = "40",
      ["53"] = "72",
      ["54"] = "40",
      ["55"] = "49",
      ["56"] = "40",
      ["57"] = "72",
      ["58"] = "40",
      ["59"] = "49",
      ["60"] = "24",
      ["61"] = "72",
      ["62"] = "40",
      ["63"] = "49",
      ["64"] = "24",
      ["65"] = "49",
      ["66"] = "24",
      ["67"] = "40",
      ["68"] = "40",
      ["69"] = "49",
      ["70"] = "24",
      ["71"] = "64",
      ["72"] = "64",
      ["73"] = "64",
      ["74"] = "64",
      ["75"] = "64",
      ["76"] = "64",
      ["77"] = "64",
      ["78"] = "64",
      ["79"] = "96",
      ["80"] = "96",
      ["81"] = "96",
      ["82"] = "96",
      ["83"] = "96",
      ["84"] = "96",
      ["85"] = "96",
      ["86"] = "96",
      ["87"] = "48",
      ["88"] = "48",
      ["89"] = "48",
      ["90"] = "48",
      ["91"] = "48",
      ["92"] = "48",
      ["93"] = "65",
      ["94"] = "65",
      ["95"] = "48",
      ["96"] = "48",
      ["97"] = "104",
      ["98"] = "104",
      ["99"] = "104",
      ["100"] = "104",
      ["101"] = "112",
      ["102"] = "112",
      ["103"] = "65",
      ["104"] = "65",
      ["105"] = "81",
      ["106"] = "81",
      ["107"] = "1",
      ["108"] = "1",
      ["109"] = "1",
      ["110"] = "1",
      ["111"] = "81",
      ["112"] = "81",
      ["113"] = "113",
      ["114"] = "113",
      ["115"] = "33",
      ["116"] = "33",
      ["117"] = "1",
      ["118"] = "1",
      ["119"] = "113",
      ["120"] = "113",
      ["121"] = "1",
      ["122"] = "1",
      ["123"] = "81",
      ["124"] = "81",
      ["125"] = "81",
      ["126"] = "81",
      ["127"] = "113",
      ["128"] = "113",
      ["129"] = "113",
      ["130"] = "113",
      ["131"] = "80",
      ["132"] = "80",
      ["133"] = "16",
      ["134"] = "16",
      ["135"] = "16",
      ["136"] = "16",
      ["137"] = "16",
      ["138"] = "16",
      ["139"] = "16",
      ["140"] = "16",
      ["141"] = "112",
      ["142"] = "112",
      ["143"] = "112",
      ["144"] = "112",
      ["145"] = "112",
      ["146"] = "112",
      ["147"] = "0",
      ["148"] = "0",
      ["149"] = "0",
      ["150"] = "0",
      ["151"] = "0",
      ["152"] = "0",
      ["153"] = "0",
      ["154"] = "0",
      ["155"] = "32",
      ["156"] = "32",
      ["157"] = "32",
      ["158"] = "32",
      ["159"] = "32",
      ["160"] = "32",
      ["161"] = "32",
      ["162"] = "32",
    };

    /// <summary>
    /// Словарь для хранения состояний точек.
    /// </summary>
    static internal readonly Dictionary<string, bool> GetPointState = new Dictionary<string, bool>()
    {
      // ["point"] = "LE"
      ["1"] = false,
      ["2"] = false,
      ["3"] = false,
      ["4"] = false,
      ["5"] = false,
      ["6"] = false,
      ["7"] = false,
      ["8"] = false,
      ["9"] = false,
      ["10"] = false,
      ["11"] = false,
      ["12"] = false,
      ["13"] = false,
      ["14"] = false,
      ["15"] = false,
      ["16"] = false,
      ["17"] = false,
      ["18"] = false,
      ["19"] = false,
      ["20"] = false,
      ["21"] = false,
      ["22"] = false,
      ["23"] = false,
      ["24"] = false,
      ["25"] = false,
      ["26"] = false,
      ["27"] = false,
      ["28"] = false,
      ["29"] = false,
      ["30"] = false,
      ["31"] = false,
      ["32"] = false,
      ["33"] = false,
      ["34"] = false,
      ["35"] = false,
      ["36"] = false,
      ["37"] = false,
      ["38"] = false,
      ["39"] = false,
      ["40"] = false,
      ["41"] = false,
      ["42"] = false,
      ["43"] = false,
      ["44"] = false,
      ["45"] = false,
      ["46"] = false,
      ["47"] = false,
      ["48"] = false,
      ["49"] = false,
      ["50"] = false,
      ["51"] = false,
      ["52"] = false,
      ["53"] = false,
      ["54"] = false,
      ["55"] = false,
      ["56"] = false,
      ["57"] = false,
      ["58"] = false,
      ["59"] = false,
      ["60"] = false,
      ["61"] = false,
      ["62"] = false,
      ["63"] = false,
      ["64"] = false,
      ["65"] = false,
      ["66"] = false,
      ["67"] = false,
      ["68"] = false,
      ["69"] = false,
      ["70"] = false,
      ["71"] = false,
      ["72"] = false,
      ["73"] = false,
      ["74"] = false,
      ["75"] = false,
      ["76"] = false,
      ["77"] = false,
      ["78"] = false,
      ["79"] = false,
      ["80"] = false,
      ["81"] = false,
      ["82"] = false,
      ["83"] = false,
      ["84"] = false,
      ["85"] = false,
      ["86"] = false,
      ["87"] = false,
      ["88"] = false,
      ["89"] = false,
      ["90"] = false,
      ["91"] = false,
      ["92"] = false,
      ["93"] = false,
      ["94"] = false,
      ["95"] = false,
      ["96"] = false,
      ["97"] = false,
      ["98"] = false,
      ["99"] = false,
      ["100"] = false,
      ["101"] = false,
      ["102"] = false,
      ["103"] = false,
      ["104"] = false,
      ["105"] = false,
      ["106"] = false,
      ["107"] = false,
      ["108"] = false,
      ["109"] = false,
      ["110"] = false,
      ["111"] = false,
      ["112"] = false,
      ["113"] = false,
      ["114"] = false,
      ["115"] = false,
      ["116"] = false,
      ["117"] = false,
      ["118"] = false,
      ["119"] = false,
      ["120"] = false,
      ["121"] = false,
      ["122"] = false,
      ["123"] = false,
      ["124"] = false,
      ["125"] = false,
      ["126"] = false,
      ["127"] = false,
      ["128"] = false,
      ["129"] = false,
      ["130"] = false,
      ["131"] = false,
      ["132"] = false,
      ["133"] = false,
      ["134"] = false,
      ["135"] = false,
      ["136"] = false,
      ["137"] = false,
      ["138"] = false,
      ["139"] = false,
      ["140"] = false,
      ["141"] = false,
      ["142"] = false,
      ["143"] = false,
      ["144"] = false,
      ["145"] = false,
      ["146"] = false,
      ["147"] = false,
      ["148"] = false,
      ["149"] = false,
      ["150"] = false,
      ["151"] = false,
      ["152"] = false,
      ["153"] = false,
      ["154"] = false,
      ["155"] = false,
      ["156"] = false,
      ["157"] = false,
      ["158"] = false,
      ["159"] = false,
      ["160"] = false,
      ["161"] = false,
      ["162"] = false,
    };

    /// <summary>
    /// Словарь для хранения значений M74HCT573_UKSH.
    /// </summary>
    static internal readonly Dictionary<string, int> GetValueM74HCT573UKSH = new Dictionary<string, int>()
    {
      ["1"] = 6,
      ["2"] = 7,
      ["3"] = 4,
      ["4"] = 5,
      ["5"] = 2,
      ["6"] = 3,
      ["7"] = 0,
      ["8"] = 1,
      ["9"] = 2,
      ["10"] = 3,
      ["11"] = 4,
      ["12"] = 5,
      ["13"] = 6,
      ["14"] = 7,
      ["15"] = 6,
      ["16"] = 7,
      ["17"] = 5,
      ["18"] = 4,
      ["19"] = 3,
      ["20"] = 2,
      ["21"] = 1,
      ["22"] = 0,
      ["23"] = 0,
      ["24"] = 1,
      ["25"] = 1,
      ["26"] = 3,
      ["27"] = 7,
      ["28"] = 5,
      ["29"] = 0,
      ["30"] = 2,
      ["31"] = 6,
      ["32"] = 4,
      ["33"] = 6,
      ["34"] = 4,
      ["35"] = 5,
      ["36"] = 7,
      ["37"] = 7,
      ["38"] = 5,
      ["39"] = 6,
      ["40"] = 4,
      ["41"] = 6,
      ["42"] = 4,
      ["43"] = 4,
      ["44"] = 6,
      ["45"] = 7,
      ["46"] = 5,
      ["47"] = 5,
      ["48"] = 7,
      ["49"] = 1,
      ["50"] = 6,
      ["51"] = 6,
      ["52"] = 4,
      ["53"] = 0,
      ["54"] = 7,
      ["55"] = 7,
      ["56"] = 5,
      ["57"] = 3,
      ["58"] = 0,
      ["59"] = 0,
      ["60"] = 2,
      ["61"] = 2,
      ["62"] = 1,
      ["63"] = 1,
      ["64"] = 3,
      ["65"] = 3,
      ["66"] = 1,
      ["67"] = 3,
      ["68"] = 2,
      ["69"] = 2,
      ["70"] = 0,
      ["71"] = 0,
      ["72"] = 1,
      ["73"] = 2,
      ["74"] = 3,
      ["75"] = 4,
      ["76"] = 5,
      ["77"] = 6,
      ["78"] = 7,
      ["79"] = 0,
      ["80"] = 1,
      ["81"] = 2,
      ["82"] = 3,
      ["83"] = 4,
      ["84"] = 5,
      ["85"] = 6,
      ["86"] = 7,
      ["87"] = 7,
      ["88"] = 6,
      ["89"] = 5,
      ["90"] = 4,
      ["91"] = 1,
      ["92"] = 0,
      ["93"] = 0,
      ["94"] = 1,
      ["95"] = 3,
      ["96"] = 2,
      ["97"] = 4,
      ["98"] = 5,
      ["99"] = 3,
      ["100"] = 2,
      ["101"] = 6,
      ["102"] = 7,
      ["103"] = 3,
      ["104"] = 2,
      ["105"] = 0,
      ["106"] = 1,
      ["107"] = 1,
      ["108"] = 0,
      ["109"] = 3,
      ["110"] = 2,
      ["111"] = 3,
      ["112"] = 2,
      ["113"] = 1,
      ["114"] = 0,
      ["115"] = 0,
      ["116"] = 1,
      ["117"] = 7,
      ["118"] = 6,
      ["119"] = 7,
      ["120"] = 6,
      ["121"] = 5,
      ["122"] = 4,
      ["123"] = 5,
      ["124"] = 4,
      ["125"] = 7,
      ["126"] = 6,
      ["127"] = 5,
      ["128"] = 4,
      ["129"] = 2,
      ["130"] = 3,
      ["131"] = 1,
      ["132"] = 0,
      ["133"] = 7,
      ["134"] = 6,
      ["135"] = 5,
      ["136"] = 4,
      ["137"] = 3,
      ["138"] = 2,
      ["139"] = 1,
      ["140"] = 0,
      ["141"] = 5,
      ["142"] = 4,
      ["143"] = 3,
      ["144"] = 2,
      ["145"] = 1,
      ["146"] = 0,
      ["147"] = 7,
      ["148"] = 6,
      ["149"] = 5,
      ["150"] = 4,
      ["151"] = 3,
      ["152"] = 2,
      ["153"] = 1,
      ["154"] = 0,
      ["155"] = 7,
      ["156"] = 6,
      ["157"] = 5,
      ["158"] = 4,
      ["159"] = 3,
      ["160"] = 2,
      ["161"] = 1,
      ["162"] = 0,
    };

    /// <summary>
    /// Словарь для хранения состояний портов M74HCT573_UKSH.
    /// Ключи представляют номера портов, а значения - их состояния.
    /// </summary>
    static internal readonly Dictionary<string, int> GetValueStatePortM74HCT573UKSH = new Dictionary<string, int>()
    {
      ["0"] = 0,
      ["1"] = 0,
      ["8"] = 0,
      ["16"] = 0,
      ["17"] = 0,
      ["24"] = 0,
      ["32"] = 0,
      ["33"] = 0,
      ["40"] = 0,
      ["48"] = 0,
      ["49"] = 0,
      ["64"] = 0,
      ["65"] = 0,
      ["72"] = 0,
      ["80"] = 0,
      ["81"] = 0,
      ["96"] = 0,
      ["97"] = 0,
      ["104"] = 0,
      ["112"] = 0,
      ["113"] = 0,
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="UKSH"/> class.
    /// </summary>
    private static readonly ConstructUKSH ConstructUKSH = new ConstructUKSH();

    #endregion

    #region Работа с шинами.

    /// <summary>
    /// Подключение ППУ к шинам.
    /// </summary>
    /// <param name="ipDevice">Адрес УКШ.</param>
    /// <returns></returns>
    public static async Task ConnectToBreakdownTester(IPAddress ipDevice)
    {
      await CommunicationManager.SendCommandAsync(ipDevice, new Command(10, 1));
    }

    /// <summary>
    /// Подключение ППУ к шинам.
    /// </summary>
    /// <param name="ipDevice">Адрес УКШ.</param>
    /// <returns></returns>
    public static async Task DisconnectToBreakdownTester(IPAddress ipDevice)
    {
      await CommunicationManager.SendCommandAsync(ipDevice, new Command(10, 0));
    }

    /// <summary>
    /// Подключение цепочки укш.
    /// </summary>
    /// <param name="ipDevice">Адрес УКШ.</param>
    /// <param name="numberBlock">Номер блока.</param>
    /// <param name="numberChain">Номер цепочки.</param>
    /// <returns>Результат выполнения.</returns>
    public static async Task<bool> ConnectChainCircuit(IPAddress ipDevice, int numberBlock, int numberChain)
    {
      Command command = new Command(4, numberBlock, numberChain, 1);
      await CommunicationManager.SendCommandAsync(ipDevice, command).ConfigureAwait(false);
      return true;
    }

    /// <summary>
    /// Отключение цепочки укш.
    /// </summary>
    /// <param name="ipDevice">Адрес УКШ.</param>
    /// <param name="numberBlock">Номер блока.</param>
    /// <param name="numberChain">Номер цепочки.</param>
    /// <returns>Результат выполнения.</returns>
    public static async Task<bool> DisconnectChainCircuit(IPAddress ipDevice, int numberBlock, int numberChain)
    {
      Command command = new Command(4, numberBlock, numberChain, 2);
      await CommunicationManager.SendCommandAsync(ipDevice, command).ConfigureAwait(false);
      return true;
    }

    /// <summary>
    /// Замыкание шины УКШ для самоконтроля МКР.
    /// </summary>
    /// <param name="ipDevice">"Ip адрес УКШ.".</param>
    /// <param name="numberBus">Номер шины УКШ.</param>
    /// <returns>Задача (Task), представляющая асинхронную операцию.</returns>
    public static async Task<bool> ConnectBusSelfControlAsync(IPAddress ipDevice, char numberBus)
    {
      if (int.TryParse(numberBus.ToString(), out int number))
      {
        Command command = new Command(3, number, 1, 0);
        await CommunicationManager.SendCommandAsync(ipDevice, command).ConfigureAwait(false);
        return true;
      }

      LogError("Ошибка номера шины УКШ!");
      return false;
    }

    /// <summary>
    /// Отключение шины УКШ для самоконтроля МКР.
    /// </summary>
    /// <param name="ipDevice">"Ip адрес УКШ.".</param>
    /// <param name="numberBus">Номер шины УКШ.</param>
    /// <returns>Задача (Task), представляющая асинхронную операцию.</returns>
    public static async Task<bool> DisconnectBusSelfControlAsync(IPAddress ipDevice, char numberBus)
    {
      if (int.TryParse(numberBus.ToString(), out int number))
      {
        Command command = new Command(3, number, 2, 0);
        await Communication.CommunicationManager.SendCommandAsync(ipDevice, command).ConfigureAwait(false);
        return true;
      }

      LogError("Ошибка номера шины УКШ!");
      return false;
    }

    /// <summary>
    /// Выполняет замыкание шины УКШ с указанными параметрами.
    /// </summary>
    /// <param name="ipDevice">"Ip адрес УКШ.".</param>
    /// <param name="connector">Разъем для подключения к шинам.</param>
    /// <param name="bus">Шина УКШ.</param>
    /// <param name="lowVoltage">Флаг низковольтной шины (true - низковольтная, false - высоковольтная).</param>
    /// <param name="polarityReversed">Флаг переполюсовки шины (true - переполюсовка, false - обычный режим).</param>
    /// <returns>Возвращает ответ от устройства.</returns>
    public static async Task<string> ConnectBusAsync(IPAddress ipDevice, MeterConnector connector, BusDeviceBusCommutation bus, bool lowVoltage, bool polarityReversed)
    {
      string action = polarityReversed ? "переполюсовка" : "обычный режим";

      if (!TryGetConnectorNumber(connector, out int connectorNumber) || !TryGetBusNumber(bus, out int busNumber) || !TryGetBusType(bus, out int busType))
      {
        return LogError("Ошибка данных шины или разъема!");
      }

      string voltageType = lowVoltage ? "низковольтную" : "высоковольтную";
      busNumber += lowVoltage ? 0 : 10;

      var command = new Command(5, (connectorNumber * 10) + busType, busNumber, !polarityReversed ? 11 : 12);
      LogInformation($"Команда: \"{command}\". Замыкаем {voltageType} шину {bus} на разъеме {connector} в режиме \"{action}\".");

      return await Communication.CommunicationManager.SendCommandAsync(ipDevice, command, 1000).ConfigureAwait(false);
    }

    /// <summary>
    /// Выполняет размыкание шины УКШ с указанными параметрами.
    /// </summary>
    /// <param name="ipDevice">"Ip адрес УКШ.".</param>
    /// <param name="connector">Разъем для подключения к шинам.</param>
    /// <param name="bus">Шина УКШ.</param>
    /// <param name="lowVoltage">Флаг низковольтной шины (true - низковольтная, false - высоковольтная).</param>
    /// <param name="polarityReversed">Флаг переполюсовки шины (true - переполюсовка, false - обычный режим).</param>
    /// <returns>Возвращает ответ от устройства.</returns>
    public static async Task<string> DisconnectBusAsync(IPAddress ipDevice, MeterConnector connector, BusDeviceBusCommutation bus, bool lowVoltage, bool polarityReversed)
    {
      string action = polarityReversed ? "переполюсовка" : "обычный режим";

      if (!TryGetConnectorNumber(connector, out int connectorNumber) || !TryGetBusNumber(bus, out int busNumber) || !TryGetBusType(bus, out int busType))
      {
        return LogError("Ошибка данных шины или разъема!");
      }

      string voltageType = lowVoltage ? "низковольтную" : "высоковольтную";
      busNumber += lowVoltage ? 0 : 10;

      var command = new Command(5, (connectorNumber * 10) + busType, busNumber, !polarityReversed ? 21 : 22);
      LogInformation($"Команда: {command}. Размыкаем {voltageType} шину {bus} на разъеме {connector} в режиме \"{action}\".");
      return await Communication.CommunicationManager.SendCommandAsync(ipDevice, command, 1000).ConfigureAwait(false);
    }

    /// <summary>
    /// Попытка получения номера разъема на основе указанного разъема УКШ.
    /// </summary>
    /// <param name="connector">Разъем для подключения к шинам.</param>
    /// <param name="connectorNumber">Выходной параметр, который содержит номер разъема, если операция успешна.</param>
    /// <returns>Возвращает true, если номер разъема успешно получен; в противном случае - false.</returns>
    private static bool TryGetConnectorNumber(MeterConnector connector, out int connectorNumber)
    {
      switch (connector)
      {
        case MeterConnector.XS3:
          connectorNumber = 1;
          return true;

        case MeterConnector.XS4:
          connectorNumber = 2;
          return true;

        case MeterConnector.XS5:
          connectorNumber = 3;
          return true;

        default:
          connectorNumber = -1;
          return false;
      }
    }

    /// <summary>
    /// Поиск номера шины из имени шины.
    /// </summary>
    private static bool TryGetBusNumber(BusDeviceBusCommutation bus, out int busNumber)
    {
      string busName = bus.ToString();
      busNumber = -1;

      foreach (char ch in busName)
      {
        if (char.IsDigit(ch))
        {
          return int.TryParse(busName.Substring(busName.IndexOf(ch)), out busNumber);
        }
      }

      return false;
    }

    /// <summary>
    /// Преобразует шину в тип (A, B, AB).
    /// </summary>
    private static bool TryGetBusType(BusDeviceBusCommutation bus, out int busType)
    {
      busType = -1;

      if (bus is BusDeviceBusCommutation.A1 || bus is BusDeviceBusCommutation.A2 || bus is BusDeviceBusCommutation.A3 || bus is BusDeviceBusCommutation.A4)
      {
        busType = 1;
      }
      else if (bus is BusDeviceBusCommutation.B1 || bus is BusDeviceBusCommutation.B2 || bus is BusDeviceBusCommutation.B3 || bus is BusDeviceBusCommutation.B4)
      {
        busType = 2;
      }
      else if (bus is BusDeviceBusCommutation.AB1 || bus is BusDeviceBusCommutation.AB2 || bus is BusDeviceBusCommutation.AB3 || bus is BusDeviceBusCommutation.AB4)
      {
        busType = 3;
      }

      return busType != -1;
    }

    #endregion

    #region Работа с резисторами.

    /// <summary>
    /// Замыкание резистора.
    /// </summary>
    /// <param name="ipDevice">"Ip адрес УКШ.".</param>
    /// <param name="number">Номер резистора.</param>
    /// <returns>Задача (Task), представляющая асинхронную операцию.</returns>
    public static async Task<bool> ConnectResistor(IPAddress ipDevice, string number)
    {
      if (int.TryParse(number, out int num))
      {
        Command command = new Command(6, 1, num, 1);
        await Communication.CommunicationManager.SendCommandAsync(ipDevice, command).ConfigureAwait(false);
        return true;
      }

      LogError("Неверный номер резистора!");
      return false;
    }

    /// <summary>
    /// Размыкание резистора.
    /// </summary>
    /// <param name="ipDevice">"Ip адрес УКШ.".</param>
    /// <param name="number">Номер резистора.</param>
    /// <returns>Задача (Task), представляющая асинхронную операцию.</returns>
    public static async Task<bool> DisconnectResistor(IPAddress ipDevice, string number)
    {
      if (int.TryParse(number, out int num))
      {
        Command command = new Command(6, 1, num, 2);
        await Communication.CommunicationManager.SendCommandAsync(ipDevice, command).ConfigureAwait(false);
        return true;
      }

      LogError("Неверный номер резистора!");
      return false;
    }

    #endregion

    #region Работа с конденстаторами.

    /// <summary>
    /// Замыкание конденсатора.
    /// </summary>
    /// <param name="ipDevice">"Ip адрес УКШ.".</param>
    /// <param name="number">Номер конденсатора.</param>
    /// <returns>Задача (Task), представляющая асинхронную операцию.</returns>
    public static async Task<bool> ConnectCapacitor(IPAddress ipDevice, string number)
    {
      if (int.TryParse(number, out int num))
      {
        Command command = new Command(6, 2, num, 1);
        await Communication.CommunicationManager.SendCommandAsync(ipDevice, command).ConfigureAwait(false);
        return true;
      }

      LogError("Неверный номер конденсатора!");
      return false;
    }

    /// <summary>
    /// Размыкание конденсатора.
    /// </summary>
    /// <param name="ipDevice">"Ip адрес УКШ.".</param>
    /// <param name="number">Номер конденсатора.</param>
    /// <returns>Задача (Task), представляющая асинхронную операцию.</returns>
    public static async Task<bool> DisconnectCapacitor(IPAddress ipDevice, string number)
    {
      if (int.TryParse(number, out int num))
      {
        Command command = new Command(6, 2, num, 2);
        await Communication.CommunicationManager.SendCommandAsync(ipDevice, command).ConfigureAwait(false);
        return true;
      }

      LogError("Неверный номер конденсатора!");
      return false;
    }
    #endregion

    #region Работа с реле.

    /// <summary>
    /// Запись подключения реле в программе.
    /// </summary>
    /// <param name="numberRelay">Номер реле, которое необходимо замкнуть.</param>
    /// <returns>Результат проверки и выполнения команды.</returns>
    static public bool ConnectRelayIdleMode(int numberRelay)
    {
      if (numberRelay < 0)
      {
        return false;
      }

      CreateCommandUKSH(numberRelay.ToString(CultureInfo.InvariantCulture), true);
      return true;
    }

    /// <summary>
    /// Подключения реле.
    /// </summary>
    /// <param name="ipDevice">IP адрес УКШ.</param>
    /// <param name="numberRelay">Номер реле, которое необходимо замкнуть.</param>
    /// <returns>Результат проверки и выполнения команды.</returns>
    static public async Task<bool> ConnectRelay(IPAddress ipDevice, int numberRelay)
    {
      if (ipDevice == null)
      {
        return false;
      }

      if (numberRelay < 0)
      {
        return false;
      }

      string cmd = CreateCommandUKSH(numberRelay.ToString(CultureInfo.InvariantCulture), true);
      if (!string.IsNullOrEmpty(cmd))
      {
        Command command = new Command(cmd);
        await Communication.CommunicationManager.SendCommandAsync(ipDevice, command).ConfigureAwait(true);
        await Task.Delay(10).ConfigureAwait(true);
        return true;
      }

      return false;
    }

    /// <summary>
    /// Запись отключения реле в программе.
    /// </summary>
    /// <param name="numberRelay">Номер реле, которое необходимо замкнуть.</param>
    /// <returns>Результат проверки и выполнения команды.</returns>
    static public bool DisconnectRelayIdleMode(int numberRelay)
    {
      if (numberRelay < 0)
      {
        return false;
      }

      CreateCommandUKSH(numberRelay.ToString(CultureInfo.InvariantCulture), false);
      return true;
    }

    /// <summary>
    /// Подключение реле.
    /// </summary>
    /// <param name="ipDevice">IP адрес УКШ.</param>
    /// <param name="numberRelay">Номер реле, которое необходимо замкнуть.</param>
    /// <returns>Результат проверки и выполнения команды.</returns>
    static public async Task<bool> DisconnectRelay(IPAddress ipDevice, int numberRelay)
    {
      if (ipDevice == null)
      {
        return false;
      }

      if (numberRelay < 0)
      {
        return false;
      }

      string cmd = CreateCommandUKSH(numberRelay.ToString(CultureInfo.InvariantCulture), false);
      if (!string.IsNullOrEmpty(cmd))
      {
        Command command = new Command(cmd);
        await Communication.CommunicationManager.SendCommandAsync(ipDevice, command).ConfigureAwait(true);
        await Task.Delay(10).ConfigureAwait(true);
        return true;
      }

      return false;
    }

    /// <summary>
    /// Создает команду для управления реле системы UKSH.
    /// </summary>
    /// <param name="numberRelay">Номер реле, которое необходимо замкнуть или разомкнуть.</param>
    /// <param name="operation">Логическое значение: true для замыкания реле, false для размыкания.</param>
    /// <returns>
    /// Возвращает команду в формате строки для управления реле.
    /// Если реле уже находится в нужном состоянии или произошла ошибка, возвращается null.
    /// Формат команды: "17.[значение_LE].[значение_StatePort].[номер реле].[операция]".
    /// Операция: 0 - замкнуть, 1 - разомкнуть.
    /// </returns>
    static public string CreateCommandUKSH(string numberRelay, bool operation)
    {
      int tmp_tmpM74HCT573;
      string command;
      try
      {
        bool flag_DoingCommand = false;
        if (!ConstructUKSH.ValueLE.TryGetValue(numberRelay, out string tmpValueLE))
        {
          return null;
        }

        if (!ConstructUKSH.ValueM74HCT573.TryGetValue(numberRelay, out int tmpM74HCT573))
        {
          return null;
        }

        if (!ConstructUKSH.ValueStatePort.TryGetValue(tmpValueLE, out int tmpStatePort))
        {
          return null;
        }

        if (!ConstructUKSH.ValuePointState.TryGetValue(numberRelay, out bool tmpPointState))
        {
          return null;
        }

        try
        {
          tmp_tmpM74HCT573 = Convert.ToInt32(Math.Pow(2, Convert.ToDouble(tmpM74HCT573)));
        }
        catch (OverflowException ex)
        {
          LogError($"Произошло переполнение при вычислении: {ex.Message}");
          return null;
        }
        catch (Exception ex)
        {
          LogError($"Произошла неожиданная ошибка: {ex.Message}");
          throw;
        }

        string operations = string.Empty;

        switch (operation)
        {
          case true: // замкнуть точку
            {
              try
              {
                tmpStatePort += tmp_tmpM74HCT573;
              }
              catch (OverflowException ex)
              {
                Console.WriteLine($"Произошло переполнение при сложении: {ex.Message}");
                return null;
              }
              catch (Exception ex)
              {
                Console.WriteLine($"Произошла неожиданная ошибка: {ex.Message}");
                throw;
              }

              ConstructUKSH.ValuePointState[numberRelay] = true;
              flag_DoingCommand = true;
              if (int.TryParse(numberRelay, out int result))
              {
                if ((result >= 105) && (result <= 118))
                {
                  operations = "0";
                }
                else
                {
                  operations = "1";
                }
              }
              else
              {
                operations = "0";
              }

              break;
            }

          case false: // разомкнуть точку
            {
              try
              {
                tmpStatePort -= tmp_tmpM74HCT573;
              }
              catch (OverflowException ex)
              {
                LogError($"Произошло переполнение при вычитании: {ex.Message}");
                return null;
              }
              catch (Exception ex)
              {
                LogError($"Произошла неожиданная ошибка: {ex.Message}");
                throw;
              }

              ConstructUKSH.ValuePointState[numberRelay] = false;
              flag_DoingCommand = true;
              if (int.TryParse(numberRelay, out int result))
              {
                if ((result >= 105) && (result <= 118))
                {
                  operations = "0";
                }
                else
                {
                  operations = "1";
                }
              }

              break;
            }
        }

        if (flag_DoingCommand)
        {
          ConstructUKSH.ValueStatePort[tmpValueLE] = tmpStatePort;
          command = "8." + tmpValueLE + "." + tmpStatePort + "." + numberRelay + ".";
        }
        else
        {
          command = null;
        }
      }
      catch (KeyNotFoundException ex)
      {
        LogError($"Ошибка: Ключ не найден. {ex.Message}");
        return null;
      }
      catch (Exception ex)
      {
        // Логирование исключения
        LogError($"Произошла непредвиденная ошибка: {ex.Message}");
        throw;
      }

      return command;
    }

    /// <summary>
    /// Создает команду для управления реле системы UKSH.
    /// </summary>
    /// <param name="numberRelay">Номер реле, которое необходимо замкнуть или разомкнуть.</param>
    /// <param name="operation">Логическое значение: true для замыкания реле, false для размыкания.</param>
    /// <returns>
    /// Возвращает команду в формате строки для управления реле.
    /// Если реле уже находится в нужном состоянии или произошла ошибка, возвращается пустая строка.
    /// В случае ошибки возвращается строка "Ошибка ввода номера реле".
    /// Формат команды: "17.[значение_LE].[значение_StatePort].[номер реле].[операция]".
    /// Операция: 0 - замкнуть, 1 - разомкнуть.
    /// </returns>
    static public string CreateCommandUKSH2(string numberRelay, bool operation)
    {
      int tmp_tmpM74HCT573;
      string command;
      try
      {
        bool flag_DoingCommand = false;
        ConstructUKSH.ValueLE.TryGetValue(numberRelay, out string tmpValueLE);
        ConstructUKSH.ValueM74HCT573.TryGetValue(numberRelay, out int tmpM74HCT573);
        ConstructUKSH.ValueStatePort.TryGetValue(tmpValueLE, out int tmpStatePort);
        ConstructUKSH.ValuePointState.TryGetValue(numberRelay, out bool tmpPointState);
        tmp_tmpM74HCT573 = Convert.ToInt32(Math.Pow(2, Convert.ToDouble(tmpM74HCT573)));

        string operations = string.Empty;

        switch (operation)
        {
          case true: // замкнуть точку
            {
              tmpStatePort += tmp_tmpM74HCT573;
              ConstructUKSH.ValuePointState[numberRelay] = true;
              flag_DoingCommand = true;
              if (int.TryParse(numberRelay, out int result))
              {
                if ((result >= 105) && (result <= 118))
                {
                  operations = "0";
                }
                else
                {
                  operations = "1";
                }
              }
              else
              {
                operations = "0";
              }

              break;
            }

          case false: // разомкнуть точку
            {
              tmpStatePort -= tmp_tmpM74HCT573;
              ConstructUKSH.ValuePointState[numberRelay] = false;
              flag_DoingCommand = true;
              if (int.TryParse(numberRelay, out int result))
              {
                if ((result >= 105) && (result <= 118))
                {
                  operations = "0";
                }
                else
                {
                  operations = "1";
                }
              }

              break;
            }
        }

        if (flag_DoingCommand)
        {
          ConstructUKSH.ValueStatePort[tmpValueLE] = tmpStatePort;
          command = "8." + tmpValueLE + "." + tmpStatePort + "." + numberRelay + ".";
        }
        else
        {
          command = string.Empty;
        }
      }
      catch (Exception e)
      {
        command = $"Ошибка ввода номера реле: {e}";
      }

      return command;
    }
    #endregion

    /// <summary>
    /// Сброс всех реле на УКШ.
    /// </summary>
    /// <param name="ipDevice">"Ip адрес УКШ.".</param>
    /// <returns>Задача (Task), представляющая асинхронную операцию.</returns>
    static public async Task<bool> ResetAsync(IPAddress ipDevice)
    {
      Command cmd = new Command(2, 0, 0, 0);
      string result = await Communication.CommunicationManager.SendCommandAsync(ipDevice, cmd, 1000).ConfigureAwait(true);
      return result == "2.0.1";
    }

    /// <summary>
    /// Инициализация устройства коммутации шин.
    /// </summary>
    /// <param name="ipDevice">"Ip адрес УКШ.".</param>
    /// <returns>Возвращает ответ, получен ли ответ от инициализации.</returns>
    public static async Task<string> Initialize(IPAddress ipDevice)
    {
      Command cmd = new Command(1, 0, 0, 0);
      return await Communication.CommunicationManager.SendCommandAsync(ipDevice, cmd, 1000).ConfigureAwait(true);
    }

    /// <summary>
    /// Замыкает разъёмы XS4 и XS9.
    /// </summary>
    /// <param name="ipDevice">"Ip адрес УКШ.".</param>
    /// <returns>Задача (Task), представляющая асинхронную операцию.</returns>
    public static async Task ConnectXs9ToXs4(IPAddress ipDevice)
    {
      Command cmd = new Command(7, 1, 0, 0);
      await Communication.CommunicationManager.SendCommandAsync(ipDevice, cmd).ConfigureAwait(true);
    }

    /// <summary>
    /// Размыкает разъёмы XS4 и XS9.
    /// </summary>
    /// <param name="ipDevice">"Ip адрес УКШ.".</param>
    /// <returns>Задача (Task), представляющая асинхронную операцию.</returns>
    public static async Task DisconnectXs9ToXs4(IPAddress ipDevice)
    {
      Command cmd = new Command(7, 2, 0, 0);
      await Communication.CommunicationManager.SendCommandAsync(ipDevice, cmd).ConfigureAwait(true);
    }
  }
}
