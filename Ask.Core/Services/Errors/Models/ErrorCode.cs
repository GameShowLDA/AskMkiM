using System.Reflection;

namespace Ask.Core.Services.Errors.Models
{
  /// <summary>
  /// Коды ошибок, используемые при анализе и разборе управляющих программ.
  /// Каждый код помечен атрибутом <see cref="ErrorCodeTagAttribute"/>,
  /// соответствует уникальной ситуации, выявленной при валидации команд.
  /// </summary>
  public enum ErrorCode
  {
    /// <summary>
    /// Неизвестная или не классифицированная ошибка.
    /// Используется по умолчанию, если код не определён явно.
    /// </summary>
    [ErrorCodeTag("UNKNOWN_ERROR")]
    Unknown,

    #region Транслятор

    #region Общие ошибки

    /// <summary> Первая команда в управляющей программе должна быть ОК. </summary>
    [ErrorCodeTag("GEN001")]
    Gen_FirstMustBeOk,

    /// <summary> Последняя команда в управляющей программе должна быть КЦ. </summary>
    [ErrorCodeTag("GEN002")]
    Gen_LastMustBeKc,

    /// <summary> В программе отсутствует обязательная команда с указанной мнемоникой. </summary>
    [ErrorCodeTag("GEN003")]
    Gen_MissingRequiredCommand,

    /// <summary> Команда с данной мнемоникой встречается более одного раза. </summary>
    [ErrorCodeTag("GEN004")]
    Gen_DuplicateCommand,

    /// <summary> Не удалось найти карту точек (РМ), необходимую для проверки точек подключения. </summary>
    [ErrorCodeTag("GEN005")]
    Gen_MissingPointsMap,

    /// <summary> В команде используется точка, отсутствующая в карте точек RM. </summary>
    [ErrorCodeTag("GEN006")]
    Gen_UnknownPoint,

    /// <summary> Одна и та же точка назначения используется в RM более одного раза. </summary>
    [ErrorCodeTag("GEN007")]
    Gen_DuplicateDestination,

    /// <summary> Обнаружена команда с нераспознанной или неизвестной мнемоникой. </summary>
    [ErrorCodeTag("GEN008")]
    Gen_UnknownCommand,

    /// <summary> В строке команды найдены нераспознанные или лишние параметры. </summary>
    [ErrorCodeTag("GEN009")]
    Gen_UnrecognizedParameters,

    /// <summary> После команды ЦУ с вопросом ("?" или "??") ожидается команда УП, но она отсутствует. </summary>
    [ErrorCodeTag("GEN010")]
    Gen_ExpectedConditionalJumpAfterCu,

    /// <summary> Команда УП следует после информационной ЦУ (без вопроса), что недопустимо по синтаксису. </summary>
    [ErrorCodeTag("GEN011")]
    Gen_ConditionalJumpAfterInformationCu,

    /// <summary> Недопустимые отступы в начале строки перед номером команды или наоборот отсутсвуют необходимые отступы в начале строки тела команды при переносе. </summary>
    [ErrorCodeTag("GEN012")]
    Gen_IndentationError,

    /// <summary> Нарушен порядок параметров команды. </summary>
    [ErrorCodeTag("GEN013")]
    Gen_InvalidParameterOrder,

    /// <summary> Неверне использование одиночной точки. </summary>
    [ErrorCodeTag("GEN014")]
    Gen_InvalidOnePointUse,

    /// <summary> Неверне использование диапазона точек точки. </summary>
    [ErrorCodeTag("GEN015")]
    Gen_InvalidRange,

    /// <summary> Попытка добавить схему с дублирующими точками. </summary>
    [ErrorCodeTag("GEN016")]
    Gen_SchemeConflict,

    /// <summary> Указанный в команде вольтаж выше максимально допустимого вольтажа пробойной установки. </summary>
    [ErrorCodeTag("GEN017")]
    Gen_VoltageConflict,

    /// <summary> Команда с указанной мнемоникой и номером уже существует. </summary>
    [ErrorCodeTag("GEN018")]
    Gen_CommandAlreadyExists,

    /// <summary> Команда с указанной мнемоникой и номером уже существует. </summary>
    [ErrorCodeTag("GEN019")]
    Gen_FastMeterNotFound,

    /// <summary> Неверне количество разобщенных цепей. </summary>
    [ErrorCodeTag("GEN020")]
    Gen_InvalidNumberOfDisconnectedRanges,

    /// <summary> Ключ нельзя использовать для указанной команды. </summary>
    [ErrorCodeTag("GEN021")]
    Gen_WrongKey,

    #endregion

    #region Режим УМ

    /// <summary> В команде УП отсутствует или некорректно указана метка перехода. </summary>
    [ErrorCodeTag("UP001")]
    Up_MissingOrInvalidUpLabel,

    /// <summary> Метка перехода, указанная в команде УП, не найдена среди доступных команд. </summary>
    [ErrorCodeTag("UP002")]
    Up_UpLabelNotFound,

    #endregion

    #region Режим ОК

    /// <summary> Не удалось корректно разобрать первую строку команды ОК. </summary>
    [ErrorCodeTag("OK001")]
    Ok_CannotParseFirstLine,

    /// <summary> В команде ОК отсутствует код объекта. </summary>
    [ErrorCodeTag("OK002")]
    Ok_MissingObjectCode,

    /// <summary> В команде ОК отсутствует наименование объекта. </summary>
    [ErrorCodeTag("OK003")]
    Ok_MissingObjectName,

    /// <summary> Тело команды ОК отсутствует или пустое. </summary>
    [ErrorCodeTag("OK004")]
    Ok_EmptyCommandBody,

    /// <summary> Не удалось корректно разобрать параметр команды ОК. </summary>
    [ErrorCodeTag("OK005")]
    Ok_CannotParseParameter,

    /// <summary> Ключ параметра в команде ОК превышает максимально допустимую длину. </summary>
    [ErrorCodeTag("OK006")]
    Ok_ParameterKeyTooLong,

    /// <summary> Значение параметра в команде ОК превышает максимально допустимую длину. </summary>
    [ErrorCodeTag("OK007")]
    Ok_ParameterValueTooLong,

    /// <summary> В команде ОК обнаружен дублирующийся ключ параметра. </summary>
    [ErrorCodeTag("OK008")]
    Ok_DuplicateKey,

    /// <summary> Код объекта в команде ОК превышает максимально допустимую длину. </summary>
    [ErrorCodeTag("OK009")]
    Ok_ObjectCodeTooLong,

    /// <summary> Наименование объекта в команде ОК превышает максимально допустимую длину. </summary>
    [ErrorCodeTag("OK010")]
    Ok_ObjectNameTooLong,

    #endregion

    #region Режим РМ

    /// <summary> Не удалось корректно разобрать выражение в команде РМ. </summary>
    [ErrorCodeTag("RM001")]
    Rm_CannotParseExpression,

    /// <summary> Левая или правая часть выражения в команде РМ отсутствует или пуста. </summary>
    [ErrorCodeTag("RM002")]
    Rm_EmptyLeftOrRight,

    /// <summary> Количество элементов слева и справа в выражении команды РМ не совпадает. </summary>
    [ErrorCodeTag("RM003")]
    Rm_MismatchedCounts,

    /// <summary> Группы точек в команде РМ не совпадают по составу или названию. </summary>
    [ErrorCodeTag("RM004")]
    Rm_GroupMismatch,

    /// <summary> Группа точек в команде РМ содержит недостаточно элементов. </summary>
    [ErrorCodeTag("RM005")]
    Rm_GroupTooShort,

    /// <summary> Диапазоны шагов в команде РМ не совпадают по количеству или значениям. </summary>
    [ErrorCodeTag("RM006")]
    Rm_StepRangeMismatch,

    /// <summary> Тело команды РМ отсутствует или пустое. </summary>
    [ErrorCodeTag("RM007")]
    Rm_EmptyCommandBody,

    /// <summary> При парсинге команды РМ найден лишний пробел. </summary>
    [ErrorCodeTag("RM008")]
    Rm_ExtraSpace,

    /// <summary> При парсинге команды РМ найдены недопустимые символы. </summary>
    [ErrorCodeTag("RM009")]
    Rm_UnacceptableSymbol,

    #endregion

    #region Режим СИ

    /// <summary> Не удалось корректно разобрать выражение в команде СИ. </summary>
    [ErrorCodeTag("SI001")]
    Si_CannotParseExpression,

    /// <summary> Не удалось корректно разобрать параметры команды СИ. </summary>
    [ErrorCodeTag("SI002")]
    Si_CannotParseParameters,

    /// <summary> В команде СИ отсутствует список точек. </summary>
    [ErrorCodeTag("SI003")]
    Si_EmptyPoints,

    /// <summary> Тело команды СИ отсутствует или пустое. </summary>
    [ErrorCodeTag("SI004")]
    Si_EmptyCommandBody,

    /// <summary> Ошибка при проверке разряда при групповом методе. </summary>
    [ErrorCodeTag("SI005")]
    Si_WrongDigitCheckForGroupedMethod,

    /// <summary> Ошибка при проверке точки про методе полного узла. </summary>
    [ErrorCodeTag("SI006")]
    Si_NodeExecutePointError,

    /// <summary> Ошибка при замкнутной цепи. </summary>
    [ErrorCodeTag("SI007")]
    Si_ChainError,

    /// <summary> Ошибка при замкнутной паре. </summary>
    [ErrorCodeTag("SI008")]
    Si_PairError,

    /// <summary> В команде СИ не указано напряжение для измерения. </summary>
    [ErrorCodeTag("SI009")]
    Si_EmptyVoltage,

    /// <summary> В команде СИ конфликт сопротивления. </summary>
    [ErrorCodeTag("SI010")]
    Si_ResistanceLimitsConflict,

    /// <summary> Ошибка: предудыщая команда не имеет точек для измерения. </summary>
    [ErrorCodeTag("SI011")]
    Si_PreviousCommandHasNoPoints,

    #endregion

    #region Режим ПИ

    /// <summary> Не удалось корректно разобрать выражение в команде ПИ. </summary>
    [ErrorCodeTag("PI001")]
    Pi_CannotParseExpression,

    /// <summary> Не удалось корректно разобрать параметры команды ПИ. </summary>
    [ErrorCodeTag("PI002")]
    Pi_CannotParseParameters,

    /// <summary> В команде ПИ отсутствует список точек. </summary>
    [ErrorCodeTag("PI003")]
    Pi_EmptyPoints,

    /// <summary> Тело команды ПИ отсутствует или пустое. </summary>
    [ErrorCodeTag("PI004")]
    Pi_EmptyCommandBody,

    /// <summary> Команда ПИ не может содержать ключ Г, если для команды СИ присвоен ключ Т1. </summary>
    [ErrorCodeTag("PI005")]
    Pi_KeysConflict,

    /// <summary> Ошибка при проверке точки про методе полного узла. </summary>
    [ErrorCodeTag("PI006")]
    Pi_NodeExecutePointError,

    /// <summary> Ошибка при замкнутной цепи. </summary>
    [ErrorCodeTag("PI007")]
    Pi_ChainError,

    /// <summary> Ошибка при замкнутной паре. </summary>
    [ErrorCodeTag("PI008")]
    Pi_PairError,

    /// <summary> Ошибка при замкнутной паре. </summary>
    [ErrorCodeTag("PI009")]
    Pi_EmptyVoltage,

    /// <summary> Ошибка: предудыщая команда не имеет точек для измерения. </summary>
    [ErrorCodeTag("PI010")]
    Pi_PreviousCommandHasNoPoints,

    #endregion

    #region Режим КС

    /// <summary> Не указаны границы сопротивления для команды КС. </summary>
    [ErrorCodeTag("KS001")]
    Ks_EmptyResistance,

    /// <summary> Не удалось корректно разобрать параметры команды КС. </summary>
    [ErrorCodeTag("KS002")]
    Ks_CannotParseParameters,

    /// <summary> В команде КС отсутствует список точек. </summary>
    [ErrorCodeTag("KS003")]
    Ks_EmptyPoints,

    /// <summary> В команде КС пустое тело метода. </summary>
    [ErrorCodeTag("KS004")]
    Ks_EmptyCommandBody,

    /// <summary> Ошибка при проверке точки про методе полного узла в команде КС. </summary>
    [ErrorCodeTag("KS006")]
    Ks_NodeExecutePointError,

    /// <summary> Ошибка при замкнутной цепи в команде КС. </summary>
    [ErrorCodeTag("KS007")]
    Ks_ChainError,

    /// <summary> В команде КС замкнутые точки. </summary>
    [ErrorCodeTag("KS008")]
    Ks_PairError,

    /// <summary> В команде КС нижняя граница сопротивления больше верхней границы сопротивления. </summary>
    [ErrorCodeTag("KS009")]
    Ks_CapacityLimitsConflict,

    #endregion

    #region Режим ИЕ

    /// <summary> Не указаны границы емкости для команды ИЕ. </summary>
    [ErrorCodeTag("IE001")]
    Ie_EmptyLowerCapacity,

    /// <summary> Не удалось корректно разобрать параметры команды ИЕ. </summary>
    [ErrorCodeTag("IE002")]
    Ie_CannotParseParameters,

    /// <summary> В команде ИЕ отсутствует список точек. </summary>
    [ErrorCodeTag("IE003")]
    Ie_EmptyPoints,

    /// <summary> В команде ИЕ пустое тело метода. </summary>
    [ErrorCodeTag("IE004")]
    Ie_EmptyCommandBody,

    /// <summary> Ошибка при проверке точки про методе полного узла в команде ИЕ. </summary>
    [ErrorCodeTag("IE006")]
    Ie_NodeExecutePointError,

    /// <summary> Ошибка при замкнутной цепи в команде ИЕ. </summary>
    [ErrorCodeTag("IE007")]
    Ie_ChainError,

    /// <summary> В команде ИЕ замкнутые точки. </summary>
    [ErrorCodeTag("IE008")]
    Ie_PairError,

    /// <summary> В команде ИЕ конфликт между границами электрической емкости. </summary>
    [ErrorCodeTag("IE009")]
    Ie_CapacityLimitsConflict,

    #endregion

    #region Режим ПР

    /// <summary> Ошибка при проверке точки про методе полного узла в команде ПР. </summary>
    [ErrorCodeTag("PR006")]
    Pr_NodeExecutePointError,

    /// <summary> Ошибка при замкнутной цепи в команде ПР. </summary>
    [ErrorCodeTag("PR007")]
    Pr_ChainError,

    /// <summary> В команде ПР замкнутые точки. </summary>
    [ErrorCodeTag("PR008")]
    Pr_PairError,

    /// <summary> Тело команды ПР отсутствует или пустое. </summary>
    [ErrorCodeTag("PR009")]
    Pr_EmptyCommandBody,


    /// <summary> В команде ПР отсутствует список точек. </summary>
    [ErrorCodeTag("PR0010")]
    Pr_EmptyPoints,

    /// <summary> В команде ПР отсутствует список точек. </summary>
    [ErrorCodeTag("PR0011")]
    Pr_EmptyResistance,

    /// <summary> В команде ПР нижняя граница сопротивления больше верхней границы сопротивления. </summary>
    [ErrorCodeTag("PR0012")]
    Pr_ResistanceLimitsConflict,

    /// <summary> В команде ПР не удалось распознать параметры. </summary>
    [ErrorCodeTag("PR0013")]
    Pr_CannotParseParameters,

    /// <summary> В команде ПР верхняя граница сопротивления больше максимально допустимой границы сопротивления.  </summary>
    [ErrorCodeTag("PR0014")]
    Pr_ResistanceMaxLimitsConflict,

    /// <summary> В команде ПР одна из границ сопротивления больше максимально измеряемой мультиметром границы сопротивления или ниже минимально измеряемой.  </summary>
    [ErrorCodeTag("PR0015")]
    Pr_EquipmentOutOfRange,


    /// <summary> Ошибка: предудыщая команда не имеет точек для измерения. </summary>
    [ErrorCodeTag("PR0016")]
    Pr_PreviousCommandHasNoPoints,
    #endregion

    #region Режим ЭТ

    /// <summary> В команде ПР не удалось распознать параметры. </summary>
    [ErrorCodeTag("EHT001")]
    Eht_CannotParseParameters,

    /// <summary> Тело команды ЭТ отсутствует или пустое. </summary>
    [ErrorCodeTag("EHT002")]
    Eht_EmptyCommandBody,

    /// <summary> В команде ЭТ отсутствует список точек. </summary>
    [ErrorCodeTag("EHT003")]
    Eht_EmptyPoints,

    /// <summary> В команде ЭТ нижняя граница сопротивления больше верхней границы сопротивления. </summary>
    [ErrorCodeTag("EHT004")]
    Eht_ResistanceLimitsConflict,

    /// <summary> В команде ЭТ верхняя граница сопротивления больше максимально допустимой границы сопротивления.  </summary>
    [ErrorCodeTag("EHT005")]
    Eht_ResistanceMaxLimitsConflict,

    [ErrorCodeTag("EHT006")]
    Eht_ResistanceOutOfRange,

    /// <summary> В команде ЭТ нет подлючения одной из точек.  </summary>
    [ErrorCodeTag("EHT007")]
    Eht_PointNotConnected,

    /// <summary> В команде ЭТ разрыв цепи. </summary>
    [ErrorCodeTag("EHT008")]
    Eht_CircuitOverload,

    #endregion

    #region Режим ПТ

    /// <summary> В команде ПТ отсутствует список точек. </summary>
    [ErrorCodeTag("PT001")]
    Pt_EmptyPoints,

    /// <summary> Тело команды ПТ отсутствует или пустое. </summary>
    [ErrorCodeTag("PT002")]
    Pt_EmptyCommandBody,

    #endregion



    #region Режим ВШ

    /// <summary> В команде ВШ указана неверная структура стойки коммутации. </summary>
    [ErrorCodeTag("VSH001")]
    Vsh_InvalidVshBusStructure,

    /// <summary> В команде ВШ не указана структура стойки коммутации в конфигурации. </summary>
    [ErrorCodeTag("VSH002")]
    Vsh_NoneVshBusStructure,

    #endregion

    #region Режим ОТ

    /// <summary> Тело команды ОТ отсутствует или пустое. </summary>
    [ErrorCodeTag("OT001")]
    Ot_EmptyCommandBody,
    #endregion

    #region Ключи команд

    /// <summary> Использованный ключ не разрешён для данной команды. </summary>
    [ErrorCodeTag("KEY001")]
    Key_NotAllowedForCommand,

    /// <summary> Ключ не распознан среди допустимых. </summary>
    [ErrorCodeTag("KEY002")]
    Key_NotRecognized,

    /// <summary> Использована конфликтная пара ключей, несовместимая в рамках одной команды. </summary>
    [ErrorCodeTag("KEY003")]
    Key_ConflictPair,

    /// <summary> Ключ не ожидается в данной команде. </summary>
    [ErrorCodeTag("KEY004")]
    Key_NotExpectedInThisCommand,
    #endregion

    #endregion

    #region Оборудование

    #region МКР (модуль коммутации реле)

    /// <summary>
    /// Ошибка: не найдено шасси с указанным номером.
    /// </summary>
    [ErrorCodeTag("MKR001")]
    Equipment_ChassisNotFound,

    /// <summary>
    /// Ошибка: не найден модуль коммутации в указанном шасси.
    /// </summary>
    [ErrorCodeTag("MKR002")]
    Equipment_ModuleNotFound,

    /// <summary>
    /// Ошибка: номер точки выходит за допустимые пределы диапазона модуля.
    /// </summary>
    [ErrorCodeTag("MKR003")]
    Equipment_PointOutOfRange,

    /// <summary>
    /// Ошибка: конфликт точек — выбранные точки не уникальны.
    /// </summary>
    [ErrorCodeTag("MKR004")]
    Equipment_PointsNotUnique,

    /// <summary>
    /// Ошибка: обнаружена некорректная точка в МКР.
    /// </summary>
    [ErrorCodeTag("MKR005")]
    MKR_PointError,

    #endregion

    #region УКШ

    #endregion

    #region ППУ

    #endregion

    #region Мультиметр

    #endregion

    #endregion

    #region Метрология

    #region Валидация данных.

    /// <summary>
    /// Ошибка: элемент ввода данных (InputField) не найден в пользовательском интерфейсе.
    /// </summary>
    [ErrorCodeTag("METROLOGY001")]
    Metrology_Validation_InputFieldNotFound,

    /// <summary>
    /// Ошибка: указана некорректная первая точка подключения в метрологической валидации.
    /// </summary>
    [ErrorCodeTag("METROLOGY002")]
    Metrology_Validation_InvalidFirstPointFormat,

    /// <summary>
    /// Ошибка: указана некорректная вторая точка подключения в метрологической валидации.
    /// </summary>
    [ErrorCodeTag("METROLOGY003")]
    Metrology_Validation_InvalidSecondPointFormat,

    /// <summary>
    /// Ошибка: введено некорректное значение напряжения при проверке метрологических параметров.
    /// </summary>
    [ErrorCodeTag("METROLOGY004")]
    Metrology_Validation_InvalidVoltage,

    /// <summary>
    /// Ошибка: задано некорректное значение времени выполнения или измерения.
    /// </summary>
    [ErrorCodeTag("METROLOGY005")]
    Metrology_Validation_InvalidTime,

    /// <summary>
    /// Ошибка: указанная точка подключения выходит за допустимые пределы диапазона.
    /// </summary>
    [ErrorCodeTag("METROLOGY006")]
    Metrology_Validation_PointOutOfRange,

    /// <summary>
    /// Ошибка: введён некорректный электрический параметр.
    /// Используется для общих ошибок ввода метрологических данных.
    /// </summary>
    [ErrorCodeTag("METROLOGY007")]
    Metrology_Validation_InvalidParameter,

    /// <summary>
    /// Ошибка: точки подключения не уникальны (повторяются).
    /// </summary>
    [ErrorCodeTag("METROLOGY008")]
    Metrology_Validation_PointsNotUnique,

    /// <summary>
    /// Ошибка: указана некорректная шина подключения.
    /// </summary>
    [ErrorCodeTag("METROLOGY009")]
    Metrology_Validation_InvalidBus,

    /// <summary>
    /// Ошибка: отсутствует оборудование, необходимое для выполнения проверки метрологических данных —
    /// не найдено шасси, модуль коммутации или требуемая точка подключения.
    /// </summary>
    [ErrorCodeTag("METROLOGY010")]
    Metrology_Validation_EquipmentNotFound,

    /// <summary>
    /// Ошибка: не удалось собрать устройства (CollectDevices) в метрологической подсистеме.
    /// </summary>
    [ErrorCodeTag("METROLOGY011")]
    Metrology_Validation_DeviceCollectFailed,

    /// <summary>
    /// Ошибка: не удалось разобрать одну или обе точки подключения.
    /// </summary>
    [ErrorCodeTag("METROLOGY012")]
    Metrology_Validation_PointParsingFailed,

    /// <summary>
    /// Ошибка: указанный метрологический режим не распознан или не поддерживается.
    /// </summary>
    [ErrorCodeTag("METROLOGY013")]
    Metrology_Validation_UnknownMetrologicalMode,

    /// <summary>
    /// Ошибка: устройство с указанной метрологической ролью не найдено или не соответствует ожидаемому типу.
    /// </summary>
    [ErrorCodeTag("METROLOGY014")]
    Metrology_Validation_DeviceByRoleNotFound,
    #endregion

    #endregion
  }

  /// <summary>
  /// Расширения для <see cref="WarningCode"/>.
  /// </summary>
  public static class ErrorCodeExtensions
  {
    /// <summary>
    /// Возвращает строковой тэг (например, TRN001), заданный через атрибут <see cref="ErrorCodeTagAttribute"/>.
    /// </summary>
    public static string? GetTag(this ErrorCode code)
    {
      var member = typeof(ErrorCode).GetMember(code.ToString()).FirstOrDefault();
      var attr = member?.GetCustomAttribute<ErrorCodeTagAttribute>();
      return attr?.Tag;
    }
  }
}
